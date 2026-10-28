using UnityEngine;

namespace ClawbearGames
{
    public class CameraRotator : MonoBehaviour
    {

        private void Update()
        {
            transform.eulerAngles += Vector3.down * 10f * Time.deltaTime;
        }
    }
}
