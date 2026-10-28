using System;
using System.Collections;
using UnityEngine;

namespace ClawbearGames
{
    public class HammerController : MonoBehaviour
    {
        [SerializeField] private AnimationCurve animationCurve = null;
        [SerializeField] private Transform hammerTrans = null;

        private Vector3 initialPosition = Vector3.zero;
        private Quaternion initialRotation = Quaternion.identity;



        /// <summary>
        /// Coroutine handle hammer animation.
        /// </summary>
        /// <returns></returns>
        public IEnumerator CRHammerAnim(Action callback)
        {
            initialPosition = hammerTrans.position;
            initialRotation = hammerTrans.rotation;

            float elapsedTime = 0f;
            Quaternion startRotation = hammerTrans.rotation;
            Quaternion endRotation = Quaternion.Euler(-20f, -35f, 0f);
            float moveTime = 0.3f;
            while (elapsedTime < moveTime)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / moveTime;
                float curveT = animationCurve.Evaluate(t);
                hammerTrans.rotation = Quaternion.Lerp(startRotation, endRotation, curveT);
                yield return null;
            }

            yield return new WaitForSeconds(0.15f);
            startRotation = transform.rotation;
            endRotation = Quaternion.Euler(90f, -35f, 0f);
            moveTime = 0.2f;
            elapsedTime = 0f;
            while (elapsedTime < moveTime)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / moveTime;
                float curveT = animationCurve.Evaluate(t);
                hammerTrans.rotation = Quaternion.Lerp(startRotation, endRotation, curveT);
                yield return null;
            }

            callback?.Invoke();
            hammerTrans.position = initialPosition;
            hammerTrans.rotation = initialRotation;
            gameObject.SetActive(false);
        }
    }
}
