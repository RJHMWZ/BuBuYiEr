using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ClawbearGames
{
    public class EndGameViewController : BaseViewController
    {
        [SerializeField] private CanvasGroup mainCanvasGroup = null;
        [SerializeField] private RectTransform topPanelTrans = null;
        [SerializeField] private RectTransform buttonsPanelTrans = null;
        [SerializeField] private RectTransform bottonPanelTrans = null;
        [SerializeField] private RectTransform nextLevelButtonTrans = null;
        [SerializeField] private RectTransform replayLevelButtonTrans = null;
        [SerializeField] private Text currentLevelText = null;
        [SerializeField] private Text totalCoinsText = null;
        [SerializeField] private ResultPanelController resultPanel = null;
        [SerializeField] private CoinEffectController coinEffectControllerPrefab = null;

        private List<CoinEffectController> listCoinEffectController = new List<CoinEffectController>();



        /// <summary>
        /// Get an inactive CoinEffectController object.
        /// </summary>
        /// <returns></returns>
        public CoinEffectController GetCoinEffectController()
        {
            //Find in the list
            CoinEffectController coinEffect = listCoinEffectController.Where(a => !a.gameObject.activeSelf).FirstOrDefault();

            if (coinEffect == null)
            {
                //Did not find one -> create new one
                coinEffect = Instantiate(coinEffectControllerPrefab, Vector3.zero, Quaternion.identity);
                coinEffect.transform.SetParent(transform);
                coinEffect.transform.SetAsLastSibling();
                coinEffect.gameObject.SetActive(false);
                listCoinEffectController.Add(coinEffect);
            }

            return coinEffect;
        }



        private void CoinManager_TotalCoinChanged(int obj)
        {
            totalCoinsText.text = obj.ToString();
        }


        /// <summary>
        ////////////////////////////////////////////////// Public Functions
        /// </summary>


        public override void OnShow()
        {
            FadeInCanvasGroup(mainCanvasGroup, 0.75f);
            MoveRectTransform(topPanelTrans, topPanelTrans.anchoredPosition, new Vector2(topPanelTrans.anchoredPosition.x, -10f), 0.5f);
            MoveRectTransform(bottonPanelTrans, bottonPanelTrans.anchoredPosition, new Vector2(bottonPanelTrans.anchoredPosition.x, 0f), 0.5f);
            ScaleRectTransform(buttonsPanelTrans, Vector3.zero, Vector3.one, 0.5f);
            ServicesManager.Instance.CoinManager.TotalCoinChanged += CoinManager_TotalCoinChanged;
            resultPanel.gameObject.SetActive(true);
            resultPanel.OnShow();


            //Update texts and others
            currentLevelText.text = "Level: " + IngameManager.Instance.CurrentLevel.ToString();
            totalCoinsText.text = ServicesManager.Instance.CoinManager.TotalCoins.ToString();
            bool levelcompleted = IngameManager.Instance.IngameState == IngameState.Ingame_CompleteLevel;
            replayLevelButtonTrans.gameObject.SetActive(!levelcompleted);
            nextLevelButtonTrans.gameObject.SetActive(levelcompleted);

            //Disable all coin effects
            foreach (CoinEffectController cashEffect in listCoinEffectController)
            {
                cashEffect.gameObject.SetActive(false);
            }
        }

        public override void OnClose()
        {
            mainCanvasGroup.alpha = 0;
            buttonsPanelTrans.localScale = Vector3.zero;
            resultPanel.gameObject.SetActive(false);
            topPanelTrans.anchoredPosition = new Vector2(topPanelTrans.anchoredPosition.x, 300f);
            bottonPanelTrans.anchoredPosition = new Vector2(bottonPanelTrans.anchoredPosition.x, -500f);
            ServicesManager.Instance.CoinManager.TotalCoinChanged -= CoinManager_TotalCoinChanged;
            gameObject.SetActive(false);
        }




        /// <summary>
        ////////////////////////////////////////////////// UI Buttons
        /// </summary>  


        public void OnClickPlayButton()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (GameDebugService.IsPreviewLevel)
            {
                if (IngameManager.Instance.IngameState == IngameState.Ingame_CompleteLevel)
                    GameDebugService.LoadAdjacentLevel(1);
                else
                    GameDebugService.RetryLevel();
                return;
            }
#endif
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Button);
            LoadScene("Ingame", 0.25f);
        }

        public void OnClickRateAppButton()
        {
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Button);
            Application.OpenURL(ServicesManager.Instance.ShareManager.AppUrl);
        }

        public void OnClickShopButton()
        {
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Button);
            ViewManager.Instance.OnShowView(ViewType.IAP_VIEW);
        }

        public void OnClickHomeButton()
        {
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Button);
            LoadScene("Home", 0.25f);
        }
    }
}
