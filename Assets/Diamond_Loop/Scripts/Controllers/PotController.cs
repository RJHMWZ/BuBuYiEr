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
        public float FillRatio => requiredCount > 0 ? Mathf.Clamp01((float)currentCount / requiredCount) : 0;
        public float DisplayedFill => displayedFill;
        public Vector3 CollectionPosition => jarView != null ? jarView.CollectionPosition : transform.position + Vector3.up * .8f;
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
        private BearJarView jarView;
        private float displayedFill, fillVelocity, bounceAmplitude, bouncePhase, bounceAfter;
        private bool inTransit, percentDirty, shaking;
        private float shakeStart;
        private Vector3 shakeOrigin;
        private Vector3 originalScale = Vector3.zero;
        private Vector2Int gridPosition = Vector2Int.zero;
        private const float FillMinY = 0.22f;
        private const float FillMaxY = 0.7f;
        private const float FillScaleNormal = 0.85f; // 0% → 70%
        private const float FillScaleMax = 0.77f; // at 100%
        private const float ScaleStartAt = 0.70f;
        private float jumpDuration = 0.3f;
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
            StopAllCoroutines();
            inTransit = percentDirty = shaking = false;
            displayedFill = fillVelocity = bounceAmplitude = bouncePhase = 0;
            transform.localScale = Vector3.one;
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
            BearMenuArt art = ViewManager.Instance != null ? ViewManager.Instance.IngameViewController.ThemeArt : null;
            if (art != null)
            {
                jarView = GetComponent<BearJarView>();
                if (jarView == null)
                {
                    jarView = gameObject.AddComponent<BearJarView>();
                    jarView.Initialize(this, art, percentText);
                }
                jarView.SetOpacity(1);
            }
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
            percentDirty = true;
            if (currentCount < requiredCount) { Bounce(); return; }
            IsFilled = true;
            CanCollect = false;
            IsOnConveyor = false;
            displayedFill = 1;
            bounceAmplitude = 0;
            percentText.text = "100%";
            percentDirty = false;
            transform.SetParent(null, true);
            StartCoroutine(CRMoveUp());
        }

        private void Update()
        {
            IngameManager game = IngameManager.Instance;
            float delta = Time.deltaTime;
            if (game == null || delta <= 0 || game.IsPaused || !game.IsRoundStarted ||
                game.IngameState != IngameState.Ingame_Playing) return;
            if (IsOnConveyor)
            {
                float x = transform.position.x;
                CanCollect = !inTransit && PotState != PotState.Frozen && x > -2.5f && x < 2.5f;
            }
            if (percentDirty)
            {
                percentText.text = Mathf.RoundToInt(FillRatio * 100) + "%";
                percentDirty = false;
            }
            float target = FillRatio;
            if (Mathf.Abs(displayedFill - target) > .0001f)
            {
                displayedFill = Mathf.SmoothDamp(displayedFill, target, ref fillVelocity, .065f, Mathf.Infinity, delta);
                if (jarView == null) SetFillImmediate(displayedFill);
            }
            if (inTransit || IsFilled) return;
            if (bounceAmplitude > .0001f && Time.time >= bounceAfter)
            {
                bouncePhase += delta * 26;
                bounceAmplitude *= Mathf.Exp(-12 * delta);
                float pulse = Mathf.Sin(bouncePhase) * bounceAmplitude;
                transform.localScale = Vector3.Scale(originalScale, new Vector3(1 + pulse, 1 - pulse * .7f, 1 + pulse));
                if (bounceAmplitude <= .0001f) transform.localScale = originalScale;
            }
            if (!shaking || Time.time < shakeStart) return;
            float t = (Time.time - shakeStart) / .18f;
            if (t >= 1) { transform.localPosition = shakeOrigin; shaking = false; }
            else transform.localPosition = shakeOrigin + Vector3.right * (Mathf.Sin(t * Mathf.PI * 5) * .045f * (1 - t));
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
            ResetFeedback();
            inTransit = true;
            CanSelect = false;
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
            Vector3 scale = originalScale;
            Vector3 compressed = Vector3.Scale(scale, new Vector3(1.08f, .94f, 1.08f));
            float elapsed = 0;
            while (elapsed < .055f)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(scale, compressed, elapsed / .055f);
                yield return null;
            }
            Vector3 start = transform.position;
            float height = Mathf.Clamp(Vector3.Distance(start, tile.PotPosition) * .22f, .55f, 1.1f);
            elapsed = 0;
            while (elapsed < jumpDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / jumpDuration);
                float smooth = t * t * (3 - 2 * t);
                Vector3 position = Vector3.Lerp(start, tile.PotPosition, smooth);
                position.y += Mathf.Sin(t * Mathf.PI) * height;
                transform.position = position;
                transform.localScale = Vector3.Lerp(compressed, scale, smooth);
                yield return null;
            }
            transform.localScale = scale;
            tile.PotController = this;
            tileController = tile;
            IsOnConveyor = true;
            inTransit = false;
            CanCollect = PotState != PotState.Frozen && transform.position.x > -2.5f && transform.position.x < 2.5f;
            decayState.ActiveCount = true;
            percentText.gameObject.SetActive(true);
            percentDirty = true;
            onComplete?.Invoke();
            EffectManager.Instance.PlayPotDeployEffect(transform);
        }

        /// <summary>
        /// Called by the Magnet power-up. Frees the conveyor tile and arcs this pot
        /// back to the given cell. The cell must be empty before calling this.
        /// </summary>
        public void ReturnToCell(CellController cell, Action onComplete = null)
        {
            ResetFeedback();
            inTransit = true;
            CanSelect = false;
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
            float duration = 0.32f;
            float arcHeight = .75f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                Vector3 pos = Vector3.Lerp(startPos, endPos, t * t * (3 - 2 * t));
                pos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
                transform.position = pos;
                yield return null;
            }

            transform.position = endPos;
            percentText.gameObject.SetActive(false);
            inTransit = false;

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
            IngameManager.Instance.GameOver("有个发烫的罐子烧坏了，下次可以先收集它，或用降温道具帮忙");
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
            Vector3 start = transform.position;
            Vector3 startScale = transform.localScale;
            if (tileController != null) { tileController.PotController = null; tileController = null; }
            // Account for completion immediately; the short departure is presentation only.
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.PotCleared);
            ViewManager.Instance.IngameViewController.CreateCoinEffect(start, IngameManager.Instance.CoinReward);
            EffectManager.Instance.PlayPotCompletedEffect(start + Vector3.up * .3f);
            IngameManager.Instance.OnPotCompleted();
            float elapsed = 0;
            const float duration = .32f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1 - (1 - t) * (1 - t);
                transform.position = start + Vector3.up * (.85f * eased);
                transform.localScale = Vector3.Lerp(startScale, originalScale * .92f, eased);
                if (jarView != null) jarView.SetOpacity(1 - t * t);
                yield return null;
            }
            topMeshRenderer.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        public void Shake(float delay)
        {
            if (inTransit || IsFilled) return;
            if (shaking) transform.localPosition = shakeOrigin;
            shakeOrigin = transform.localPosition;
            shakeStart = Time.time + Mathf.Max(0, delay);
            shaking = true;
        }

        public void Bounce(float delay = 0)
        {
            if (IsFilled || inTransit) return;
            if (bounceAmplitude <= .0001f) bouncePhase = 0;
            bounceAmplitude = Mathf.Min(.04f, bounceAmplitude + .012f);
            bounceAfter = Time.time + Mathf.Max(0, delay);
        }

        private void ResetFeedback()
        {
            if (shaking) transform.localPosition = shakeOrigin;
            shaking = false;
            bounceAmplitude = 0;
            transform.localScale = originalScale;
        }
    }
}
