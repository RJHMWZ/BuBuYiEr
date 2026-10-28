using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public class PurchasedItemController : MonoBehaviour
    {
        [SerializeField] private Image productIcon = null;
        [SerializeField] private Text amountText = null;




        /// <summary>
        /// Init this purchased item.
        /// </summary>
        /// <param name="reward"></param>
        public void OnInit(ProductReward reward)
        {
            if (reward.RewardType == RewardType.RemoveAds)
            {
                amountText.text = "x1";
            }

            productIcon.sprite = reward.ProductIcon;
            StartCoroutine(CRAnimateText(reward.Amount));
        }



        /// <summary>
        /// Coroutine animate the amount text.
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        private IEnumerator CRAnimateText(int amount)
        {
            float t = 0;
            int current = 0;
            float animateTime = 0.2f;
            while (t < animateTime)
            {
                t += Time.deltaTime;
                float factor = t / animateTime;
                int newAmount = (int)Mathf.Lerp(current, amount, factor);
                amountText.text = "+" + newAmount.ToString();
                yield return null;
            }
            amountText.text = "+" + amount.ToString();
        }
    }
}
