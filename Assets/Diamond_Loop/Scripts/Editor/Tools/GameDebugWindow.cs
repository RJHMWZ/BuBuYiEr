using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClawbearGames
{
    /// <summary>Editor companion to the development-build panel; shares every gameplay command.</summary>
    public sealed class GameDebugWindow : EditorWindow
    {
        private const string HomePath = "Assets/Diamond_Loop/Scenes/Home.unity";
        private const string StartSceneKey = "BuBuYiEr.Debug.PreviousStartScene";
        private const string RestoreStartKey = "BuBuYiEr.Debug.RestoreStartScene";
        private int level = 1, coins = 10000, stars = 3;
        private Vector2 scroll;

        [MenuItem("Tools/BuBuYiEr/开发调试面板")]
        public static void Open() => GetWindow<GameDebugWindow>("开发调试");

        [InitializeOnLoadMethod]
        private static void RegisterPlayModeCallback()
        {
            EditorApplication.playModeStateChanged -= RestoreStartScene;
            EditorApplication.playModeStateChanged += RestoreStartScene;
        }

        private static void RestoreStartScene(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode && state != PlayModeStateChange.EnteredEditMode) return;
            if (!SessionState.GetBool(RestoreStartKey, false)) return;
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(StartSceneKey, ""));
            SessionState.SetBool(RestoreStartKey, false);
        }

        private void OnEnable() => minSize = new Vector2(360, 540);
        private void OnInspectorUpdate() { if (EditorApplication.isPlaying) Repaint(); }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("BuBuYiEr 开发调试", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("游戏内：点击右上角“调试 F1”，或按 F1。跳关、重试和模拟结算均使用试玩模式，不写入关卡进度。", MessageType.Info);
            if (!EditorApplication.isPlaying)
            {
                using (new EditorGUI.DisabledScope(EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode))
                    if (GUILayout.Button("从主页启动调试", GUILayout.Height(34)))
                    {
                        SceneAsset home = AssetDatabase.LoadAssetAtPath<SceneAsset>(HomePath);
                        if (home != null)
                        {
                            SessionState.SetString(StartSceneKey, AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
                            SessionState.SetBool(RestoreStartKey, true);
                            EditorSceneManager.playModeStartScene = home;
                            EditorApplication.isPlaying = true;
                        }
                    }
                EditorGUILayout.HelpBox("进入 Play Mode 后可使用下方操作。启动按钮临时从 Home 运行，保留当前编辑场景。", MessageType.None);
            }

            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || GameDebugService.IsLoading))
            {
                if (EditorApplication.isPlaying)
                {
                    PlayerData data = GameDebugService.GetPlayerData();
                    IngameManager game = IngameManager.Instance;
                    EditorGUILayout.LabelField("场景 / 状态", SceneManager.GetActiveScene().name + " / " + (game != null ? game.IngameState.ToString() : "—"));
                    EditorGUILayout.LabelField("当前 / 存档关卡", (game != null ? game.CurrentLevel.ToString() : "—") + " / " + data.CurrentLevel);
                    EditorGUILayout.LabelField("模式", GameDebugService.IsPreviewLevel ? "试玩（进度不保存）" : "正常游玩");
                    if (GUILayout.Button(GameDebugService.PanelVisible ? "关闭游戏内面板" : "显示游戏内面板"))
                        GameDebugService.SetPanelVisible(!GameDebugService.PanelVisible);
                }

                SpaceTitle("关卡操作");
                level = EditorGUILayout.IntField("目标关卡", level);
                if (GUILayout.Button("跳到指定关卡")) GameDebugService.JumpToLevel(level);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("上一关")) GameDebugService.LoadAdjacentLevel(-1);
                if (GUILayout.Button("重试")) GameDebugService.RetryLevel();
                if (GUILayout.Button("下一关")) GameDebugService.LoadAdjacentLevel(1);
                EditorGUILayout.EndHorizontal();
                SpaceTitle("自动打关");
                EditorGUILayout.HelpBox("自动点击糖砖，使用本关蜂蜜购买并升级六种小球，通过真实碰撞完成关卡。", MessageType.None);
                using (new EditorGUI.DisabledScope(GameDebugService.IsAutoPlaying || !GameDebugService.CanFinishLevel))
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("自动打本关")) GameDebugService.StartAutoPlay();
                    if (GUILayout.Button("连续自动打关")) GameDebugService.StartAutoPlay(true);
                    EditorGUILayout.EndHorizontal();
                }
                using (new EditorGUI.DisabledScope(!GameDebugService.IsAutoPlaying))
                    if (GUILayout.Button("停止自动打关")) GameDebugService.StopAutoPlay();
                EditorGUILayout.HelpBox(GameDebugService.AutoPlayStatus, MessageType.None);
                stars = EditorGUILayout.IntSlider("胜利星级", stars, 1, 3);
                using (new EditorGUI.DisabledScope(!GameDebugService.CanFinishLevel))
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("模拟胜利")) GameDebugService.FinishLevel(true, stars);
                    if (GUILayout.Button("模拟失败")) GameDebugService.FinishLevel(false);
                    EditorGUILayout.EndHorizontal();
                }
                if (GUILayout.Button("跳过引导并重试")) GameDebugService.SkipTutorial();
                if (GUILayout.Button("返回主页 / 退出试玩")) GameDebugService.ReturnHome();

                SpaceTitle("运行速度");
                EditorGUILayout.LabelField("当前倍速", GameDebugService.GameSpeed.ToString("0.##") + "x（游戏内面板打开时暂停）");
                EditorGUILayout.BeginHorizontal();
                foreach (float speed in new[] { 0f, 0.25f, 0.5f, 1f, 2f, 4f })
                    if (GUILayout.Button(speed == 0 ? "暂停" : speed + "x")) GameDebugService.SetGameSpeed(speed);
                EditorGUILayout.EndHorizontal();

                SpaceTitle("资源与存档");
                EditorGUILayout.HelpBox("金币和引导状态会保存。首次修改自动备份一份存档；恢复和重置会返回主页。", MessageType.None);
                if (EditorApplication.isPlaying)
                {
                    PlayerData data = GameDebugService.GetPlayerData();
                    EditorGUILayout.LabelField("金币", data.TotalCoins.ToString());
                }
                coins = EditorGUILayout.IntField("目标金币", coins);
                if (GUILayout.Button("设置金币")) GameDebugService.SetCoins(coins);
                if (GUILayout.Button("备份当前存档"))
                    if (!GameDebugService.HasBackup || EditorUtility.DisplayDialog("覆盖备份", "将覆盖现有的一份备份。", "覆盖", "取消"))
                        GameDebugService.BackupPlayerData();
                using (new EditorGUI.DisabledScope(!GameDebugService.HasBackup))
                    if (GUILayout.Button("恢复备份并回主页") && EditorUtility.DisplayDialog("恢复备份", "将覆盖当前游戏存档。", "恢复", "取消"))
                        GameDebugService.RestorePlayerData();
                if (GUILayout.Button("重置新玩家并回主页") && EditorUtility.DisplayDialog("重置游戏存档", "已有备份会保留；没有备份时会先自动备份。", "重置", "取消"))
                    GameDebugService.ResetPlayerData();

                SpaceTitle("日志");
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("导出日志")) GameDebugService.ExportLogs();
                if (GUILayout.Button("清空日志")) GameDebugService.ClearLogs();
                EditorGUILayout.EndHorizontal();
                var logs = GameDebugService.Logs;
                for (int i = Mathf.Max(0, logs.Count - 5); i < logs.Count; i++)
                    EditorGUILayout.LabelField(logs[i], EditorStyles.wordWrappedMiniLabel);
            }
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(GameDebugService.Status, MessageType.None);
            EditorGUILayout.EndScrollView();
        }

        private static void SpaceTitle(string text)
        {
            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
        }
    }
}
