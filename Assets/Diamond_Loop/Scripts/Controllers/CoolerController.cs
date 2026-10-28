using System;
using System.Collections;
using UnityEngine;

namespace ClawbearGames
{
    public class CoolerController : MonoBehaviour
    {
        [SerializeField] private AnimationCurve animationCurve = null;
        [SerializeField] private Transform coolerTrans = null;
        [SerializeField] private ParticleSystem particle = null;


        private Vector3 initialPosition = Vector3.zero;
        private Quaternion initialRotation = Quaternion.identity;



        /// <summary>
        /// Coroutine handle cooler animation.
        /// </summary>
        /// <returns></returns>
        public IEnumerator CRCoolerAnim(Action callback)
        {
            initialPosition = coolerTrans.position;
            initialRotation = coolerTrans.localRotation;
            particle.Stop();

            float elapsedTime = 0f;
            Quaternion startlocalRotation = coolerTrans.localRotation;
            Quaternion endlocalRotation = Quaternion.Euler(-20f, -70f, 0f);
            float moveTime = 0.3f;
            while (elapsedTime < moveTime)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / moveTime;
                float curveT = animationCurve.Evaluate(t);
                coolerTrans.localRotation = Quaternion.Lerp(startlocalRotation, endlocalRotation, curveT);
                yield return null;
            }

            yield return new WaitForSeconds(0.15f);
            startlocalRotation = transform.localRotation;
            endlocalRotation = Quaternion.Euler(40f, -70f, 0f);
            moveTime = 0.2f;
            elapsedTime = 0f;
            while (elapsedTime < moveTime)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / moveTime;
                float curveT = animationCurve.Evaluate(t);
                coolerTrans.localRotation = Quaternion.Lerp(startlocalRotation, endlocalRotation, curveT);
                yield return null;
            }

            particle.Play();
            yield return new WaitForSeconds(1f);

            callback?.Invoke();
            coolerTrans.position = initialPosition;
            coolerTrans.localRotation = initialRotation;
            gameObject.SetActive(false);
        }
    }
}
