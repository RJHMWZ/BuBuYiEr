#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClawbearGames
{
    /// <summary>Shared commands for the runtime panel and editor window. Excluded from release builds.</summary>
    public static class GameDebugService
    {
        private const string BackupKey = "BUBUYIER_DEBUG_PLAYER_BACKUP";
        private const int MaxLogs = 120;
        private static readonly List<string> logs = new List<string>();
        private static readonly List<int> levelNumbers = new List<int>();
        private static LevelData levelOverride;
        private static GameDebugPanel host;
        private static int blockThroughFrame;

        public static event Action Changed;
        public static string Status { get; private set; } = "调试工具已就绪";
        public static bool PanelVisible { get; private set; }
        public static bool IsLoading { get; private set; }
        public static float GameSpeed { get; private set; } = 1f;
        public static bool IsPreviewLevel => levelOverride != null;
        public static bool HasBackup => PlayerPrefs.HasKey(BackupKey);
        public static bool IsBlockingInput => PanelVisible || IsLoading || GameSpeed == 0 || Time.frameCount <= blockThroughFrame;
        public static IReadOnlyList<string> Logs => logs;
        public static IReadOnlyList<int> LevelNumbers => levelNumbers;
        public static bool CanFinishLevel => !IsLoading && IngameManager.Instance != null &&
            IngameManager.Instance.IngameState == IngameState.Ingame_Playing &&
            ViewManager.Instance != null && ViewManager.Instance.ActiveViewType == ViewType.INGAME_VIEW;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Application.logMessageReceived -= OnLog;
            Changed = null;
            logs.Clear();
            levelNumbers.Clear();
            host = null;
            levelOverride = null;
            PanelVisible = IsLoading = false;
            GameSpeed = 1f;
            blockThroughFrame = -1;
            Status = "调试工具已就绪";
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            Application.logMessageReceived += OnLog;
            var root = new GameObject("[Development] Game Debug Tools");
            UnityEngine.Object.DontDestroyOnLoad(root);
            host = root.AddComponent<GameDebugPanel>();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            IsLoading = false;
            if (scene.name == "Home" || scene.name == "Level") levelOverride = null;
            EnsurePlayerData();
            Changed?.Invoke();
        }

        public static int GetLevelNumber(LevelData level)
        {
            if (level != null && level.name.StartsWith("Level_", StringComparison.Ordinal) &&
                int.TryParse(level.name.Substring(6), out int number)) return number;
            return level != null ? level.levelNumber : 0;
        }

        public static void RefreshLevels()
        {
            levelNumbers.Clear();
            foreach (LevelData level in Resources.LoadAll<LevelData>("Levels"))
            {
                int number = GetLevelNumber(level);
                if (number > 0 && level.name == "Level_" + number && !levelNumbers.Contains(number))
                    levelNumbers.Add(number);
            }
            levelNumbers.Sort();
        }

        public static bool TryGetLevelOverride(out LevelData level)
        {
            level = levelOverride;
            return level != null;
        }

        public static void SetPanelVisible(bool visible)
        {
            PanelVisible = visible;
            blockThroughFrame = Time.frameCount + 1;
            ApplyTimeScale();
            Changed?.Invoke();
        }

        public static bool SetGameSpeed(float speed)
        {
            if (IsLoading || float.IsNaN(speed) || float.IsInfinity(speed) || speed < 0 || speed > 4)
                return Report(false, "倍速应在 0～4 之间，且不能在切场景时修改");
            GameSpeed = speed;
            ApplyTimeScale();
            return Report(true, speed == 0 ? "已暂停；面板关闭后仍保持暂停" : $"运行速度设为 {speed:0.##}x；关闭面板后生效");
        }

        private static void ApplyTimeScale() => Time.timeScale = PanelVisible ? 0f : GameSpeed;

        public static bool JumpToLevel(int number)
        {
            if (IsLoading) return Report(false, "正在切换场景，请稍候");
            LevelData level = Resources.Load<LevelData>("Levels/Level_" + number);
            if (number < 1 || level == null) return Report(false, $"找不到关卡 Level_{number}，请检查 Resources/Levels");
            if (!CanLoadScene("Ingame")) return false;
            levelOverride = level;
            return LoadScene("Ingame", $"试玩第 {number} 关（不写入关卡进度）");
        }

        public static bool LoadAdjacentLevel(int direction)
        {
            if (direction != -1 && direction != 1) return Report(false, "方向必须为 -1 或 1");
            if (IsLoading) return Report(false, "正在切换场景，请稍候");
            if (levelNumbers.Count == 0) RefreshLevels();
            int current = IngameManager.Instance != null ? IngameManager.Instance.CurrentLevel : GetPlayerData().CurrentLevel;
            int next = direction > 0 ? levelNumbers.Find(n => n > current) : levelNumbers.FindLast(n => n < current);
            if (next <= 0) return Report(false, direction > 0 ? "已经是最后一个可用关卡" : "已经是第一个可用关卡");
            return JumpToLevel(next);
        }

        public static bool RetryLevel()
        {
            if (IsLoading) return Report(false, "正在切换场景，请稍候");
            IngameManager game = IngameManager.Instance;
            if (game == null || game.DebugLevelData == null) return Report(false, "请先进入一个关卡");
            if (!CanLoadScene("Ingame")) return false;
            // Preserve an inspector test asset too, even if it is outside Resources.
            levelOverride = game.DebugLevelData;
            return LoadScene("Ingame", "已重试当前关卡（试玩模式）");
        }

        public static bool ReturnHome()
        {
            if (IsLoading || !CanLoadScene("Home")) return false;
            levelOverride = null;
            return LoadScene("Home", "已返回主页；下次正常开始使用存档关卡");
        }

        public static bool FinishLevel(bool won, int stars = 3)
        {
            if (!CanFinishLevel) return Report(false, "只能在关卡进行中模拟结算");
            // Suppress progression and interstitials before the state-change event fires.
            levelOverride = IngameManager.Instance.DebugLevelData;
            GameSpeed = 1f;
            SetPanelVisible(false);
            IngameManager.Instance.DebugFinishLevel(won, stars);
            return Report(true, won ? $"已模拟 {Mathf.Clamp(stars, 1, 3)} 星胜利（不写入关卡进度）" : "已模拟失败");
        }

        public static bool SkipTutorial()
        {
            if (IsLoading) return false;
            EnsureAutomaticBackup();
            PlayerDataHandler.UpdateWatchedTutorial();
            PlayerPrefs.Save();
            if (IngameManager.Instance != null) return RetryLevel();
            return Report(true, "已跳过新手引导，下次进关生效");
        }

        private static bool CanLoadScene(string name)
        {
            if (Application.CanStreamedLevelBeLoaded(name)) return true;
            return Report(false, $"场景 {name} 未加入 Build Settings，请添加 Diamond_Loop/Scenes 下的场景");
        }

        private static bool LoadScene(string name, string message)
        {
            if (IsLoading || host == null) return Report(false, "调试工具未就绪或正在切换场景");
            IsLoading = true; // Set before starting the coroutine to reject repeated clicks.
            GameSpeed = 1f;
            SetPanelVisible(false);
            host.StartCoroutine(LoadSceneRoutine(name));
            return Report(true, message);
        }

        private static IEnumerator LoadSceneRoutine(string name)
        {
            AsyncOperation operation = null;
            try { operation = SceneManager.LoadSceneAsync(name); }
            catch (Exception ex) { Report(false, "场景加载失败：" + ex.Message); }
            if (operation != null) yield return operation;
            IsLoading = false;
            Changed?.Invoke();
        }

        private static PlayerData DefaultPlayerData()
        {
            var data = new PlayerData { SlotCount = 1, MagnetCount = 1, HammerCount = 1, KeyCount = 1, CoolerCount = 1 };
            if (ServicesManager.Instance != null && ServicesManager.Instance.CoinManager != null)
                data.TotalCoins = ServicesManager.Instance.CoinManager.InitialCoins;
            return data;
        }

        private static void EnsurePlayerData()
        {
            if (!PlayerPrefs.HasKey(PlayerDataHandler.PLAYER_DATA)) PlayerDataHandler.SaveData(DefaultPlayerData());
        }

        public static PlayerData GetPlayerData()
        {
            EnsurePlayerData();
            return JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PlayerDataHandler.PLAYER_DATA));
        }

        private static void EnsureAutomaticBackup()
        {
            if (!HasBackup) BackupPlayerData();
        }

        public static bool BackupPlayerData()
        {
            if (IsLoading) return Report(false, "请等待场景切换完成");
            EnsurePlayerData();
            PlayerPrefs.SetString(BackupKey, PlayerPrefs.GetString(PlayerDataHandler.PLAYER_DATA));
            PlayerPrefs.Save();
            return Report(true, "已备份当前存档（保留一份，可跨运行恢复）");
        }

        public static bool RestorePlayerData()
        {
            if (IsLoading || !HasBackup) return Report(false, "没有可恢复的备份，或正在切换场景");
            if (!CanLoadScene("Home")) return false;
            PlayerData data;
            try { data = JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(BackupKey)); }
            catch (Exception ex) { return Report(false, "备份解析失败：" + ex.Message); }
            if (data == null || data.CurrentLevel < 1 || data.ListStarData == null || data.ListPurchasedProduct == null)
                return Report(false, "备份格式无效，当前存档未修改");
            StopCoinAnimations();
            PlayerDataHandler.SaveData(data);
            PlayerPrefs.Save();
            return ReturnHome();
        }

        public static bool ResetPlayerData()
        {
            if (IsLoading || !CanLoadScene("Home")) return false;
            EnsureAutomaticBackup();
            StopCoinAnimations();
            PlayerDataHandler.SaveData(DefaultPlayerData());
            PlayerPrefs.Save();
            return ReturnHome();
        }

        public static bool SetCoins(int amount)
        {
            if (IsLoading || amount < 0 || amount > 100000000) return Report(false, "金币应在 0～100000000 之间");
            EnsureAutomaticBackup();
            if (ServicesManager.Instance != null && ServicesManager.Instance.CoinManager != null)
                ServicesManager.Instance.CoinManager.DebugSetTotalCoins(amount);
            else PlayerDataHandler.UpdateTotalCoin(amount);
            PlayerPrefs.Save();
            return Report(true, $"金币已设为 {amount}");
        }

        private static void StopCoinAnimations()
        {
            if (ServicesManager.Instance == null || ServicesManager.Instance.CoinManager == null) return;
            ServicesManager.Instance.CoinManager.StopAllCoroutines();
            ServicesManager.Instance.CoinManager.SetCollectedCoins(0);
        }

        public static bool AddAllBoosts(int amount)
        {
            PlayerData data = GetPlayerData();
            if (IsLoading || amount < 1 || amount > 9999 ||
                (long)data.SlotCount + amount > 99999 || (long)data.MagnetCount + amount > 99999 ||
                (long)data.HammerCount + amount > 99999 || (long)data.KeyCount + amount > 99999 ||
                (long)data.CoolerCount + amount > 99999) return Report(false, "道具增量应为 1～9999，单项总量不能超过 99999");
            EnsureAutomaticBackup();
            data.SlotCount += amount;
            data.MagnetCount += amount;
            data.HammerCount += amount;
            data.KeyCount += amount;
            data.CoolerCount += amount;
            PlayerDataHandler.SaveData(data);
            PlayerPrefs.Save();
            if (ViewManager.Instance != null && ViewManager.Instance.IngameViewController != null)
                foreach (BoostButtonController button in ViewManager.Instance.IngameViewController.GetComponentsInChildren<BoostButtonController>(true))
                    button.UpdateCurrentAmount();
            return Report(true, $"五种道具各增加 {amount}（解锁等级仍按关卡配置）");
        }

        private static void OnLog(string message, string stackTrace, LogType type)
        {
            string entry = $"[{DateTime.Now:HH:mm:ss}] [{type}] {message}";
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) entry += "\n" + stackTrace;
            if (entry.Length > 4000) entry = entry.Substring(0, 4000) + "…";
            if (logs.Count >= MaxLogs) logs.RemoveAt(0);
            logs.Add(entry);
        }

        public static void ClearLogs() { logs.Clear(); Changed?.Invoke(); }

        public static bool ExportLogs()
        {
            try
            {
                string path = Path.Combine(Application.persistentDataPath, "debug-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".txt");
                File.WriteAllText(path, $"BuBuYiEr | Unity {Application.unityVersion} | {Application.platform}\nScene: {SceneManager.GetActiveScene().name}\n" +
                    string.Join("\n\n", logs), Encoding.UTF8);
                return Report(true, "日志已导出：" + path);
            }
            catch (Exception ex) { return Report(false, "日志导出失败：" + ex.Message); }
        }

        public static bool Report(bool success, string message)
        {
            Status = message;
            if (success) Debug.Log("[GameDebug] " + message);
            else Debug.LogWarning("[GameDebug] " + message);
            Changed?.Invoke();
            return success;
        }
    }
}
#endif
