using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UIAnimationPresets.Samples.Editor
{
    public static class UIAnimationDemoBuilder
    {
        private const string Root = "Assets/UIAnimationPresets";
        private const string ScenePath = Root + "/Samples/Scenes/UIAnimationDemo.unity";

        [MenuItem("Tools/UI Animation/创建或重建示例场景")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            UIAnimationPresets.Editor.UIAnimationPreview.Stop();
            Directory.CreateDirectory(Root + "/Presets");
            Directory.CreateDirectory(Root + "/Samples/Scenes");
            Directory.CreateDirectory(Root + "/Samples/Resources");
            AssetDatabase.Refresh();
            foreach (UIAnimationPreset preset in Enum.GetValues(typeof(UIAnimationPreset)))
            {
                string path = Root + "/Presets/" + preset + ".asset";
                if (AssetDatabase.LoadAssetAtPath<UIAnimationPresetAsset>(path) != null) continue;
                var asset = ScriptableObject.CreateInstance<UIAnimationPresetAsset>();
                asset.animation = UIPresetLibrary.Create(preset);
                AssetDatabase.CreateAsset(asset, path);
            }
            string fontPath = Root + "/Samples/Resources/UIAnimationDemoFont.asset";
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if (font == null)
            {
                if (Shader.Find("TextMeshPro/Mobile/Distance Field") == null)
                {
                    Debug.LogWarning("请先导入 TMP Essential Resources，再手动创建动画示例。游戏菜单不依赖此示例。");
                    return;
                }
                // Built-in runtime fonts may omit font data and return null from FontEngine.
                Font source = AssetDatabase.FindAssets("t:Font", new[] { "Assets" })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(path => AssetImporter.GetAtPath(path) is TrueTypeFontImporter importer && importer.includeFontData)
                    .Select(AssetDatabase.LoadAssetAtPath<Font>).FirstOrDefault(value => value != null);
                if (source == null) { Debug.LogWarning("示例场景需要一份勾选 Include Font Data 的字体资源。"); return; }
                font = TMP_FontAsset.CreateFontAsset(source);
                if (font == null) { Debug.LogWarning("无法创建示例字体，请检查源字体的 Font Data 配置。"); return; }
                font.name = "UIAnimationDemoFont";
                AssetDatabase.CreateAsset(font, fontPath);
                if (font.material != null) AssetDatabase.AddObjectToAsset(font.material, font);
                foreach (Texture2D texture in font.atlasTextures) if (texture != null) AssetDatabase.AddObjectToAsset(texture, font);
                EditorUtility.SetDirty(font);
            }
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool close = !scene.IsValid() || !scene.isLoaded;
            if (close) scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                UIAnimationDemo demo = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    demo = root.GetComponent<UIAnimationDemo>();
                    if (demo != null) break;
                }
                if (demo == null)
                {
                    var go = new GameObject("UI Animation Demo");
                    SceneManager.MoveGameObjectToScene(go, scene);
                    demo = go.AddComponent<UIAnimationDemo>();
                }
                demo.demoFont = font;
                demo.Build();
                EditorUtility.SetDirty(demo);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
                AssetImporter importer = AssetImporter.GetAtPath(ScenePath);
                if (importer != null) { importer.userData = "UIAnimationDemo_v1"; importer.SaveAndReimport(); }
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (close && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            }
        }

        [MenuItem("Tools/UI Animation/打开示例场景")]
        public static void Open()
        {
            if (!File.Exists(ScenePath)) Build();
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
        }
    }
}
