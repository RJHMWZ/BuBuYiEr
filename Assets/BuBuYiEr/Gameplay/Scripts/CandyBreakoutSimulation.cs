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
        public static readonly string[] Descriptions = { "轻快弹跳", "邻近溅射", "高速弹跳", "持续融化", "重击穿透", "分裂火花" };
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
            public readonly Vector2[] Trail = new Vector2[8];
            public int TrailCount;
            public float Impact;
        }
        public struct Spark
        {
            public Vector2 Position, Velocity;
            public float Life;
            public Color32 Color;
        }
        public readonly List<Brick> Bricks = new List<Brick>(91);
        public readonly List<Ball> Balls = new List<Ball>(BallLimit);
        public readonly List<Spark> Sparks = new List<Spark>(240);
        public readonly int[] Counts = new int[6], Upgrades = new int[6];
        public int Honey { get; private set; }
        public int Destroyed { get; private set; }
        public int ClickLevel { get; private set; }
        public int Total => Bricks.Count;
        public bool Complete => Destroyed == Total && Total > 0;
        public float Elapsed { get; private set; }
        public event Action<Vector2, bool> Hit;
        private float clickReady;
        private double accumulatedTime;
        private const float FixedDelta = 1f / 120;
        private const float ContactSkin = .02f;
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
            if (Complete || delta <= 0 || float.IsNaN(delta) || float.IsInfinity(delta)) return;
            accumulatedTime += Mathf.Min(delta, .25f);
            while (accumulatedTime + .00000001 >= FixedDelta && !Complete)
            {
                accumulatedTime -= FixedDelta;
                Tick(FixedDelta);
            }
        }
        private void Tick(float delta)
        {
            Elapsed += delta;
            foreach (Brick brick in Bricks)
            {
                brick.Flash = Mathf.Max(0, brick.Flash - delta);
                if (!brick.Alive || brick.PoisonTime <= 0) continue;
                brick.PoisonTime -= delta;
                brick.PoisonTick -= delta;
                if (brick.PoisonTick <= 0) { brick.PoisonTick = .5f; Hurt(brick, brick.PoisonDamage); }
            }
            foreach (Ball ball in Balls)
            {
                ball.Previous = ball.Position;
                for (int i = ball.Trail.Length - 1; i > 0; i--) ball.Trail[i] = ball.Trail[i - 1];
                ball.Trail[0] = ball.Position;
                ball.TrailCount = Mathf.Min(ball.Trail.Length, ball.TrailCount + 1);
                ball.Impact = Mathf.Max(0, ball.Impact - delta * 7);
                Move(ball, delta);
            }
            int aliveSparks = 0;
            for (int i = 0; i < Sparks.Count; i++)
            {
                Spark spark = Sparks[i];
                spark.Life -= delta;
                spark.Position += spark.Velocity * delta;
                if (spark.Life > 0) Sparks[aliveSparks++] = spark;
            }
            if (aliveSparks < Sparks.Count) Sparks.RemoveRange(aliveSparks, Sparks.Count - aliveSparks);
        }
        // Sweep the circle against the four faces and rounded corners of a rectangle.
        // The earliest time of impact wins, independent of brick list order or frame rate.
        private static bool Sweep(Vector2 p, Vector2 travel, Rect r, out float time, out Vector2 normal)
        {
            time = 2;
            normal = Vector2.zero;
            if (Mathf.Max(p.x, p.x + travel.x) < r.xMin - Radius || Mathf.Min(p.x, p.x + travel.x) > r.xMax + Radius ||
                Mathf.Max(p.y, p.y + travel.y) < r.yMin - Radius || Mathf.Min(p.y, p.y + travel.y) > r.yMax + Radius) return false;
            Vector2 closest = new Vector2(Mathf.Clamp(p.x, r.xMin, r.xMax), Mathf.Clamp(p.y, r.yMin, r.yMax));
            Vector2 offset = p - closest;
            if (offset.sqrMagnitude < Radius * Radius - .001f)
            {
                if (offset.sqrMagnitude > .0001f) normal = offset.normalized;
                else
                {
                    float nearest = p.x - r.xMin;
                    normal = Vector2.left;
                    if (r.xMax - p.x < nearest) { nearest = r.xMax - p.x; normal = Vector2.right; }
                    if (p.y - r.yMin < nearest) { nearest = p.y - r.yMin; normal = Vector2.down; }
                    if (r.yMax - p.y < nearest) normal = Vector2.up;
                }
                time = 0;
                return true;
            }
            if (travel.x > .000001f) Face((r.xMin - Radius - p.x) / travel.x, p.y, travel.y, r.yMin, r.yMax, Vector2.left, ref time, ref normal);
            if (travel.x < -.000001f) Face((r.xMax + Radius - p.x) / travel.x, p.y, travel.y, r.yMin, r.yMax, Vector2.right, ref time, ref normal);
            if (travel.y > .000001f) Face((r.yMin - Radius - p.y) / travel.y, p.x, travel.x, r.xMin, r.xMax, Vector2.down, ref time, ref normal);
            if (travel.y < -.000001f) Face((r.yMax + Radius - p.y) / travel.y, p.x, travel.x, r.xMin, r.xMax, Vector2.up, ref time, ref normal);
            float a = travel.sqrMagnitude;
            if (a > .00000001f)
                for (int corner = 0; corner < 4; corner++)
                {
                    Vector2 c = new Vector2(corner % 2 == 0 ? r.xMin : r.xMax, corner < 2 ? r.yMin : r.yMax);
                    Vector2 d = p - c;
                    float b = Vector2.Dot(d, travel);
                    float discriminant = b * b - a * (d.sqrMagnitude - Radius * Radius);
                    if (discriminant < 0) continue;
                    float t = (-b - Mathf.Sqrt(discriminant)) / a;
                    if (t < 0 || t > 1 || t >= time) continue;
                    Vector2 n = (p + travel * t - c).normalized;
                    if ((corner % 2 == 0 ? n.x <= 0 : n.x >= 0) && (corner < 2 ? n.y <= 0 : n.y >= 0) && Vector2.Dot(travel, n) < -.000001f)
                    { time = t; normal = n; }
                }
            return time <= 1;
        }
        private static void Face(float t, float position, float travel, float min, float max, Vector2 n, ref float earliest, ref Vector2 normal)
        {
            if (t < 0 || t > 1 || t >= earliest) return;
            float contact = position + travel * t;
            if (contact < min || contact > max) return;
            earliest = t;
            normal = n;
        }
        private void Move(Ball ball, float dt)
        {
            ball.Position = new Vector2(Mathf.Clamp(ball.Position.x, Radius, Width - Radius), Mathf.Clamp(ball.Position.y, Radius, Height - Radius));
            float remaining = dt;
            for (int contact = 0; contact < 12 && remaining > .000001f; contact++)
            {
                Vector2 travel = ball.Velocity * remaining;
                float first = 1;
                Vector2 normal = Vector2.zero;
                int hit = -1;
                if (travel.x < 0) Face((Radius - ball.Position.x) / travel.x, 0, 0, -1, 1, Vector2.right, ref first, ref normal);
                if (travel.x > 0) Face((Width - Radius - ball.Position.x) / travel.x, 0, 0, -1, 1, Vector2.left, ref first, ref normal);
                if (travel.y < 0) Face((Radius - ball.Position.y) / travel.y, 0, 0, -1, 1, Vector2.up, ref first, ref normal);
                if (travel.y > 0) Face((Height - Radius - ball.Position.y) / travel.y, 0, 0, -1, 1, Vector2.down, ref first, ref normal);
                for (int i = 0; i < Bricks.Count; i++)
                {
                    if (!Bricks[i].Alive || !Sweep(ball.Position, travel, Bricks[i].Bounds, out float t, out Vector2 n) || t > first) continue;
                    first = t; normal = n; hit = i;
                }
                ball.Position += travel * first;
                remaining *= 1 - first;
                if (normal == Vector2.zero) break;
                bool approaching = Vector2.Dot(ball.Velocity, normal) < 0;
                bool pierce = false;
                if (hit >= 0)
                {
                    Brick brick = Bricks[hit];
                    Rect r = brick.Bounds;
                    // Recover a pre-existing overlap fully, including a centre inside a brick.
                    Vector2 closest = new Vector2(Mathf.Clamp(ball.Position.x, r.xMin, r.xMax), Mathf.Clamp(ball.Position.y, r.yMin, r.yMax));
                    if (r.Contains(ball.Position))
                    {
                        if (normal.x < 0) ball.Position.x = r.xMin - Radius;
                        else if (normal.x > 0) ball.Position.x = r.xMax + Radius;
                        else if (normal.y < 0) ball.Position.y = r.yMin - Radius;
                        else ball.Position.y = r.yMax + Radius;
                    }
                    else ball.Position += normal * Mathf.Max(0, Radius - Vector2.Distance(ball.Position, closest));
                    if (approaching)
                    {
                        Strike(ball, brick);
                        pierce = ball.Kind == 4 && !brick.Alive;
                        ball.LastBrick = hit;
                    }
                }
                if (approaching && !pierce)
                {
                    ball.Velocity = Vector2.Reflect(ball.Velocity, normal);
                    ball.Impact = 1;
                }
                ball.Position += (pierce ? ball.Velocity.normalized : normal) * ContactSkin;
            }
        }
        private void Strike(Ball ball, Brick brick)
        {
            Hurt(brick, Damage(ball.Kind));
            if (ball.Kind == 1 || ball.Kind == 5)
            {
                float range = ball.Kind == 1 ? 125 : 190;
                int hits = 0;
                foreach (Brick other in Bricks)
                {
                    if (other == brick || !other.Alive || Vector2.Distance(other.Bounds.center, brick.Bounds.center) > range) continue;
                    Hurt(other, Mathf.Max(1, Damage(ball.Kind) / 2));
                    if (ball.Kind == 5 && ++hits >= 3) break;
                }
            }
            if (ball.Kind == 3 && brick.Alive)
            { brick.PoisonTime = 3; brick.PoisonDamage = Damage(3); brick.PoisonTick = .5f; }
            if (brick.Alive)
                for (int i = 0; i < 3 && Sparks.Count < 240; i++)
                    Sparks.Add(new Spark { Position = ball.Position, Life = .18f, Color = Colors[brick.Color],
                        Velocity = -ball.Velocity * .16f + new Vector2((float)random.NextDouble() * 100 - 50, (float)random.NextDouble() * 100 - 50) });
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
