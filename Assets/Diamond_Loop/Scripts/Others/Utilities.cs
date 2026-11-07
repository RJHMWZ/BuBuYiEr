using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ClawbearGames
{
    public class Utilities
    {


        /// <summary>
        /// Check if user is touching an UI game object.
        /// </summary>
        /// <returns></returns>
        public static bool IsPointerOverUIObject() => InputManager.IsPointerOverUI();

        /// <summary>
        /// Check if user is touching the give target object.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static bool IsPointerOverUIObject(GameObject target) => InputManager.IsTouchingUIObject(target);

        /// <summary>
        /// Convert the given float number to time format.
        /// </summary>
        /// <param name="totalSeconds"></param>
        /// <returns></returns>
        public static string FloatToTime(float totalSeconds)
        {
            int hours = Mathf.FloorToInt(totalSeconds / 3600f);
            int minutes = Mathf.FloorToInt((totalSeconds % 3600f) / 60f);
            int seconds = Mathf.FloorToInt(totalSeconds % 60f);

            if (hours > 0)
                return string.Format("{0:D2}:{1:D2}:{2:D2}", hours, minutes, seconds);
            else
                return string.Format("{0:D2}:{1:D2}", minutes, seconds);
        }


        /// <summary>
        /// Covert the given seconds to time format.
        /// </summary>
        /// <param name="seconds"></param>
        /// <returns></returns>
        public static string SecondsToTimeFormat(double seconds)
        {
            int hours = (int)seconds / 3600;
            int mins = ((int)seconds % 3600) / 60;
            seconds = Math.Round(seconds % 60, 0);
            return hours + ":" + mins + ":" + seconds;
        }

        /// <summary>
        /// Covert the given seconds to minutes format.
        /// </summary>
        /// <param name="seconds"></param>
        /// <returns></returns>
        public static string SecondsToMinutesFormat(double seconds)
        {
            int mins = ((int)seconds % 3600) / 60;
            seconds = Math.Round(seconds % 60, 0);
            return mins + ":" + seconds;
        }


        /// <summary>
        /// Convert color to hex
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static string ColorToHex(Color32 color)
        {
            string hex = color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
            return hex;
        }

        /// <summary>
        /// Convert hex to color
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public static Color HexToColor(string hex)
        {
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return new Color32(r, g, b, 255);
        }
    }
}
