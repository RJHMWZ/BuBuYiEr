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
            ApplyTotalDelta(amount);
            CompleteLater(delay, callback);
        }

        public void AddCollectedCoins(int amount, float delay, Action callback = null)
        {
            long updated = (long)CollectedCoins + amount;
            CollectedCoins = (int)Math.Max(0L, Math.Min(int.MaxValue, updated));
            CollectedCoinChanged?.Invoke(CollectedCoins);
            CompleteLater(delay, callback);
        }

        public void RemoveTotalCoins(int amount, float delay, Action callback = null)
        {
            ApplyTotalDelta(-Math.Max(0, amount));
            CompleteLater(delay, callback);
        }

        public bool TrySpendCoins(int amount)
        {
            if (amount < 0 || TotalCoins < amount) return false;
            ApplyTotalDelta(-amount);
            return true;
        }

        private void ApplyTotalDelta(int amount)
        {
            // Persist one transaction at a time. Decorative delays must not overwrite later transactions.
            long updated = (long)TotalCoins + amount;
            TotalCoins = (int)Math.Max(0L, Math.Min(int.MaxValue, updated));
            PlayerPrefs.Save();
        }

        private void CompleteLater(float delay, Action callback)
        {
            if (callback == null) return;
            if (delay <= 0) callback();
            else StartCoroutine(DelayedCallback(delay, callback));
        }

        private IEnumerator DelayedCallback(float delay, Action callback)
        {
            yield return new WaitForSecondsRealtime(delay);
            callback?.Invoke();
        }
    }
}
