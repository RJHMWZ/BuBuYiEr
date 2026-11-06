using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClawbearGames
{
    // 900 x 760 board, with Y increasing downwards. No scene, save or UI dependencies.
    public sealed class CandyBreakoutSimulation
    {
        public const float Width = 900, Height = 760, Radius = 11;
        public const int BallLimit = 24, UpgradeLimit = 10;
        public static readonly string[] Names = { "奶糖球", "草莓球", "星星球", "薄荷球", "可可球", "彩虹球" };
        public static readonly string[] Descriptions = { "轻快弹跳", "邻近溅射", "追踪糖砖", "持续融化", "重击穿透", "分裂火花" };
        public static readonly Color32[] Colors = {
            new Color32(255,218,108,255), new Color32(255,153,174,255),
            new Color32(161,191,255,255), new Color32(152,218,177,255),
            new Color32(195,143,112,255), new Color32(203,166,234,255) };
        private static readonly int[] Costs = { 25, 70, 100, 120, 160, 200 };
        private static readonly float[] Speeds = { 380, 330, 430, 350, 290, 400 };
        private static readonly int[] Powers = { 1, 2, 2, 1, 5, 2 };
        public sealed class Brick
        {
            public Rect Bounds;
            public int Health, MaxHealth, Color, Reward;
            public float PoisonTime, PoisonTick, Flash;
            public int PoisonDamage;
            public bool Boss;
            public bool Alive => Health > 0;
        }
        public sealed class Ball
        {
            public Vector2 Position, Velocity, Previous;
            public int Kind;
            public int LastBrick = -1;
        }
        public struct Spark
        {
            public Vector2 Position, Velocity;
            public float Life;
            public Color32 Color;
        }
        public readonly List<Brick> Bricks = new List<Brick>();
        public readonly List<Ball> Balls = new List<Ball>();
        public readonly List<Spark> Sparks = new List<Spark>();
        public readonly int[] Counts = new int[6], Upgrades = new int[6];
        public int Honey { get; private set; }
        public int Destroyed { get; private set; }
        public int ClickLevel { get; private set; }
        public int Total => Bricks.Count;
        public bool Complete => Destroyed == Total && Total > 0;
        public float Elapsed { get; private set; }
        public event Action<Vector2, bool> Hit;
        private float clickReady;
        private readonly System.Random random;

        public CandyBreakoutSimulation(LevelData level)
        {
            random = new System.Random(level.levelNumber * 7919);
            Honey = Mathf.Max(0, level.startingHoney);
            int cols = Mathf.Clamp(level.columns, 5, 10), rows = Mathf.Clamp(level.rows, 4, 9);
            float cellW = 820f / cols;
            for (int y = 0; y < rows; y++)
            for (int x = 0; x < cols; x++)
            {
                bool gap = level.pattern == 1 && (x + y) % 5 == 0 ||
                    level.pattern == 2 && x > 1 && x < cols - 2 && y > 1 && y < rows - 2 ||
                    level.pattern == 3 && ((y % 2 == 0 && x == cols / 2) || (y % 2 == 1 && (x == 0 || x == cols - 1)));
                if (gap) continue;
                int hp = Mathf.Max(1, level.brickHealth + (rows - y - 1) / 2);
                Bricks.Add(new Brick { Bounds = new Rect(40 + x * cellW + 3, 55 + y * 49, cellW - 6, 41),
                    Health = hp, MaxHealth = hp, Color = (x / 2 + y + level.levelNumber) % 6, Reward = 8 + hp });
            }
            if (level.boss)
            {
                int hp = Mathf.Max(20, level.brickHealth * 10);
                Bricks.Add(new Brick { Bounds = new Rect(320, 530, 260, 90), Health = hp, MaxHealth = hp,
                    Color = 4, Reward = 100, Boss = true });
            }
            Spawn(0);
        }
        public int BuyCost(int kind) => Mathf.CeilToInt(Costs[kind] * Mathf.Pow(1.42f, Counts[kind]));
        public int UpgradeCost(int kind) => Mathf.CeilToInt((35 + kind * 18) * Mathf.Pow(1.65f, Upgrades[kind]));
        public int ClickCost => Mathf.CeilToInt(30 * Mathf.Pow(1.8f, ClickLevel));
        public int Damage(int kind) => Powers[kind] * (1 + Upgrades[kind]);
        public bool Buy(int kind)
        {
            if (kind < 0 || kind >= 6 || Complete || Balls.Count >= BallLimit || Honey < BuyCost(kind)) return false;
            Honey -= BuyCost(kind);
            Spawn(kind);
            return true;
        }
        public bool Upgrade(int kind)
        {
            if (kind < 0 || kind >= 6 || Complete || Counts[kind] == 0 || Upgrades[kind] >= UpgradeLimit || Honey < UpgradeCost(kind)) return false;
            Honey -= UpgradeCost(kind);
            Upgrades[kind]++;
            foreach (Ball ball in Balls)
                if (ball.Kind == kind) ball.Velocity = ball.Velocity.normalized * Speed(kind);
            return true;
        }
        public bool UpgradeClick()
        {
            if (Complete || ClickLevel >= UpgradeLimit || Honey < ClickCost) return false;
            Honey -= ClickCost;
            ClickLevel++;
            return true;
        }
        private float Speed(int kind) => Speeds[kind] * (1 + Upgrades[kind] * .06f);
        private void Spawn(int kind)
        {
            float angle = (float)(random.NextDouble() * 1.8 + .65);
            Vector2 pos = new Vector2(100 + (float)random.NextDouble() * 700, 680);
            Balls.Add(new Ball { Kind = kind, Position = pos, Previous = pos,
                Velocity = new Vector2(Mathf.Cos(angle), -Mathf.Sin(angle)) * Speed(kind) });
            Counts[kind]++;
        }
        public bool Click(Vector2 point)
        {
            if (Complete || Elapsed < clickReady) return false;
            foreach (Brick brick in Bricks)
                if (brick.Alive && brick.Bounds.Contains(point))
                {
                    clickReady = Elapsed + .09f;
                    Hurt(brick, 1 + ClickLevel * 2);
                    return true;
                }
            return false;
        }
        public void Step(float delta)
        {
            if (Complete || delta <= 0 || float.IsNaN(delta)) return;
            delta = Mathf.Min(delta, .1f);
            Elapsed += delta;
            foreach (Brick brick in Bricks)
            {
                brick.Flash = Mathf.Max(0, brick.Flash - delta);
                if (!brick.Alive || brick.PoisonTime <= 0) continue;
                brick.PoisonTime -= delta;
                brick.PoisonTick -= delta;
                if (brick.PoisonTick <= 0) { brick.PoisonTick = .5f; Hurt(brick, brick.PoisonDamage); }
            }
            int steps = Mathf.CeilToInt(delta / .009f);
            float dt = delta / steps;
            foreach (Ball ball in Balls)
            {
                ball.Previous = ball.Position;
                if (ball.Kind == 2)
                {
                    Brick target = Nearest(ball.Position);
                    if (target != null)
                        ball.Velocity = Vector2.Lerp(ball.Velocity.normalized, (target.Bounds.center - ball.Position).normalized,
                            delta * 1.5f).normalized * Speed(ball.Kind);
                }
                for (int i = 0; i < steps; i++) Move(ball, dt);
            }
            for (int i = Sparks.Count - 1; i >= 0; i--)
            {
                Spark spark = Sparks[i];
                spark.Life -= delta;
                spark.Position += spark.Velocity * delta;
                if (spark.Life <= 0) Sparks.RemoveAt(i); else Sparks[i] = spark;
            }
        }
        private Brick Nearest(Vector2 pos)
        {
            Brick best = null;
            float distance = float.MaxValue;
            foreach (Brick brick in Bricks)
            {
                float d = (brick.Bounds.center - pos).sqrMagnitude;
                if (brick.Alive && d < distance) { best = brick; distance = d; }
            }
            return best;
        }
        private void Move(Ball ball, float dt)
        {
            Vector2 previous = ball.Position;
            ball.Position += ball.Velocity * dt;
            if (ball.Position.x < Radius || ball.Position.x > Width - Radius)
            { ball.Position.x = Mathf.Clamp(ball.Position.x, Radius, Width - Radius); ball.Velocity.x *= -1; }
            if (ball.Position.y < Radius || ball.Position.y > Height - Radius)
            { ball.Position.y = Mathf.Clamp(ball.Position.y, Radius, Height - Radius); ball.Velocity.y *= -1; }
            int touching = -1;
            for (int i = 0; i < Bricks.Count; i++)
            {
                Brick brick = Bricks[i];
                if (!brick.Alive) continue;
                Rect r = brick.Bounds;
                Vector2 closest = new Vector2(Mathf.Clamp(ball.Position.x, r.xMin, r.xMax), Mathf.Clamp(ball.Position.y, r.yMin, r.yMax));
                if ((ball.Position - closest).sqrMagnitude > Radius * Radius) continue;
                touching = i;
                if (ball.LastBrick == i) break;
                Hurt(brick, Damage(ball.Kind));
                if (ball.Kind == 1 || ball.Kind == 5)
                {
                    float range = ball.Kind == 1 ? 125 : 190;
                    int hits = 0;
                    foreach (Brick other in Bricks)
                    {
                        if (other == brick || !other.Alive || Vector2.Distance(other.Bounds.center, r.center) > range) continue;
                        Hurt(other, Mathf.Max(1, Damage(ball.Kind) / 2));
                        if (ball.Kind == 5 && ++hits >= 3) break;
                    }
                }
                if (ball.Kind == 3 && brick.Alive)
                { brick.PoisonTime = 3; brick.PoisonDamage = Damage(3); }
                if (ball.Kind != 4 || brick.Alive)
                {
                    if (previous.y <= r.yMin || previous.y >= r.yMax)
                    {
                        ball.Velocity.y *= -1;
                        ball.Position.y = previous.y < r.center.y ? r.yMin - Radius - .1f : r.yMax + Radius + .1f;
                    }
                    else
                    {
                        ball.Velocity.x *= -1;
                        ball.Position.x = previous.x < r.center.x ? r.xMin - Radius - .1f : r.xMax + Radius + .1f;
                    }
                }
                break;
            }
            ball.LastBrick = touching;
        }
        private void Hurt(Brick brick, int damage)
        {
            if (!brick.Alive) return;
            brick.Health = Mathf.Max(0, brick.Health - damage);
            brick.Flash = .12f;
            if (!brick.Alive)
            {
                Destroyed++;
                Honey += brick.Reward;
                for (int i = 0; i < 6 && Sparks.Count < 240; i++)
                    Sparks.Add(new Spark { Position = brick.Bounds.center, Life = .5f,
                        Velocity = new Vector2((float)random.NextDouble() * 240 - 120, (float)random.NextDouble() * 240 - 120), Color = Colors[brick.Color] });
            }
            Hit?.Invoke(brick.Bounds.center, !brick.Alive);
        }
    }
}
