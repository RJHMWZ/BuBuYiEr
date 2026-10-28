using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ClawbearGames
{
    public class CameraParentController : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private float shakeDuration = 0.5f;
        [SerializeField] private float shakeAmount = 0.25f;
        [SerializeField] private float decreaseFactor = 1.5f;
        [SerializeField] private CameraConfig[] cameraConfigs = null;


        [Header("Camera References")]
        [SerializeField] private Camera cam = null;

        public Transform CameraTrans => cam.transform;
        public Camera MainCamera => cam;


        /// <summary>
        /// Shake this camera.
        /// </summary>
        /// <param name="delay"></param>
        public void Shake(float delay)
        {
            StartCoroutine(CRShake(delay));
        }


        /// <summary>
        /// Coroutine skae this camera.
        /// </summary>
        /// <param name="delay"></param>
        /// <returns></returns>
        private IEnumerator CRShake(float delay)
        {
            yield return new WaitForSeconds(delay);
            Vector3 originalPos = transform.position;
            float shakeDurationTemp = shakeDuration;
            while (shakeDurationTemp > 0)
            {
                Vector3 newPos = originalPos + UnityEngine.Random.insideUnitSphere * shakeAmount;
                newPos.z = originalPos.z;
                transform.position = newPos;
                shakeDurationTemp -= Time.deltaTime * decreaseFactor;
                yield return null;
            }
            transform.position = originalPos;
        }


        public void SetCameraWithLevel(int gridWidth)
        {
            foreach(CameraConfig config in cameraConfigs)
            {
                if(config.GridWidth == gridWidth)
                {
                    Vector3 pos = Vector3.zero;
                    pos.x = cam.transform.position.x;
                    pos.y = config.CamYPos;
                    pos.z = config.CamZPos;
                    cam.transform.position = pos;
                    break;
                }    
            }    
        }    
    }
}
