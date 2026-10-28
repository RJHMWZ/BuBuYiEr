using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClawbearGames
{
    /// <summary>
    /// ScriptableObject holding all data for one puzzle level.
    /// Data init order: Image → Grid → Pots → States.
    /// </summary>
    [CreateAssetMenu(fileName = "Level_New", menuName = "Game/Level Data", order = 1)]
    public class LevelData : ScriptableObject
    {
        [Header("Level Info")]
        public int levelNumber = 1;

        [Header("Image")]
        public ImageData imageData = new ImageData();

        [Header("Grid")]
        [SerializeField] private GridData gridData;

        [Header("Pots")]
        public List<PotData> pots = new List<PotData>();
        public List<PotColorConfig> potColorConfigs = new List<PotColorConfig>();
        public PotDistributionMethod potDistributionMethod = PotDistributionMethod.RandomShuffle;
        public StateProbabilityConfig stateProbability = new StateProbabilityConfig();

        [Header("Power-Ups")]
        public List<PowerUpData> powerUps = new List<PowerUpData>();

        [Header("Coin Reward")]
        [Tooltip("Minimum coins earned per pot cleared")]
        public int minCoinReward = 5;
        [Tooltip("Maximum coins earned per pot cleared")]
        public int maxCoinReward = 15;

        [Header("Star Rating (Time-based)")]
        [Tooltip("Total time allowed to complete the level")]
        public float totalLevelTime = 180f;

        [Range(0, 100)]
        [Tooltip("Percentage of time remaining required for 1 star")]
        public float oneStarPercent = 30f;

        [Range(0, 100)]
        [Tooltip("Percentage of time remaining required for 2 stars")]
        public float twoStarPercent = 60f;

        [Range(0, 100)]
        [Tooltip("Percentage of time remaining required for 3 stars")]
        public float threeStarPercent = 90f;

        [Header("Background Colors")]
        public Color backgroundTopColor = Color.white;
        public Color backgroundBottomColor = Color.gray;


        [Header("Conveyor")]
        [Tooltip("Number of slots available on the conveyor belt for this level")]
        public int conveyorSlot = 1;


        // ─── Grid Access ───────────────────────────────────────────

        /// <summary>Read-only access to the underlying GridData.</summary>
        public GridData GridData => gridData;

        /// <summary>Returns the grid width, or 0 if not initialized.</summary>
        public int GetGridWidth() => gridData?.width ?? 0;

        /// <summary>Returns the grid height, or 0 if not initialized.</summary>
        public int GetGridHeight() => gridData?.height ?? 0;

        /// <summary>Returns true if the grid has been created.</summary>
        public bool IsGridInitialized() => gridData != null;

        /// <summary>Creates or replaces the grid with the given dimensions.</summary>
        public void InitializeGrid(int width, int height)
        {
            gridData = new GridData(width, height);
        }


        // ─── Pot Queries ───────────────────────────────────────────

        /// <summary>Returns all pots sitting on the given tile.</summary>
        public List<PotData> GetPotsAtTile(Vector2Int tile)
            => pots.FindAll(p => p.tilePosition == tile);

        /// <summary>Returns every enabled tile position in the grid.</summary>
        public List<Vector2Int> GetEnabledTiles()
        {
            var result = new List<Vector2Int>();
            if (gridData == null) return result;
            for (int y = 0; y < gridData.height; y++)
                for (int x = 0; x < gridData.width; x++)
                    if (gridData.IsEnabled(x, y))
                        result.Add(new Vector2Int(x, y));
            return result;
        }

        /// <summary>
        /// Rolls a weighted random PotState for every pot and assigns it.
        /// Weights come from stateProbability — they are relative, not required to sum to 100.
        /// Pots that land on Decay receive the default decayTime of 5 s (fine-tune per pot in State Mode).
        /// </summary>
        public void ApplyStateProbabilities()
        {
            int total = stateProbability.TotalWeight;
            if (total <= 0) return;   // all weights are 0 — nothing to do

            foreach (PotData pot in pots)
            {
                int roll = UnityEngine.Random.Range(0, total);

                PotState result;
                if (roll < stateProbability.noneWeight)
                    result = PotState.None;
                else if (roll < stateProbability.noneWeight + stateProbability.frozenWeight)
                    result = PotState.Frozen;
                else if (roll < stateProbability.noneWeight + stateProbability.frozenWeight + stateProbability.lockedWeight)
                    result = PotState.Locked;
                else
                    result = PotState.Decay;

                pot.state = result;
                if (result == PotState.Decay)
                    pot.decayTime = UnityEngine.Random.Range(stateProbability.minDecayTime, stateProbability.maxDecayTime);
            }
        }


        public bool TileHasColorPot(Vector2Int tile, Color32 color)
            => pots.Exists(p => p.tilePosition == tile && Color32Equals(p.color, color));

        /// <summary>
        /// Syncs potColorConfigs to the current image palette.
        /// New colors get default values (min 1, max 3). Existing configs are preserved.
        /// Colors no longer in the palette are removed.
        /// </summary>
        public void SyncPotColorConfigs()
        {
            if (imageData == null || !imageData.IsInitialized) return;
            var newConfigs = new List<PotColorConfig>();
            foreach (Color32 c in imageData.colorPalette)
            {
                var existing = potColorConfigs.Find(cfg => Color32Equals(cfg.color, c));
                newConfigs.Add(existing ?? new PotColorConfig(c));
            }
            potColorConfigs = newConfigs;
        }

        /// <summary>Compares two Color32 values channel by channel (r, g, b, a).</summary>
        public static bool Color32Equals(Color32 a, Color32 b)
            => a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;


        // ─── Coin Reward ───────────────────────────────────────────

        /// <summary>Returns a random coin reward between minCoinReward and maxCoinReward (inclusive).</summary>
        public int GetCoinReward() => UnityEngine.Random.Range(minCoinReward, maxCoinReward + 1);


        // ─── Star Rating ───────────────────────────────────────────

        /// <summary>Returns the time-remaining threshold in seconds for the given star count (1–3).</summary>
        public float GetStarTimeThreshold(int starCount)
        {
            starCount = Mathf.Clamp(starCount, 1, 3);
            switch (starCount)
            {
                case 1: return totalLevelTime * (oneStarPercent / 100f);
                case 2: return totalLevelTime * (twoStarPercent / 100f);
                case 3: return totalLevelTime * (threeStarPercent / 100f);
                default: return 0f;
            }
        }


        // ─── Power-Up Setup ────────────────────────────────────────

        /// <summary>Replaces powerUps with the default set of five power-ups.</summary>
        public void InitializeDefaultPowerUps()
        {
            powerUps = new List<PowerUpData>
            {
                new PowerUpData(PowerUpType.Slot,    cost: 15,  unlockLevel: 1),
                new PowerUpData(PowerUpType.Magnet, cost: 20, unlockLevel: 15),
                new PowerUpData(PowerUpType.Hammer, cost: 50,  unlockLevel: 30),
                new PowerUpData(PowerUpType.Key,    cost: 75,  unlockLevel: 45),
                new PowerUpData(PowerUpType.Cooler, cost: 100,  unlockLevel: 60),
            };
        }

        /// <summary>Returns the PowerUpData for the given type, or null if not found.</summary>
        public PowerUpData GetPowerUpData(PowerUpType type)
        {
            foreach (PowerUpData data in powerUps)
                if (data.powerUpType == type) return data;
            return null;
        }


        // ─── Validation ────────────────────────────────────────────

        /// <summary>Returns true if the level is in a playable state, logging any problems found.</summary>
        public bool IsLevelValid()
        {
            if (!IsGridInitialized())
            {
                Debug.LogWarning($"[Level {levelNumber}] Grid is not initialized.");
                return false;
            }
            if (imageData == null || !imageData.IsInitialized)
            {
                Debug.LogWarning($"[Level {levelNumber}] Image data is not initialized.");
                return false;
            }
            if (pots.Count == 0)
            {
                Debug.LogWarning($"[Level {levelNumber}] No pots placed.");
                return false;
            }
            return true;
        }

        /// <summary>Clamps grid dimensions and coin reward values whenever the asset changes in the Inspector.</summary>
        private void OnValidate()
        {
            if (gridData == null) return;
            gridData.width = Mathf.Clamp(gridData.width, GridData.MIN_SIZE, GridData.MAX_SIZE);
            gridData.height = Mathf.Clamp(gridData.height, GridData.MIN_SIZE, GridData.MAX_SIZE);
            minCoinReward = Mathf.Max(0, minCoinReward);
            maxCoinReward = Mathf.Max(minCoinReward, maxCoinReward);

            oneStarPercent = Mathf.Clamp(oneStarPercent, 0f, twoStarPercent);
            twoStarPercent = Mathf.Clamp(twoStarPercent, oneStarPercent, threeStarPercent);
            threeStarPercent = Mathf.Clamp(threeStarPercent, twoStarPercent, 100f);

        }
    }


    // ──────────────────────────────────────────────────────────────
    //  ImageData
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// All data derived from the source image. Stores normalized pixel colors,
    /// the extracted palette, and the normalizeStep used during initialization.
    /// </summary>
    [Serializable]
    public class ImageData
    {
        [Tooltip("Source texture from Resources/Images. Must have Read/Write enabled.")]
        public Texture2D sourceImage;

        [Range(2f, 50f)]
        [Tooltip("Quantization step — lower value keeps less colors. Set before running Init.")]
        public float normalizeStep = 5f;

        [Range(0f, 80f)]
        [Tooltip("After normalization, palette colors within this RGB distance (0–255 scale) are merged. 0 = off.")]
        public float mergeThreshold = 20f;


        /// <summary>Image width written on Init (expected 32).</summary>
        public int imageWidth;

        /// <summary>Image height written on Init (expected 32).</summary>
        public int imageHeight;

        /// <summary>Flat normalized color array, indexed y * imageWidth + x.</summary>
        public Color32[] pixelColors;

        /// <summary>Unique normalized colors found in the image.</summary>
        public List<Color32> colorPalette = new List<Color32>();

        /// <summary>Pixel count per color, parallel to colorPalette by index.</summary>
        public List<int> colorCounts = new List<int>();

        /// <summary>Returns true once the palette has been built from the source image.</summary>
        public bool IsInitialized => colorPalette != null && colorPalette.Count > 0;

        /// <summary>
        /// Quantizes a Color to a Color32 using normalizeStep.
        /// Mirrors ImageLoader.Normalize() exactly.
        /// </summary>
        public Color32 Normalize(Color c)
        {
            float r = Mathf.Round(c.r * normalizeStep) / normalizeStep;
            float g = Mathf.Round(c.g * normalizeStep) / normalizeStep;
            float b = Mathf.Round(c.b * normalizeStep) / normalizeStep;
            return new Color32(
                (byte)Mathf.RoundToInt(Mathf.Clamp01(r) * 255),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(g) * 255),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(b) * 255),
                255);
        }

        /// <summary>Returns the normalized pixel at position (x, y).</summary>
        public Color32 GetPixel(int x, int y) => pixelColors[y * imageWidth + x];

        /// <summary>Returns the palette index of color c, or -1 if not found.</summary>
        public int GetColorIndex(Color32 c)
        {
            for (int i = 0; i < colorPalette.Count; i++)
            {
                Color32 p = colorPalette[i];
                if (LevelData.Color32Equals(p, c)) return i;
            }
            return -1;
        }

        /// <summary>Returns the pixel count for color c, or 0 if not in the palette.</summary>
        public int GetColorCount(Color32 c)
        {
            int idx = GetColorIndex(c);
            return idx >= 0 ? colorCounts[idx] : 0;
        }



        /// <summary>
        /// Post-process pass: collapses palette entries that are within mergeThreshold
        /// RGB distance of each other. The color with more pixels absorbs the other.
        /// pixelColors is remapped in-place. Repeats until no pair is close enough.
        /// </summary>
        public void MergeSimilarColors()
        {
            if (mergeThreshold <= 0f || colorPalette.Count <= 1) return;

            bool anyMerged = true;
            while (anyMerged)
            {
                anyMerged = false;

                for (int i = 0; i < colorPalette.Count && !anyMerged; i++)
                {
                    for (int j = i + 1; j < colorPalette.Count && !anyMerged; j++)
                    {
                        if (RgbDistance(colorPalette[i], colorPalette[j]) > mergeThreshold) continue;

                        // Keep whichever has more pixels; absorb the other
                        int keepIdx = colorCounts[i] >= colorCounts[j] ? i : j;
                        int dropIdx = keepIdx == i ? j : i;

                        Color32 keepColor = colorPalette[keepIdx];
                        Color32 dropColor = colorPalette[dropIdx];

                        // Remap pixels
                        for (int p = 0; p < pixelColors.Length; p++)
                        {
                            Color32 px = pixelColors[p];
                            if (px.r == dropColor.r && px.g == dropColor.g && px.b == dropColor.b)
                                pixelColors[p] = keepColor;
                        }

                        colorCounts[keepIdx] += colorCounts[dropIdx];
                        colorPalette.RemoveAt(dropIdx);
                        colorCounts.RemoveAt(dropIdx);

                        anyMerged = true; // palette changed — restart the search
                    }
                }
            }
        }


        /// <summary>
        /// Calculate rgb distance
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static float RgbDistance(Color32 a, Color32 b)
        {
            float dr = a.r - b.r;
            float dg = a.g - b.g;
            float db = a.b - b.b;
            return Mathf.Sqrt(dr * dr + dg * dg + db * db);
        }
    }


    // ──────────────────────────────────────────────────────────────
    //  PotColorConfig
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Designer-facing per-color pot generation settings.
    /// Parallel to imageData.colorPalette by index.
    /// </summary>
    [Serializable]
    public class PotColorConfig
    {
        /// <summary>Matches the corresponding entry in imageData.colorPalette.</summary>
        public Color32 color;

        [Tooltip("Minimum pots to generate for this color")]
        public int minPots = 1;

        [Tooltip("Maximum pots to generate for this color")]
        public int maxPots = 3;

        /// <summary>Default constructor for serialization.</summary>
        public PotColorConfig() { }

        /// <summary>Initializes config for the given palette color with default min/max.</summary>
        public PotColorConfig(Color32 c) { color = c; }
    }


    // ──────────────────────────────────────────────────────────────
    //  PotData
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// One pot — a single independent gameplay unit placed on a grid tile.
    /// Constraint: no two PotData with the same color may share the same tilePosition.
    /// </summary>
    [Serializable]
    public class PotData
    {
        /// <summary>Unique ID within the level, auto-assigned (max existing ID + 1).</summary>
        public int potID;

        /// <summary>Color of this pot — must match an entry in imageData.colorPalette.</summary>
        public Color32 color;

        /// <summary>Grid tile this pot occupies. Multiple pots can share a tile.</summary>
        public Vector2Int tilePosition;

        /// <summary>Current gameplay state of this pot.</summary>
        public PotState state = PotState.None;

        /// <summary>Seconds until self-destruct. Only used when state == Decay. Minimum 1s.</summary>
        public float decayTime = 5f;


        /// <summary>
        /// How many pixels/cubes of this color this pot must collect to be completed.
        /// Assigned during pot initialization by evenly distributing the color's total
        /// pixel count across all pots of the same color.
        /// </summary>
        public int requiredAmount;


        /// <summary>Default constructor for serialization.</summary>
        public PotData() { }

        /// <summary>Initializes a pot with the given ID, color, and tile position.</summary>
        public PotData(int id, Color32 color, Vector2Int tile)
        {
            potID = id;
            this.color = color;
            tilePosition = tile;
        }

        /// <summary>Initializes a pot with the given ID, color, tile position and required amount.</summary>
        public PotData(int id, Color32 color, Vector2Int tile, int requiredAmount = 0)
        {
            potID = id;
            this.color = color;
            tilePosition = tile;
            this.requiredAmount = requiredAmount;
        }


        /// <summary>Sets decayTime, enforcing a minimum of 1 second.</summary>
        public void SetDecayTime(float t) => decayTime = Mathf.Max(1f, t);
    }


    // ──────────────────────────────────────────────────────────────
    //  PotState
    // ──────────────────────────────────────────────────────────────

    public enum PotState
    {
        None = 0,  // Normal — tap to send to conveyor
        Frozen = 1,  // Can tap, but cannot receive or collect cubes until thawed
        Locked = 2,  // Cannot be tapped at all
        Decay = 3,  // Self-destructs after decayTime seconds
    }


    // ──────────────────────────────────────────────────────────────
    //  StateProbabilityConfig
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Designer-facing weights for bulk state assignment.
    /// Values are relative weights, not strict percentages — they do not need to sum to 100.
    /// e.g. (70, 20, 10, 0) means None is 70/100 = 70%, Frozen 20%, Locked 10%, Decay 0%.
    /// </summary>
    [Serializable]
    public class StateProbabilityConfig
    {
        [Tooltip("Weight for PotState.None (normal pot)")]
        [Range(0, 100)] public int noneWeight = 100;

        [Tooltip("Weight for PotState.Frozen")]
        [Range(0, 100)] public int frozenWeight = 0;

        [Tooltip("Weight for PotState.Locked")]
        [Range(0, 100)] public int lockedWeight = 0;

        [Tooltip("Weight for PotState.Decay")]
        [Range(0, 100)] public int decayWeight = 0;

        [Tooltip("Minimum decay time in seconds when Decay state is assigned")]
        [Range(1f, 60f)] public float minDecayTime = 15f;

        [Tooltip("Maximum decay time in seconds when Decay state is assigned")]
        [Range(1f, 60f)] public float maxDecayTime = 45f;


        /// <summary>Sum of all weights. Used to normalize effective percentages.</summary>
        public int TotalWeight => noneWeight + frozenWeight + lockedWeight + decayWeight;
    }


    // ──────────────────────────────────────────────────────────────
    //  PotDistributionMethod
    // ──────────────────────────────────────────────────────────────

    public enum PotDistributionMethod
    {
        RandomShuffle = 0,  // Randomize pot order, assign to any valid tile
        EvenlySpreadByColor = 1,  // Spread each color's pots spatially across the grid
    }


    // ──────────────────────────────────────────────────────────────
    //  GridData
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Flat bool array tracking which tiles are enabled or disabled in a 2D grid.
    /// </summary>
    [Serializable]
    public class GridData
    {
        public int width = 5;
        public int height = 5;

        [SerializeField] private bool[] tiles;

        public const int MIN_SIZE = 1;
        public const int MAX_SIZE = 50;

        /// <summary>Allocates a new grid of the given size, all tiles disabled by default.</summary>
        public GridData(int width, int height)
        {
            this.width = Mathf.Clamp(width, MIN_SIZE, MAX_SIZE);
            this.height = Mathf.Clamp(height, MIN_SIZE, MAX_SIZE);
            tiles = new bool[this.width * this.height];
        }

        /// <summary>Returns true if the tile at (x, y) is within bounds and enabled.</summary>
        public bool IsEnabled(int x, int y) => IsValidPosition(x, y) && tiles[GetIndex(x, y)];

        /// <summary>Returns true if the tile at pos is within bounds and enabled.</summary>
        public bool IsEnabled(Vector2Int pos) => IsEnabled(pos.x, pos.y);

        /// <summary>Sets the enabled state of the tile at (x, y).</summary>
        public void SetEnabled(int x, int y, bool v) { if (IsValidPosition(x, y)) tiles[GetIndex(x, y)] = v; }

        /// <summary>Sets the enabled state of the tile at pos.</summary>
        public void SetEnabled(Vector2Int pos, bool v) => SetEnabled(pos.x, pos.y, v);

        /// <summary>Enables every tile in the grid.</summary>
        public void FillAll()
        {
            for (int i = 0; i < tiles.Length; i++) tiles[i] = true;
        }

        /// <summary>Disables every tile in the grid.</summary>
        public void Clear()
        {
            for (int i = 0; i < tiles.Length; i++) tiles[i] = false;
        }

        /// <summary>Returns true if (x, y) is within the grid bounds.</summary>
        public bool IsValidPosition(int x, int y) => x >= 0 && x < width && y >= 0 && y < height;

        /// <summary>Returns true if pos is within the grid bounds.</summary>
        public bool IsValidPosition(Vector2Int pos) => IsValidPosition(pos.x, pos.y);

        /// <summary>Converts (x, y) to a flat array index.</summary>
        private int GetIndex(int x, int y) => y * width + x;

        /// <summary>Returns a deep copy preserving all tile states. Used during grid resize.</summary>
        public GridData Clone()
        {
            var copy = new GridData(width, height);
            if (tiles != null) Array.Copy(tiles, copy.tiles, tiles.Length);
            return copy;
        }
    }


    // ──────────────────────────────────────────────────────────────
    //  PowerUpType
    // ──────────────────────────────────────────────────────────────

    public enum PowerUpType
    {
        Slot = 0,
        Magnet = 1,
        Hammer = 2,
        Key = 3,
        Cooler = 4,
    }


    // ──────────────────────────────────────────────────────────────
    //  PowerUpData
    // ──────────────────────────────────────────────────────────────

    /// <summary>Per-level configuration for a single power-up: coin cost and unlock level.</summary>
    [Serializable]
    public class PowerUpData
    {
        public PowerUpType powerUpType;

        [Tooltip("Cost in coins to use this power-up")]
        public int cost;

        [Tooltip("Player level at which this power-up becomes available")]
        public int unlockLevel;

        /// <summary>Default constructor for serialization.</summary>
        public PowerUpData() { }

        /// <summary>Initializes a power-up with type, cost, and unlock level.</summary>
        public PowerUpData(PowerUpType type, int cost, int unlockLevel)
        {
            powerUpType = type;
            this.cost = cost;
            this.unlockLevel = unlockLevel;
        }
    }
}