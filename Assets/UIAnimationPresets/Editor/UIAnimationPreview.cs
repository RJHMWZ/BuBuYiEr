using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UIAnimationPresets.Editor
{
    [InitializeOnLoad]
    public static class UIAnimationPreview
    {
        private sealed class Target
        {
            internal UIAnimationBinding binding;
            internal UIAnimationPose rest;
        }
        private sealed class Segment
        {
            internal Target target;
            internal UIAnimationDefinition animation;
            internal UIAnimationTrack[] tracks;
            internal UIAnimationPose start;
            internal double time;
            internal double interruptedAt = double.PositiveInfinity;
            internal UIAnimationStopMode interruptedMode;
        }
        private static readonly List<Target> targets = new List<Target>();
        private static readonly List<Segment> segments = new List<Segment>();
        private static UIAnimator owner;
        private static bool running, paused, ownsMode;
        private static double elapsed, lastTime, duration, sequenceCycle;
        public static bool IsRunning => running;
        public static bool IsPaused => paused;
        public static UIAnimator Owner => owner;
        public static double Elapsed => elapsed;
        public static double Duration => duration;
        public static string LastError { get; private set; }

        static UIAnimationPreview()
        {
            UIAnimator.EditorPreviewRequested = Start;
            UIAnimator.EditorStopRequested = animator => { if (owner == animator) Stop(); };
            AssemblyReloadEvents.beforeAssemblyReload += Stop;
            EditorApplication.quitting += Stop;
            EditorApplication.playModeStateChanged += state => Stop();
            Selection.selectionChanged += Stop;
            Undo.undoRedoPerformed += Stop;
            EditorSceneManager.sceneSaving += (scene, path) => Stop();
            EditorSceneManager.sceneClosing += (scene, removing) => Stop();
        }

        public static void Start(UIAnimator animator)
        {
            if (animator == null) return;
            Stop();
            if (!Prepare()) return;
            owner = animator;
            if (animator.source == UIAnimationSource.Sequence)
            {
                UIAnimationSequence timeline = UIAnimationSequence.FromSteps(animator);
                duration = timeline.duration;
                sequenceCycle = timeline.duration;
                var ordered = timeline.entries.ToArray();
                Array.Sort(ordered, (a, b) => a.start != b.start ? a.start.CompareTo(b.start) : a.order.CompareTo(b.order));
                foreach (UIAnimationScheduledStep entry in ordered)
                    if (entry.target != null && entry.animation != null) Add(entry.target, entry.animation, entry.start);
                if (animator.sequenceLoops < 0) duration = double.PositiveInfinity;
                else duration *= Mathf.Max(1, animator.sequenceLoops);
            }
            else
            {
                UIAnimationDefinition definition = animator.Definition;
                if (definition != null) { duration = definition.TotalDuration; Add(animator.Target, definition, 0); }
            }
            Begin();
        }

        public static void Start(UIAnimationDefinition definition, RectTransform target)
        {
            Stop();
            if (target == null || definition == null || !Prepare()) return;
            duration = definition.TotalDuration;
            Add(target, definition, 0);
            Begin();
        }

        private static bool Prepare()
        {
            LastError = null;
            if (Application.isPlaying) { LastError = "请使用运行时播放按钮。"; return false; }
            if (AnimationMode.InAnimationMode()) { LastError = "请先停止 Animation 窗口或其他工具的动画预览。"; return false; }
            Canvas.ForceUpdateCanvases();
            return true;
        }

        private static void Add(RectTransform rect, UIAnimationDefinition definition, double time)
        {
            if (EditorUtility.IsPersistent(rect)) { LastError = "请选择场景或 Prefab 编辑模式中的 UI 实例。"; return; }
            Target target = targets.Find(item => item.binding.target == rect);
            if (target == null)
            {
                if (rect.GetComponent<CanvasGroup>() == null) Undo.AddComponent<CanvasGroup>(rect.gameObject);
                var binding = new UIAnimationBinding(rect, false);
                target = new Target { binding = binding, rest = UIAnimationPose.Capture(binding) };
                targets.Add(target);
            }
            UIAnimationTrack[] tracks = UIPresetLibrary.Tracks(definition);
            UIAnimationChannels channels = UIAnimationEvaluator.Channels(tracks);
            foreach (Segment previous in segments)
            {
                if (previous.target != target || time < previous.time || time >= previous.interruptedAt ||
                    time >= previous.time + previous.animation.TotalDuration ||
                    (UIAnimationEvaluator.Channels(previous.tracks) & channels) == 0) continue;
                if (definition.conflict == UIAnimationConflict.Ignore) return;
            }
            foreach (Segment previous in segments)
            {
                if (previous.target != target || time < previous.time || time >= previous.interruptedAt ||
                    time >= previous.time + previous.animation.TotalDuration ||
                    (UIAnimationEvaluator.Channels(previous.tracks) & channels) == 0) continue;
                previous.interruptedAt = time;
                previous.interruptedMode = definition.interruptedState;
            }
            UIAnimationPose start = target.rest;
            foreach (Segment previous in segments)
            {
                if (previous.target != target || time < previous.time) continue;
                Merge(ref start, Evaluate(previous, time), UIAnimationEvaluator.Channels(previous.tracks));
            }
            segments.Add(new Segment { target = target, animation = definition.Clone(), tracks = tracks, start = start, time = time });
        }

        private static UIAnimationPose Evaluate(Segment segment, double time)
        {
            bool interrupted = time >= segment.interruptedAt;
            if (interrupted && segment.interruptedMode == UIAnimationStopMode.RestoreState) return segment.target.rest;
            double local = Math.Min(time, segment.interruptedAt) - segment.time;
            int iteration; bool done;
            float t = UIAnimationEvaluator.NormalizedTime(segment.animation, local, out iteration, out done);
            if (interrupted && segment.interruptedMode == UIAnimationStopMode.Complete)
            {
                if (double.IsInfinity(segment.animation.TotalDuration))
                    t = segment.animation.loopMode == UIAnimationLoopMode.PingPong && (iteration & 1) != 0 ? 0 : 1;
                else t = UIAnimationEvaluator.NormalizedTime(segment.animation, segment.animation.TotalDuration, out iteration, out done);
                done = true;
            }
            if (done && segment.animation.completion == UIAnimationCompletion.RestoreState) return segment.target.rest;
            return UIAnimationEvaluator.Sample(segment.animation, segment.tracks, segment.target.rest, segment.start, t);
        }

        private static void Begin()
        {
            if (segments.Count == 0) { targets.Clear(); owner = null; return; }
            AnimationMode.StartAnimationMode();
            ownsMode = running = true;
            paused = false; elapsed = 0; lastTime = EditorApplication.timeSinceStartup;
            foreach (Target target in targets) Record(target);
            if (owner != null) owner.IsPreviewing = true;
            EditorApplication.update += Tick;
            Sample();
        }

        private static void Record(Target target)
        {
            RectTransform rect = target.binding.target;
            AddProperty(rect, typeof(RectTransform), "m_AnchoredPosition.x", rect.anchoredPosition.x);
            AddProperty(rect, typeof(RectTransform), "m_AnchoredPosition.y", rect.anchoredPosition.y);
            AddProperty(rect, typeof(Transform), "m_LocalPosition.z", rect.localPosition.z);
            Vector3 scale = rect.localScale;
            AddProperty(rect, typeof(Transform), "m_LocalScale.x", scale.x);
            AddProperty(rect, typeof(Transform), "m_LocalScale.y", scale.y);
            AddProperty(rect, typeof(Transform), "m_LocalScale.z", scale.z);
            Quaternion rotation = rect.localRotation;
            AddProperty(rect, typeof(Transform), "m_LocalRotation.x", rotation.x);
            AddProperty(rect, typeof(Transform), "m_LocalRotation.y", rotation.y);
            AddProperty(rect, typeof(Transform), "m_LocalRotation.z", rotation.z);
            AddProperty(rect, typeof(Transform), "m_LocalRotation.w", rotation.w);
            if (target.binding.group != null) AddProperty(target.binding.group, typeof(CanvasGroup), "m_Alpha", target.rest.alpha);
            if (target.binding.graphic == null) return;
            Type type = target.binding.graphic.GetType();
            AddProperty(target.binding.graphic, type, "m_Color.r", target.rest.color.r);
            AddProperty(target.binding.graphic, type, "m_Color.g", target.rest.color.g);
            AddProperty(target.binding.graphic, type, "m_Color.b", target.rest.color.b);
            AddProperty(target.binding.graphic, type, "m_Color.a", target.rest.color.a);
        }

        private static void AddProperty(UnityEngine.Object target, Type type, string property, float value)
        {
            AnimationMode.AddPropertyModification(EditorCurveBinding.FloatCurve("", type, property),
                new PropertyModification { target = target, propertyPath = property, value = value.ToString("R", CultureInfo.InvariantCulture) }, true);
        }

        private static void Tick()
        {
            if (!running || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            { Stop(); return; }
            for (int i = 0; i < targets.Count; i++) if (targets[i].binding.target == null) { Stop(); return; }
            double now = EditorApplication.timeSinceStartup;
            if (!paused) elapsed += now - lastTime;
            lastTime = now;
            if (!paused && !double.IsInfinity(duration) && elapsed > duration + .2) { Stop(); return; }
            Sample();
        }

        private static void Sample()
        {
            if (!running) return;
            AnimationMode.BeginSampling();
            try
            {
                double time = Math.Min(elapsed, duration);
                if (owner != null && owner.source == UIAnimationSource.Sequence)
                {
                    double cycle = sequenceCycle;
                    if (cycle > 0 && !double.IsInfinity(cycle)) time = elapsed < duration ? elapsed % cycle : cycle;
                }
                foreach (Target target in targets)
                {
                    UIAnimationPose result = target.rest;
                    UIAnimationChannels channels = UIAnimationChannels.None;
                    foreach (Segment segment in segments)
                    {
                        if (segment.target != target || time < segment.time) continue;
                        UIAnimationPose pose = Evaluate(segment, time);
                        UIAnimationChannels affected = UIAnimationEvaluator.Channels(segment.tracks);
                        Merge(ref result, pose, affected);
                        channels |= affected;
                    }
                    result.Apply(target.binding, channels);
                }
            }
            finally { AnimationMode.EndSampling(); }
            Canvas.ForceUpdateCanvases();
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }

        private static void Merge(ref UIAnimationPose target, UIAnimationPose value, UIAnimationChannels channels)
        {
            if ((channels & UIAnimationChannels.Position) != 0) target.position = value.position;
            if ((channels & UIAnimationChannels.Scale) != 0) target.scale = value.scale;
            if ((channels & UIAnimationChannels.Rotation) != 0) target.rotation = value.rotation;
            if ((channels & UIAnimationChannels.Alpha) != 0) target.alpha = value.alpha;
            if ((channels & UIAnimationChannels.Color) != 0) target.color = value.color;
        }

        public static void Pause(bool value) { paused = value; lastTime = EditorApplication.timeSinceStartup; }
        public static void Seek(float normalized)
        {
            if (!running) return;
            double length = double.IsInfinity(duration) ? 2 : duration;
            elapsed = Mathf.Clamp01(normalized) * length;
            paused = true; Sample();
        }

        public static void Stop()
        {
            EditorApplication.update -= Tick;
            if (owner != null) owner.IsPreviewing = false;
            running = paused = false;
            try
            {
                if (ownsMode && AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                foreach (Target target in targets)
                    if (target.binding.target != null) target.rest.Apply(target.binding, UIAnimationChannels.All);
            }
            finally
            {
                ownsMode = false; owner = null; targets.Clear(); segments.Clear();
                sequenceCycle = 0;
                SceneView.RepaintAll();
            }
        }
    }
}
