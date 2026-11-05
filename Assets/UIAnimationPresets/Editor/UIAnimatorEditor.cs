using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UIAnimationPresets.Editor
{
    [CustomEditor(typeof(UIAnimator)), CanEditMultipleObjects]
    public sealed class UIAnimatorEditor : UnityEditor.Editor
    {
        private bool events, triggers;
        private void OnEnable()
        {
            serializedObject.FindProperty("animation").isExpanded = true;
            EditorApplication.update += RepaintPlaying;
        }
        private void OnDisable() { EditorApplication.update -= RepaintPlaying; if (UIAnimationPreview.Owner == target) UIAnimationPreview.Stop(); }
        private void RepaintPlaying() { if (Application.isPlaying || UIAnimationPreview.IsRunning) Repaint(); }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var animator = (UIAnimator)target;
            EditorGUILayout.LabelField("UI ANIMATION PRESETS", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("animationTarget"), new GUIContent("动画目标（空 = 自身）"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("source"), new GUIContent("配置来源"));
            UIAnimationSource source = (UIAnimationSource)serializedObject.FindProperty("source").enumValueIndex;
            if (source == UIAnimationSource.Asset)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("presetAsset"), new GUIContent("预设资产"));
            else if (source == UIAnimationSource.Sequence)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("sequenceLoops"), new GUIContent("组合循环次数 (-1 无限)"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("sequenceUnscaledTime"), new GUIContent("组合使用独立时间"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("steps"), new GUIContent("组合步骤（Append / Join / Insert）"), true);
            }
            else EditorGUILayout.PropertyField(serializedObject.FindProperty("animation"), new GUIContent("动画配置"), true);
            triggers = EditorGUILayout.Foldout(triggers, "自动触发与生命周期", true);
            if (triggers)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("playOnEnable"), new GUIContent("启用时播放"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("activateOnPlay"), new GUIContent("播放时激活目标"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stopOnDisable"), new GUIContent("禁用时中断方式"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("restoreWhenDisabled"), new GUIContent("禁用时恢复原始状态"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("animateClick"), new GUIContent("点击时播放"));
                if (serializedObject.FindProperty("animateClick").boolValue) EditorGUILayout.PropertyField(serializedObject.FindProperty("clickPreset"), new GUIContent("点击效果"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("animateHover"), new GUIContent("悬停时播放"));
                if (serializedObject.FindProperty("animateHover").boolValue) EditorGUILayout.PropertyField(serializedObject.FindProperty("hoverPreset"), new GUIContent("悬停效果"));
            }
            events = EditorGUILayout.Foldout(events, "播放事件", true);
            if (events)
            {
                foreach (string name in new[] { "onStarted", "onCompleted", "onCanceled", "onLoop" }) EditorGUILayout.PropertyField(serializedObject.FindProperty(name));
            }
            if (EditorGUI.EndChangeCheck())
            {
                UIAnimationPreview.Stop();
                serializedObject.ApplyModifiedProperties();
            }
            EditorGUILayout.Space(7);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(Application.isPlaying ? "播放" : "▶ 预览", GUILayout.Height(28)))
                {
                    if (Application.isPlaying) foreach (UnityEngine.Object item in targets) ((UIAnimator)item).Play();
                    else UIAnimationPreview.Start(animator);
                }
                if (GUILayout.Button("暂停 / 继续", GUILayout.Height(28)))
                {
                    if (Application.isPlaying) { if (animator.Handle.IsPaused) animator.Resume(); else animator.Pause(); }
                    else UIAnimationPreview.Pause(!UIAnimationPreview.IsPaused);
                }
                if (GUILayout.Button("■ 停止并恢复", GUILayout.Height(28)))
                {
                    if (Application.isPlaying) foreach (UnityEngine.Object item in targets) ((UIAnimator)item).Stop();
                    else UIAnimationPreview.Stop();
                }
            }
            if (UIAnimationPreview.Owner == animator && UIAnimationPreview.IsRunning)
            {
                double length = double.IsInfinity(UIAnimationPreview.Duration) ? 2 : Math.Max(.001, UIAnimationPreview.Duration);
                float current = (float)(UIAnimationPreview.Elapsed / length);
                EditorGUI.BeginChangeCheck();
                float value = EditorGUILayout.Slider("预览进度", Mathf.Clamp01(current), 0, 1);
                if (EditorGUI.EndChangeCheck()) UIAnimationPreview.Seek(value);
            }
            if (Application.isPlaying)
            {
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), animator.Handle.Progress, animator.Handle.IsPaused ? "Paused" : "Playback");
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("立即完成")) animator.Complete();
                    if (GUILayout.Button("中断并保留当前状态")) animator.Stop(UIAnimationStopMode.KeepCurrentState);
                }
            }
            using (new EditorGUI.DisabledScope(UIAnimationPreview.IsRunning))
            {
                if (GUILayout.Button("记录当前为原始状态")) animator.CaptureRestPose();
                using (new EditorGUI.DisabledScope(source == UIAnimationSource.Sequence || animator.Definition == null))
                    if (GUILayout.Button("保存为可复用预设资产")) SaveAsset(animator);
            }
            if (animator.Target.parent != null && animator.Target.parent.GetComponent<LayoutGroup>() != null)
                EditorGUILayout.HelpBox("父级 LayoutGroup 会控制位置。建议把动画目标设为布局项内部的视觉子节点。", MessageType.Info);
            if (source == UIAnimationSource.Asset && animator.presetAsset == null)
                EditorGUILayout.HelpBox("请指定预设资产。", MessageType.Warning);
            if (!string.IsNullOrEmpty(UIAnimationPreview.LastError)) EditorGUILayout.HelpBox(UIAnimationPreview.LastError, MessageType.Warning);
            EditorGUILayout.HelpBox("编辑模式预览会恢复原始属性，不触发运行时事件或隐藏对象。淡入淡出通过 CanvasGroup 生效，支持 UGUI 与 TextMeshPro。", MessageType.None);
            serializedObject.ApplyModifiedProperties();
        }

        private static void SaveAsset(UIAnimator animator)
        {
            if (animator.Definition == null) return;
            string path = EditorUtility.SaveFilePanelInProject("保存 UI 动画预设", animator.Definition.preset.ToString(), "asset", "选择保存位置");
            if (string.IsNullOrEmpty(path)) return;
            var asset = ScriptableObject.CreateInstance<UIAnimationPresetAsset>();
            asset.animation = animator.Definition.Clone();
            AssetDatabase.CreateAsset(asset, path); AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(asset);
        }
    }

    [CustomEditor(typeof(UIAnimationPresetAsset))]
    public sealed class UIAnimationPresetAssetEditor : UnityEditor.Editor
    {
        private RectTransform previewTarget;
        private void OnEnable() => serializedObject.FindProperty("animation").isExpanded = true;
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("animation"), new GUIContent("预设配置"), true);
            if (EditorGUI.EndChangeCheck()) UIAnimationPreview.Stop();
            serializedObject.ApplyModifiedProperties();
            previewTarget = (RectTransform)EditorGUILayout.ObjectField("预览目标", previewTarget, typeof(RectTransform), true);
            RectTransform rect = previewTarget != null ? previewTarget : Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<RectTransform>() : null;
            using (new EditorGUI.DisabledScope(rect == null || Application.isPlaying))
                if (GUILayout.Button("在选中的 UI 上预览")) UIAnimationPreview.Start(((UIAnimationPresetAsset)target).animation, rect);
            if (GUILayout.Button("停止预览并恢复")) UIAnimationPreview.Stop();
        }
    }
}
