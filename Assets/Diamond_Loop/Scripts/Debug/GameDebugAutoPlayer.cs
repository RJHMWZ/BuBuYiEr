#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
namespace ClawbearGames
{
    public sealed class GameDebugAutoPlayer : MonoBehaviour
    {
        public bool IsRunning { get; private set; }
        public bool Continuous { get; private set; }
        public int Moves { get; private set; }
        public int LevelsCompleted { get; private set; }
        public string Status { get; private set; } = "自动打关未启动";
        private IngameManager current;
        private float decision, wait;
        private bool counted;
        public void Begin(IngameManager game, bool continuous)
        {
            IsRunning = true; Continuous = continuous; Moves = LevelsCompleted = 0;
            Attach(game);
        }
        private void Attach(IngameManager game) { current = game; wait = decision = 0; counted = false; }
        public void Stop(string reason, bool success = true, bool notify = true)
        {
            if (!IsRunning) return;
            IsRunning = false; Status = reason + "（操作 " + Moves + " 次，通关 " + LevelsCompleted + " 关）";
            if (notify) GameDebugService.Report(success, Status);
        }
        private void Update()
        {
            if (!IsRunning || GameDebugService.IsLoading || GameDebugService.PanelVisible || Time.timeScale <= 0) return;
            IngameManager game = IngameManager.Instance;
            if (game == null) { Stop("已离开关卡"); return; }
            if (current != game) Attach(game);
            if (game.Board == null) return;
            if (game.IngameState == IngameState.Ingame_CompleteLevel)
            {
                if (!counted) { counted = true; LevelsCompleted++; }
                if (!Continuous) { Stop("本关完成"); return; }
                wait += Time.deltaTime;
                if (wait > 2 && !GameDebugService.AdvanceAutoPlay()) Stop("已到最后一关");
                return;
            }
            if (game.IngameState != IngameState.Ingame_Playing) { Stop("本局已结束"); return; }
            if (game.IsPaused) return;
            if (!game.IsRoundStarted) game.OnTutorialClosed();
            decision += Time.deltaTime;
            if (decision < .15f) return;
            decision = 0;
            var board = game.Board;
            for (int i = 5; i >= 0; i--)
                if (board.Counts[i] < 3 && board.Buy(i)) { Moves++; break; }
            for (int i = 0; i < 6; i++)
                if (board.Upgrades[i] < 4 && board.Upgrade(i)) { Moves++; break; }
            foreach (var brick in board.Bricks)
                if (brick.Alive) { if (board.Click(brick.Bounds.center)) Moves++; break; }
            Status = "自动点击、购买与升级 · 剩余糖砖 " + (board.Total - board.Destroyed);
        }
    }
}
#endif
