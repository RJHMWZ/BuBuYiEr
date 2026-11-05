using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    [DefaultExecutionOrder(-100)]
    public class ConveyorManager : MonoBehaviour
    {
        [SerializeField] private TileController[] tiles = null;
        [SerializeField] private Text slotText = null;
        [SerializeField] private ParticleSystem particle = null;
        [SerializeField] private Transform canvasTrans = null;


        private Coroutine cRShake = null;
        private Vector3 canvasPos = Vector3.zero;
        private float tileWidth = 0f;
        private float totalWidth = 0f;
        private float leftEdge = 0f;
        private float rightEdge = 0f;
        private float moveSpeed = 2f;
        private float updateInterval = 0.2f;
        private float updateCount = 0f;
        private int currentSlot = 0;
        private Transform[] tileTransforms;
        private int displayedUsed = -1, displayedCapacity = -1;
        public bool CanIncreaseSlot => currentSlot < tiles.Length;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public int DebugSlotCapacity => currentSlot;
#endif


        /// <summary>
        /// Init this conveyor with params.
        /// </summary>
        /// <param name="initSlot"></param>
        public void OnInit(int initSlot)
        {
            moveSpeed = 1.5f;
            currentSlot = initSlot;
            tileTransforms = new Transform[tiles.Length];
            for (int i = 0; i < tiles.Length; i++) tileTransforms[i] = tiles[i].transform;
            slotText.text = GetUsedTile().ToString() + "/" + initSlot.ToString();
            canvasPos = canvasTrans.localPosition;

            if (tiles.Length == 0) return;

            // Assumes tiles are evenly spaced — measure gap between first two
            tileWidth = (tiles.Length > 1)
                ? Mathf.Abs(tiles[1].transform.localPosition.x - tiles[0].transform.localPosition.x)
                : 1f;

            totalWidth = tileWidth * tiles.Length;

            // Left/right bounds based on first and last tile positions
            leftEdge = tiles[0].transform.localPosition.x;
            rightEdge = tiles[tiles.Length - 1].transform.localPosition.x;
        }


        private void Update()
        {
            //Stop if not Playing state
            IngameManager game = IngameManager.Instance;
            if (game == null || tileTransforms == null || game.IsPaused || !game.IsRoundStarted || game.IngameState != IngameState.Ingame_Playing || Time.deltaTime <= 0) return;

            float delta = moveSpeed * Time.deltaTime;
            foreach (Transform tile in tileTransforms)
            {
                // Move right
                Vector3 position = tile.localPosition;
                position.x += delta;

                // If tile passed the right edge, wrap it to the left
                if (position.x > rightEdge + tileWidth * 0.5f)
                {
                    position.x -= totalWidth;
                }
                tile.localPosition = position;
            }


            //Update slot text
            updateCount += Time.deltaTime;
            if (updateCount >= updateInterval)
            {
                updateCount = 0;
                int used = GetUsedTile();
                if (used != displayedUsed || currentSlot != displayedCapacity)
                {
                    displayedUsed = used;
                    displayedCapacity = currentSlot;
                    slotText.text = used + "/" + currentSlot;
                }
            }
        }



        /// <summary>
        /// Get the current count of tile that being use.
        /// </summary>
        /// <returns></returns>
        public int GetUsedTile()
        {
            int count = 0;
            foreach(var tile in tiles)
            {
                if (tile.PotController != null)
                    count++;
            }
            return count;
        }


        /// <summary>
        /// Increase conveyor slot by 1.
        /// </summary>
        public void IncreaseSlot()
        {
            particle.Play();
            currentSlot++;
            if (currentSlot >= tiles.Length) currentSlot = tiles.Length;
            slotText.text = GetUsedTile().ToString() + "/" + currentSlot.ToString();
            StartCoroutine(CRScaleCanvas());
        }


        /// <summary>
        /// Coroutine scale the canvas.
        /// </summary>
        /// <returns></returns>
        private IEnumerator CRScaleCanvas()
        {
            Vector3 originalScale = canvasTrans.localScale;
            Vector3 endScale = originalScale * 1.5f;

            float t = 0;
            float scalingTime = 0.25f;
            while (t < scalingTime)
            {
                t += Time.deltaTime;
                float factor = EasyType.MatchedLerpType(LerpType.EaseOutQuart, t / scalingTime);
                canvasTrans.localScale = Vector3.Lerp(originalScale, endScale, factor);
                yield return null;
            }

            t = 0;
            while (t < scalingTime)
            {
                t += Time.deltaTime;
                float factor = EasyType.MatchedLerpType(LerpType.EaseOutQuart, t / scalingTime);
                canvasTrans.localScale = Vector3.Lerp(endScale, originalScale, factor);
                yield return null;
            }
            canvasTrans.localScale = originalScale;
        }



        /// <summary>
        /// Get the closest empty tile.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public TileController GetClosestEmptyTile(Vector3 position)
        {
            if (GetUsedTile() >= currentSlot) return null;

            TileController closest = null;
            float minDist = float.MaxValue;
            foreach (var tile in tiles)
            {
                var tileController = tile;
                if (tileController == null || tileController.HasPot()) continue;

                float dist = (tile.transform.position - position).sqrMagnitude;
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = tileController;
                }
            }
            return closest;
        }



        /// <summary>
        /// Shake the canvas.
        /// </summary>
        public void ShakeCanvas()
        {
            if (cRShake != null)
            {
                canvasTrans.localPosition = canvasPos;
                StopCoroutine(cRShake);
            }

            cRShake = StartCoroutine(CRShakeCanvas());
        }

        /// <summary>
        /// Coroutine shake the canvas.
        /// </summary>
        /// <returns></returns>
        private IEnumerator CRShakeCanvas()
        {
            float shakeDuration = 0.5f;
            float shakeAmount = 0.1f;
            Vector3 originalPos = canvasTrans.localPosition;
            while (shakeDuration > 0)
            {
                Vector3 newPos = originalPos + UnityEngine.Random.insideUnitSphere * shakeAmount;
                newPos.y = originalPos.y;
                canvasTrans.localPosition = newPos;
                shakeDuration -= Time.deltaTime;
                yield return null;
            }
            canvasTrans.localPosition = originalPos;
        }
    }
}
