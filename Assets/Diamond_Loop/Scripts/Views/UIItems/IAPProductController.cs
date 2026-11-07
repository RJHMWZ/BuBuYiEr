using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public class IAPProductController : MonoBehaviour
    {
        [SerializeField] private Text productNameText = null;
        [SerializeField] private Text productPriceText = null;
        [SerializeField] private Text coinProductAmount = null;
        [SerializeField] private Image coinProductIcon = null;
        [SerializeField] private RectTransform bestDealPanel = null;
        [SerializeField] private RectTransform coinProductPanel = null;
        [SerializeField] private RectTransform rewardProductPanel = null;
        [SerializeField] private IAPRewardItemController iAPRewardItemPrefab = null;


        private IAPProduct product = null;
        private List<IAPRewardItemController> listIAPRewardItem = new List<IAPRewardItemController>();



        /// <summary>
        /// Get an inactive IAPRewardItemController object.
        /// </summary>
        /// <returns></returns>
        public IAPRewardItemController GetIAPRewardItemController()
        {
            //Find in the list
            IAPRewardItemController rewardItem = null;
            for (int i = 0; i < listIAPRewardItem.Count; i++)
            {
                var candidate = listIAPRewardItem[i];
                if (candidate != null && !candidate.gameObject.activeSelf) { rewardItem = candidate; break; }
            }

            if (rewardItem == null)
            {
                //Did not find one -> create new one
                rewardItem = Instantiate(iAPRewardItemPrefab, Vector3.zero, Quaternion.identity);
                rewardItem.gameObject.SetActive(false);
                rewardItem.transform.SetParent(rewardProductPanel);
                rewardItem.transform.SetAsLastSibling();
                rewardItem.transform.localScale = Vector3.one;
                listIAPRewardItem.Add(rewardItem);
            }

            return rewardItem;
        }



        /// <summary>
        /// Init this product with IAPProductConfiguration.
        /// </summary>
        /// <param name="iAPProduct"></param>
        public void OnInit(IAPProduct iAPProduct)
        {
            product = iAPProduct;
            productNameText.text = iAPProduct.ProductName;
            bestDealPanel.gameObject.SetActive(product.BestDeal);
            productPriceText.text = ServicesManager.Instance.IAPManager.GetLocalizedPriceString(iAPProduct);

            if (product.ProductType == UnityEngine.Purchasing.ProductType.Consumable)
            {
                coinProductPanel.gameObject.SetActive(true);
                rewardProductPanel.gameObject.SetActive(false);

                coinProductIcon.sprite = product.Rewards[0].ProductIcon;
                coinProductAmount.text = "+" + product.Rewards[0].Amount.ToString();
            }
            else
            {
                coinProductPanel.gameObject.SetActive(false);
                rewardProductPanel.gameObject.SetActive(true);

                for(int i = 0; i < product.Rewards.Count; i++)
                {
                    ProductReward reward = product.Rewards[i];
                    IAPRewardItemController iAPRewardItem = GetIAPRewardItemController();
                    iAPRewardItem.gameObject.SetActive(true);
                    iAPRewardItem.OnInit(reward);
                }
            }
        }


        public void OnClickPurchaseButton()
        {
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Button);
            ServicesManager.Instance.IAPManager.PurchaseProduct(product, (iAPProduct) =>
            {
                ViewManager.Instance.IAPViewController.OnPurchasedProduct(product);

                //Add product id to data
                if (product.ProductType == UnityEngine.Purchasing.ProductType.NonConsumable)
                {
                    PlayerDataHandler.UpdatePurchasedProduct(product.ProductID);
                    foreach(ProductReward reward in product.Rewards)
                    {
                        if (reward.RewardType == RewardType.RemoveAds)
                        {
                            PlayerDataHandler.EnableRemoveAds();
                            ServicesManager.Instance.AdManager.HideBannerAd();
                        }
                        else if (reward.RewardType == RewardType.Slot)
                        {
                            PlayerDataHandler.UpdateSlotCount(reward.Amount);
                        }
                        else if (reward.RewardType == RewardType.Hammer)
                        {
                            PlayerDataHandler.UpdateHammerCount(reward.Amount);
                        }
                        else if(reward.RewardType == RewardType.Key)
                        {
                            PlayerDataHandler.UpdateKeyCount(reward.Amount);
                        }
                        else if (reward.RewardType == RewardType.Cooler)
                        {
                            PlayerDataHandler.UpdateCoolerCountt(reward.Amount);
                        }
                        else if (reward.RewardType == RewardType.Magnet)
                        {
                            PlayerDataHandler.UpdateMagnetCount(reward.Amount);
                        }
                    }

                    gameObject.SetActive(false);
                }
            });
        }
    }
}
