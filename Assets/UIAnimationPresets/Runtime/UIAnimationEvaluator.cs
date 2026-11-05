using System;
using UnityEngine;
using UnityEngine.UI;

namespace UIAnimationPresets
{
    public sealed class UIAnimationBinding
    {
        public readonly RectTransform target;
        public readonly CanvasGroup group;
        public readonly Graphic graphic;
        public UIAnimationBinding(RectTransform target, bool createCanvasGroup = true)
        {
            this.target = target;
            group = target.GetComponent<CanvasGroup>();
            if (group == null && createCanvasGroup) group = target.gameObject.AddComponent<CanvasGroup>();
            graphic = target.GetComponent<Graphic>();
        }
    }

    public struct UIAnimationPose
    {
        public Vector3 position, scale;
        public Quaternion rotation;
        public float alpha;
        public Color color;
        public static UIAnimationPose Capture(UIAnimationBinding binding) => new UIAnimationPose {
            position = binding.target.anchoredPosition3D, scale = binding.target.localScale,
            rotation = binding.target.localRotation, alpha = binding.group != null ? binding.group.alpha : 1,
            color = binding.graphic != null ? binding.graphic.color : Color.white
        };

        public void Apply(UIAnimationBinding binding, UIAnimationChannels channels)
        {
            if (binding.target == null) return;
            if ((channels & UIAnimationChannels.Position) != 0 && binding.target.anchoredPosition3D != position) binding.target.anchoredPosition3D = position;
            if ((channels & UIAnimationChannels.Scale) != 0 && binding.target.localScale != scale) binding.target.localScale = scale;
            if ((channels & UIAnimationChannels.Rotation) != 0 && binding.target.localRotation != rotation) binding.target.localRotation = rotation;
            if ((channels & UIAnimationChannels.Alpha) != 0 && binding.group != null && !Mathf.Approximately(binding.group.alpha, alpha)) binding.group.alpha = Mathf.Clamp01(alpha);
            if ((channels & UIAnimationChannels.Color) != 0 && binding.graphic != null && binding.graphic.color != color) binding.graphic.color = color;
        }
    }

    public static class UIAnimationEvaluator
    {
        public static UIAnimationChannels Channels(UIAnimationTrack[] tracks)
        {
            UIAnimationChannels channels = UIAnimationChannels.None;
            for (int i = 0; i < tracks.Length; i++) if (tracks[i].enabled) channels |= (UIAnimationChannels)(1 << (int)tracks[i].property);
            return channels;
        }

        public static float NormalizedTime(UIAnimationDefinition definition, double elapsed, out int iteration, out bool complete)
        {
            double active = elapsed - Math.Max(0, definition.delay);
            complete = false;
            iteration = 0;
            if (active < 0) return 0;
            int loops = definition.loops < 0 ? -1 : Mathf.Max(1, definition.loops);
            double duration = Math.Max(definition.loops < 0 ? .001 : 0, definition.duration);
            double gap = Math.Max(0, definition.loopDelay);
            double cycle = Math.Max(.001, duration + gap);
            if (loops > 0 && elapsed >= definition.TotalDuration)
            {
                iteration = loops - 1;
                complete = true;
                return definition.loopMode == UIAnimationLoopMode.PingPong && (iteration & 1) != 0 ? 0 : 1;
            }
            iteration = (int)Math.Min(int.MaxValue - 1, Math.Floor(active / cycle));
            float t = duration <= 0 ? 1 : Mathf.Clamp01((float)((active - iteration * cycle) / duration));
            return definition.loopMode == UIAnimationLoopMode.PingPong && (iteration & 1) != 0 ? 1 - t : t;
        }

        public static UIAnimationPose Sample(UIAnimationDefinition definition, UIAnimationTrack[] tracks,
            UIAnimationPose rest, UIAnimationPose start, float normalized)
        {
            UIAnimationPose result = rest;
            for (int i = 0; i < tracks.Length; i++)
            {
                UIAnimationTrack track = tracks[i];
                if (!track.enabled) continue;
                float t = Ease(track.overrideEase ? track.ease : definition.ease, normalized,
                    track.overrideEase ? track.curve : definition.customCurve);
                float q = track.wave == UIAnimationWave.Tween ? t : Wave(track.wave, Mathf.Clamp01(t), definition.vibrato, definition.damping);
                float factor = track.useDistance ? definition.distance : track.useAngle ? definition.angle : 1;
                Vector3 from = track.from * factor, to = track.to * factor;
                Vector3 vector = track.wave == UIAnimationWave.Tween ? Vector3.LerpUnclamped(from, to, q)
                    : from + to * (q * definition.strength);
                switch (track.property)
                {
                    case UIAnimationProperty.Position:
                        if (track.fromCurrent)
                        {
                            Vector3 end = track.valueMode == UIAnimationValueMode.Absolute ? to
                                : track.valueMode == UIAnimationValueMode.Multiply ? Vector3.Scale(rest.position, to) : rest.position + to;
                            result.position = Vector3.LerpUnclamped(start.position, end, q);
                        }
                        else if (track.additive) result.position += vector;
                        else result.position = track.valueMode == UIAnimationValueMode.Absolute ? vector
                            : track.valueMode == UIAnimationValueMode.Multiply ? Vector3.Scale(rest.position, vector) : rest.position + vector;
                        break;
                    case UIAnimationProperty.Scale:
                        if (track.fromCurrent)
                            result.scale = Vector3.LerpUnclamped(start.scale, track.valueMode == UIAnimationValueMode.Multiply ? Vector3.Scale(rest.scale, to)
                                : track.valueMode == UIAnimationValueMode.Relative ? rest.scale + to : to, q);
                        else if (track.additive) result.scale = Vector3.Scale(result.scale, vector);
                        else result.scale = track.valueMode == UIAnimationValueMode.Absolute ? vector
                            : track.valueMode == UIAnimationValueMode.Multiply ? Vector3.Scale(rest.scale, vector) : rest.scale + vector;
                        break;
                    case UIAnimationProperty.Rotation:
                        if (track.fromCurrent)
                        {
                            Quaternion relative = track.valueMode == UIAnimationValueMode.Absolute ? start.rotation : Quaternion.Inverse(rest.rotation) * start.rotation;
                            Vector3 angles = relative.eulerAngles;
                            angles = new Vector3(Mathf.DeltaAngle(0, angles.x), Mathf.DeltaAngle(0, angles.y), Mathf.DeltaAngle(0, angles.z));
                            vector = Vector3.LerpUnclamped(angles, to, q);
                        }
                        result.rotation = track.additive ? result.rotation * Quaternion.Euler(vector)
                            : track.valueMode == UIAnimationValueMode.Absolute ? Quaternion.Euler(vector) : rest.rotation * Quaternion.Euler(vector);
                        break;
                    case UIAnimationProperty.Alpha:
                        float alpha = track.wave == UIAnimationWave.Tween ? Mathf.LerpUnclamped(track.fromValue, track.toValue, q)
                            : track.fromValue + track.toValue * q * definition.strength;
                        if (track.fromCurrent)
                            result.alpha = Mathf.LerpUnclamped(start.alpha, track.valueMode == UIAnimationValueMode.Multiply ? rest.alpha * track.toValue
                                : track.valueMode == UIAnimationValueMode.Relative ? rest.alpha + track.toValue : track.toValue, q);
                        else result.alpha = track.additive ? result.alpha * alpha : track.valueMode == UIAnimationValueMode.Multiply ? rest.alpha * alpha
                            : track.valueMode == UIAnimationValueMode.Relative ? rest.alpha + alpha : alpha;
                        break;
                    case UIAnimationProperty.Color:
                        Color begin = track.fromCurrent ? start.color : track.colorFromRest ? rest.color : track.fromColor;
                        Color color = Color.LerpUnclamped(begin, track.toColor, q);
                        result.color = track.additive ? result.color * color : track.valueMode == UIAnimationValueMode.Multiply ? rest.color * color
                            : track.valueMode == UIAnimationValueMode.Relative ? rest.color + color : color;
                        break;
                }
            }
            return result;
        }

        private static float Wave(UIAnimationWave wave, float t, int vibrato, float damping)
        {
            if (t <= 0 || t >= 1) return 0;
            switch (wave)
            {
                case UIAnimationWave.Punch: return Mathf.Sin(t * Mathf.PI * 2 * Mathf.Max(1, vibrato)) * Mathf.Exp(-damping * t) * (1 - t);
                case UIAnimationWave.Shake: return Mathf.Sin(t * Mathf.PI * 2 * Mathf.Max(1, vibrato) * 1.73f) * Mathf.Sin(t * Mathf.PI) * Mathf.Exp(-damping * t * .2f);
                case UIAnimationWave.Bounce: return Mathf.Abs(Mathf.Sin(t * Mathf.PI * Mathf.Max(1, vibrato))) * Mathf.Exp(-damping * t * .4f) * (1 - t);
                case UIAnimationWave.Heartbeat:
                    float first = (t - .2f) / .075f, second = (t - .43f) / .09f;
                    return (Mathf.Exp(-first * first) + .65f * Mathf.Exp(-second * second)) * Mathf.Sin(t * Mathf.PI);
                default: return .5f - .5f * Mathf.Cos(t * Mathf.PI * 2);
            }
        }

        public static float Ease(UIAnimationEase ease, float value, AnimationCurve curve = null)
        {
            float t = Mathf.Clamp01(value);
            switch (ease)
            {
                case UIAnimationEase.InSine: return 1 - Mathf.Cos(t * Mathf.PI * .5f);
                case UIAnimationEase.OutSine: return Mathf.Sin(t * Mathf.PI * .5f);
                case UIAnimationEase.InOutSine: return (1 - Mathf.Cos(Mathf.PI * t)) * .5f;
                case UIAnimationEase.InQuad: return t * t;
                case UIAnimationEase.OutQuad: return 1 - (1 - t) * (1 - t);
                case UIAnimationEase.InOutQuad: return t < .5f ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) * .5f;
                case UIAnimationEase.InCubic: return t * t * t;
                case UIAnimationEase.OutCubic: return 1 - Mathf.Pow(1 - t, 3);
                case UIAnimationEase.InOutCubic: return t < .5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) * .5f;
                case UIAnimationEase.InQuart: return t * t * t * t;
                case UIAnimationEase.OutQuart: return 1 - Mathf.Pow(1 - t, 4);
                case UIAnimationEase.InOutQuart: return t < .5f ? 8 * t * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 4) * .5f;
                case UIAnimationEase.InExpo: return t == 0 ? 0 : Mathf.Pow(2, 10 * t - 10);
                case UIAnimationEase.OutExpo: return t == 1 ? 1 : 1 - Mathf.Pow(2, -10 * t);
                case UIAnimationEase.InOutExpo: return t == 0 || t == 1 ? t : t < .5f ? Mathf.Pow(2, 20 * t - 10) * .5f : (2 - Mathf.Pow(2, -20 * t + 10)) * .5f;
                case UIAnimationEase.InBack: return 2.70158f * t * t * t - 1.70158f * t * t;
                case UIAnimationEase.OutBack: return 1 + 2.70158f * Mathf.Pow(t - 1, 3) + 1.70158f * Mathf.Pow(t - 1, 2);
                case UIAnimationEase.InOutBack:
                    const float c = 2.5949095f;
                    return t < .5f ? Mathf.Pow(2 * t, 2) * ((c + 1) * 2 * t - c) * .5f : (Mathf.Pow(2 * t - 2, 2) * ((c + 1) * (2 * t - 2) + c) + 2) * .5f;
                case UIAnimationEase.OutBounce: return Bounce(t);
                case UIAnimationEase.InOutBounce: return t < .5f ? (1 - Bounce(1 - 2 * t)) * .5f : (1 + Bounce(2 * t - 1)) * .5f;
                case UIAnimationEase.OutElastic: return t == 0 || t == 1 ? t : Mathf.Pow(2, -10 * t) * Mathf.Sin((t * 10 - .75f) * (Mathf.PI * 2 / 3)) + 1;
                case UIAnimationEase.Custom: return curve != null && curve.length > 0 ? curve.Evaluate(t) : t;
                default: return t;
            }
        }

        private static float Bounce(float t)
        {
            if (t < 1 / 2.75f) return 7.5625f * t * t;
            if (t < 2 / 2.75f) { t -= 1.5f / 2.75f; return 7.5625f * t * t + .75f; }
            if (t < 2.5f / 2.75f) { t -= 2.25f / 2.75f; return 7.5625f * t * t + .9375f; }
            t -= 2.625f / 2.75f;
            return 7.5625f * t * t + .984375f;
        }
    }
}
