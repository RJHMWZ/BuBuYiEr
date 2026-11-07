using UnityEngine;

namespace ClawbearGames
{
    public static class BearSoloProgress
    {
        private static int cachedRequest = -1, cachedLevel = 1;
        public static int PendingStory = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            cachedRequest = -1; cachedLevel = 1; PendingStory = -1;
        }

        public static int PlayableLevel(int requested)
        {
            requested = Mathf.Max(1, requested);
            if (cachedRequest == requested) return cachedLevel;
            cachedRequest = requested;
            if (Resources.Load<LevelData>("Levels/Level_" + requested) != null)
                return cachedLevel = requested;
            int best = 0, first = int.MaxValue;
            foreach (LevelData level in Resources.LoadAll<LevelData>("Levels"))
            {
                int number;
                if (!int.TryParse(level.name.Replace("Level_", ""), out number)) continue;
                first = Mathf.Min(first, number);
                if (number <= requested) best = Mathf.Max(best, number);
            }
            return cachedLevel = best > 0 ? best : first != int.MaxValue ? first : 1;
        }

        public static bool HasNext(int level) => Resources.Load<LevelData>("Levels/Level_" + (level + 1)) != null;
    }
}
