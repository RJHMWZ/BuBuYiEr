using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace ClawbearGames
{
    public class LoadingManager : MonoBehaviour
    {
        private static string targetScene = string.Empty;

        private void Start()
        {
            ViewManager.Instance.OnShowView(ViewType.LOADING_VIEW);
            StartCoroutine(CRLoadScene());
        }



        /// <summary>
        /// Coroutine load the targetScene using LoadSceneAsync.
        /// </summary>
        /// <returns></returns>
        private IEnumerator CRLoadScene()
        {
            float loadingAmount = 0f;
            while (loadingAmount < 0.95f)
            {
                yield return new WaitForSeconds(0.01f);
                loadingAmount += 0.02f;
                ViewManager.Instance.LoadingViewController.SetLoadingAmount(loadingAmount);
            }

            AsyncOperation asyn = SceneManager.LoadSceneAsync(targetScene);
            while (!asyn.isDone)
            {
                yield return null;
            }
        }

        /// <summary>
        /// Set target scene.
        /// </summary>
        /// <param name="sceneName"></param>
        public static void SetTargetScene(string sceneName)
        {
            targetScene = sceneName;
        }
    }
}
