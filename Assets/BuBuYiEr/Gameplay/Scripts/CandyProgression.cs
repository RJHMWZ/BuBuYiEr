using UnityEngine;

namespace ClawbearGames
{
    // Shared by the map, collection, shop and simulation. Progress means highest reached level,
    // so replaying an early level never removes a companion already earned.
    public static class CandyProgression
    {
        public static readonly int[] BallUnlockClears = { 0, 3, 8, 15, 24, 35 };
        public static readonly string[] BallTips = {
            "轻快弹跳，成本最低。先补充数量，再升级威力，适合整盘糖砖。",
            "碰撞时伤害附近糖砖。砖块越密集，草莓的溅射收益越高。",
            "速度最快，能更频繁地来回碰撞。适合收尾和分散的糖砖。",
            "命中后持续融化 3 秒。让薄荷处理高数字，点击清理低数字。",
            "单次威力最高；击碎糖砖后继续穿过。适合厚砖与大糖砖。",
            "火花最多扩散到附近 3 块糖砖。搭配薄荷和可可，清理密集厚砖。"
        };
        public static readonly string[] ChapterNames = { "糖罐初遇", "草莓野餐", "薄荷花园", "可可工坊", "星空茶会", "彩虹旅途" };
        public static readonly string[] ChapterHints = {
            "先买伙伴，再强化；点击也能帮忙收尾", "空心与错列糖砖登场，试试不同的小球组合",
            "厚糖砖越来越多，持续融化更有用", "大糖砖需要集中威力，也别忘记升级",
            "疏密交错的糖砖，搭配高速与范围攻击", "集齐六种伙伴，用你喜欢的小队完成旅程"
        };
        public static readonly int[] ChapterStarts = { 1, 9, 21, 41, 61, 81 };
        public static readonly int[] JourneyGoals = { 3, 8, 15, 24, 35, 50, 70, 101, 15, 45, 120, 240 };
        public static readonly int[] JourneyCoins = { 60, 80, 100, 120, 150, 180, 220, 400, 60, 100, 200, 300 };
        public const int JourneyCount = 12;
        public static int ReachedLevel => Mathf.Max(1, PlayerDataHandler.GetCurrentLevel());
        public static int ClearedLevels => Mathf.Clamp(ReachedLevel - 1, 0, 101);
        public static bool BallUnlocked(int kind, int reachedLevel) => kind >= 0 && kind < 6 && reachedLevel > BallUnlockClears[kind];
        public static string UnlockText(int kind) => kind == 0 ? "初始伙伴" : "通关第 " + BallUnlockClears[kind] + " 关解锁";
        public static int Chapter(int level)
        {
            for (int i = ChapterStarts.Length - 1; i >= 0; i--) if (level >= ChapterStarts[i]) return i;
            return 0;
        }
        public static int NextBall(int reachedLevel)
        {
            for (int i = 1; i < 6; i++) if (!BallUnlocked(i, reachedLevel)) return i;
            return -1;
        }
        public static string NextGoal(int reachedLevel)
        {
            int next = NextBall(reachedLevel);
            return next >= 0 ? "通关第 " + BallUnlockClears[next] + " 关 · 迎接" + CandyBreakoutSimulation.Names[next]
                : "六位伙伴已集齐 · 收集关卡三星与旅程奖励";
        }
        public static int TotalStars => PlayerDataHandler.GetTotalStars();
        public static int JourneyProgress(int index) => index < 8 ? ClearedLevels : TotalStars;
        public static string JourneyTitle(int index) => index < 8 ? "甜蜜旅程 · 通关 " + JourneyGoals[index] + " 关" : "星光收藏 · 收集 " + JourneyGoals[index] + " 颗星";
        public static int RoundReward(LevelData level, int stars, int previousStars)
        {
            int baseCoins = previousStars == 0 ? level.coinReward : Mathf.Max(8, level.coinReward / 4);
            return baseCoins + Mathf.Max(0, stars - previousStars) * 10;
        }
    }
}
