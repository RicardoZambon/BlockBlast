#nullable enable
using System.Globalization;
using BlockPuzzle.Core;
using UnityEngine;

namespace BlockPuzzle.Game
{
    /// <summary>The Daily's result as one line of text, posted through the phone's share sheet.</summary>
    public static class DailyShare
    {
        /// <summary>Added to the text once the game has a store page or a website (tasks 0.5 and 4.3).</summary>
        public const string Link = "";

        public static string Text(string product, int day, int score, int streak)
        {
            string date = DailyPuzzle.DateOf(day).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            string text = Strings.Format("share.result", product, date, score);
            if (streak > 0) text += Strings.Format("share.streak", streak);
            return Link.Length > 0 ? text + " " + Link : text;
        }

        /// <summary>Android's share sheet. Elsewhere (the Editor, and iOS until it gets a native sheet) the text goes to the clipboard.</summary>
        public static void Share(string text)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using var intentClass = new AndroidJavaClass("android.content.Intent");
            using var intent = new AndroidJavaObject("android.content.Intent");
            // Each builder call returns the intent again, as a new wrapper to dispose.
            intent.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND")).Dispose();
            intent.Call<AndroidJavaObject>("setType", "text/plain").Dispose();
            intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), text).Dispose();
            using var chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intent, Strings.Get("share.chooser"));
            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            activity.Call("startActivity", chooser);
#else
            GUIUtility.systemCopyBuffer = text;
            Debug.Log("Copied the Daily result: " + text);
#endif
        }
    }
}
