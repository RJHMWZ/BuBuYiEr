using UnityEngine;
using UnityEngine.UI;

namespace ClawbearGames
{
    public class LeaderboardItemController : MonoBehaviour
    {
        [SerializeField] private Text usernameText = null;
        [SerializeField] private Text levelText = null;

        
        /// <summary>
        /// Setup this leaderboard item.
        /// </summary>
        /// <param name="indexRank"></param>
        /// <param name="data"></param>
        public void OnSetup(int indexRank, LeaderboardParams data)
        {
            transform.localScale = Vector3.one;
            usernameText.text = "#" + indexRank.ToString() + "." + " " + data.Username;
            levelText.text = "Level: " + data.Level.ToString();

            if (indexRank == 1)
            {
                usernameText.color = Color.red;
                levelText.color = Color.red;
            }
            else if (indexRank == 2)
            {
                usernameText.color = Color.yellow;
                levelText.color = Color.yellow;
            }
            else if (indexRank == 3)
            {
                usernameText.color = Color.blue;
                levelText.color = Color.blue;
            }
            else if (indexRank == 4)
            {
                usernameText.color = Color.green;
                levelText.color = Color.green;
            }
            else if (indexRank == 5)
            {
                usernameText.color = Color.magenta;
                levelText.color = Color.magenta;
            }
        }
    }
}
