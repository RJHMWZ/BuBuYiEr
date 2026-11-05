using System;
using UnityEngine;

namespace UIAnimationPresets
{
    public static class UIPresetLibrary
    {
        private static readonly UIAnimationTrack[][] cached = BuildTracks();
        public static UIAnimationDefinition Create(UIAnimationPreset preset)
        {
            var definition = new UIAnimationDefinition();
            ApplyDefaults(definition, preset);
            return definition;
        }

        public static UIAnimationCategory Category(UIAnimationPreset preset)
        {
            if (preset <= UIAnimationPreset.FlipIn) return UIAnimationCategory.Entrance;
            if (preset <= UIAnimationPreset.RotateOut) return UIAnimationCategory.Exit;
            if (preset <= UIAnimationPreset.ClickTilt) return UIAnimationCategory.Click;
            if (preset <= UIAnimationPreset.Heartbeat) return UIAnimationCategory.Loop;
            if (preset <= UIAnimationPreset.Flash) return UIAnimationCategory.Emphasis;
            return UIAnimationCategory.Reward;
        }

        public static void ApplyDefaults(UIAnimationDefinition definition, UIAnimationPreset preset)
        {
            definition.preset = preset;
            definition.category = Category(preset);
            definition.duration = definition.category == UIAnimationCategory.Click ? .22f : definition.category == UIAnimationCategory.Loop ? 1.2f : .4f;
            definition.delay = definition.loopDelay = 0;
            definition.loops = definition.category == UIAnimationCategory.Loop ? -1 : 1;
            definition.loopMode = UIAnimationLoopMode.Restart;
            definition.ease = definition.category == UIAnimationCategory.Loop || definition.category == UIAnimationCategory.Emphasis ||
                definition.category == UIAnimationCategory.Click ? UIAnimationEase.Linear : UIAnimationEase.OutCubic;
            definition.completion = definition.category == UIAnimationCategory.Exit || preset == UIAnimationPreset.CoinFly
                ? UIAnimationCompletion.DisableGameObject : UIAnimationCompletion.KeepFinalState;
            definition.blockInput = definition.category == UIAnimationCategory.Entrance || definition.category == UIAnimationCategory.Exit;
            definition.useCustomTracks = false;
            definition.strength = 1;
            definition.distance = 160;
            definition.angle = 15;
            definition.vibrato = 4;
            definition.damping = 5;
            if (preset == UIAnimationPreset.PopIn || preset == UIAnimationPreset.RewardPop) definition.ease = UIAnimationEase.OutBack;
            if (preset == UIAnimationPreset.Spin) definition.duration = 2;
            if (preset == UIAnimationPreset.CoinFly) definition.duration = .65f;
        }

        internal static UIAnimationTrack[] Tracks(UIAnimationDefinition definition) => definition.useCustomTracks
            ? definition.tracks != null ? definition.tracks.ToArray() : Array.Empty<UIAnimationTrack>() : cached[(int)definition.preset];

        private static UIAnimationTrack Position(Vector3 from, Vector3 to, UIAnimationWave wave = UIAnimationWave.Tween)
        {
            var track = UIAnimationTrack.Vector(UIAnimationProperty.Position, from, to, UIAnimationValueMode.Relative, wave);
            track.useDistance = true;
            return track;
        }
        private static UIAnimationTrack Scale(float from, float to, UIAnimationWave wave = UIAnimationWave.Tween) =>
            UIAnimationTrack.Vector(UIAnimationProperty.Scale, Vector3.one * from, Vector3.one * to, UIAnimationValueMode.Multiply, wave);
        private static UIAnimationTrack Rotate(float from, float to, UIAnimationWave wave = UIAnimationWave.Tween)
        {
            var track = UIAnimationTrack.Vector(UIAnimationProperty.Rotation, Vector3.forward * from, Vector3.forward * to, UIAnimationValueMode.Relative, wave);
            track.useAngle = true;
            return track;
        }
        private static UIAnimationTrack GlowTrack(Color color) => new UIAnimationTrack {
            enabled = true, property = UIAnimationProperty.Color, wave = UIAnimationWave.Pulse,
            colorFromRest = true, toColor = color, valueMode = UIAnimationValueMode.Absolute
        };

        private static UIAnimationTrack[][] BuildTracks()
        {
            var result = new UIAnimationTrack[Enum.GetValues(typeof(UIAnimationPreset)).Length][];
            result[(int)UIAnimationPreset.FadeIn] = new[] { UIAnimationTrack.Alpha(0, 1) };
            result[(int)UIAnimationPreset.ScaleIn] = new[] { Scale(.6f, 1) };
            result[(int)UIAnimationPreset.PopIn] = new[] { Scale(.65f, 1), UIAnimationTrack.Alpha(0, 1) };
            result[(int)UIAnimationPreset.SlideInLeft] = new[] { Position(Vector3.left, Vector3.zero), UIAnimationTrack.Alpha(0, 1) };
            result[(int)UIAnimationPreset.SlideInRight] = new[] { Position(Vector3.right, Vector3.zero), UIAnimationTrack.Alpha(0, 1) };
            result[(int)UIAnimationPreset.SlideInUp] = new[] { Position(Vector3.down, Vector3.zero), UIAnimationTrack.Alpha(0, 1) };
            result[(int)UIAnimationPreset.SlideInDown] = new[] { Position(Vector3.up, Vector3.zero), UIAnimationTrack.Alpha(0, 1) };
            result[(int)UIAnimationPreset.RotateIn] = new[] { Rotate(-1, 0), Scale(.8f, 1), UIAnimationTrack.Alpha(0, 1) };
            var flip = UIAnimationTrack.Vector(UIAnimationProperty.Rotation, new Vector3(0, 85, 0), Vector3.zero);
            result[(int)UIAnimationPreset.FlipIn] = new[] { flip, UIAnimationTrack.Alpha(0, 1) };
            result[(int)UIAnimationPreset.FadeOut] = new[] { UIAnimationTrack.Alpha(1, 0) };
            result[(int)UIAnimationPreset.ScaleOut] = new[] { Scale(1, .1f), UIAnimationTrack.Alpha(1, 0) };
            result[(int)UIAnimationPreset.PopOut] = new[] { Scale(1, 1.15f), UIAnimationTrack.Alpha(1, 0) };
            result[(int)UIAnimationPreset.SlideOutLeft] = new[] { Position(Vector3.zero, Vector3.left), UIAnimationTrack.Alpha(1, 0) };
            result[(int)UIAnimationPreset.SlideOutRight] = new[] { Position(Vector3.zero, Vector3.right), UIAnimationTrack.Alpha(1, 0) };
            result[(int)UIAnimationPreset.SlideOutUp] = new[] { Position(Vector3.zero, Vector3.up), UIAnimationTrack.Alpha(1, 0) };
            result[(int)UIAnimationPreset.SlideOutDown] = new[] { Position(Vector3.zero, Vector3.down), UIAnimationTrack.Alpha(1, 0) };
            result[(int)UIAnimationPreset.RotateOut] = new[] { Rotate(0, 1), Scale(1, .8f), UIAnimationTrack.Alpha(1, 0) };
            result[(int)UIAnimationPreset.ClickScale] = new[] { Scale(1, -.08f, UIAnimationWave.Pulse) };
            result[(int)UIAnimationPreset.ClickPunch] = new[] { Scale(1, .13f, UIAnimationWave.Punch) };
            result[(int)UIAnimationPreset.ClickTilt] = new[] { Rotate(0, .35f, UIAnimationWave.Punch) };
            result[(int)UIAnimationPreset.Pulse] = new[] { Scale(1, .075f, UIAnimationWave.Pulse) };
            result[(int)UIAnimationPreset.Float] = new[] { Position(Vector3.zero, Vector3.up * .1f, UIAnimationWave.Pulse) };
            var spin = Rotate(0, 360); spin.useAngle = false;
            result[(int)UIAnimationPreset.Spin] = new[] { spin };
            result[(int)UIAnimationPreset.Breathe] = new[] { Scale(1, .045f, UIAnimationWave.Pulse), UIAnimationTrack.Alpha(1, -.18f, UIAnimationWave.Pulse) };
            result[(int)UIAnimationPreset.Heartbeat] = new[] { Scale(1, .12f, UIAnimationWave.Heartbeat) };
            result[(int)UIAnimationPreset.Shake] = new[] { Position(Vector3.zero, new Vector3(.07f, .025f, 0), UIAnimationWave.Shake) };
            result[(int)UIAnimationPreset.Punch] = new[] { Scale(1, .2f, UIAnimationWave.Punch) };
            result[(int)UIAnimationPreset.Wiggle] = new[] { Rotate(0, .7f, UIAnimationWave.Punch) };
            result[(int)UIAnimationPreset.Bounce] = new[] { Position(Vector3.zero, Vector3.up * .18f, UIAnimationWave.Bounce) };
            result[(int)UIAnimationPreset.Flash] = new[] { UIAnimationTrack.Alpha(1, -.6f, UIAnimationWave.Pulse) };
            result[(int)UIAnimationPreset.RewardPop] = new[] { Scale(.25f, 1), UIAnimationTrack.Alpha(0, 1) };
            result[(int)UIAnimationPreset.CoinFly] = new[] { Position(Vector3.zero, Vector3.up * .8f), Scale(1, .65f), UIAnimationTrack.Alpha(1, 0) };
            result[(int)UIAnimationPreset.StarBurst] = new[] { Scale(1, .3f, UIAnimationWave.Punch), Rotate(0, 1, UIAnimationWave.Punch), GlowTrack(new Color(1, .85f, .35f)) };
            result[(int)UIAnimationPreset.Celebrate] = new[] { Position(Vector3.zero, Vector3.up * .2f, UIAnimationWave.Bounce), Rotate(0, .5f, UIAnimationWave.Punch), Scale(1, .1f, UIAnimationWave.Pulse) };
            result[(int)UIAnimationPreset.Glow] = new[] { GlowTrack(new Color(1, .82f, .35f)) };
            return result;
        }
    }
}
