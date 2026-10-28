using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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


		/// <summary>
		/// Coroutine connect to Dreamlo services. 
		/// </summary>
		/// <param name="callback"></param>
		/// <returns></returns>
		private IEnumerator CRConnectDreamloServices(Action<bool> callback)
		{
			string requestUrl = GetLeaderboardDataUrl + "/pipe";
			UnityWebRequest unityWebRequest = new UnityWebRequest(requestUrl);
			DownloadHandlerBuffer dH = new DownloadHandlerBuffer();
			unityWebRequest.downloadHandler = dH;
			yield return unityWebRequest.SendWebRequest();

			float timeCount = 0;
			while (unityWebRequest.result == UnityWebRequest.Result.InProgress)
			{
				yield return null;
				timeCount += Time.deltaTime;
				if (timeCount >= 3f)
				{
					break;
				}
			}

			if (unityWebRequest.result == UnityWebRequest.Result.Success)
			{
				callback?.Invoke(true);
			}
			else
			{
				callback?.Invoke(false);
			}
		}



		/// <summary>
		/// Coroutine get the leaderboard data from Dreamlo services.
		/// </summary>
		/// <param name="callback"></param>
		/// <returns></returns>
		private IEnumerator CRGetPlayerLeaderboardData(Action<string> callback)
		{
			string requestUrl = GetLeaderboardDataUrl + "/pipe";
			UnityWebRequest unityWebRequest = new UnityWebRequest(requestUrl);
			DownloadHandlerBuffer dH = new DownloadHandlerBuffer();
			unityWebRequest.downloadHandler = dH;
			yield return unityWebRequest.SendWebRequest();

			float timeCount = 0;
			while (unityWebRequest.result == UnityWebRequest.Result.InProgress)
			{
				yield return null;
				timeCount += Time.deltaTime;
				if (timeCount >= 3f)
				{
					break;
				}
			}

			if (unityWebRequest.result == UnityWebRequest.Result.Success)
			{
				callback?.Invoke(unityWebRequest.downloadHandler.text);
			}
			else
			{
				callback?.Invoke(string.Empty);
			}
		}





		/// <summary>
		/// Checking conection to Dreamlo services. 
		/// </summary>
		/// <param name="callback"></param>
		public void CheckConnectedToDreamloServices(Action<bool> callback)
		{
			StartCoroutine(CRConnectDreamloServices((isConnected) =>
			{
				callback?.Invoke(isConnected);
			}));
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
						print(chars[0] + " " + username);
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
			UnityWebRequest www = new UnityWebRequest(requestUrl);
			www.SendWebRequest();
		}





		/// <summary>
		/// Get a list of leaderboard data with callback.
		/// </summary>
		/// <param name="callback"></param>
		public void GetPlayerLeaderboardData(Action<List<LeaderboardParams>> callback)
		{
			StartCoroutine(CRGetPlayerLeaderboardData((stringDatas) =>
			{
				if (!stringDatas.Equals(string.Empty))
				{
					//Convert datas from string to  PlayerLeaderboardData type.
					string[] rows = stringDatas.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
					LeaderboardParams[] playerLeaderboardDatas = new LeaderboardParams[rows.Length];
					for (int i = 0; i < rows.Length; i++)
					{
						string[] chars = rows[i].Split(new char[] { '|' }, StringSplitOptions.None);
						LeaderboardParams leaderboardData = new LeaderboardParams();
						leaderboardData.SetUsername(chars[0]);
						leaderboardData.SetLevel(int.Parse(chars[1]));
						playerLeaderboardDatas[i] = leaderboardData;
					}
					IComparer<LeaderboardParams> comparer = new LeaderboardComparer();
					Array.Sort(playerLeaderboardDatas, comparer);

					if (maxUserAmount == -1)
					{
						//Return all datas
						callback?.Invoke(playerLeaderboardDatas.ToList());
					}
					else
					{
						//Return an amount of data that matched to maxUser
						List<LeaderboardParams> listPlayerData = new List<LeaderboardParams>();
						for (int i = 0; i < playerLeaderboardDatas.Length; i++)
						{
							listPlayerData.Add(playerLeaderboardDatas[i]);
							if (i == maxUserAmount - 1)
							{
								break;
							}
						}
						callback?.Invoke(listPlayerData);
					}
				}
				else
				{
					callback?.Invoke(new List<LeaderboardParams>());
				}
			}));
		}
	}
}
