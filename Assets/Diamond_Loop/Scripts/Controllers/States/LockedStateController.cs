using UnityEngine;

namespace ClawbearGames
{
    public class LockedStateController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer lockedSprite;


        /// <summary>
        /// Initialize the frozen state.
        /// Enables the frozen overlay and plays the frozen particle effect.
        /// </summary>
        public void OnInit()
        {
            // Enable frozen overlay sprite
            if (lockedSprite != null)
                lockedSprite.enabled = true;
        }


        /// <summary>
        /// Breaks the frozen state.
        /// Stops visual effects and disables the frozen state object.
        /// </summary>
        public void BreakState()
        {
            if (lockedSprite != null)
                lockedSprite.enabled = false;

            // Disable this state
            gameObject.SetActive(false);
        }
    }
}

