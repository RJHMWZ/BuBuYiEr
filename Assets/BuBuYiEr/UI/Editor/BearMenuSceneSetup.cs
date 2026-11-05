using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace ClawbearGames.EditorTools
{
    [InitializeOnLoad]
    public static class BearMenuSceneSetup
    {
        public const string HomeScenePath = "Assets/Diamond_Loop/Scenes/Home.unity";
        private static readonly string[] PageNames = { "主菜单", "每日奖励", "图鉴收藏", "商店", "设置" };

        static BearMenuSceneSetup()
        {
            EditorSceneManager.sceneOpened += (scene, mode) =>
            {
                if (scene.path == HomeScenePath && !EditorApplication.isPlayingOrWillChangePlaymode)
                    EditorApplication.delayCall += () => FrameMenu(scene);
            };
        }

        [MenuItem("Tools/BuBuYiEr/将主菜单生成到 Home 场景")]
        public static void Apply()
            => ApplyInternal(false);

        public static void RebuildSceneMenu()
            => ApplyInternal(false);

        private static void ApplyInternal(bool replaceMenu)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("请先退出 Play Mode 再生成场景 UI。");

            Scene scene = SceneManager.GetSceneByPath(HomeScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                bool hasUnsavedScene = Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty);
                scene = EditorSceneManager.OpenScene(HomeScenePath, hasUnsavedScene ? OpenSceneMode.Additive : OpenSceneMode.Single);
            }
            SceneManager.SetActiveScene(scene);
            ViewManager manager = FindInScene<ViewManager>(scene).SingleOrDefault();
            if (manager == null) throw new InvalidOperationException("Home 场景需要一个 ViewManager。");

            CleanMenuPrefab();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BearMenuBuilder.PrefabPath);
            if (prefab == null) throw new InvalidOperationException("请先执行“应用参考图主菜单”生成预制体。");
            BearHomeMenuController menu = FindInScene<BearHomeMenuController>(scene).SingleOrDefault();
            if(replaceMenu && menu!=null)
            {
                Undo.DestroyObjectImmediate(menu.gameObject);
                menu=null;
            }
            if (menu == null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                Undo.RegisterCreatedObjectUndo(instance, "生成主菜单 UI");
                instance.name = "HomeMenuCanvas";
                menu = instance.GetComponent<BearHomeMenuController>();
            }
            menu.gameObject.SetActive(true);
            menu.transform.SetParent(null, false);
            menu.transform.localScale = Vector3.one;
            menu.themeRoot.gameObject.SetActive(true);

            EventSystem events = FindInScene<EventSystem>(scene).SingleOrDefault();
            var managerData = new SerializedObject(manager);
            if (events == null)
            {
                EventSystem template = managerData.FindProperty("eventSystemPrefab").objectReferenceValue as EventSystem;
                GameObject eventObject = template != null
                    ? (GameObject)PrefabUtility.InstantiatePrefab(template.gameObject, scene)
                    : new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                if (eventObject.scene != scene) SceneManager.MoveGameObjectToScene(eventObject, scene);
                Undo.RegisterCreatedObjectUndo(eventObject, "生成菜单事件系统");
                events = eventObject.GetComponent<EventSystem>();
            }
            Undo.RecordObject(manager, "绑定场景主菜单");
            managerData.FindProperty("homeViewControllerPrefab").objectReferenceValue = prefab.GetComponent<HomeViewController>();
            managerData.FindProperty("sceneHomeViewController").objectReferenceValue = menu.GetComponent<HomeViewController>();
            managerData.FindProperty("sceneEventSystem").objectReferenceValue = events;
            managerData.ApplyModifiedProperties();

            int removed = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name != "Environment" && root.name != "Background" && root.name != "Directional Light") continue;
                removed += root.GetComponentsInChildren<Transform>(true).Length;
                Undo.DestroyObjectImmediate(root);
            }
            Camera camera = FindInScene<Camera>(scene).FirstOrDefault(c => c.CompareTag("MainCamera"));
            if (camera != null)
            {
                Undo.RecordObject(camera, "切换菜单二维相机");
                camera.orthographic = true;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color32(255, 247, 232, 255);
                camera.cullingMask = 0;
                Undo.RecordObject(camera.transform, "设置菜单相机位置");
                camera.transform.SetPositionAndRotation(new Vector3(0, 0, -10), Quaternion.identity);
            }
            RenderSettings.skybox = null;
            RenderSettings.sun = null;
            RenderSettings.fog = false;
            PreviewPage(menu, 0);
            PrefabUtility.RecordPrefabInstancePropertyModifications(menu.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("保存 Home 场景失败。");
            AssetDatabase.SaveAssets();
            FrameMenu(scene);
            Debug.Log($"[BearMenu] 已将可编辑 UI 保存到 Home 场景，清理 {removed} 个旧三维展示物体。无需运行即可预览。");
        }

        private static void CleanMenuPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(BearMenuBuilder.PrefabPath) == null) return;
            GameObject contents = PrefabUtility.LoadPrefabContents(BearMenuBuilder.PrefabPath);
            try
            {
                BearHomeMenuController menu = contents.GetComponent<BearHomeMenuController>();
                if (menu == null || menu.themeRoot == null) throw new InvalidOperationException("主菜单预制体缺少界面层级。");
                bool changed = false;
                foreach (Transform child in contents.transform.Cast<Transform>().ToArray())
                {
                    if (child == menu.themeRoot) continue;
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                    changed = true;
                }
                var data = new SerializedObject(contents.GetComponent<HomeViewController>());
                SerializedProperty legacyEffect = data.FindProperty("coinEffectControllerPrefab");
                if (legacyEffect.objectReferenceValue != null)
                {
                    legacyEffect.objectReferenceValue = null;
                    data.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }
                if (changed) PrefabUtility.SaveAsPrefabAsset(contents, BearMenuBuilder.PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }

        public static void PreviewPage(BearHomeMenuController menu, int index)
        {
            if (Application.isPlaying) { menu.OpenPage(index); return; }
            Undo.RegisterFullObjectHierarchyUndo(menu.gameObject, "预览" + PageNames[index]);
            menu.themeRoot.gameObject.SetActive(true);
            for (int i = 0; i < menu.pages.Length; i++) menu.pages[i].SetActive(i == index);
            menu.modal.SetActive(false);
            menu.RefreshDailyRewards(1,0);
            if (menu.CollectionPanel != null) menu.CollectionPanel.SelectTab(1);
            menu.PreviewShopCategory(0);
            menu.soundSlider.SetValueWithoutNotify(.575f);
            menu.musicSlider.SetValueWithoutNotify(.415f);
            foreach (RectTransform knob in menu.toggleKnobs) knob.anchoredPosition = new Vector2(97, -30);
            Canvas.ForceUpdateCanvases();
            foreach (BearMenuLayout layout in menu.GetComponentsInChildren<BearMenuLayout>(true)) layout.Refresh();
            PrefabUtility.RecordPrefabInstancePropertyModifications(menu.gameObject);
            SceneView.RepaintAll();
        }

        public static void FrameMenu(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || EditorApplication.isPlayingOrWillChangePlaymode || Application.isBatchMode) return;
            BearHomeMenuController menu = FindInScene<BearHomeMenuController>(scene).FirstOrDefault();
            if (menu == null) return;
            Selection.activeGameObject = menu.gameObject;
            SceneView view = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView : EditorWindow.GetWindow<SceneView>();
            view.in2DMode = true;
            view.sceneLighting = false;
            view.FrameSelected();
            view.Repaint();
        }

        private static T[] FindInScene<T>(Scene scene) where T : Component
            => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
    }

    [CustomEditor(typeof(BearHomeMenuController))]
    public sealed class BearHomeMenuControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (Application.isPlaying) return;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("编辑模式页面预览", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("无需 Play 即可查看和编辑。预览不会读取或修改玩家存档。", MessageType.Info);
            string[] names = { "主菜单", "每日奖励", "图鉴收藏", "商店", "设置" };
            var menu = (BearHomeMenuController)target;
            for (int i = 0; i < names.Length; i++)
                if (GUILayout.Button(names[i])) BearMenuSceneSetup.PreviewPage(menu, i);
        }
    }
}
