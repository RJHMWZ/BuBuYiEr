using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


namespace ClawbearGames
{
    public class LeaderboardManager : MonoBehaviour
	{

		[Header("Maximum user amount when getting user data. Set -1 to get all user data.")]
		[SerializeField] private int maxUserAmount = 100;
		[Header("Private And Public Code")]
		[SerializeField] private string leaderboardPrivateCode = "UF62grN4UkOI4l2JPjz7cwhuG1io04TEy7vLWMwcI20A";
		[SerializeField] private string leaderboardPublicCode = "61e7d9b68f40bb1034601d53";


		private string SetLeaderboardDataUrl { get { return "https://dreamlo.com/lb/" + leaderboardPrivateCode; } }
		private string GetLeaderboardDataUrl { get { return "https://dreamlo.com/lb/" + leaderboardPublicCode; } }


        private readonly HashSet<UnityWebRequest> activeRequests = new HashSet<UnityWebRequest>();

        private IEnumerator Request(string url, Action<bool, string> callback)
        {
            var request = UnityWebRequest.Get(url);
            request.timeout = 3;
            activeRequests.Add(request);
            try
            {
                yield return request.SendWebRequest();
                bool success = request.result == UnityWebRequest.Result.Success;
                callback?.Invoke(success, success ? request.downloadHandler.text : string.Empty);
            }
            finally
            {
                if (activeRequests.Remove(request)) request.Dispose();
            }
        }

        private IEnumerator CRConnectDreamloServices(Action<bool> callback)
            => Request(GetLeaderboardDataUrl + "/pipe", (success, _) => callback?.Invoke(success));

        private IEnumerator CRGetPlayerLeaderboardData(Action<string> callback)
            => Request(GetLeaderboardDataUrl + "/pipe", (success, text) => callback?.Invoke(text));

        private void OnDisable()
        {
            StopAllCoroutines();
            foreach (var request in activeRequests) { request.Abort(); request.Dispose(); }
            activeRequests.Clear();
        }

		/// <summary>
		/// Checking conection to Dreamlo services. 
		/// </summary>
		/// <param name="callback"></param>
		public void CheckConnectedToDreamloServices(Action<bool> callback)
		{
			StartCoroutine(CRConnectDreamloServices(callback));
		}



		/// <summary>
		/// Is the user set username or not. 
		/// </summary>
		/// <returns></returns>
		public bool IsSetUsername()
		{
			return !string.IsNullOrEmpty(PlayerDataHandler.GetUserName());
        }



		/// <summary>
		/// Checking whether the given username already exists.
		/// </summary>
		/// <param name="username"></param>
		/// <param name="callback"></param>
		public void CheckUsernameExists(string username, Action<bool> callback)
		{
			//Connect to Dreamlo services
			StartCoroutine(CRGetPlayerLeaderboardData((stringDatas) =>
			{
				if (!stringDatas.Equals(string.Empty))
				{
					//Connected to Deamlo services and got datas
					bool isMatchedUsername = false;
					string[] rows = stringDatas.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
					for (int i = 0; i < rows.Length; i++)
					{
						string[] chars = rows[i].Split(new char[] { '|' }, StringSplitOptions.None);
						if (chars[0].Equals(username))
						{
							isMatchedUsername = true;
							break;
						}
					}
					callback?.Invoke(isMatchedUsername);
				}
				else
				{
					//Not connect to Deamlo services
					callback?.Invoke(true);
				}
			}));
		}





		/// <summary>
		/// Update the player leaderboard data.
		/// </summary>
		public void UpdatePlayerLeaderboardData()
		{
            string username = PlayerDataHandler.GetUserName();
            int level = PlayerDataHandler.GetCurrentLevel();
            string requestUrl = SetLeaderboardDataUrl + "/add-pipe/" + UnityWebRequest.EscapeURL(username) + "/" + level.ToString();
			StartCoroutine(Request(requestUrl, null));
		}





		/// <summary>
		/// Get a list of leaderboard data with callback.
		/// </summary>
		/// <param name="callback"></param>
        public void GetPlayerLeaderboardData(Action<List<LeaderboardParams>> callback)
        {
            StartCoroutine(CRGetPlayerLeaderboardData(text => callback?.Invoke(ParseLeaderboard(text))));
        }

        private List<LeaderboardParams> ParseLeaderboard(string text)
        {
            var entries = new List<LeaderboardParams>();
            if (string.IsNullOrEmpty(text)) return entries;
            string[] rows = text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < rows.Length; i++)
            {
                string[] values = rows[i].Split('|');
                if (values.Length < 2 || !int.TryParse(values[1], out int level)) continue;
                var entry = new LeaderboardParams();
                entry.SetUsername(values[0]);
                entry.SetLevel(level);
                entries.Add(entry);
            }
            entries.Sort(new LeaderboardComparer());
            if (maxUserAmount > 0 && entries.Count > maxUserAmount)
                entries.RemoveRange(maxUserAmount, entries.Count - maxUserAmount);
            return entries;
        }
    }
}
