using System.Collections.Generic;
using UnityEngine;

namespace ClawbearGames
{
    /// <summary>
    /// Reads a LevelData asset and spawns TileControllers + PotControllers into the scene.
    /// Spawn order: Cells first → Pots second.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        public LevelData LevelData => currentLevel;
        public float TileSpacing => tileSpacing;
        public float GridWorldY => transform.position.y;


        private Dictionary<Vector2Int, CellController> spawnedCells = new Dictionary<Vector2Int, CellController>();
        private List<PotController> spawnedPots = new List<PotController>();
        private LevelData currentLevel;
        private float tileSpacing = 1f;
        private readonly List<PotController> collectingPots = new List<PotController>(12);
        private int collectingFrame = -1;

        public IReadOnlyList<PotController> CollectingPots
        {
            get
            {
                if (collectingFrame != Time.frameCount)
                {
                    collectingFrame = Time.frameCount;
                    collectingPots.Clear();
                    foreach (PotController pot in spawnedPots)
                        if (pot != null && pot.IsOnConveyor && !pot.IsFilled && pot.CanCollect) collectingPots.Add(pot);
                }
                return collectingPots;
            }
        }




        /// <summary>
        /// Loads a level: clears any existing objects, then spawns tiles and pots.
        /// </summary>
        public void OnInit(LevelData levelData)
        {
            if (levelData == null || !levelData.IsLevelValid()) return;

            currentLevel = levelData;
            Clear();
            SpawnCells();
            SpawnPots();
        }



        /// <summary>
        /// Spawns a CellController for every enabled cell in the grid.
        /// Disabled cells are skipped entirely — no tile is created for them.
        /// </summary>
        private void SpawnCells()
        {
            GridData grid = currentLevel.GridData;

            for (int y = 0; y < grid.height; y++)
            {
                for (int x = 0; x < grid.width; x++)
                {
                    if (!grid.IsEnabled(x, y)) continue;

                    Vector2Int gridPos = new Vector2Int(x, y);
                    Vector3 worldPos = GridToWorldPosition(x, y);

                    CellController cell = PoolManager.Instance.GetCellController();
                    cell.transform.position = worldPos;
                    cell.transform.SetParent(transform);
                    cell.gameObject.SetActive(true);
                    cell.GridPos = gridPos;
                    cell.OnInit(grid);
                    spawnedCells[gridPos] = cell;
                }
            }
        }


        /// <summary>
        /// Spawns a PotController for every PotData in the level.
        /// Each pot is positioned at its cell's world position.
        /// Multiple pots on the same cell stack — offset logic is handled by PotController.
        /// </summary>
        private void SpawnPots()
        {
            // Group all PotData by their cell position
            var potsByCell = new Dictionary<Vector2Int, List<PotData>>();
            foreach (PotData potData in currentLevel.pots)
            {
                if (!potsByCell.ContainsKey(potData.tilePosition))
                    potsByCell[potData.tilePosition] = new List<PotData>();
                potsByCell[potData.tilePosition].Add(potData);
            }

            foreach (var kvp in potsByCell)
            {
                if (!spawnedCells.TryGetValue(kvp.Key, out CellController cell))
                {
                    Debug.LogWarning($"[GridManager] No cell at {kvp.Key}. Skipping.");
                    continue;
                }

                List<PotController> cellPots = new List<PotController>();
                foreach (PotData potData in kvp.Value)
                {
                    // Spawn disabled — CellController will enable first one
                    PotController pot = PoolManager.Instance.GetPotController();
                    pot.transform.position = cell.transform.position;
                    pot.transform.rotation = Quaternion.identity;
                    pot.gameObject.SetActive(true);
                    pot.OnInit(potData);
                    pot.OwnerCell = cell;
                    spawnedPots.Add(pot);
                    cellPots.Add(pot);
                }

                // Hand the entire ordered list to the cell
                cell.PotQueue = cellPots;
            }

            //Disable all pots and show first pot
            foreach (var kvp in potsByCell)
            {
                if (!spawnedCells.TryGetValue(kvp.Key, out CellController cell))
                {
                    continue;
                }
                cell.DisablePots();
                cell.ShowNextPot();
            }
        }



        /// <summary>
        /// Destroys all spawned tiles and pots and clears internal collections.
        /// </summary>
        private void Clear()
        {
            foreach (var cell in spawnedCells.Values)
            {
                if (cell != null) cell.gameObject.SetActive(false);  // return to pool
            }
            spawnedCells.Clear();

            foreach (var pot in spawnedPots)
            {
                if (pot != null)
                {
                    pot.transform.SetParent(null);   // unparent from tile/cell
                    pot.gameObject.SetActive(false); // return to pool
                }
            }
            spawnedPots.Clear();
        }



        /// <summary>
        /// Returns the CellController at the given grid position, or null if not spawned.
        /// </summary>
        public CellController GetCellAt(Vector2Int gridPos)
            => spawnedCells.TryGetValue(gridPos, out CellController cell) ? cell : null;

        /// <summary>
        /// Returns all PotControllers sitting on the given grid tile.
        /// </summary>
        public List<PotController> GetPotsAt(Vector2Int gridPos)
            => spawnedPots.FindAll(p => p.GridPosition == gridPos);


        /// <summary>
        /// Returns all currently active PotControllers.
        /// </summary>
        public IReadOnlyList<PotController> AllPots => spawnedPots;


        /// <summary>
        /// Returns all currently active CellController.
        /// </summary>
        public IReadOnlyList<CellController> AllCells()
        {
            List<CellController> cells = new List<CellController>();
            foreach(var kvp in spawnedCells)
            {
                cells.Add(kvp.Value);
            }
            return cells;
        }


        /// <summary>
        /// Remove the given pot
        /// </summary>
        /// <param name="pot"></param>
        public void RemovePot(PotController pot)
        {
            if (spawnedPots.Contains(pot))
                spawnedPots.Remove(pot);
        }


        /// <summary>
        /// Converts a grid coordinate (x, y) to a centred world position on the XZ plane.
        /// grid.x → world X  |  grid.y → world Z  |  Y is always gridOffset.y
        /// </summary>
        public Vector3 GridToWorldPosition(int x, int y)
        {
            GridData grid = currentLevel.GridData;
            float centerOffsetX = (grid.width - 1) / 2.0f;
            float centerOffsetZ = (grid.height - 1) / 2.0f;

            return new Vector3(
                (x - centerOffsetX) * tileSpacing + transform.position.x,
                transform.position.y,
                (y - centerOffsetZ) * tileSpacing + transform.position.z
            );
        }

        /// <summary>
        /// Overload that accepts a Vector2Int grid position.
        /// </summary>
        public Vector3 GridToWorldPosition(Vector2Int pos)
            => GridToWorldPosition(pos.x, pos.y);

        /// <summary>
        /// Converts a world position back to the nearest grid coordinate.
        /// </summary>
        public Vector2Int WorldToGridPosition(Vector3 worldPos)
        {
            GridData grid = currentLevel.GridData;
            float centerOffsetX = (grid.width - 1) / 2.0f;
            float centerOffsetZ = (grid.height - 1) / 2.0f;

            int gridX = Mathf.RoundToInt((worldPos.x - transform.position.x) / tileSpacing + centerOffsetX);
            int gridY = Mathf.RoundToInt((worldPos.z - transform.position.z) / tileSpacing + centerOffsetZ);

            return new Vector2Int(gridX, gridY);
        }



        /// <summary>
        /// Returns the closest pot to worldPos that is not yet on the conveyor,
        /// within maxDistance (measured on the XZ plane). Returns null if none found.
        /// </summary>
        public PotController GetClosestPot(Vector3 worldPos, float maxDistance = 3f)
        {
            CellController closest = null;
            float minDist = maxDistance;

            foreach (var cell in spawnedCells.Values)
            {
                Vector3 cellPos = cell.transform.position;
                float dist = Vector2.Distance(
                    new Vector2(cellPos.x, cellPos.z),
                    new Vector2(worldPos.x, worldPos.z));

                if (dist < minDist)
                {
                    minDist = dist;
                    closest = cell;
                }
            }

            if (closest != null)
            {
                PotController pot = closest.GetCurrentPot();
                if (pot == null || pot.IsOnConveyor) return null;
                return pot;
            }
            return null;
        }



        /// <summary>
        /// Get world position from screen position.
        /// </summary>
        /// <param name="screenPos"></param>
        /// <returns></returns>
        private Vector3 GetWorldPos(Vector2 screenPos)
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0));
            Ray ray = Camera.main.ScreenPointToRay(InputManager.GetMousePosition());
            Plane ground = new Plane(Vector3.up, new Vector3(0f, GridWorldY, 0f));
            if (ground.Raycast(ray, out float enter))
            {
                worldPos = ray.GetPoint(enter);
            }
            worldPos.y = 0;
            return worldPos;
        }

        /// <summary>
        /// Handle Magnet power-up at screen position.
        /// The player taps an empty cell → the most stranded conveyor pot is pulled back to it.
        /// callback(false) if conditions aren't met; callback(true) on success.
        /// </summary>
        public void HandleMagnet(Vector2 screenPos, System.Action<bool> callback)
        {
            // ── Step 1: Resolve a cell from tap position ──────────────────────────
            Vector3 worldPos = GetWorldPos(screenPos);

            CellController targetCell = null;
            float closestDist = 3f; // tap radius threshold

            foreach (var cell in spawnedCells.Values)
            {
                float dist = Vector2.Distance(
                    new Vector2(cell.transform.position.x, cell.transform.position.z),
                    new Vector2(worldPos.x, worldPos.z));

                if (dist < closestDist)
                {
                    closestDist = dist;
                    targetCell = cell;
                }
            }

            // No cell tapped, or cell already has an active pot
            if (targetCell == null || targetCell.GetCurrentPot() != null)
            {
                callback?.Invoke(false);
                return;
            }

            // ── Step 2: Find the most stranded pot on the conveyor ────────────────
            PotController strandedPot = null;
            int lowestMatchCount = int.MaxValue;

            foreach (var pot in spawnedPots)
            {
                if (pot == null || !pot.IsOnConveyor || pot.IsFilled) continue;

                //Found frozen pot
                if (pot.PotState == PotState.Frozen) { strandedPot = pot; break; }

                int matchCount = IngameManager.Instance.CubeManager.GetMatchingCubeCountForPot(pot);
                if (matchCount < lowestMatchCount)
                {
                    lowestMatchCount = matchCount;
                    strandedPot = pot;
                }
            }

            if (strandedPot == null)
            {
                callback?.Invoke(false);
                return;
            }

            // ── Step 3: Spawn MagnetController and animate ────────────────────────
            MagnetController magnet = PoolManager.Instance.GetMagnetController();
            magnet.transform.position = targetCell.transform.position;
            magnet.gameObject.SetActive(true);

            StartCoroutine(magnet.CRMagnetAnim(() =>
            {
                ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.MagnetPull);
                strandedPot.ReturnToCell(targetCell, () =>
                {
                    callback?.Invoke(true);
                });
            }));
        }



        /// <summary>
        /// Handle Hammer power-up at screen position.
        /// First checks for a frozen pot near tap → breaks frozen state.
        /// callback(false) if no condition is met; callback(true) on success.
        /// </summary>
        /// <param name="screenPos">Screen position the player tapped.</param>
        /// <param name="callback">Callback with result: false = not consumed, true = hammer applied.</param>
        private PotController FindVisibleStatePot(Vector2 screenPoint, PotState state)
        {
            Camera camera = IngameManager.Instance.CameraParent.MainCamera;
            PotController nearest = null;
            float best = float.MaxValue;
            foreach (PotController pot in spawnedPots)
            {
                if (pot == null || !pot.gameObject.activeInHierarchy || pot.PotState != state || pot.IsFilled) continue;
                Vector3 center = camera.WorldToScreenPoint(pot.transform.position + Vector3.up * .65f);
                if (center.z <= 0) continue;
                Vector2 origin = camera.WorldToScreenPoint(pot.transform.position);
                Vector2 edge = camera.WorldToScreenPoint(pot.transform.position + Vector3.right * tileSpacing);
                float radius = Mathf.Max(18, Vector2.Distance(origin, edge) * .7f);
                float distance = (screenPoint - (Vector2)center).sqrMagnitude;
                if (distance <= radius * radius && distance < best) { nearest = pot; best = distance; }
            }
            return nearest;
        }

        public void HandleHammer(Vector2 screenPos, System.Action<bool> callback)
        {
            PotController closestFrozenPot = FindVisibleStatePot(screenPos, PotState.Frozen);

            if (closestFrozenPot != null)
            {
                HammerController hammer = PoolManager.Instance.GetHammerController();
                hammer.transform.position = closestFrozenPot.transform.position;
                hammer.gameObject.SetActive(true);
                StartCoroutine(hammer.CRHammerAnim(() =>
                {
                    ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.HammerSmash);
                    closestFrozenPot.DisableFrozenState();
                    callback?.Invoke(true);
                }));
                return;
            }

            // Nothing valid found
            callback?.Invoke(false);
        }




        /// <summary>
        /// Handle Key power-up at screen position.
        /// First checks for a locked pot near tap → breaks locked state.
        /// callback(false) if no condition is met; callback(true) on success.
        /// </summary>
        /// <param name="screenPos">Screen position the player tapped.</param>
        /// <param name="callback">Callback with result: false = not consumed, true = hammer applied.</param>
        public void HandleKey(Vector2 screenPos, System.Action<bool> callback)
        {
            PotController closestFrozenPot = FindVisibleStatePot(screenPos, PotState.Locked);

            if (closestFrozenPot != null)
            {
                KeyController key = PoolManager.Instance.GetKeyController();
                key.transform.position = closestFrozenPot.transform.position;
                key.gameObject.SetActive(true);
                StartCoroutine(key.CRKeyAnim(() =>
                {
                    //EffectManager.Instance.PlayHiddenBreakEffect(closestFrozenPot.transform.position);
                    closestFrozenPot.DisableLockedState();
                    callback?.Invoke(true);
                }));
                return;
            }

            // Nothing valid found
            callback?.Invoke(false);
        }



        /// <summary>
        /// Handle Key power-up at screen position.
        /// First checks for a decay pot near tap → breaks decay state.
        /// callback(false) if no condition is met; callback(true) on success.
        /// </summary>
        /// <param name="screenPos">Screen position the player tapped.</param>
        /// <param name="callback">Callback with result: false = not consumed, true = hammer applied.</param>
        public void HandleCooler(Vector2 screenPos, System.Action<bool> callback)
        {
            PotController closestFrozenPot = FindVisibleStatePot(screenPos, PotState.Decay);

            if (closestFrozenPot != null)
            {
                CoolerController cooler = PoolManager.Instance.GetCoolerController();
                cooler.transform.position = closestFrozenPot.transform.position;
                cooler.gameObject.SetActive(true);
                StartCoroutine(cooler.CRCoolerAnim(() =>
                {
                    //EffectManager.Instance.PlayHiddenBreakEffect(closestFrozenPot.transform.position);
                    closestFrozenPot.DisableDecayState();
                    callback?.Invoke(true);
                }));
                return;
            }

            // Nothing valid found
            callback?.Invoke(false);
        }
    }
}
