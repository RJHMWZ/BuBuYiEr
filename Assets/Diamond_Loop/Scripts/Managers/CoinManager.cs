using System;
using System.Collections;
using UnityEngine;

namespace ClawbearGames
{
    public class CoinManager : MonoBehaviour
    {
        public event System.Action<int> TotalCoinChanged = delegate { };
        public event System.Action<int> CollectedCoinChanged = delegate { };

        [Header("Coin Manager Configuration")]
        [SerializeField] private int initialCoins = 0;
        [SerializeField] private int minRewardedCoins = 100;
        [SerializeField] private int maxRewardedCoins = 150;

        public int InitialCoins => initialCoins;
        public int CollectedCoins { private set; get; }


        public int TotalCoins
        {
            private set
            {
                PlayerDataHandler.UpdateTotalCoin(value);
                TotalCoinChanged?.Invoke(value);
            }
            get { return PlayerDataHandler.GetTotalCoin(); }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void DebugSetTotalCoins(int amount)
        {
            // A delayed reward animation must not overwrite a debug edit/restore later.
            StopAllCoroutines();
            TotalCoins = amount;
        }
#endif


        /// <summary>
        /// Set the CollectedCoins by the given amount.
        /// </summary>
        /// <param name="amount"></param>
        public void SetCollectedCoins(int amount)
        {
            CollectedCoins = amount;
            CollectedCoinChanged?.Invoke(CollectedCoins);
        }


        /// <summary>
        /// Get an amount of coins to reward to user.
        /// </summary>
        /// <returns></returns>
        public int GetRewardedCoins()
        {
            return UnityEngine.Random.Range(minRewardedCoins, maxRewardedCoins) / 5 * 5;
        }




        /// <summary>
        /// Add an amount of coins to TotalCoins with given delay time and call back.
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="delay"></param>
        /// <param name="callback"></param>
        public void AddTotalCoins(int amount, float delay, Action callback = null)
        {
            int startCoins = TotalCoins;
            int endCoins = TotalCoins + amount;
            StartCoroutine(CRUpdateCoins(0, startCoins, endCoins, delay, callback));
        }


        /// <summary>
        /// Add an amount of coins to CollectedCoins with given delay time and callback.
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="delay"></param>
        /// <param name="callback"></param>
        public void AddCollectedCoins(int amount, float delay, Action callback = null)
        {
            int startCoins = CollectedCoins;
            int endCoins = CollectedCoins + amount;
            StartCoroutine(CRUpdateCoins(1, startCoins, endCoins, delay, callback));
        }




        /// <summary>
        /// Remove an amount of total coins with delay time and callback.
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="delay"></param>
        /// <param name="callback"></param>
        public void RemoveTotalCoins(int amount, float delay, Action callback = null)
        {
            int startCoins = TotalCoins;
            int endCoins = TotalCoins - amount;
            StartCoroutine(CRUpdateCoins(0, startCoins, endCoins, delay, callback));
        }



        /// <summary>
        /// Coroutine updating the coins with start coins, end coins and delay time.
        /// index == 1: updating TotalCoins.
        /// index == 1: updating CollectedCoins.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="startCoins"></param>
        /// <param name="endCoins"></param>
        /// <param name="delay"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        private IEnumerator CRUpdateCoins(int index, int startCoins, int endCoins, float delay, Action callback = null)
        {
            //Delay == 0
            if (delay == 0)
            {
                if (index == 1)
                {
                    //Set CollectedCoins directly
                    CollectedCoins = endCoins;
                    CollectedCoinChanged?.Invoke(CollectedCoins);
                    callback?.Invoke();
                }
                else
                {
                    //Set TotalCoins directly
                    TotalCoins = endCoins;
                    callback?.Invoke();
                }

                yield break;
            }

            yield return new WaitForSeconds(delay);
            float t = 0;
            float runTime = 0.25f;
            while (t < runTime)
            {
                t += Time.deltaTime;
                float factor = EasyType.MatchedLerpType(LerpType.EaseOutQuad, t / runTime);
                int newCoins = Mathf.RoundToInt(Mathf.Lerp(startCoins, endCoins, factor));
                if (index == 0) { TotalCoins = newCoins; }
                else { SetCollectedCoins(newCoins); }
                yield return null;
            }
            if (index == 0) { TotalCoins = endCoins; ; }
            else { SetCollectedCoins(endCoins); }
            callback?.Invoke();
        }
    }
}
