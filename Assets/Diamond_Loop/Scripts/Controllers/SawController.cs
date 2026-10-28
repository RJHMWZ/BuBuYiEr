using System;
using System.Collections;
using UnityEngine;

namespace ClawbearGames
{
    public class SawController : MonoBehaviour
    {
        [SerializeField] private AnimationCurve animationCurve = null;
        [SerializeField] private Transform sawTrans = null;
        [SerializeField] private Transform bladeTrans = null;
        [SerializeField] private ParticleSystem sparksParticle = null;

        private Vector3 initialPosition = Vector3.zero;
        private Quaternion initialRotation = Quaternion.identity;



        /// <summary>
        /// Coroutine handle saw animation.
        /// </summary>
        /// <param name="middleCallback"></param>
        /// <param name="endCallback"></param>
        /// <returns></returns>
        public IEnumerator CRSawAnim(Action middleCallback, Action endCallback)
        {
            StartCoroutine(CRBladeAnim());
            initialPosition = sawTrans.position;
            initialRotation = sawTrans.rotation;
            sparksParticle.Stop();

            float elapsedTime = 0f;
            Quaternion startRotation = sawTrans.rotation;
            Quaternion endRotation = Quaternion.Euler(-45f, 0f, 0f);
            float moveTime = 0.3f;
            while (elapsedTime < moveTime)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / moveTime;
                float curveT = animationCurve.Evaluate(t);
                sawTrans.rotation = Quaternion.Lerp(startRotation, endRotation, curveT);
                yield return null;
            }

            yield return new WaitForSeconds(0.15f);
            startRotation = transform.rotation;
            endRotation = Quaternion.Euler(0f, 0f, 0f);
            moveTime = 0.2f;
            elapsedTime = 0f;
            while (elapsedTime < moveTime)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / moveTime;
                float curveT = animationCurve.Evaluate(t);
                sawTrans.rotation = Quaternion.Lerp(startRotation, endRotation, curveT);
                yield return null;
            }

            middleCallback?.Invoke();
            sparksParticle.Play();
            yield return new WaitForSeconds(2f);
            endCallback?.Invoke();
            sawTrans.position = initialPosition;
            sawTrans.rotation = initialRotation;
            gameObject.SetActive(false);
        }




        /// <summary>
        /// Coroutine run the blade anim.
        /// </summary>
        /// <returns></returns>
        private IEnumerator CRBladeAnim()
        {
            bladeTrans.localEulerAngles = Vector3.zero;
            while (gameObject.activeSelf)
            {
                bladeTrans.localEulerAngles += Vector3.right * 150f * Time.deltaTime;
                yield return null;
            }
        }
    }
}
