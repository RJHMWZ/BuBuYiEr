using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ClawbearGames
{
    public class MapManager : MonoBehaviour
    {
        [Header("MapManager Configuration")]
        [SerializeField] private int totalLevels = 100;
        public int TotalLevels => Resources.LoadAll<LevelData>("Levels").Length;
        [SerializeField] private int levelItemPerMap = 5;

        [Header("MapManager References")]
        [SerializeField] private Transform mapTrans = null;
        [SerializeField] private MapItemController[] mapItemPrefabs;

        private List<LevelItemController> levelItems = new List<LevelItemController>();
        private LevelItemController closestItem = null;
        private LevelItemController targetItem = null;
        private LevelItemController currentItem = null;
        private Vector2 lastInputPos = Vector3.zero;
        private float scrollSpeed = 10f;
        private float elasticity = 50f;
        private float smoothing = 0.2f;
        private float upperLimit = 0f;
        private float lowerLimit = 0f;
        private float velocityY = 0f;
        private float targetZ = 0f;
        private float originalZ = 0f;
        private bool dragging = false;

        private void Start()
        {
            Application.targetFrameRate = 60;
            ViewManager.Instance.OnShowView(ViewType.LEVEL_VIEW);
            if (ViewManager.Instance.LevelViewController.UsesBearTheme)
            {
                // The illustrated uGUI route replaces the template's 3D map and physics input.
                Camera camera = Camera.main;
                if (camera != null)
                {
                    camera.clearFlags = CameraClearFlags.SolidColor;
                    camera.backgroundColor = BearGameTheme.Cream;
                    camera.cullingMask = 0;
                }
                enabled = false;
                return;
            }
            InputManager.Initialize();

            //Create the map items
            Vector3 mapPos = Vector3.zero;
            int mapItemCount = Mathf.CeilToInt((float)totalLevels / levelItemPerMap);
            int currentLevel = 1;

            for (int i = 0; i < mapItemCount; i++)
            {
                int prefabIndex = i % mapItemPrefabs.Length;
                MapItemController prefab = mapItemPrefabs[prefabIndex];
                MapItemController mapItem = Instantiate(prefab, mapPos, Quaternion.identity, mapTrans);


                // Slice the correct level range
                int remainingLevels = totalLevels - currentLevel + 1;
                int count = Mathf.Min(levelItemPerMap, remainingLevels);
                int[] levels = new int[count];
                for (int j = 0; j < count; j++)
                {
                    levels[j] = currentLevel++;
                }
                mapItem.OnInit(levels);


                //Find the transform of the current level node
                foreach(int level in levels)
                {
                    LevelItemController levelItem = mapItem.FindLevelItem(level);
                    if (levelItem != null)
                    {
                        levelItems.Add(levelItem);
                        if (level == PlayerDataHandler.GetCurrentLevel())
                        {
                            currentItem = levelItem;
                            closestItem = levelItem;
                        }
                    }
                }
                mapPos = mapItem.transform.position + Vector3.forward * mapItem.Length;
            }


            // Set scroll limits after building
            upperLimit = 0f;
            lowerLimit = -(mapPos.z - mapItemPrefabs[0].Length);


            //Snap to current level node
            float targetLevelZ = currentItem.transform.position.z;
            targetZ = Mathf.Clamp(-targetLevelZ, lowerLimit, upperLimit);
            originalZ = targetZ;

            //Cache next level items
            for (int i = 0; i < levelItems.Count; i++)
            {
                LevelItemController current = levelItems[i];
                LevelItemController next = (i < levelItems.Count - 1) ? levelItems[i + 1] : null;
                current.CacheNextLevel(next);
            }
        }


        private void Update()
        {
            InputManager.Update();
            HandleInput();
            float currentZ = mapTrans.position.z;
            currentZ = Mathf.SmoothDamp(currentZ, targetZ, ref velocityY, smoothing);
            mapTrans.position = new Vector3(0f, 0f, currentZ);

            float distance = Mathf.Abs(currentZ - targetZ);
            if (distance <= 0.2f && closestItem != null && targetItem != closestItem)
            {
                targetItem = closestItem;
                StartCoroutine(CRScaleItem(closestItem, true));
            }
        }

        private void HandleInput()
        {
            if (InputManager.GetMouseButtonDown() && !Utilities.IsPointerOverUIObject())
            {
                Ray ray = Camera.main.ScreenPointToRay(InputManager.GetMousePosition());
                RaycastHit raycastHit;
                if (Physics.Raycast(ray, out raycastHit, 1000f))
                {
                    //Hit an level item
                    LevelItemController levelItem = FindLevelItem(raycastHit.collider.transform);
                    if (levelItem != null)
                    {
                        ViewManager.Instance.LevelViewController.ShowConfirmPanel(levelItem.Level);
                    }
                }
                else
                {
                    dragging = true;
                    lastInputPos = InputManager.GetMousePosition();
                }
            }
            else if (InputManager.GetMouseButton() && dragging)
            {
                Vector2 delta = (Vector2)InputManager.GetMousePosition() - lastInputPos;
                ScrollMap(delta.y);
                lastInputPos = InputManager.GetMousePosition();
            }
            else if (InputManager.GetMouseButtonUp() && dragging)
            {
                StartCoroutine(CRScaleItem(closestItem, false));
                dragging = false;
                closestItem = null;
                SnapToClosest();
            }
        }


        /// <summary>
        /// Scroll the map.
        /// </summary>
        /// <param name="moveDelta"></param>
        private void ScrollMap(float moveDelta)
        {
            float newZ = mapTrans.position.z + moveDelta * scrollSpeed * Time.deltaTime;
            if (newZ > upperLimit)
                newZ = Mathf.Lerp(newZ, upperLimit, Time.deltaTime * elasticity);
            else if (newZ < lowerLimit)
                newZ = Mathf.Lerp(newZ, lowerLimit, Time.deltaTime * elasticity);
            newZ = Mathf.Clamp(newZ, lowerLimit - 5, upperLimit + 5);
            targetZ = newZ;
        }


        /// <summary>
        /// Snap to closest level item.
        /// </summary>
        private void SnapToClosest()
        {
            LevelItemController closest = null;
            float minDist = float.MaxValue;

            foreach (LevelItemController item in levelItems)
            {
                // Project where this item will be when map reaches targetZ
                float localZ = item.transform.position.z - mapTrans.position.z;
                float projectedWorldZ = targetZ + localZ;
                float dist = Mathf.Abs(projectedWorldZ);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = item;
                }
            }

            if (closest != null)
            {
                closestItem = closest;
                float localZ = closest.transform.position.z - mapTrans.position.z;
                targetZ = Mathf.Clamp(-localZ, lowerLimit, upperLimit);
            }

            BounceBackIfNeeded();
        }



        /// <summary>
        /// Bounce back.
        /// </summary>
        private void BounceBackIfNeeded()
        {
            float currentZ = mapTrans.position.z;
            if (currentZ > upperLimit)
                targetZ = upperLimit;
            else if (currentZ < lowerLimit)
                targetZ = lowerLimit;
        }



        /// <summary>
        /// Find a level item with given transform.
        /// </summary>
        /// <param name="trans"></param>
        /// <returns></returns>
        private LevelItemController FindLevelItem(Transform trans)
        {
            foreach(LevelItemController levelItem in levelItems)
            {
                if (levelItem.transform.Equals(trans))
                {
                    return levelItem;
                }
            }
            return null;
        }




        /// <summary>
        /// Coroutine scale the given level item.
        /// </summary>
        /// <param name="levelItem"></param>
        /// <param name="focus"></param>
        /// <returns></returns>
        private IEnumerator CRScaleItem(LevelItemController levelItem, bool focus)
        {
            Transform child = levelItem.transform.GetChild(0);
            Vector3 currentScale = child.transform.localScale;
            Vector3 focusScale = new Vector3(0.015f, 0.015f, 1f);
            Vector3 normalScale = new Vector3(0.01f, 0.01f, 1f);

            float t = 0;
            float scaleTime = 0.25f;
            while (t < scaleTime)
            {
                t += Time.deltaTime;
                float factor = t / scaleTime;
                child.localScale = Vector3.Lerp(currentScale, focus ? focusScale : normalScale, factor);
                yield return null;
            }
            child.localScale = focus ? focusScale : normalScale;
        }


        /// <summary>
        /// Snap to current level node.
        /// </summary>
        public void SnapToCurrent()
        {
            //Snap to current level node
            targetZ = originalZ;
            if (closestItem != null)
            {
                StartCoroutine(CRScaleItem(closestItem, false));
            }
            closestItem = currentItem;
        }
    }
}
