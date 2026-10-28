using System;
using System.Collections;
using UnityEngine;

namespace ClawbearGames
{
    public class MagnetController : MonoBehaviour
    {
        [SerializeField] private AnimationCurve animationCurve = null;
        [SerializeField] private Transform magnetTrans = null;
        [SerializeField] private ParticleSystem leftSparks = null;
        [SerializeField] private ParticleSystem rightSparks = null;
        [SerializeField] private ParticleSystem squareFade = null;

        private Vector3 initialPosition = Vector3.zero;
        private Quaternion initialRotation = Quaternion.identity;



        /// <summary>
        /// Coroutine handle magnet animation.
        /// </summary>
        /// <returns></returns>
        public IEnumerator CRMagnetAnim(Action callback)
        {
            initialPosition = magnetTrans.localPosition;
            initialRotation = magnetTrans.localRotation;
            leftSparks.Stop();
            rightSparks.Stop();
            squareFade.Play();

            float elapsedTime = 0f;
            Vector3 startScale = Vector3.zero;
            Vector3 endScale = Vector3.one;
            float moveTime = 0.3f;
            while (elapsedTime < moveTime)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / moveTime;
                float curveT = animationCurve.Evaluate(t);
                magnetTrans.localScale = Vector3.Lerp(startScale, endScale, curveT);
                yield return null;
            }

            leftSparks.Play();
            rightSparks.Play();

            yield return new WaitForSeconds(0.15f);
            float shakeDuration = 1f;
            float shakeAmount = 0.1f;
            Vector3 originalPos = magnetTrans.localPosition;
            while (shakeDuration > 0)
            {
                Vector3 newPos = originalPos + UnityEngine.Random.insideUnitSphere * shakeAmount;
                newPos.y = originalPos.y;
                magnetTrans.localPosition = newPos;
                shakeDuration -= Time.deltaTime;
                yield return null;
            }
            magnetTrans.localPosition = originalPos;


            callback?.Invoke();
            magnetTrans.localPosition = initialPosition;
            magnetTrans.localRotation = initialRotation;
            gameObject.SetActive(false);
        }
    }
}
