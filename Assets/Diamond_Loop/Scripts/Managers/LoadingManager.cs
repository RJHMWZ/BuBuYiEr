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
            Time.timeScale = 1f;
            ViewManager.Instance.OnShowView(ViewType.LOADING_VIEW);
            StartCoroutine(CRLoadScene());
        }



        /// <summary>
        /// Coroutine load the targetScene using LoadSceneAsync.
        /// </summary>
        /// <returns></returns>
        private IEnumerator CRLoadScene()
        {
            string scene = string.IsNullOrEmpty(targetScene) || !Application.CanStreamedLevelBeLoaded(targetScene) ? "Home" : targetScene;
            AsyncOperation operation = SceneManager.LoadSceneAsync(scene);
            operation.allowSceneActivation = false;
            float displayed = 0f;
            while (displayed < 1f)
            {
                float progress = Mathf.Clamp01(operation.progress / .9f);
                displayed = Mathf.MoveTowards(displayed, progress, Time.unscaledDeltaTime * 4f);
                ViewManager.Instance.LoadingViewController.SetLoadingAmount(displayed);
                yield return null;
            }
            operation.allowSceneActivation = true;
            yield return operation;
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
