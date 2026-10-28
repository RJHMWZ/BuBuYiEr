using System;
using System.Collections;
using UnityEngine;

namespace ClawbearGames
{
    public class CubeController : MonoBehaviour
    {
        [SerializeField] private MeshRenderer meshRenderer = null;

        public Color32 Color { get; private set; }
        public bool IsLocked { get; private set; }
        public bool IsJumpPending => canJump;

        private Coroutine activeCoroutine = null;
        private Vector3 fallTarget = Vector3.zero;
        private Action onFallComplete = null;
        private Action removeFromColumn = null;
        private float fallVelocity = 0f;
        private float jumpDelay = 0f;
        private bool canJump = false;
        private bool isFalling = false;


        /// <summary>
        /// Init this cube with params.
        /// </summary>
        /// <param name="color"></param>
        public void Init(Color32 color)
        {
            // Stop any leftover coroutine from pool reuse
            if (activeCoroutine != null)
            {
                StopCoroutine(activeCoroutine);
                activeCoroutine = null;
            }

            Color = color;
            canJump = false;
            IsLocked = false;
            isFalling = false;
            fallVelocity = 0f;
            onFallComplete = null;
            removeFromColumn = null;
            MaterialPropertyBlock block = IngameManager.Instance.MPB;
            block.SetColor("_Color", color);
            meshRenderer.SetPropertyBlock(block);
        }


        /// <summary>
        /// Called by CubeManager to mark this as the active bottom cube.
        /// removeCallback removes it from the column list the moment it claims a pot.
        /// </summary>
        public void SetCanJump(Action removeCallback, float delay = 0f)
        {
            if (IsLocked) return;
            if (canJump) return;
            removeFromColumn = removeCallback;
            jumpDelay = delay;
            canJump = true;
        }



        /// <summary>
        /// Coroutine jump to target
        /// </summary>
        /// <param name="target"></param>
        /// <param name="onArrived"></param>
        /// <returns></returns>
        private IEnumerator JumpCoroutine(Transform target, Action onArrived)
        {
            Vector3 start = transform.position;
            float duration = 0.4f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                if (target == null || !target.gameObject.activeInHierarchy)
                {
                    Destroy(gameObject);
                    yield break;
                }

                Vector3 pos = Vector3.Lerp(start, target.position, t);
                pos.y += Mathf.Sin(t * Mathf.PI) * 1.5f;
                transform.position = pos;

                yield return null;
            }

            onArrived?.Invoke();
            Destroy(gameObject);
        }



        /// <summary>
        /// Fall to target
        /// </summary>
        /// <param name="targetWorldPos"></param>
        /// <param name="onComplete"></param>
        public void FallTo(Vector3 targetWorldPos, Action onComplete = null)
        {
            // Stop any slide still in progress
            if (activeCoroutine != null)
            {
                StopCoroutine(activeCoroutine);
                activeCoroutine = null;
            }


            if (isFalling)
            {
                // Fire the old callback so the counter doesn't get orphaned
                onFallComplete?.Invoke();
                onFallComplete = null;
            }

            // If already at target (or above it in Z), snap immediately and return
            if (targetWorldPos.z >= transform.position.z)
            {
                transform.position = targetWorldPos;
                IsLocked = false;
                onComplete?.Invoke();
                return;
            }

            IsLocked = true;
            fallTarget = targetWorldPos;
            fallVelocity = 0f;         // starts from rest, gravity accelerates it
            isFalling = true;
            onFallComplete = onComplete;
        }


        /// <summary>
        /// Smooth diagonal slide to an exact world position (used by sand-slide pass).
        /// Locks the cube during animation so it can't be stamped or re-migrated.
        /// </summary>
        public void SlideTo(Vector3 targetWorldPos, Action onComplete = null)
        {
            // Stop any previous animation
            if (activeCoroutine != null)
            {
                StopCoroutine(activeCoroutine);
                activeCoroutine = null;
            }

            IsLocked = true;
            isFalling = false;
            fallVelocity = 0f;
            activeCoroutine = StartCoroutine(SlideCoroutine(targetWorldPos, onComplete));
        }

        private IEnumerator SlideCoroutine(Vector3 target, Action onComplete)
        {
            Vector3 start = transform.position;
            float duration = 0.15f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                transform.position = Vector3.Lerp(start, target, t);
                yield return null;
            }

            transform.position = target;
            activeCoroutine = null;
            IsLocked = false;
            onComplete?.Invoke();
        }



        private void Update()
        {
            // ── Fall physics ──────────────────────────────────────────────
            if (isFalling)
            {
                fallVelocity += 55f * Time.deltaTime;
                Vector3 pos = transform.position;
                float newZ = pos.z - fallVelocity * Time.deltaTime;

                if (newZ <= fallTarget.z)
                {
                    isFalling = false;
                    IsLocked = false;
                    transform.position = fallTarget;
                    activeCoroutine = null;
                    onFallComplete?.Invoke();
                    onFallComplete = null;
                }
                else
                {
                    transform.position = new Vector3(pos.x, pos.y, newZ);
                }
            }

            // ── Pot scan — only when stamped as bottom cube and free ──────
            if (canJump && !IsLocked)
            {
                if (jumpDelay > 0f)
                {
                    jumpDelay -= Time.deltaTime;
                    return;
                }

                PotController pot = FindMatchingPot();
                if (pot != null)
                {
                    canJump = false;
                    removeFromColumn?.Invoke();   // column updated instantly, sync
                    removeFromColumn = null;
                    IsLocked = true;
                    activeCoroutine = StartCoroutine(JumpCoroutine(pot.transform, () =>
                    {
                        activeCoroutine = null;
                        pot.OnCubeArrived();
                    }));
                }
            }
        }



        /// <summary>
        /// Find matching pot
        /// </summary>
        /// <returns></returns>
        private PotController FindMatchingPot()
        {
            PotController best = null;
            float minDist = 100f;

            foreach (PotController pot in IngameManager.Instance.GridManager.AllPots)
            {
                if (pot == null) continue;
                if (!pot.IsOnConveyor || pot.IsFilled || !pot.CanCollect) continue;
                if (pot.RemainingCapacity <= 0) continue;
                if (!LevelData.Color32Equals(Color, pot.TargetColor)) continue;

                // X-axis proximity check — cube only claims pots directly above/below it
                float xDist = Mathf.Abs(transform.position.x - pot.transform.position.x);
                if (xDist > 1f) continue;

                if (xDist < minDist)
                {
                    minDist = xDist;
                    best = pot;
                }
            }

            // Reserve atomically — if TryReserve fails another cube beat us this frame
            if (best != null && !best.TryReserve())
                best = null;

            return best;
        }
    }
}
