using UnityEngine;

namespace ClawbearGames
{
    public class TileController : MonoBehaviour
    {

        /// <summary>
        /// The position for the pot to stand on
        /// </summary>
        public Vector3 PotPosition => transform.position + Vector3.up * 0.4f;


        /// <summary>
        /// The pot that occupied this tile
        /// </summary>
        public PotController PotController
        {
            set
            {
                pot = value;
                if(pot != null)
                {
                    pot.transform.SetParent(transform);
                    pot.transform.localPosition = new Vector3(0f, 0.4f, 0f);
                    pot.transform.localRotation = Quaternion.identity;
                }
            }
            get {  return pot; }
        }
        private PotController pot = null;




        /// <summary>
        /// Check if this tile has pot.
        /// </summary>
        /// <returns></returns>
        public bool HasPot()
        {
            return pot != null;
        }
    }
}