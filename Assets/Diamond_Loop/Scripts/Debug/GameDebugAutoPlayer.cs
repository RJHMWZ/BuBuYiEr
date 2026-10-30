#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEngine;

namespace ClawbearGames
{
    /// <summary>Development-only greedy player. Uses real deployment and collection; never forces a win.</summary>
    public sealed class GameDebugAutoPlayer : MonoBehaviour
    {
        private const float DecisionInterval = 0.5f;
        private const float StallTimeout = 45f;
        private readonly Dictionary<Color32, int> exposed = new Dictionary<Color32, int>();
        private readonly Dictionary<Color32, int> remaining = new Dictionary<Color32, int>();
        private readonly Dictionary<Color32, int> committed = new Dictionary<Color32, int>();
        private IngameManager currentGame;
        private float decisionTime, idleTime, victoryTime, startupTime;
        private int lastCubeCount, lastFilledCount;
        private bool countedVictory;
        private bool awaitingStart;

        public bool IsRunning { get; private set; }
        public bool Continuous { get; private set; }
        public int Moves { get; private set; }
        public int LevelsCompleted { get; private set; }
        public string Status { get; private set; } = "自动打关未启动";

        public void Begin(IngameManager game, bool continuous)
        {
            IsRunning = true;
            Continuous = continuous;
            Moves = LevelsCompleted = 0;
            AttachLevel(game);
            Status = "自动打关已启动";
        }

        public void Stop(string reason, bool success = true, bool notify = true)
        {
            if (!IsRunning) return;
            IsRunning = false;
            Status = $"{reason}（投放 {Moves} 次，通关 {LevelsCompleted} 关）";
            if (notify) GameDebugService.Report(success, Status);
        }

        private void AttachLevel(IngameManager game)
        {
            currentGame = game;
            decisionTime = idleTime = victoryTime = startupTime = 0;
            awaitingStart = game.IngameState != IngameState.Ingame_Playing;
            lastCubeCount = lastFilledCount = -1;
            countedVictory = false;
        }

        private void Update()
        {
            if (!IsRunning || GameDebugService.IsLoading || GameDebugService.PanelVisible || Time.timeScale <= 0) return;
            IngameManager game = IngameManager.Instance;
            if (game == null) { Stop("已离开关卡，自动打关停止"); return; }
            if (game != currentGame) AttachLevel(game);
            // Ingame starts in GameOver and invokes StartGame after 0.15s. A continuous
            // run must wait for that startup, rather than treating it as a real defeat.
            if (game.DebugLevelData == null || (awaitingStart && game.IngameState != IngameState.Ingame_Playing))
            {
                Status = "等待新关卡初始化";
                startupTime += Time.deltaTime;
                if (startupTime >= 5f) Stop("新关卡未能开始，请检查初始化或引导状态", false);
                return;
            }
            awaitingStart = false;

            if (game.IngameState == IngameState.Ingame_CompleteLevel)
            {
                if (!countedVictory) { countedVictory = true; LevelsCompleted++; }
                if (!Continuous) { Stop("当前关卡已自动通关"); return; }
                Status = $"第 {game.CurrentLevel} 关已通关，等待结算后进入下一关";
                victoryTime += Time.deltaTime;
                if (victoryTime >= 3f && !GameDebugService.AdvanceAutoPlay())
                    Stop("已到最后一个可用关卡，连续自动打关完成");
                return;
            }
            if (game.IngameState != IngameState.Ingame_Playing)
            {
                Stop($"第 {game.CurrentLevel} 关失败或进入复活流程，自动打关停止", false);
                return;
            }
            // Shopping, power-up targeting and an in-flight user action take precedence.
            if (ViewManager.Instance == null || ViewManager.Instance.ActiveViewType != ViewType.INGAME_VIEW ||
                ViewManager.Instance.IngameViewController.MessagePanel.gameObject.activeSelf) return;

            decisionTime += Time.deltaTime;
            if (decisionTime < DecisionInterval) return;
            float elapsed = decisionTime;
            decisionTime = 0;
            int cubes = game.CubeManager.DebugReadCubeColors(exposed, remaining);
            int filled = 0;
            committed.Clear();
            foreach (PotController pot in game.GridManager.AllPots)
            {
                if (pot == null) continue;
                if (pot.IsFilled) filled++;
                if (!pot.IsOnConveyor || pot.IsFilled) continue;
                committed.TryGetValue(pot.TargetColor, out int capacity);
                committed[pot.TargetColor] = capacity + pot.RemainingCapacity;
            }
            if (cubes != lastCubeCount || filled != lastFilledCount) idleTime = 0;
            else idleTime += elapsed;
            lastCubeCount = cubes;
            lastFilledCount = filled;

            if (idleTime >= StallTimeout)
            {
                Stop("45 秒内没有收集进展，可能被槽位、锁定/冰冻罐或颜色顺序卡住；请手动检查后重新启动", false);
                return;
            }
            if (game.StopControl) { Status = "等待当前投放动画完成"; return; }
            if (game.ConveyorManager.GetUsedTile() >= game.ConveyorManager.DebugSlotCapacity)
            {
                Status = $"等待传送带空位 · 剩余方块 {cubes} · 已投放 {Moves} 次";
                return;
            }

            PotController best = null;
            float bestScore = float.NegativeInfinity;
            foreach (CellController cell in game.GridManager.AllCells())
            {
                PotController pot = cell.GetCurrentPot();
                if (!CanDeploy(pot)) continue;
                exposed.TryGetValue(pot.TargetColor, out int visible);
                remaining.TryGetValue(pot.TargetColor, out int total);
                committed.TryGetValue(pot.TargetColor, out int reserved);
                int useful = visible - reserved;
                // Do not spend a slot on a frozen pot, an unavailable color, or duplicate capacity.
                if (useful <= 0 || total - reserved < pot.RemainingCapacity) continue;
                float ratio = Mathf.Min(useful, pot.RemainingCapacity) / (float)Mathf.Max(1, pot.RemainingCapacity);
                float score = ratio * 100f + Mathf.Min(useful, pot.RemainingCapacity);
                if (pot.PotState == PotState.Decay) score -= 20f;
                if (score > bestScore) { best = pot; bestScore = score; }
            }
            // Some levels deliberately hide the useful color behind a front pot.
            // Spend one buffer slot to reveal that next pot, keeping a slot free to deploy it.
            if (best == null && game.ConveyorManager.DebugSlotCapacity - game.ConveyorManager.GetUsedTile() >= 2)
            {
                foreach (CellController cell in game.GridManager.AllCells())
                {
                    PotController front = cell.GetCurrentPot();
                    PotController next = cell.DebugPeekNextPot();
                    if (!CanDeploy(front) || next == null || next.PotState == PotState.Locked || next.PotState == PotState.Frozen) continue;
                    remaining.TryGetValue(front.TargetColor, out int frontTotal);
                    committed.TryGetValue(front.TargetColor, out int frontReserved);
                    exposed.TryGetValue(next.TargetColor, out int visible);
                    committed.TryGetValue(next.TargetColor, out int nextReserved);
                    if (frontTotal - frontReserved < front.RemainingCapacity || visible <= nextReserved) continue;
                    float score = visible - nextReserved;
                    if (score > bestScore) { best = front; bestScore = score; }
                }
            }
            if (best != null && game.DebugTryDeployPot(best))
            {
                Moves++;
                idleTime = 0;
                Status = $"第 {game.CurrentLevel} 关 · 已投放 {Moves} 次 · 剩余方块 {cubes}";
            }
            else Status = $"等待可收集颜色 · 剩余方块 {cubes} · 无进展 {idleTime:0}/{StallTimeout:0} 秒";
        }

        private static bool CanDeploy(PotController pot) => pot != null && pot.gameObject.activeInHierarchy &&
            pot.CanSelect && !pot.IsOnConveyor && !pot.IsFilled && pot.RemainingCapacity > 0 &&
            pot.PotState != PotState.Locked && pot.PotState != PotState.Frozen;

        private void OnDestroy() => IsRunning = false;
    }
}
#endif
