using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UIAnimationPresets.Samples.Editor
{
    [InitializeOnLoad]
    public static class UIAnimationDemoBuilder
    {
        private const string Root = "Assets/UIAnimationPresets";
        private const string ScenePath = Root + "/Samples/Scenes/UIAnimationDemo.unity";
        static UIAnimationDemoBuilder()
        {
            EditorApplication.delayCall += Install;
            EditorApplication.playModeStateChanged += state => { if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += Install; };
        }
        private static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += Install; return; }
            AssetImporter importer = AssetImporter.GetAtPath(ScenePath);
            if (importer != null && importer.userData == "UIAnimationDemo_v1") return;
            Scene open = SceneManager.GetSceneByPath(ScenePath);
            if (open.IsValid() && open.isLoaded && open.isDirty) return;
            Build();
        }

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
                font = TMP_FontAsset.CreateFontAsset(Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
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
