using System;
using UnityEditor;
using UnityEngine;

namespace UIAnimationPresets.Editor
{
    public sealed class UIPresetBrowser : EditorWindow
    {
        private int category;
        private string search = "";
        private Vector2 scroll;
        [MenuItem("Tools/UI Animation/预设浏览器")]
        public static void Open() => GetWindow<UIPresetBrowser>("UI Animation Presets");
        [MenuItem("Tools/UI Animation/停止编辑模式预览")]
        public static void StopPreview() => UIAnimationPreview.Stop();

        private void OnGUI()
        {
            EditorGUILayout.LabelField("UI 动画预设库", EditorStyles.boldLabel);
            search = EditorGUILayout.TextField("搜索预设", search);
            category = GUILayout.SelectionGrid(category, UIAnimationEditorGUI.Categories, 3, GUILayout.Height(48));
            RectTransform rect = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<RectTransform>() : null;
            EditorGUILayout.HelpBox(rect != null ? "目标：" + rect.name + "。点击预设可预览；应用后可在 Inspector 继续调整。" : "先在 Hierarchy 选择一个 UI 对象。", MessageType.Info);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (UIAnimationPreset preset in Enum.GetValues(typeof(UIAnimationPreset)))
            {
                if ((int)UIPresetLibrary.Category(preset) != category || preset.ToString().IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0) continue;
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(preset.ToString()), EditorStyles.boldLabel);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        using (new EditorGUI.DisabledScope(rect == null || Application.isPlaying))
                            if (GUILayout.Button("预览")) UIAnimationPreview.Start(UIPresetLibrary.Create(preset), rect);
                        using (new EditorGUI.DisabledScope(rect == null))
                        {
                            if (GUILayout.Button("应用到组件"))
                            {
                                UIAnimationPreview.Stop();
                                UIAnimator animator = rect.GetComponent<UIAnimator>();
                                if (animator == null) animator = Undo.AddComponent<UIAnimator>(rect.gameObject);
                                Undo.RecordObject(animator, "应用 UI 动画预设");
                                animator.source = UIAnimationSource.Preset;
                                animator.animation = UIPresetLibrary.Create(preset);
                                EditorUtility.SetDirty(animator);
                                PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
                                if (Application.isPlaying) animator.Play();
                            }
                        }
                        if (GUILayout.Button("另存为资产")) CreateAsset(preset);
                    }
                }
            }
            EditorGUILayout.EndScrollView();
            if (GUILayout.Button("停止预览并恢复原始状态")) UIAnimationPreview.Stop();
            if (!string.IsNullOrEmpty(UIAnimationPreview.LastError)) EditorGUILayout.HelpBox(UIAnimationPreview.LastError, MessageType.Warning);
        }

        private static void CreateAsset(UIAnimationPreset preset)
        {
            string path = EditorUtility.SaveFilePanelInProject("创建预设资产", preset.ToString(), "asset", "保存动画配置");
            if (string.IsNullOrEmpty(path)) return;
            var asset = CreateInstance<UIAnimationPresetAsset>();
            asset.animation = UIPresetLibrary.Create(preset);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(asset);
        }
    }
}
