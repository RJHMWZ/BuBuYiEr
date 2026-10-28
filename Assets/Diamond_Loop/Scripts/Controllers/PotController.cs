using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public class PotController : MonoBehaviour
    {
        [SerializeField] private Text percentText = null;
        [SerializeField] private Transform fillImage = null;
        [SerializeField] private MeshRenderer bodyMeshRenderer = null;
        [SerializeField] private MeshRenderer topMeshRenderer = null;
        [SerializeField] private FrozenStateController frozenState = null;
        [SerializeField] private DecayStateController decayState = null;
        [SerializeField] private LockedStateController lockedState = null;
        [SerializeField] private AnimationCurve animCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public Vector2Int GridPosition
        {
            set { gridPosition = value; }
            get { return gridPosition; }
        }
        public FrozenStateController FrozenState => frozenState;
        public DecayStateController DecayState => decayState;
        public LockedStateController LockedState => lockedState;
        public int RemainingCapacity => Mathf.Max(0, requiredCount - reservedCount);
        public bool IsOnConveyor { get; private set; }
        public bool CanCollect { get; private set; }
        public bool IsFilled { get; private set; }
        public bool CanSelect {  get; private set; }

        public CellController OwnerCell
        {
            set { cellController = value; }
            get { return cellController; }
        }

        public Color32 TargetColor { get; private set; }
        public PotState PotState { private set; get; }

        private PotData potData = null;
        private TileController tileController = null;
        private CellController cellController = null;
        private Coroutine fillCoroutine = null;
        private Coroutine bounceCoroutine = null;
        private Vector3 originalScale = Vector3.zero;
        private Vector2Int gridPosition = Vector2Int.zero;
        private const float FillMinY = 0.22f;
        private const float FillMaxY = 0.7f;
        private const float FillScaleNormal = 0.85f; // 0% → 70%
        private const float FillScaleMax = 0.77f; // at 100%
        private const float ScaleStartAt = 0.70f;
        private float fillAnimDuration = 0.3f;
        private float jumpDuration = 0.3f;
        private float jumpHeight = 2f;
        private float revealDuration = 0.3f;
        private int requiredCount = 0;
        private int currentCount = 0;
        private int reservedCount = 0;



        /// <summary>
        /// Init this pot with data
        /// </summary>
        /// <param name="data"></param>
        public void OnInit(PotData data)
        {
            potData = data;
            IsFilled = false;
            CanSelect = false;
            CanCollect = false;
            IsOnConveyor = false;
            cellController = null;
            tileController = null;
            TargetColor = data.color;
            PotState = data.state;
            currentCount = 0;
            reservedCount = 0;
            originalScale = transform.localScale;
            percentText.gameObject.SetActive(false);
            requiredCount = data.requiredAmount;
            topMeshRenderer.gameObject.SetActive(false);

            MaterialPropertyBlock block = IngameManager.Instance.MPB;
            block.SetColor("_Color", data.color);
            bodyMeshRenderer.SetPropertyBlock(block);
            topMeshRenderer.SetPropertyBlock(block);

            // Reset fill image
            SetFillImmediate(0f);
        }


        /// <summary>
        /// Applies the current pot state (Locked, Frozen, Decay)
        /// and enables/disables the corresponding state controllers.
        /// </summary>
        private void ApplyPotState()
        {
            if (lockedState != null) lockedState.gameObject.SetActive(false);
            if (frozenState != null) frozenState.gameObject.SetActive(false);
            if (decayState != null) decayState.gameObject.SetActive(false);

            // Enable the appropriate state
            switch (PotState)
            {
                case PotState.Locked:
                    if (lockedState != null)
                    {
                        lockedState.gameObject.SetActive(true);
                        lockedState.OnInit();
                    }
                    break;
                case PotState.Frozen:
                    if (frozenState != null)
                    {
                        frozenState.gameObject.SetActive(true);
                        frozenState.OnInit();
                    }
                    break;
                case PotState.Decay:
                    if (decayState != null)
                    {
                        decayState.gameObject.SetActive(true);
                        decayState.OnInit(this, potData.decayTime);
                    }
                    break;
                default:
                    break;
            }
        }



        /// <summary>
        /// Called by CellController to animate this pot into view.
        /// </summary>
        public void ShowWithScaleUp()
        {
            CanSelect = false;

            //Apply pot state
            ApplyPotState();

            transform.localScale = Vector3.zero;
            StartCoroutine(CRScaleUp());
        }


        /// <summary>
        /// Coroutine scale this pot up.
        /// </summary>
        /// <returns></returns>
        private IEnumerator CRScaleUp()
        {
            float elapsed = 0f;
            while (elapsed < revealDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / revealDuration);
                transform.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, t);
                yield return null;
            }
            transform.localScale = Vector3.one;
            CanSelect = true;
        }



        /// <summary>
        /// Called by CubeController the moment it claims this pot.
        /// Decrements capacity atomically — no visual change yet.
        /// </summary>
        public bool TryReserve()
        {
            if (reservedCount >= requiredCount || IsFilled) return false;
            reservedCount++;
            return true;
        }



        /// <summary>
        /// Called by CubeController when it physically arrives at the pot.
        /// Drives fill visual and completion logic only.
        /// </summary>
        public void OnCubeArrived()
        {
            if (currentCount >= requiredCount || IsFilled) return;

            currentCount++;
            float fillRatio = Mathf.Clamp01((float)currentCount / requiredCount);
            percentText.text = Mathf.RoundToInt(fillRatio * 100).ToString() + "%";

            if (fillCoroutine != null) StopCoroutine(fillCoroutine);
            fillCoroutine = StartCoroutine(CRAnimateFill(fillRatio));

            if (currentCount >= requiredCount)
            {
                IsFilled = true;
                CanCollect = false;
                IsOnConveyor = false;
                transform.SetParent(null);
                StartCoroutine(CRMoveUp());
            }
            else
            {
                Bounce();
            }
        }



        /// <summary>
        /// Animate fill image to target ratio.
        /// </summary>
        private IEnumerator CRAnimateFill(float targetRatio)
        {
            float elapsed = 0f;
            Vector3 startPos = fillImage.localPosition;
            Vector3 startScale = fillImage.localScale;

            float targetY = Mathf.Lerp(FillMinY, FillMaxY, targetRatio);
            float targetScale = targetRatio >= ScaleStartAt
                ? Mathf.Lerp(FillScaleNormal, FillScaleMax, (targetRatio - ScaleStartAt) / (1f - ScaleStartAt))
                : FillScaleNormal;

            Vector3 endPos = new Vector3(fillImage.localPosition.x, targetY, fillImage.localPosition.z);
            Vector3 endScale = new Vector3(targetScale, targetScale, fillImage.localScale.z);

            while (elapsed < fillAnimDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fillAnimDuration);
                float smooth = Mathf.SmoothStep(0f, 1f, t);

                fillImage.localPosition = Vector3.Lerp(startPos, endPos, smooth);
                fillImage.localScale = Vector3.Lerp(startScale, endScale, smooth);

                yield return null;
            }

            fillImage.localPosition = endPos;
            fillImage.localScale = endScale;
            fillCoroutine = null;
        }


        /// <summary>
        /// Set fill image instantly without animation (used on init/reset).
        /// </summary>
        private void SetFillImmediate(float fillRatio)
        {
            if (fillImage == null) return;

            float y = Mathf.Lerp(FillMinY, FillMaxY, fillRatio);
            float scale = fillRatio >= ScaleStartAt
                ? Mathf.Lerp(FillScaleNormal, FillScaleMax, (fillRatio - ScaleStartAt) / (1f - ScaleStartAt))
                : FillScaleNormal;

            fillImage.localPosition = new Vector3(fillImage.localPosition.x, y, fillImage.localPosition.z);
            fillImage.localScale = new Vector3(scale, scale, fillImage.localScale.z);
        }



        /// <summary>
        /// Jump to given tile with callback.
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="onComplete"></param>
        public void JumpToTile(TileController tile, Action onComplete = null)
        {
            CanCollect = false;
            decayState.ActiveCount = false;
            StartCoroutine(CRJumpToTile(tile, onComplete));
        }


        /// <summary>
        /// Coroutine jump to given tile with callback.
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="onComplete"></param>
        /// <returns></returns>
        private IEnumerator CRJumpToTile(TileController tile, Action onComplete)
        {
            Vector3 originalScale = transform.localScale;
            Vector3 originalPos = transform.position;

            //Squash down (0.3s) ---
            Vector3 squashedScale = new Vector3(originalScale.x * 1.5f, originalScale.y * 0.7f, originalScale.z * 1.5f);
            float elapsed = 0f;
            float scaleDuration = 0.1f;
            while (elapsed < scaleDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / scaleDuration);
                transform.localScale = Vector3.Lerp(originalScale, squashedScale, t);
                yield return null;
            }
            transform.localScale = squashedScale;


            elapsed = 0f;
            Vector3 startPos = transform.position;
            float duration = jumpDuration;
            float height = jumpHeight;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                Vector3 currentTarget = tile.PotPosition;
                Vector3 horizontal = Vector3.Lerp(startPos, currentTarget, t);
                float arc = height * Mathf.Sin(t * Mathf.PI);

                transform.position = new Vector3(horizontal.x, horizontal.y + arc, horizontal.z);
                transform.localScale = Vector3.Lerp(squashedScale, originalScale, t);
                yield return null;
            }

            transform.localScale = originalScale;
            transform.SetParent(tile.transform);
            transform.localPosition = tile.PotPosition;
            tile.PotController = this;
            IsOnConveyor = true;
            tileController = tile;
            decayState.ActiveCount = true;
            onComplete?.Invoke();
            percentText.gameObject.SetActive(true);
            float fillRatio = Mathf.Clamp01((float)currentCount / requiredCount);
            percentText.text = Mathf.RoundToInt(fillRatio * 100).ToString() + "%";
            EffectManager.Instance.PlayPotDeployEffect(transform);

            while (IsOnConveyor)
            {
                if (PotState == PotState.Frozen)
                {
                    CanCollect = false;
                }
                else
                {
                    float currentX = transform.position.x;
                    if (currentX < 2.5f && currentX > -2.5f) { CanCollect = true; }
                    else { CanCollect = false; }
                }
                yield return null;
            }
        }




        /// <summary>
        /// Called by the Magnet power-up. Frees the conveyor tile and arcs this pot
        /// back to the given cell. The cell must be empty before calling this.
        /// </summary>
        public void ReturnToCell(CellController cell, Action onComplete = null)
        {
            // Break the CRJumpToTile while-loop and stop cube collection
            IsOnConveyor = false;
            CanCollect = false;

            // Free the conveyor tile slot immediately
            if (tileController != null)
            {
                tileController.PotController = null;
                tileController = null;
            }

            transform.SetParent(null);
            OwnerCell = cell;
            decayState.ActiveCount = false;
            StartCoroutine(CRReturnToCell(cell, onComplete));
        }


        /// <summary>
        /// Coroutine return to cell with callback
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="onComplete"></param>
        /// <returns></returns>
        private IEnumerator CRReturnToCell(CellController cell, Action onComplete)
        {
            Vector3 startPos = transform.position;
            Vector3 endPos = cell.transform.position;
            float duration = 0.45f;
            float arcHeight = 1.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                Vector3 pos = Vector3.Lerp(startPos, endPos, t);
                pos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
                transform.position = pos;
                yield return null;
            }

            transform.position = endPos;
            percentText.gameObject.SetActive(false);

            // Hand back to the cell so it shows this pot as current
            cell.ReceivePotFromMagnet(this);
            onComplete?.Invoke();
        }




        /// <summary>
        /// Handle this pot decayed.
        /// </summary>
        public void PotDecayed()
        {
            IsOnConveyor = false;
            CanCollect = false;
            IsFilled = false;

            //Play effect, unparent from tile, disable
            IngameManager.Instance.GridManager.RemovePot(this);
            EffectManager.Instance.PlayPotDecayedEffect(transform.position);
            if (tileController != null) tileController.PotController = null;
            transform.SetParent(null);
            gameObject.SetActive(false);
        }



        /// <summary>
        /// Disable frozen state.
        /// </summary>
        public void DisableFrozenState()
        {
            if (frozenState != null && frozenState.gameObject.activeSelf)
            {
                // Create effect and reset
                ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.FrozenBreak);
                EffectManager.Instance.PlayFrozenBreakEffect(transform.position);
                frozenState.BreakState();
                PotState = PotState.None;
            }
        }


        /// <summary>
        /// Disable decay state.
        /// </summary>
        public void DisableDecayState()
        {
            if (decayState != null && decayState.gameObject.activeSelf)
            {
                ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.PotCoolOff);
                EffectManager.Instance.PlayDecayCoolOffEffect(transform.position);
                decayState.BreakState();
                PotState = PotState.None;
            }
        }



        /// <summary>
        /// Disable locked state.
        /// </summary>
        public void DisableLockedState()
        {
            if (lockedState != null && lockedState.gameObject.activeSelf)
            {
                ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.PotUnlocked);
                EffectManager.Instance.PlayLockedBreakExplodeEffect(transform.position);
                lockedState.BreakState();
                PotState = PotState.None;
            }
        }




        /// <summary>
        /// Coroutine move up and disable. after filled up 100%.
        /// </summary>
        /// <returns></returns>
        private IEnumerator CRMoveUp()
        {
            Vector3 originalScale = transform.localScale;
            Vector3 originalPos = transform.position;

            //Squash down (0.3s) ---
            Vector3 squashedScale = new Vector3(originalScale.x * 1.5f, originalScale.y * 0.7f, originalScale.z * 1.5f);
            float elapsed = 0f;
            float scaleDuration = 0.1f;
            while (elapsed < scaleDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / scaleDuration);
                transform.localScale = Vector3.Lerp(originalScale, squashedScale, t);
                yield return null;
            }
            transform.localScale = squashedScale;

            //Move up & restore scale (0.5s) ---
            Vector3 raisedPos = originalPos + Vector3.up * 3f;
            elapsed = 0f;
            float moveUpDuration = 0.3f;
            while (elapsed < moveUpDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / moveUpDuration);
                transform.localScale = Vector3.Lerp(squashedScale, originalScale, t);
                transform.position = Vector3.Lerp(originalPos, raisedPos, t);
                yield return null;
            }
            transform.localScale = originalScale;
            transform.position = raisedPos;

            //Close the top
            topMeshRenderer.gameObject.SetActive(true);
            Vector3 topPos = topMeshRenderer.transform.localPosition;
            Vector3 targetPos = new Vector3(0f, 1f, 0f);
            elapsed = 0;
            float duration = 0.2f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float curveT = animCurve.Evaluate(t);
                topMeshRenderer.transform.localPosition = Vector3.Lerp(topPos, targetPos, curveT);
                yield return null;
            }

            tileController.PotController = null;
            topMeshRenderer.gameObject.SetActive(false);
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.PotCleared);
            ViewManager.Instance.IngameViewController.CreateCoinEffect(transform.position, IngameManager.Instance.CoinReward);
            EffectManager.Instance.PlayPotCompletedEffect(transform.position + Vector3.up * 0.3f);
            IngameManager.Instance.OnPotCompleted();
            gameObject.SetActive(false);
        }




        /// <summary>
        /// Shake this pot.
        /// </summary>
        /// <param name="delay"></param>
        public void Shake(float delay)
        {
            StartCoroutine(CRShake(delay));
        }


        /// <summary>
        /// Coroutine shake this pot.
        /// </summary>
        /// <param name="delay"></param>
        /// <returns></returns>
        private IEnumerator CRShake(float delay)
        {
            yield return new WaitForSeconds(delay);
            float shakeDuration = 1f;
            float shakeAmount = 0.1f;
            float decreaseFactor = 0.1f;
            Vector3 originalPos = transform.localPosition;
            float shakeDurationTemp = shakeDuration;
            while (shakeDurationTemp > 0)
            {
                Vector3 newPos = originalPos + UnityEngine.Random.insideUnitSphere * shakeAmount;
                newPos.y = originalPos.y;
                transform.localPosition = newPos;
                shakeDurationTemp -= Time.deltaTime * decreaseFactor;
                yield return null;
            }
            transform.localPosition = originalPos;
        }



        /// <summary>
        /// Bounce this pot.
        /// </summary>
        /// <param name="delay"></param>
        public void Bounce(float delay = 0f)
        {
            if (bounceCoroutine != null)
                StopCoroutine(bounceCoroutine);

            // Snap back before restarting so animation always starts clean
            transform.localScale = originalScale;

            bounceCoroutine = StartCoroutine(CRBounce(delay));
        }


        /// <summary>
        /// Coroutine bounce this pot.
        /// </summary>
        /// <param name="delay"></param>
        /// <returns></returns>
        private IEnumerator CRBounce(float delay = 0f)
        {
            yield return new WaitForSeconds(delay);

            float squashDuration = 0.08f;
            float stretchDuration = 0.12f;
            float settleDuration = 0.07f;

            Vector3 squashScale = new Vector3(originalScale.x * 1.3f, originalScale.y * 0.6f, originalScale.z * 1.3f);
            Vector3 stretchScale = new Vector3(originalScale.x * 0.85f, originalScale.y * 1.25f, originalScale.z * 0.85f);

            // Squash
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / squashDuration;
                float s = Mathf.SmoothStep(0f, 1f, t);
                transform.localScale = Vector3.Lerp(originalScale, squashScale, s);
                yield return null;
            }

            // Stretch up
            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / stretchDuration;
                float s = Mathf.SmoothStep(0f, 1f, t);
                transform.localScale = Vector3.Lerp(squashScale, stretchScale, s);
                yield return null;
            }

            // Settle
            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / settleDuration;
                transform.localScale = Vector3.Lerp(stretchScale, originalScale, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            transform.localScale = originalScale;
        }
    }
}
