using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public class DecayStateController : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer flamesRenderer = null;
        [SerializeField] private Text countdownText = null;
        [SerializeField] private Sprite[] flamesSprites = null;

        public bool ActiveCount { set { activeCount = value; } }

        private PotController owner = null;
        private Coroutine countdownCoroutine = null;
        private Coroutine flamesAnimCoroutine = null;
        private float flamesAnimSpeed = 0.1f;
        private float timeRemaining = 0f;
        private float decayTime = 0f;
        private bool activeCount = true;


        /// <summary>
        /// Initialize the decay state for a pot object.
        /// Sets the owner, decay count, updates UI, and starts flame animation.
        /// </summary>
        /// <param name="ownerPot">The BlockController that owns this decay state.</param>
        /// <param name="time">Initial number of time before the pot is destroyed.</param>
        public void OnInit(PotController ownerPot, float time)
        {
            owner = ownerPot;
            decayTime = time;

            //Start flames animation
            if (flamesRenderer != null && flamesSprites != null && flamesSprites.Length > 0)
            {
                flamesRenderer.gameObject.SetActive(true);
                flamesAnimCoroutine = StartCoroutine(CRAnimateFlames());
            }

            // Start countdown
            countdownCoroutine = StartCoroutine(CRCountdownTimer());
        }


        // <summary>
        // Coroutine that animates the flame sprites in a loop while the decay state is active.
        // </summary>
        // <returns>IEnumerator for coroutine execution.</returns>
        private IEnumerator CRAnimateFlames()
        {
            int index = 0;
            while (gameObject.activeSelf)
            {
                flamesRenderer.sprite = flamesSprites[index];
                index = (index + 1) % flamesSprites.Length;
                yield return new WaitForSeconds(flamesAnimSpeed);
            }
        }


        /// <summary>
        /// Coroutine that runs the countdown timer for the bomb.
        /// Updates the UI text each second and triggers the explosion when time runs out.
        /// </summary>
        /// <returns>IEnumerator for coroutine execution.</returns>
        private IEnumerator CRCountdownTimer()
        {
            timeRemaining = decayTime;

            while (timeRemaining > 0)
            {
                if (countdownText != null)
                    countdownText.text = Mathf.CeilToInt(timeRemaining).ToString();

                yield return new WaitForSeconds(1f);
                timeRemaining--;

                while (!activeCount) { yield return null; }
            }

            if (countdownText != null)
                countdownText.text = "0";

            if (owner == null) yield break;
            owner.PotDecayed();
            BreakState();
        }



        /// <summary>
        /// Deactivates the decay state object.
        /// </summary>
        public void BreakState()
        {
            if (countdownCoroutine != null)
            {
                StopCoroutine(countdownCoroutine);
                countdownCoroutine = null;
            }

            if (flamesAnimCoroutine != null)
            {
                StopCoroutine(flamesAnimCoroutine);
                flamesAnimCoroutine = null;
            }
            gameObject.SetActive(false);
        }
    }
}

