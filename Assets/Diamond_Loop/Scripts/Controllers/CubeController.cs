using UnityEngine;

namespace ClawbearGames
{
    public class CubeController : MonoBehaviour
    {
        [SerializeField] private MeshRenderer meshRenderer = null;
        private enum Motion { Idle, Fall, Slide, Collect }
        private Motion motion;
        private Transform cachedTransform;
        private Vector3 startPosition, destination;
        private PotController collectingPot;
        private float elapsed, duration, fallVelocity;
        internal bool MotionRegistered;
        internal bool IsSliding => motion == Motion.Slide;
        internal bool IsCollecting => motion == Motion.Collect;
        public Color32 Color { get; private set; }
        public bool IsLocked => motion != Motion.Idle;
        public bool IsJumpPending => motion == Motion.Collect;

        public void Init(Color32 color)
        {
            cachedTransform = transform;
            motion = Motion.Idle;
            MotionRegistered = false;
            collectingPot = null;
            fallVelocity = elapsed = 0;
            Color = color;
            BearGameTheme.ApplyPixelMaterial(meshRenderer);
            MaterialPropertyBlock block = IngameManager.Instance.MPB;
            block.SetColor("_Color", color);
            meshRenderer.SetPropertyBlock(block);
        }

        public void FallTo(Vector3 target)
        {
            if (motion == Motion.Collect || motion == Motion.Slide) return;
            if ((cachedTransform.position - target).sqrMagnitude <= .000001f) return;
            if (motion != Motion.Fall) fallVelocity = 0;
            destination = target;
            motion = Motion.Fall;
        }

        public void SlideTo(Vector3 target)
        {
            startPosition = cachedTransform.position;
            destination = target;
            duration = Mathf.Clamp(Vector3.Distance(startPosition, target) / 6.5f, .12f, .32f);
            elapsed = 0;
            fallVelocity = 0;
            motion = Motion.Slide;
        }

        public void CollectInto(PotController pot)
        {
            collectingPot = pot;
            startPosition = cachedTransform.position;
            destination = pot.CollectionPosition;
            duration = Mathf.Clamp(Vector3.Distance(startPosition, destination) / 13f, .24f, .36f);
            elapsed = 0;
            motion = Motion.Collect;
        }

        // Called once by CubeManager, and only while this cube is moving.
        internal bool Advance(float deltaTime)
        {
            if (motion == Motion.Idle) return true;
            if (motion == Motion.Fall)
            {
                float distance = fallVelocity * deltaTime + 16f * deltaTime * deltaTime;
                fallVelocity = Mathf.Min(6.5f, fallVelocity + 32f * deltaTime);
                cachedTransform.position = Vector3.MoveTowards(cachedTransform.position, destination, distance);
                if ((cachedTransform.position - destination).sqrMagnitude > .000001f) return false;
                cachedTransform.position = destination;
                motion = Motion.Idle;
                return true;
            }
            elapsed += deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (motion == Motion.Slide)
            {
                cachedTransform.position = Vector3.LerpUnclamped(startPosition, destination, t * t * (3 - 2 * t));
                if (t < 1) return false;
                motion = Motion.Idle;
                return true;
            }
            if (collectingPot == null || !collectingPot.gameObject.activeInHierarchy)
            {
                Release();
                return true;
            }
            // Follow the actual illustrated bottle opening, including conveyor movement.
            destination = collectingPot.CollectionPosition;
            Vector3 control = (startPosition + destination) * .5f + Vector3.up * .8f;
            float inverse = 1 - t;
            cachedTransform.position = inverse * inverse * startPosition + 2 * inverse * t * control + t * t * destination;
            if (t < 1) return false;
            PotController target = collectingPot;
            collectingPot = null;
            target.OnCubeArrived();
            Release();
            return true;
        }

        private void Release()
        {
            motion = Motion.Idle;
            collectingPot = null;
            PoolManager pool = PoolManager.Instance;
            if (pool != null) pool.ReleaseCubeController(this);
            else gameObject.SetActive(false);
        }
    }
}
