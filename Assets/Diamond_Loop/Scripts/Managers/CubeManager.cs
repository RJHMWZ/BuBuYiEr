using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ClawbearGames
{
    /// <summary>
    /// Spawns the cube image grid from LevelData, runs the fall simulation,
    /// and sends matching bottom-row cubes to the active pot.
    /// </summary>
    public class CubeManager : MonoBehaviour
    {
        private List<CubeController>[] columnn = null;
        private const float cubeSize = 0.21f;
        private bool simulationRunning = false;
        private int width, height;


        /// <summary>
        /// Initialises CubeManager from LevelData.
        /// Call this after GridManager.OnInit so pots are already in the scene.
        /// </summary>
        public void OnInit(LevelData levelData)
        {
            ImageData img = levelData.imageData;

            width = img.imageWidth;
            height = img.imageHeight;
            SpawnGrid(img);
        }


        /// <summary>
        /// Spawns a CubeController for every pixel in the image grid.
        /// </summary>
        private void SpawnGrid(ImageData img)
        {
            columnn = new List<CubeController>[width];
            for (int x = 0; x < width; x++)
                columnn[x] = new List<CubeController>();

            // y=0 is added first → _columns[x][0] is always the logical bottom
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color32 color = img.GetPixel(x, y);
                    CubeController cube = PoolManager.Instance.GetCubeController();
                    cube.transform.position = GridToWorld(x, y);
                    cube.gameObject.SetActive(true);
                    cube.Init(color);
                    columnn[x].Add(cube);
                }
            }
        }



        /// <summary>
        /// Start the simulation
        /// </summary>
        public void StartSimulation()
        {
            if (simulationRunning) return;
            simulationRunning = true;
            StartCoroutine(CRSimulationTick());
        }


        /// <summary>
        /// Coroutine simulation tick.
        /// </summary>
        /// <returns></returns>
        private IEnumerator CRSimulationTick()
        {
            while (simulationRunning)
            {
                //Stop if not Playing state
                while (IngameManager.Instance.IngameState != IngameState.Ingame_Playing) yield return null;

                // Stamp bottom cubes — their Update() will scan + reserve + remove this frame
                StampBottomCubes();

                // One frame: all cube Updates fire, bottom cubes claim pots and call RemoveAt(0)
                yield return null;

                // Fall — columns already updated, cubes snap down via gravity
                yield return StartCoroutine(CRApplyFall());
                yield return StartCoroutine(CRApplySandSlide());

                yield return new WaitForSeconds(0.016f);
            }
        }


        /// <summary>
        /// Marks the bottom cube of each column as eligible to jump this frame.
        /// Passes a removal callback so the cube can evict itself from the column
        /// synchronously the moment it claims a pot.
        /// </summary>
        private void StampBottomCubes()
        {
            List<PotController> activePots = GetActiveConveyorPots();

            var jumpable = new List<(CubeController cube, int col)>();

            for (int x = 0; x < width; x++)
            {
                if (columnn[x].Count == 0) continue;

                int rowsToCheck = Mathf.Min(3, columnn[x].Count);
                for (int row = 0; row < rowsToCheck; row++)
                {
                    CubeController cube = columnn[x][row];

                    // Only include cubes that have at least one matching pot in range
                    bool hasMatch = activePots.Exists(p =>
                        LevelData.Color32Equals(cube.Color, p.TargetColor) &&
                        p.RemainingCapacity > 0 &&
                        p.PotState != PotState.Frozen &&
                        Mathf.Abs(cube.transform.position.x - p.transform.position.x) <= 1.5f);

                    if (hasMatch)
                        jumpable.Add((cube, x));
                }
            }

            if (jumpable.Count == 0) return;

            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.CubeJumped);
            float delayStep = 0.3f / jumpable.Count;
            for (int i = 0; i < jumpable.Count; i++)
            {
                var (cube, col) = jumpable[i];
                int captured = col;
                CubeController capturedCube = cube;
                cube.SetCanJump(() => columnn[captured].Remove(capturedCube), i * delayStep);
            }
        }


        /// <summary>
        /// Get all active pots on the conveyor
        /// </summary>
        /// <returns></returns>
        private List<PotController> GetActiveConveyorPots()
        {
            var result = new List<PotController>();
            foreach (PotController pot in IngameManager.Instance.GridManager.AllPots)
                if (pot != null && pot.IsOnConveyor && !pot.IsFilled && pot.CanCollect)
                    result.Add(pot);
            return result;
        }



        /// <summary>
        /// Drops every cube in every column to its correct world position
        /// (= GridToWorld(x, listIndex)) and waits until all fall animations finish.
        /// One pass resolves any number of stacked gaps simultaneously.
        /// </summary>
        private IEnumerator CRApplyFall()
        {
            int falling = 0;

            for (int x = 0; x < width; x++)
            {
                for (int i = 0; i < columnn[x].Count; i++)
                {
                    CubeController cube = columnn[x][i];
                    if (cube == null) continue;
                    Vector3 target = GridToWorld(x, i);

                    // Only animate if the cube is meaningfully above its target
                    if (Mathf.Abs(cube.transform.position.z - target.z) > 0.005f)
                    {
                        falling++;
                        cube.FallTo(target, () => falling--);
                    }
                }
            }

            if (falling > 0)
                yield return new WaitUntil(() => falling <= 0);
            else
                yield return null;
        }


        /// <summary>
        /// Sand-simulation pass: if a column is steeply taller than a neighbour,
        /// its top cube slides into that neighbour. Runs until stable or pass cap hit.
        /// Skips cubes that are locked (falling) or jump-pending (stamped for a pot).
        /// </summary>
        private IEnumerator CRApplySandSlide()
        {
            const int steepThreshold = 5;
            const int maxPasses = 8;

            for (int pass = 0; pass < maxPasses; pass++)
            {
                bool anySlid = false;
                int sliding = 0;

                // Alternate direction each pass to eliminate directional bias
                bool leftToRight = (pass % 2 == 0);
                int startX = leftToRight ? 0 : width - 1;
                int endX = leftToRight ? width : -1;
                int stepX = leftToRight ? 1 : -1;

                for (int x = startX; x != endX; x += stepX)
                {
                    int myH = columnn[x].Count;
                    if (myH == 0) continue;

                    int targetCol = -1;
                    int bestDiff = steepThreshold;

                    if (x > 0)
                    {
                        int diff = myH - columnn[x - 1].Count;
                        if (diff >= bestDiff) { bestDiff = diff; targetCol = x - 1; }
                    }
                    if (x < width - 1)
                    {
                        int diff = myH - columnn[x + 1].Count;
                        if (diff >= bestDiff) { bestDiff = diff; targetCol = x + 1; }
                    }

                    if (targetCol < 0) continue;

                    int topIdx = myH - 1;
                    CubeController cube = columnn[x][topIdx];
                    if (cube == null || cube.IsLocked || cube.IsJumpPending) continue;

                    int newIndex = columnn[targetCol].Count;
                    Vector3 slideTarget = GridToWorld(targetCol, newIndex);

                    columnn[x].RemoveAt(topIdx);
                    columnn[targetCol].Add(cube);

                    sliding++;
                    cube.SlideTo(slideTarget, () => sliding--);
                    anySlid = true;
                }

                if (sliding > 0)
                    yield return new WaitUntil(() => sliding <= 0);

                if (!anySlid) break;
            }
        }


        /// <summary>
        /// Returns how many cubes in the bottom <rowDepth> rows of each column
        /// match the pot's color and are within X-range of the pot's current position.
        /// Used by GridManager to decide if a conveyor pot is stranded.
        /// </summary>
        public int GetMatchingCubeCountForPot(PotController pot, int rowDepth = 3)
        {
            int count = 0;
            for (int x = 0; x < width; x++)
            {
                int rows = Mathf.Min(rowDepth, columnn[x].Count);
                for (int row = 0; row < rows; row++)
                {
                    CubeController cube = columnn[x][row];
                    if (cube == null) continue;
                    if (!LevelData.Color32Equals(cube.Color, pot.TargetColor)) continue;

                    float xDist = Mathf.Abs(cube.transform.position.x - pot.transform.position.x);
                    if (xDist <= 1.5f) count++;
                }
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
