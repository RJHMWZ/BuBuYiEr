using UnityEngine;

namespace ClawbearGames
{
    public class FrozenStateController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer frozenSprite;


        /// <summary>
        /// Initialize the frozen state.
        /// Enables the frozen overlay and plays the frozen particle effect.
        /// </summary>
        public void OnInit()
        {
            // Enable frozen overlay sprite
            if (frozenSprite != null)
                frozenSprite.enabled = true;
        }


        /// <summary>
        /// Breaks the frozen state.
        /// Stops visual effects and disables the frozen state object.
        /// </summary>
        public void BreakState()
        {
            if (frozenSprite != null)
                frozenSprite.enabled = false;

            // Disable this state
            gameObject.SetActive(false);
        }
    }
}
