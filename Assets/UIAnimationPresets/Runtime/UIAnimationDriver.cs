using UnityEngine;

namespace UIAnimationPresets
{
    [DefaultExecutionOrder(10000)]
    [AddComponentMenu("")]
    public sealed class UIAnimationDriver : MonoBehaviour
    {
        private bool quitting;
        private void LateUpdate() => UIAnimationManager.Tick(Time.deltaTime, Time.unscaledDeltaTime);
        private void OnApplicationQuit() { quitting = true; UIAnimationManager.Shutdown(true); }
        private void OnDestroy() => UIAnimationManager.Shutdown(quitting);
    }
}
