using System.IO;
using System.Collections;
using UnityEngine;

namespace ClawbearGames
{
    public class ShareManager : MonoBehaviour
    {
        [Header("App Url Configuration")]
        [SerializeField] private string appUrl = "https://play.google.com/store/apps/details?id=com.BrightFuture.KnifeShooter";

        public string AppUrl { get { return appUrl; } }
    }
}