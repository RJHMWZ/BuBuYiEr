using System;
using System.Collections;
using UnityEngine;

namespace ClawbearGames
{
    public class KeyController : MonoBehaviour
    {
        [SerializeField] private AnimationCurve animationCurve = null;
        [SerializeField] private Transform keyTrans = null;
        [SerializeField] private ParticleSystem sparksParticle = null;

        private Vector3 initialPosition = Vector3.zero;
        private Quaternion initialRotation = Quaternion.identity;



        /// <summary>
        /// Coroutine handle key animation.
        /// </summary>
        /// <returns></returns>
        public IEnumerator CRKeyAnim(Action callback)
        {
            initialPosition = keyTrans.localPosition;
            initialRotation = keyTrans.localRotation;
            sparksParticle.Stop();

            yield return new WaitForSeconds(0.15f);
            Quaternion startRotation = keyTrans.localRotation;
            Vector3 startPosition = keyTrans.localPosition;
            Vector3 endPosition = new Vector3(startPosition.x, 1.5f, startPosition.z);

            float startEulerY = keyTrans.localEulerAngles.y;
            float totalRotation = 720f; // 2 full rounds

            float moveTime = 0.75f;
            float elapsedTime = 0f;

            while (elapsedTime < moveTime)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / moveTime);
                float curveT = animationCurve.Evaluate(t);

                keyTrans.localPosition = Vector3.Lerp(startPosition, endPosition, curveT);

                float currentY = startEulerY + totalRotation * curveT;
                keyTrans.localRotation = Quaternion.Euler(0f, currentY, 0f);

                yield return null;
            }

            EffectManager.Instance.PlayLockedUnlockEffect(transform.position + Vector3.up);
            yield return new WaitForSeconds(1f);
            callback?.Invoke();
            keyTrans.localPosition = initialPosition;
            keyTrans.localRotation = initialRotation;
            gameObject.SetActive(false);
        }
    }
}
