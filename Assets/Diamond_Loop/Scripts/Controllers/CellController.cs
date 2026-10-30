using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public class CellController : MonoBehaviour
    {
        [Header("Cell References")]
        [SerializeField] private GameObject cellRoot = null;
        [SerializeField] private GameObject canvasObject = null;
        [SerializeField] private Text potText = null;

        [Header("Borders")]
        [SerializeField] private Transform topBorder = null;
        [SerializeField] private Transform leftBorder = null;
        [SerializeField] private Transform rightBorder = null;
        [SerializeField] private Transform bottomBorder = null;

        [Header("Corners")]
        [SerializeField] private Transform topLeftCorner = null;
        [SerializeField] private Transform topRightCorner = null;
        [SerializeField] private Transform bottomLeftCorner = null;
        [SerializeField] private Transform bottomRightCorner = null;

        public Vector2Int GridPos
        {
            set { gridPos = value; }
            get { return gridPos; }
        }


        /// <summary>
        /// True if at least one pot is still waiting.
        /// </summary>
        public bool HasAvailablePot => potQueue.Count > 0;


        /// <summary>
        /// Returns the currently visible (front) pot, or null if empty.
        /// </summary>
        /// <returns></returns>
        public PotController GetCurrentPot()
            => potQueue.Count > 0 ? potQueue.Peek() : null;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public PotController DebugPeekNextPot()
        {
            bool first = true;
            foreach (PotController pot in potQueue)
            {
                if (first) { first = false; continue; }
                return pot;
            }
            return null;
        }
#endif


        /// <summary>
        /// Setup the pot queue.
        /// </summary>
        public List<PotController> PotQueue
        {
            set
            {
                potQueue.Clear();
                foreach (var pot in value)
                {
                    potQueue.Enqueue(pot);
                }
                canvasObject.SetActive(true);
                potText.text = potQueue.Count.ToString();
            }
        }


        private Vector2Int gridPos;
        private Queue<PotController> potQueue = new Queue<PotController>();



        /// <summary>
        /// Initialise this cell..
        /// Call UpdateDecoration immediately after to activate the correct borders/corners.
        /// </summary>
        /// <param name="gridData">The level's grid, used by UpdateDecoration.</param>
        public void OnInit(GridData gridData)
        {
            canvasObject.SetActive(false);
            UpdateDecoration(gridData);
        }


        /// <summary>
        /// Activates the correct border and corner child objects for this cell.
        ///
        /// A border is shown on any side where the neighbour cell is absent
        /// (disabled or out of bounds). A corner is shown where two such borders meet.
        ///
        /// Call this once after spawning, and again if the grid layout changes at runtime.
        /// </summary>
        /// <param name="gridData">The level's grid configuration.</param>
        public void UpdateDecoration(GridData gridData)
        {
            // Disable everything first — only enable what's needed
            SetBordersActive(false);
            SetCornersActive(false);

            bool isEnabled = gridData.IsEnabled(gridPos.x, gridPos.y);

            if (cellRoot != null)
                cellRoot.SetActive(isEnabled);

            if (!isEnabled) return;

            // ── Borders: show on each side where the neighbour is absent ──
            bool emptyTop = IsNeighbourAbsent(gridPos.x, gridPos.y + 1, gridData);
            bool emptyRight = IsNeighbourAbsent(gridPos.x + 1, gridPos.y, gridData);
            bool emptyBottom = IsNeighbourAbsent(gridPos.x, gridPos.y - 1, gridData);
            bool emptyLeft = IsNeighbourAbsent(gridPos.x - 1, gridPos.y, gridData);

            if (topBorder != null) topBorder.gameObject.SetActive(emptyTop);
            if (rightBorder != null) rightBorder.gameObject.SetActive(emptyRight);
            if (bottomBorder != null) bottomBorder.gameObject.SetActive(emptyBottom);
            if (leftBorder != null) leftBorder.gameObject.SetActive(emptyLeft);

            // ── Corners: show where two adjacent borders meet ──────────────
            if (topLeftCorner != null) topLeftCorner.gameObject.SetActive(emptyTop && emptyLeft);
            if (topRightCorner != null) topRightCorner.gameObject.SetActive(emptyTop && emptyRight);
            if (bottomLeftCorner != null) bottomLeftCorner.gameObject.SetActive(emptyBottom && emptyLeft);
            if (bottomRightCorner != null) bottomRightCorner.gameObject.SetActive(emptyBottom && emptyRight);
        }



        /// <summary>
        /// Returns true when the neighbour at (x, y) is out of bounds or disabled —
        /// meaning this tile needs a border on that side.
        /// </summary>
        private bool IsNeighbourAbsent(int x, int y, GridData grid)
        {
            return !grid.IsValidPosition(x, y) || !grid.IsEnabled(x, y);
        }


        /// <summary>
        /// Set borders active/deactive.
        /// </summary>
        /// <param name="active"></param>
        private void SetBordersActive(bool active)
        {
            if (topBorder != null) topBorder.gameObject.SetActive(active);
            if (rightBorder != null) rightBorder.gameObject.SetActive(active);
            if (bottomBorder != null) bottomBorder.gameObject.SetActive(active);
            if (leftBorder != null) leftBorder.gameObject.SetActive(active);
        }



        /// <summary>
        /// Set corners active/deactive.
        /// </summary>
        /// <param name="active"></param>
        private void SetCornersActive(bool active)
        {
            if (topLeftCorner != null) topLeftCorner.gameObject.SetActive(active);
            if (topRightCorner != null) topRightCorner.gameObject.SetActive(active);
            if (bottomLeftCorner != null) bottomLeftCorner.gameObject.SetActive(active);
            if (bottomRightCorner != null) bottomRightCorner.gameObject.SetActive(active);
        }



        /// <summary>
        /// Disable all pots in quque.
        /// </summary>
        public void DisablePots()
        {
            foreach (var pot in potQueue)
            {
                pot.gameObject.SetActive(false);
            }
        }


        /// <summary>
        /// Dequque and show next pot.
        /// </summary>
        public void OnPotLeft()
        {
            if (potQueue.Count == 0)
            {
                canvasObject.SetActive(false);
                return;
            }
            potQueue.Dequeue();
            ShowNextPot();
        }


        /// <summary>
        /// Show the next pot
        /// </summary>
        public void ShowNextPot()
        {
            if (potQueue.Count == 0)
            {
                canvasObject.SetActive(false);
                return;
            }
            PotController next = potQueue.Peek();
            next.gameObject.SetActive(true);
            next.ShowWithScaleUp();
            potText.text = potQueue.Count.ToString();
        }



        /// <summary>
        /// Called after the Magnet arc completes. Re-seats the pot as this cell's
        /// current active pot, parented and visible.
        /// </summary>
        public void ReceivePotFromMagnet(PotController pot)
        {
            // Insert at front of queue so it's the next shown
            if (potQueue == null) potQueue = new Queue<PotController>();
            potQueue.Enqueue(pot);

            pot.transform.SetParent(transform);
            pot.transform.localPosition = Vector3.zero;
            pot.transform.localScale = Vector3.one;
            pot.gameObject.SetActive(true);
            pot.ShowWithScaleUp();

            canvasObject.SetActive(true);
            potText.text = potQueue.Count.ToString();
        }
    }
}
