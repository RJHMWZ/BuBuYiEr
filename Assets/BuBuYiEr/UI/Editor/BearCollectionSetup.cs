using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClawbearGames.EditorTools
{
    [InitializeOnLoad]
    public static class BearCollectionSetup
    {
        static BearCollectionSetup()
        {
            EditorApplication.delayCall += Apply;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += Apply;
            };
        }

        private static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Apply;
                return;
            }
            BearMenuArt art = AssetDatabase.LoadAssetAtPath<BearMenuArt>(BearMenuBuilder.ThemePath);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BearMenuBuilder.PrefabPath);
            if (art == null || prefab == null) return;
            BearCollectionPanel panel = prefab.GetComponentInChildren<BearCollectionPanel>(true);
            bool current = panel != null && panel.layoutVersion == BearCollectionPanel.LayoutVersion
                && art.collectionSprites != null && art.collectionSprites.Length >= 16;
            try
            {
                if (!current) BearMenuBuilder.RebuildCollectionFromCollectArt();
                if (prefab.transform.Find("BearMenuUI/Home/PlayAction/SoloPlay") == null)
                    BearMenuBuilder.ApplySoloMenuActions();
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    if (!scene.isLoaded || scene.path != BearMenuSceneSetup.HomeScenePath) continue;
                    bool wasDirty = scene.isDirty;
                    bool changed = false;
                    foreach (BearHomeMenuController menu in scene.GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<BearHomeMenuController>(true)))
                    {
                        if (menu.art != art || menu.pages == null || menu.pages.Length < 3) continue;
                        BearMenuFactory.EnsureSoloActions(menu);
                        if (menu.CollectionPanel != null && menu.CollectionPanel.layoutVersion == BearCollectionPanel.LayoutVersion) continue;
                        Undo.RegisterFullObjectHierarchyUndo(menu.gameObject, "应用图鉴收藏素材");
                        BearMenuFactory.RebuildCollection(menu);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(menu);
                        changed = true;
                    }
                    if (changed)
                    {
                        EditorSceneManager.MarkSceneDirty(scene);
                        if (!wasDirty) EditorSceneManager.SaveScene(scene);
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
