using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public class SettingPanelController : MonoBehaviour
    {
        [SerializeField] private RectTransform mainPanelTrans = null;
        [SerializeField] private RectTransform soundOnTrans = null;
        [SerializeField] private RectTransform soundOffTrans = null;
        [SerializeField] private RectTransform musicOnTrans = null;
        [SerializeField] private RectTransform musicOffTrans = null;
        [SerializeField] private Slider soundSlider = null;
        [SerializeField] private Slider musicSlider = null;


        public void OnShow()
        {
            ScaleRectTransform(mainPanelTrans, Vector2.zero, Vector2.one, 0.5f);
            soundSlider.value = PlayerDataHandler.GetSoundValue();
            musicSlider.value = PlayerDataHandler.GetMusicValue();
            UpdateImages();
        }


        public void OnHide()
        {
            mainPanelTrans.localScale = Vector2.zero;
            gameObject.SetActive(false);
        }



        public void OnSoundValueChanged()
        {
            float value = soundSlider.value;
            PlayerDataHandler.UpdateSoundValue(value);
            ServicesManager.Instance.SoundManager.UpdateVolumes();
            UpdateImages();
        }



        public void OnMusicValueChanged()
        {
            float value = musicSlider.value;
            PlayerDataHandler.UpdateMusicValue(value);
            ServicesManager.Instance.SoundManager.UpdateVolumes();
            UpdateImages();
        }




        public void OnClickLeaderboardButton()
        {
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Button);
            ViewManager.Instance.OnShowView(ViewType.LEADERBOARD_VIEW);
        }

        public void OnClickRateAppButton()
        {
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Button);
            Application.OpenURL(ServicesManager.Instance.ShareManager.AppUrl);
        }
        public void OnClickRemoveAdsButton()
        {
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Button);
        }

        public void OnClickCloseButton()
        {
            ServicesManager.Instance.SoundManager.PlaySound(ServicesManager.Instance.SoundManager.Button);
            OnHide();
        }



        /// <summary>
        /// Update the sound and music images.
        /// </summary>
        private void UpdateImages()
        {
            //Update sound images
            if (soundSlider.value == 0)
            {
                soundOnTrans.gameObject.SetActive(false);
                soundOffTrans.gameObject.SetActive(true);
            }
            else
            {
                soundOnTrans.gameObject.SetActive(true);
                soundOffTrans.gameObject.SetActive(false);
            }

            //Update music images
            if (musicSlider.value == 0)
            {
                musicOnTrans.gameObject.SetActive(false);
                musicOffTrans.gameObject.SetActive(true);
            }
            else
            {
                musicOnTrans.gameObject.SetActive(true);
                musicOffTrans.gameObject.SetActive(false);
            }
        }


        /// <summary>
        /// Scale the given ViewTransform from startScale to endScale with scalingTime.
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="startScale"></param>
        /// <param name="endScale"></param>
        /// <param name="scalingTime"></param>
        public void ScaleRectTransform(RectTransform rect, Vector2 startScale, Vector2 endScale, float scalingTime)
        {
            StartCoroutine(CRScaleRect(rect, startScale, endScale, scalingTime));
        }

        /// <summary>
        /// Coroutine scale the given ViewTransform.
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="startScale"></param>
        /// <param name="endScale"></param>
        /// <param name="scalingTime"></param>
        /// <returns></returns>
        private IEnumerator CRScaleRect(RectTransform rect, Vector2 startScale, Vector2 endScale, float scalingTime)
        {
            rect.localScale = startScale;
            float t = 0;
            while (t < scalingTime)
            {
                t += Time.deltaTime;
                float factor = EasyType.MatchedLerpType(LerpType.EaseOutQuart, t / scalingTime);
                rect.localScale = Vector2.Lerp(startScale, endScale, factor);
                yield return null;
            }
        }
    }
}
