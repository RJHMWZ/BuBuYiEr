using UnityEngine;

namespace ClawbearGames
{
    [CreateAssetMenu(fileName = "Level_New", menuName = "BuBuYiEr/糖果打砖块关卡")]
    public class LevelData : ScriptableObject
    {
        public int levelNumber = 1;
        [Range(5, 10)] public int columns = 9;
        [Range(4, 9)] public int rows = 6;
        [Min(1)] public int brickHealth = 5;
        [Range(0, 3)] public int pattern;
        public bool boss;
        [Min(0)] public int startingHoney = 90;
        [Min(1)] public int coinReward = 30;
        [Min(10)] public float threeStarTime = 100;
        [Min(10)] public float twoStarTime = 180;
    }
}
