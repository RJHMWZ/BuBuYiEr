using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UIAnimationPresets.Editor
{
    internal static class UIAnimationEditorGUI
    {
        internal static readonly string[] Categories = { "入场 Entrance", "退场 Exit", "点击 Click", "循环 Loop", "强调 Emphasis", "奖励反馈 Reward" };
        internal static Rect Next(ref Rect area, float height = -1)
        {
            if (height < 0) height = EditorGUIUtility.singleLineHeight;
            var row = new Rect(area.x, area.y, area.width, height);
            area.y += height + EditorGUIUtility.standardVerticalSpacing;
            return row;
        }
        internal static void Field(ref Rect area, SerializedProperty property, string label)
        {
            float height = EditorGUI.GetPropertyHeight(property, true);
            EditorGUI.PropertyField(Next(ref area, height), property, new GUIContent(label), true);
        }
        internal static void Pair(ref Rect area, SerializedProperty left, string leftLabel, SerializedProperty right, string rightLabel)
        {
            Rect row = Next(ref area);
            float old = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Math.Min(88, row.width * .22f);
            float width = (row.width - 10) * .5f;
            EditorGUI.PropertyField(new Rect(row.x, row.y, width, row.height), left, new GUIContent(leftLabel));
            EditorGUI.PropertyField(new Rect(row.x + width + 10, row.y, width, row.height), right, new GUIContent(rightLabel));
            EditorGUIUtility.labelWidth = old;
        }
        internal static void Apply(SerializedProperty property, UIAnimationPreset preset)
        {
            UIAnimationDefinition value = UIPresetLibrary.Create(preset);
            property.FindPropertyRelative("category").enumValueIndex = (int)value.category;
            property.FindPropertyRelative("preset").enumValueIndex = (int)value.preset;
            property.FindPropertyRelative("duration").floatValue = value.duration;
            property.FindPropertyRelative("delay").floatValue = value.delay;
            property.FindPropertyRelative("ease").enumValueIndex = (int)value.ease;
            property.FindPropertyRelative("loops").intValue = value.loops;
            property.FindPropertyRelative("loopDelay").floatValue = value.loopDelay;
            property.FindPropertyRelative("loopMode").enumValueIndex = (int)value.loopMode;
            property.FindPropertyRelative("completion").enumValueIndex = (int)value.completion;
            property.FindPropertyRelative("blockInput").boolValue = value.blockInput;
            property.FindPropertyRelative("useCustomTracks").boolValue = false;
            property.FindPropertyRelative("distance").floatValue = value.distance;
            property.FindPropertyRelative("angle").floatValue = value.angle;
            property.FindPropertyRelative("strength").floatValue = value.strength;
            property.FindPropertyRelative("vibrato").intValue = value.vibrato;
            property.FindPropertyRelative("damping").floatValue = value.damping;
        }
    }

    [CustomPropertyDrawer(typeof(UIAnimationDefinition))]
    public sealed class UIAnimationDefinitionDrawer : PropertyDrawer
    {
        private static readonly Dictionary<string, bool> advanced = new Dictionary<string, bool>();
        private static readonly UIAnimationPreset[][] presets = BuildPresets();
        private static readonly string[][] names = BuildNames();
        private static UIAnimationPreset[][] BuildPresets()
        {
            var values = new UIAnimationPreset[6][];
            for (int i = 0; i < 6; i++)
            {
                var group = new List<UIAnimationPreset>();
                foreach (UIAnimationPreset preset in Enum.GetValues(typeof(UIAnimationPreset))) if ((int)UIPresetLibrary.Category(preset) == i) group.Add(preset);
                values[i] = group.ToArray();
            }
            return values;
        }
        private static string[][] BuildNames()
        {
            var result = new string[6][];
            for (int i = 0; i < 6; i++)
            {
                result[i] = new string[presets[i].Length];
                for (int j = 0; j < result[i].Length; j++) result[i][j] = ObjectNames.NicifyVariableName(presets[i][j].ToString());
            }
            return result;
        }
        private static string Key(SerializedProperty p) => p.serializedObject.targetObject.GetInstanceID() + ":" + p.propertyPath;
        private static bool Advanced(SerializedProperty p) => advanced.TryGetValue(Key(p), out bool value) && value;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float line = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            if (!property.isExpanded) return line;
            int rows = 8;
            if (property.FindPropertyRelative("ease").enumValueIndex == (int)UIAnimationEase.Custom) rows++;
            float height = rows * line;
            if (Advanced(property))
            {
                height += 8 * line;
                if (property.FindPropertyRelative("useCustomTracks").boolValue)
                    height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("tracks"), true) + EditorGUIUtility.standardVerticalSpacing;
            }
            return height + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("markers"), true) + EditorGUIUtility.standardVerticalSpacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            property.isExpanded = EditorGUI.Foldout(UIAnimationEditorGUI.Next(ref position), property.isExpanded, label, true);
            if (!property.isExpanded) { EditorGUI.EndProperty(); return; }
            EditorGUI.indentLevel++;
            SerializedProperty category = property.FindPropertyRelative("category");
            int group = Mathf.Clamp(category.enumValueIndex, 0, 5);
            EditorGUI.BeginChangeCheck();
            int chosenGroup = EditorGUI.Popup(UIAnimationEditorGUI.Next(ref position), "动画分类", group, UIAnimationEditorGUI.Categories);
            if (EditorGUI.EndChangeCheck()) { UIAnimationEditorGUI.Apply(property, presets[chosenGroup][0]); group = chosenGroup; }
            int selected = Array.IndexOf(presets[group], (UIAnimationPreset)property.FindPropertyRelative("preset").enumValueIndex);
            EditorGUI.BeginChangeCheck();
            int choice = EditorGUI.Popup(UIAnimationEditorGUI.Next(ref position), "动画预设", Mathf.Max(0, selected), names[group]);
            if (EditorGUI.EndChangeCheck()) UIAnimationEditorGUI.Apply(property, presets[group][choice]);
            UIAnimationEditorGUI.Pair(ref position, property.FindPropertyRelative("duration"), "时长 / 秒", property.FindPropertyRelative("delay"), "延迟 / 秒");
            UIAnimationEditorGUI.Field(ref position, property.FindPropertyRelative("ease"), "缓动曲线");
            if (property.FindPropertyRelative("ease").enumValueIndex == (int)UIAnimationEase.Custom)
                UIAnimationEditorGUI.Field(ref position, property.FindPropertyRelative("customCurve"), "自定义曲线");
            UIAnimationEditorGUI.Pair(ref position, property.FindPropertyRelative("loops"), "次数 (-1∞)", property.FindPropertyRelative("loopMode"), "循环方式");
            UIAnimationEditorGUI.Pair(ref position, property.FindPropertyRelative("loopDelay"), "循环间隔", property.FindPropertyRelative("useUnscaledTime"), "独立时间");
            bool showAdvanced = EditorGUI.Foldout(UIAnimationEditorGUI.Next(ref position), Advanced(property), "高级参数与自定义轨道", true);
            advanced[Key(property)] = showAdvanced;
            if (showAdvanced)
            {
                UIAnimationEditorGUI.Pair(ref position, property.FindPropertyRelative("distance"), "移动距离", property.FindPropertyRelative("angle"), "旋转角度");
                UIAnimationEditorGUI.Pair(ref position, property.FindPropertyRelative("strength"), "效果强度", property.FindPropertyRelative("vibrato"), "振动次数");
                UIAnimationEditorGUI.Field(ref position, property.FindPropertyRelative("damping"), "振动衰减");
                UIAnimationEditorGUI.Field(ref position, property.FindPropertyRelative("blockInput"), "播放期间屏蔽交互");
                UIAnimationEditorGUI.Field(ref position, property.FindPropertyRelative("completion"), "完成后的状态");
                UIAnimationEditorGUI.Field(ref position, property.FindPropertyRelative("conflict"), "同属性冲突策略");
                UIAnimationEditorGUI.Field(ref position, property.FindPropertyRelative("interruptedState"), "替换旧动画时");
                UIAnimationEditorGUI.Field(ref position, property.FindPropertyRelative("useCustomTracks"), "使用自定义轨道");
                if (property.FindPropertyRelative("useCustomTracks").boolValue)
                    UIAnimationEditorGUI.Field(ref position, property.FindPropertyRelative("tracks"), "动画轨道");
            }
            UIAnimationEditorGUI.Field(ref position, property.FindPropertyRelative("markers"), "时间点事件（每轮）");
            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(UIAnimationTrack))]
    public sealed class UIAnimationTrackDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty p, GUIContent label)
        {
            float line = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            if (!p.isExpanded) return line;
            var kind = (UIAnimationProperty)p.FindPropertyRelative("property").enumValueIndex;
            int rows = 5 + (kind == UIAnimationProperty.Alpha ? 1 : kind == UIAnimationProperty.Color ? 3 : 0);
            float extra = kind == UIAnimationProperty.Alpha || kind == UIAnimationProperty.Color ? 0
                : EditorGUI.GetPropertyHeight(p.FindPropertyRelative("from")) + EditorGUI.GetPropertyHeight(p.FindPropertyRelative("to")) + EditorGUIUtility.standardVerticalSpacing * 2;
            if (kind == UIAnimationProperty.Position || kind == UIAnimationProperty.Rotation) rows++;
            if (p.FindPropertyRelative("overrideEase").boolValue)
            {
                rows++;
                if (p.FindPropertyRelative("ease").enumValueIndex == (int)UIAnimationEase.Custom) rows++;
            }
            return rows * line + extra;
        }
        public override void OnGUI(Rect area, SerializedProperty p, GUIContent label)
        {
            p.isExpanded = EditorGUI.Foldout(UIAnimationEditorGUI.Next(ref area), p.isExpanded, label, true);
            if (!p.isExpanded) return;
            EditorGUI.indentLevel++;
            UIAnimationEditorGUI.Pair(ref area, p.FindPropertyRelative("enabled"), "启用", p.FindPropertyRelative("property"), "属性");
            UIAnimationEditorGUI.Pair(ref area, p.FindPropertyRelative("wave"), "运动方式", p.FindPropertyRelative("valueMode"), "数值模式");
            UIAnimationEditorGUI.Pair(ref area, p.FindPropertyRelative("fromCurrent"), "当前值起步", p.FindPropertyRelative("additive"), "叠加轨道");
            var kind = (UIAnimationProperty)p.FindPropertyRelative("property").enumValueIndex;
            if (kind == UIAnimationProperty.Alpha)
                UIAnimationEditorGUI.Pair(ref area, p.FindPropertyRelative("fromValue"), "起点", p.FindPropertyRelative("toValue"), "终点 / 振幅");
            else if (kind == UIAnimationProperty.Color)
            {
                UIAnimationEditorGUI.Field(ref area, p.FindPropertyRelative("colorFromRest"), "原始颜色起步");
                UIAnimationEditorGUI.Field(ref area, p.FindPropertyRelative("fromColor"), "起始颜色");
                UIAnimationEditorGUI.Field(ref area, p.FindPropertyRelative("toColor"), "目标颜色");
            }
            else
            {
                UIAnimationEditorGUI.Field(ref area, p.FindPropertyRelative("from"), "起点");
                UIAnimationEditorGUI.Field(ref area, p.FindPropertyRelative("to"), "终点 / 振幅");
            }
            if (kind == UIAnimationProperty.Position) UIAnimationEditorGUI.Field(ref area, p.FindPropertyRelative("useDistance"), "乘以移动距离");
            if (kind == UIAnimationProperty.Rotation) UIAnimationEditorGUI.Field(ref area, p.FindPropertyRelative("useAngle"), "乘以旋转角度");
            UIAnimationEditorGUI.Field(ref area, p.FindPropertyRelative("overrideEase"), "独立缓动曲线");
            if (p.FindPropertyRelative("overrideEase").boolValue)
            {
                UIAnimationEditorGUI.Field(ref area, p.FindPropertyRelative("ease"), "缓动");
                if (p.FindPropertyRelative("ease").enumValueIndex == (int)UIAnimationEase.Custom)
                    UIAnimationEditorGUI.Field(ref area, p.FindPropertyRelative("curve"), "曲线");
            }
            EditorGUI.indentLevel--;
        }
    }
}
