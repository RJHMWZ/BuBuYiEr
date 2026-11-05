using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIAnimationPresets
{
    internal abstract class UIAnimationTask
    {
        internal int id, index = -1;
        internal bool paused, stopRequested, finished;
        internal UIAnimationStopMode stopMode;
        internal abstract float Progress { get; }
        internal abstract bool UsesTarget(RectTransform target);
    }

    internal sealed class UIAnimationJob : UIAnimationTask
    {
        internal UIAnimationBinding binding;
        internal UIAnimationPose rest, start;
        internal UIAnimationDefinition definition;
        internal UIAnimationTrack[] tracks;
        internal UIAnimationChannels channels;
        internal UIAnimationCallbacks callbacks;
        internal double elapsed, previous = -1e-9;
        internal int reportedLoops;
        internal bool started, inputLocked;
        internal int inputKey;
        internal override float Progress => double.IsInfinity(definition.TotalDuration)
            ? (float)((elapsed % Math.Max(.001, definition.duration + definition.loopDelay)) / Math.Max(.001, definition.duration + definition.loopDelay))
            : definition.TotalDuration <= 0 ? 1 : Mathf.Clamp01((float)(elapsed / definition.TotalDuration));
        internal override bool UsesTarget(RectTransform target) => binding != null && binding.target == target;

        internal void Sample(double time)
        {
            int iteration; bool complete;
            float t = UIAnimationEvaluator.NormalizedTime(definition, time, out iteration, out complete);
            UIAnimationEvaluator.Sample(definition, tracks, rest, start, t).Apply(binding, channels);
        }

        internal void Tick(float delta)
        {
            if (paused || stopRequested) return;
            if (binding.target == null) { stopRequested = true; stopMode = UIAnimationStopMode.KeepCurrentState; return; }
            elapsed += delta;
            if (elapsed < Math.Max(0, definition.delay)) return;
            if (!started)
            {
                started = true;
                UIAnimationManager.Dispatch(callbacks.onStart);
            }
            double duration = Math.Max(definition.loops < 0 ? .001 : 0, definition.duration);
            double gap = Math.Max(0, definition.loopDelay);
            double active = elapsed - Math.Max(0, definition.delay);
            double before = previous - Math.Max(0, definition.delay);
            double cycle = Math.Max(.001, duration + gap);
            int maxLoops = definition.loops < 0 ? int.MaxValue : Mathf.Max(1, definition.loops);
            int ended = Math.Min(maxLoops, (int)Math.Min(int.MaxValue, Math.Floor((active + gap) / cycle)));
            for (int count = 0; reportedLoops < ended && count < 128; count++)
            {
                reportedLoops++;
                UIAnimationManager.Dispatch(callbacks.onLoop, reportedLoops);
            }
            if (definition.markers != null)
            {
                int first = Math.Max(0, (int)Math.Min(int.MaxValue - 1, Math.Floor(Math.Max(0, before) / cycle)));
                int last = Math.Min(maxLoops - 1, (int)Math.Min(int.MaxValue - 1, Math.Floor(active / cycle)));
                for (int loop = first; loop <= last && loop - first < 128; loop++)
                {
                    bool reverse = definition.loopMode == UIAnimationLoopMode.PingPong && (loop & 1) != 0;
                    for (int i = 0; i < definition.markers.Count; i++)
                    {
                        UIAnimationMarker marker = definition.markers[i];
                        if (marker == null) continue;
                        double trigger = loop * cycle + (reverse ? 1 - Mathf.Clamp01(marker.time) : Mathf.Clamp01(marker.time)) * duration;
                        if (trigger > before && trigger <= active) UIAnimationManager.Dispatch(marker.Callback);
                    }
                }
            }
            Sample(elapsed);
            previous = elapsed;
            if (!double.IsInfinity(definition.TotalDuration) && elapsed >= definition.TotalDuration) finished = true;
        }
    }

    public static class UIAnimationManager
    {
        private struct Invocation
        {
            internal Action action;
            internal Action<int> integerAction;
            internal int value;
        }
        private sealed class InputLock
        {
            internal CanvasGroup group;
            internal bool interactable, blocks;
            internal int count;
        }
        private static readonly List<UIAnimationJob> jobs = new List<UIAnimationJob>(64);
        private static readonly List<UIAnimationJob> pending = new List<UIAnimationJob>(16);
        private static readonly List<UIAnimationSequenceRunner> sequences = new List<UIAnimationSequenceRunner>(16);
        private static readonly Stack<UIAnimationJob> pool = new Stack<UIAnimationJob>(64);
        private static readonly Dictionary<int, UIAnimationTask> lookup = new Dictionary<int, UIAnimationTask>(128);
        private static readonly Dictionary<int, InputLock> inputLocks = new Dictionary<int, InputLock>(32);
        private static readonly List<Invocation> invocations = new List<Invocation>(64);
        private static readonly List<UIAnimator> autoPlay = new List<UIAnimator>(32);
        private static readonly HashSet<int> completingTargets = new HashSet<int>();
        private static readonly HashSet<int> activatingTargets = new HashSet<int>();
        private static UIAnimationDriver driver;
        private static bool stepping, dispatching, quitting;
        private static int nextId;
        private static int commandDepth;
        public static int ActiveCount => lookup.Count;
        public static int PooledCount => pool.Count;
        internal static int Generation { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            jobs.Clear(); pending.Clear(); sequences.Clear(); lookup.Clear(); inputLocks.Clear();
            invocations.Clear(); autoPlay.Clear(); pool.Clear();
            completingTargets.Clear();
            activatingTargets.Clear(); commandDepth = 0;
            driver = null; stepping = dispatching = quitting = false;
            Generation++;
        }

        public static UIAnimationHandle Play(RectTransform target, UIAnimationPreset preset, Action onComplete = null)
        {
            return Play(target, UIPresetLibrary.Create(preset), new UIAnimationCallbacks { onComplete = onComplete });
        }
        public static UIAnimationHandle Play(RectTransform target, UIAnimationPresetAsset preset, Action onComplete = null) =>
            preset == null ? default : Play(target, preset.animation, new UIAnimationCallbacks { onComplete = onComplete });
        public static UIAnimationHandle Play(RectTransform target, UIAnimationDefinition animation, UIAnimationCallbacks callbacks = default) =>
            PlayInternal(target, animation, callbacks, null, 0);

        internal static UIAnimationHandle PlayInternal(RectTransform target, UIAnimationDefinition animation,
            UIAnimationCallbacks callbacks, UIAnimationPose? baseline, double initialElapsed)
        {
            if (!Application.isPlaying || quitting || target == null || animation == null) return default;
            EnsureDriver();
            UIAnimationTrack[] tracks = UIPresetLibrary.Tracks(animation);
            UIAnimationChannels channels = UIAnimationEvaluator.Channels(tracks);
            if (channels == UIAnimationChannels.None) return default;
            if (animation.conflict == UIAnimationConflict.Ignore && HasConflict(target, channels)) return default;
            commandDepth++;
            try { CancelConflicts(target, channels, animation.interruptedState); }
            finally { commandDepth--; }
            var job = pool.Count > 0 ? pool.Pop() : new UIAnimationJob();
            job.id = NextId(); job.index = -1;
            job.binding = new UIAnimationBinding(target);
            job.rest = baseline ?? UIAnimationPose.Capture(job.binding);
            job.start = UIAnimationPose.Capture(job.binding);
            job.definition = animation.Clone();
            job.tracks = tracks; job.channels = channels; job.callbacks = callbacks;
            job.elapsed = Math.Max(0, initialElapsed); job.previous = -1e-9;
            job.reportedLoops = 0; job.started = job.inputLocked = job.paused = job.stopRequested = job.finished = false;
            lookup.Add(job.id, job);
            if (stepping) pending.Add(job);
            else Activate(job);
            return new UIAnimationHandle(job.id);
        }

        private static int NextId()
        {
            do { nextId = unchecked(nextId + 1); } while (nextId == 0 || lookup.ContainsKey(nextId));
            return nextId;
        }

        private static void Activate(UIAnimationJob job)
        {
            job.index = jobs.Count;
            jobs.Add(job);
            if (job.definition.blockInput && job.binding.group != null)
            {
                int key = job.binding.group.GetInstanceID();
                if (!inputLocks.TryGetValue(key, out InputLock state))
                {
                    state = new InputLock { group = job.binding.group, interactable = job.binding.group.interactable,
                        blocks = job.binding.group.blocksRaycasts };
                    inputLocks.Add(key, state);
                }
                state.count++;
                state.group.interactable = state.group.blocksRaycasts = false;
                job.inputLocked = true;
                job.inputKey = key;
            }
            job.Sample(job.elapsed);
        }

        private static bool HasConflict(RectTransform target, UIAnimationChannels channels)
        {
            for (int i = 0; i < jobs.Count; i++) if (!jobs[i].stopRequested && jobs[i].UsesTarget(target) && (jobs[i].channels & channels) != 0) return true;
            for (int i = 0; i < pending.Count; i++) if (!pending[i].stopRequested && pending[i].UsesTarget(target) && (pending[i].channels & channels) != 0) return true;
            return false;
        }

        private static void CancelConflicts(RectTransform target, UIAnimationChannels channels, UIAnimationStopMode mode)
        {
            for (int i = jobs.Count - 1; i >= 0; i--)
                if (jobs[i].UsesTarget(target) && (jobs[i].channels & channels) != 0) RequestStop(jobs[i], mode);
            for (int i = pending.Count - 1; i >= 0; i--)
                if (pending[i].UsesTarget(target) && (pending[i].channels & channels) != 0) RequestStop(pending[i], mode);
        }

        public static void StopAll(RectTransform target, UIAnimationStopMode mode = UIAnimationStopMode.RestoreState)
        {
            commandDepth++;
            try
            {
                for (int i = autoPlay.Count - 1; i >= 0; i--)
                    if (autoPlay[i] == null || autoPlay[i].Target == target) autoPlay.RemoveAt(i);
                for (int i = sequences.Count - 1; i >= 0; i--) if (i < sequences.Count && sequences[i].UsesTarget(target)) RequestStop(sequences[i], mode);
                CancelConflicts(target, UIAnimationChannels.All, mode);
            }
            finally { commandDepth--; FlushCallbacks(); }
        }

        public static void StopEverything(UIAnimationStopMode mode = UIAnimationStopMode.RestoreState)
        {
            commandDepth++;
            try
            {
                autoPlay.Clear();
                for (int i = sequences.Count - 1; i >= 0; i--) if (i < sequences.Count) RequestStop(sequences[i], mode);
                for (int i = jobs.Count - 1; i >= 0; i--) if (i < jobs.Count) RequestStop(jobs[i], mode);
                for (int i = pending.Count - 1; i >= 0; i--) if (i < pending.Count) RequestStop(pending[i], mode);
            }
            finally { commandDepth--; FlushCallbacks(); }
        }

        internal static bool Exists(int id) => lookup.ContainsKey(id);
        internal static bool IsPlaying(int id) => lookup.TryGetValue(id, out UIAnimationTask task) && !task.paused && !task.stopRequested;
        internal static bool IsPaused(int id) => lookup.TryGetValue(id, out UIAnimationTask task) && task.paused;
        internal static float Progress(int id) => lookup.TryGetValue(id, out UIAnimationTask task) ? task.Progress : 1;
        internal static void Pause(int id, bool value)
        {
            if (!lookup.TryGetValue(id, out UIAnimationTask task)) return;
            task.paused = value;
            if (task is UIAnimationSequenceRunner sequence) sequence.PauseChildren(value);
        }
        internal static void Stop(int id, UIAnimationStopMode mode)
        {
            commandDepth++;
            try { if (lookup.TryGetValue(id, out UIAnimationTask task)) RequestStop(task, mode); }
            finally { commandDepth--; FlushCallbacks(); }
        }
        private static void RequestStop(UIAnimationTask task, UIAnimationStopMode mode)
        {
            if (task.stopRequested) return;
            task.stopRequested = true; task.stopMode = mode;
            if (!stepping)
            {
                if (task is UIAnimationJob job) Finish(job, mode == UIAnimationStopMode.Complete);
                else FinishSequence((UIAnimationSequenceRunner)task, mode == UIAnimationStopMode.Complete);
            }
        }

        internal static void Dispatch(Action callback)
        {
            if (callback != null) invocations.Add(new Invocation { action = callback });
        }
        internal static void Dispatch(Action<int> callback, int value)
        {
            if (callback != null) invocations.Add(new Invocation { integerAction = callback, value = value });
        }
        private static void FlushCallbacks()
        {
            if (stepping || dispatching || quitting || commandDepth > 0) return;
            dispatching = true;
            int count = 0;
            try
            {
                while (count < invocations.Count && count < 1024)
                {
                    Invocation item = invocations[count++];
                    try { if (item.action != null) item.action(); else item.integerAction?.Invoke(item.value); }
                    catch (Exception exception) { Debug.LogException(exception); }
                }
            }
            finally
            {
                if (count > 0) invocations.RemoveRange(0, count);
                dispatching = false;
            }
        }

        private static void Finish(UIAnimationJob job, bool completed)
        {
            if (!lookup.Remove(job.id)) return;
            if (job.index >= 0)
            {
                int last = jobs.Count - 1;
                jobs[job.index] = jobs[last]; jobs[job.index].index = job.index;
                jobs.RemoveAt(last);
            }
            else pending.Remove(job);
            job.index = -1;
            bool valid = job.binding.target != null;
            if (valid)
            {
                if (completed)
                {
                    double end = double.IsInfinity(job.definition.TotalDuration)
                        ? Math.Max(0, job.definition.delay) + Math.Max(.001, job.definition.duration) : job.definition.TotalDuration;
                    if (double.IsInfinity(job.definition.TotalDuration))
                    {
                        int iteration; bool done;
                        UIAnimationEvaluator.NormalizedTime(job.definition, job.elapsed, out iteration, out done);
                        float endpoint = job.definition.loopMode == UIAnimationLoopMode.PingPong && (iteration & 1) != 0 ? 0 : 1;
                        UIAnimationEvaluator.Sample(job.definition, job.tracks, job.rest, job.start, endpoint).Apply(job.binding, job.channels);
                    }
                    else job.Sample(end);
                }
                bool restore = completed ? job.definition.completion == UIAnimationCompletion.RestoreState : job.stopMode == UIAnimationStopMode.RestoreState;
                if (restore) job.rest.Apply(job.binding, job.channels);
            }
            if (job.inputLocked)
            {
                int key = job.inputKey;
                if (inputLocks.TryGetValue(key, out InputLock state) && --state.count <= 0)
                {
                    if (state.group != null) { state.group.interactable = state.interactable; state.group.blocksRaycasts = state.blocks; }
                    inputLocks.Remove(key);
                }
            }
            UIAnimationManager.Dispatch(completed ? job.callbacks.onComplete : job.callbacks.onCancel);
            if (valid && completed && job.definition.completion == UIAnimationCompletion.DisableGameObject) Deactivate(job.binding.target);
            job.binding = null; job.definition = null; job.tracks = null; job.callbacks = default;
            pool.Push(job);
        }

        internal static UIAnimationHandle StartSequence(UIAnimationSequenceRunner runner)
        {
            if (!Application.isPlaying || quitting) return default;
            EnsureDriver();
            runner.id = NextId(); runner.index = sequences.Count;
            sequences.Add(runner); lookup.Add(runner.id, runner);
            return new UIAnimationHandle(runner.id);
        }
        private static void FinishSequence(UIAnimationSequenceRunner runner, bool completed)
        {
            if (!lookup.Remove(runner.id)) return;
            int last = sequences.Count - 1;
            sequences[runner.index] = sequences[last]; sequences[runner.index].index = runner.index;
            sequences.RemoveAt(last); runner.index = -1;
            runner.Finish(completed);
        }

        internal static void ScheduleAutoPlay(UIAnimator animator)
        {
            if (!Application.isPlaying) return;
            EnsureDriver();
            if (!autoPlay.Contains(animator)) autoPlay.Add(animator);
        }
        internal static void CancelAutoPlay(UIAnimator animator) => autoPlay.Remove(animator);
        internal static void Tick(float scaled, float unscaled)
        {
            if (autoPlay.Count > 0)
            {
                Canvas.ForceUpdateCanvases();
                while (autoPlay.Count > 0)
                {
                    int last = autoPlay.Count - 1;
                    UIAnimator animator = autoPlay[last];
                    autoPlay.RemoveAt(last);
                    if (animator != null && animator.isActiveAndEnabled) animator.PlayAutomatically();
                }
            }
            stepping = true;
            try
            {
                for (int i = 0; i < jobs.Count; i++) jobs[i].Tick(jobs[i].definition.useUnscaledTime ? unscaled : scaled);
                for (int i = jobs.Count - 1; i >= 0; i--)
                    if (jobs[i].finished || jobs[i].stopRequested) Finish(jobs[i], jobs[i].finished || jobs[i].stopMode == UIAnimationStopMode.Complete);
                int sequenceCount = sequences.Count;
                for (int i = 0; i < sequenceCount; i++) sequences[i].Tick(scaled, unscaled);
                for (int i = sequences.Count - 1; i >= 0; i--)
                    if (sequences[i].finished || sequences[i].stopRequested) FinishSequence(sequences[i], sequences[i].finished || sequences[i].stopMode == UIAnimationStopMode.Complete);
                for (int i = jobs.Count - 1; i >= 0; i--)
                    if (jobs[i].stopRequested) Finish(jobs[i], jobs[i].stopMode == UIAnimationStopMode.Complete);
                while (pending.Count > 0)
                {
                    UIAnimationJob job = pending[0];
                    if (job.stopRequested) Finish(job, job.stopMode == UIAnimationStopMode.Complete);
                    else { pending.RemoveAt(0); Activate(job); }
                }
            }
            finally { stepping = false; }
            FlushCallbacks();
        }

        private static void EnsureDriver()
        {
            if (driver != null) return;
            var root = new GameObject("[UI Animation Manager]") { hideFlags = HideFlags.HideAndDontSave };
            UnityEngine.Object.DontDestroyOnLoad(root);
            driver = root.AddComponent<UIAnimationDriver>();
        }
        internal static void Shutdown(bool applicationQuitting)
        {
            quitting = applicationQuitting;
            driver = null;
            StopEverything();
            if (quitting) invocations.Clear();
        }

        internal static bool IsCompletingTarget(RectTransform target) => target != null && completingTargets.Contains(target.GetInstanceID());
        internal static bool IsActivatingTarget(RectTransform target) => target != null && activatingTargets.Contains(target.GetInstanceID());
        internal static void ActivateTarget(RectTransform target)
        {
            if (target == null || target.gameObject.activeSelf) return;
            int key = target.GetInstanceID();
            activatingTargets.Add(key);
            try { target.gameObject.SetActive(true); }
            finally { activatingTargets.Remove(key); }
        }
        internal static void Deactivate(RectTransform target)
        {
            if (target == null) return;
            int key = target.GetInstanceID();
            completingTargets.Add(key);
            try { target.gameObject.SetActive(false); }
            finally { completingTargets.Remove(key); }
        }
    }
}
