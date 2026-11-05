using System.Collections.Generic;
using UnityEngine;

namespace ClawbearGames
{
    public class CubeManager : MonoBehaviour
    {
        private const float cubeSize = .21f;
        private const float CollectionsPerSecond = 96;
        private const int MaxCollectionsPerFrame = 8;
        private const int MaxFlyingCubes = 40;
        private List<CubeController>[] columnn;
        private bool[] dirtyColumns;
        private readonly List<CubeController> moving = new List<CubeController>();
        private readonly List<PotController> activePots = new List<PotController>(16);
        private bool simulationRunning, reverseSlide;
        private int width, height, collectionCursor, flyingCubes;
        private float collectionCredit = 1, nextSlide, nextSound;
        private IngameManager game;

        public void OnInit(LevelData data)
        {
            game = IngameManager.Instance;
            ImageData image = data.imageData;
            width = image.imageWidth;
            height = image.imageHeight;
            columnn = new List<CubeController>[width];
            dirtyColumns = new bool[width];
            moving.Clear();
            moving.Capacity = Mathf.Max(moving.Capacity, width * height);
            flyingCubes = collectionCursor = 0;
            collectionCredit = 1;
            simulationRunning = false;
            for (int x = 0; x < width; x++) columnn[x] = new List<CubeController>(height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    CubeController cube = PoolManager.Instance.GetCubeController();
                    cube.transform.position = GridToWorld(x, y);
                    cube.Init(image.GetPixel(x, y));
                    cube.gameObject.SetActive(true);
                    columnn[x].Add(cube);
                }
            }
        }

        public void StartSimulation() => simulationRunning = true;

        private void LateUpdate()
        {
            if (!simulationRunning || game == null || game.IsPaused || !game.IsRoundStarted ||
                game.IngameState != IngameState.Ingame_Playing || Time.deltaTime <= 0) return;
            float delta = Time.deltaTime;
            AdvanceMotions(delta);
            if (game.IngameState != IngameState.Ingame_Playing) return;
            activePots.Clear();
            IReadOnlyList<PotController> collecting = game.GridManager.CollectingPots;
            for (int i = 0; i < collecting.Count; i++) activePots.Add(collecting[i]);
            collectionCredit = activePots.Count == 0 ? 1 : Mathf.Min(MaxCollectionsPerFrame, collectionCredit + delta * CollectionsPerSecond);
            CollectExposedCubes();
            CompactDirtyColumns();
            if (Time.time >= nextSlide)
            {
                nextSlide = Time.time + .04f;
                SlideUnevenColumns();
            }
        }

        private void AdvanceMotions(float delta)
        {
            for (int i = moving.Count - 1; i >= 0; i--)
            {
                CubeController cube = moving[i];
                bool wasFlying = cube != null && cube.IsCollecting;
                if (cube != null && cube.gameObject.activeSelf && !cube.Advance(delta)) continue;
                if (wasFlying) flyingCubes--;
                if (cube != null) cube.MotionRegistered = false;
                int last = moving.Count - 1;
                moving[i] = moving[last];
                moving.RemoveAt(last);
            }
        }

        private void TrackMotion(CubeController cube)
        {
            if (cube.MotionRegistered) return;
            cube.MotionRegistered = true;
            moving.Add(cube);
        }

        private void CollectExposedCubes()
        {
            if (activePots.Count == 0 || collectionCredit < 1 || flyingCubes >= MaxFlyingCubes) return;
            int launched = 0;
            for (int offset = 0; offset < width && collectionCredit >= 1 && flyingCubes < MaxFlyingCubes; offset++)
            {
                int x = (collectionCursor + offset) % width;
                List<CubeController> column = columnn[x];
                int row = 0;
                for (int examined = 0; examined < 3 && row < column.Count && collectionCredit >= 1 && flyingCubes < MaxFlyingCubes; examined++)
                {
                    CubeController cube = column[row];
                    if (cube.IsLocked) { row++; continue; }
                    PotController pot = FindMatchingPot(cube);
                    if (pot == null || !pot.TryReserve()) { row++; continue; }
                    column.RemoveAt(row);
                    dirtyColumns[x] = true;
                    cube.CollectInto(pot);
                    TrackMotion(cube);
                    flyingCubes++;
                    collectionCredit--;
                    launched++;
                }
            }
            collectionCursor = (collectionCursor + 1) % width;
            if (launched > 0 && Time.time >= nextSound)
            {
                nextSound = Time.time + .12f;
                ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.CubeJumped);
            }
        }

        private PotController FindMatchingPot(CubeController cube)
        {
            PotController closest = null;
            float distance = 1.001f;
            foreach (PotController pot in activePots)
            {
                if (pot == null || !pot.IsOnConveyor || !pot.CanCollect || pot.IsFilled || pot.PotState == PotState.Frozen ||
                    pot.RemainingCapacity <= 0 || !LevelData.Color32Equals(cube.Color, pot.TargetColor)) continue;
                float gap = Mathf.Abs(cube.transform.position.x - pot.transform.position.x);
                if (gap < distance) { closest = pot; distance = gap; }
            }
            return closest;
        }

        private void CompactDirtyColumns()
        {
            for (int x = 0; x < width; x++)
            {
                if (!dirtyColumns[x]) continue;
                bool waitingForSlide = false;
                List<CubeController> column = columnn[x];
                for (int row = 0; row < column.Count; row++)
                {
                    CubeController cube = column[row];
                    if (cube.IsSliding) { waitingForSlide = true; continue; }
                    Vector3 target = GridToWorld(x, row);
                    if ((cube.transform.position - target).sqrMagnitude <= .000001f) continue;
                    cube.FallTo(target);
                    TrackMotion(cube);
                }
                dirtyColumns[x] = waitingForSlide;
            }
        }

        private void SlideUnevenColumns()
        {
            int slides = 0;
            reverseSlide = !reverseSlide;
            for (int step = 0; step < width && slides < 4; step++)
            {
                int x = reverseSlide ? width - 1 - step : step;
                int count = columnn[x].Count;
                if (count == 0) continue;
                CubeController cube = columnn[x][count - 1];
                if (cube.IsLocked) continue;
                int destination = -1, difference = 5;
                if (x > 0 && count - columnn[x - 1].Count >= difference)
                { destination = x - 1; difference = count - columnn[x - 1].Count; }
                if (x + 1 < width && count - columnn[x + 1].Count >= difference) destination = x + 1;
                if (destination < 0) continue;
                int row = columnn[destination].Count;
                columnn[x].RemoveAt(count - 1);
                columnn[destination].Add(cube);
                dirtyColumns[x] = dirtyColumns[destination] = true;
                cube.SlideTo(GridToWorld(destination, row));
                TrackMotion(cube);
                slides++;
            }
        }

        public PotController FindTutorialPot(IReadOnlyList<PotController> pots)
        {
            if (columnn == null) return null;
            for (int x = 0; x < width; x++)
                for (int row = 0; row < Mathf.Min(3, columnn[x].Count); row++)
                    foreach (PotController pot in pots)
                        if (pot != null && pot.PotState == PotState.None && pot.OwnerCell != null &&
                            pot.OwnerCell.GetCurrentPot() == pot && LevelData.Color32Equals(columnn[x][row].Color, pot.TargetColor)) return pot;
            return null;
        }

        public int GetMatchingCubeCountForPot(PotController pot, int rowDepth = 3)
        {
            int count = 0;
            for (int x = 0; x < width; x++)
                for (int row = 0; row < Mathf.Min(rowDepth, columnn[x].Count); row++)
                {
                    CubeController cube = columnn[x][row];
                    if (LevelData.Color32Equals(cube.Color, pot.TargetColor) && Mathf.Abs(cube.transform.position.x - pot.transform.position.x) <= 1.5f) count++;
                }
            return count;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Read the logical columns, so cubes already reserved for a pot are excluded.
        // The conveyor moves across the whole image: inspect all columns, not the pot's current X.
        public int DebugReadCubeColors(Dictionary<Color32, int> exposed, Dictionary<Color32, int> remaining)
        {
            exposed.Clear();
            remaining.Clear();
            int count = 0;
            if (columnn == null) return count;
            foreach (List<CubeController> column in columnn)
            {
                if (column == null) continue;
                for (int row = 0; row < column.Count; row++)
                {
                    CubeController cube = column[row];
                    if (cube == null) continue;
                    count++;
                    remaining.TryGetValue(cube.Color, out int total);
                    remaining[cube.Color] = total + 1;
                    if (row >= 3) continue;
                    exposed.TryGetValue(cube.Color, out int bottom);
                    exposed[cube.Color] = bottom + 1;
                }
            }
            return count;
        }
#endif



        /// <summary>
        /// Convert grid to world position.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private Vector3 GridToWorld(int x, int y)
        {
            float offsetX = (width - 1) * cubeSize * 0.5f;
            float offsetZ = (height - 1) * cubeSize * 0.5f;
            return transform.position + new Vector3(
                x * cubeSize - offsetX,
                0f,
                y * cubeSize - offsetZ);
        }


        /// <summary>
        /// Check if the grid is empty.
        /// </summary>
        /// <returns></returns>
        public bool IsEmpty()
        {
            for (int x = 0; x < width; x++)
                if (columnn[x] != null && columnn[x].Count > 0)
                    return false;
            return true;
        }
    }
}
