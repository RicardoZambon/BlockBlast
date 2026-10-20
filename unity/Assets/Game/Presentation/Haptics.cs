#nullable enable
using UnityEngine;

namespace BlockPuzzle.Game
{
    /// <summary>Short vibrations on Android: light, medium or heavy. Elsewhere, and on phones without a vibrator, nothing.</summary>
    public static class Haptics
    {
        public static void Play(Haptic haptic)
        {
            if (haptic == Haptic.None) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            Vibrate(haptic == Haptic.Light ? 12L : haptic == Haptic.Medium ? 25L : 50L,
                haptic == Haptic.Light ? 60 : haptic == Haptic.Medium ? 140 : 255);
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject? _vibrator;
        static int _sdk = -1;

        static void Vibrate(long milliseconds, int amplitude)
        {
            try
            {
                if (_sdk < 0)
                {
                    using var version = new AndroidJavaClass("android.os.Build$VERSION");
                    _sdk = version.GetStatic<int>("SDK_INT");
                }
                if (_vibrator == null)
                {
                    using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                    _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
                if (_vibrator == null) return;
                if (_sdk >= 26)
                {
                    using var effects = new AndroidJavaClass("android.os.VibrationEffect");
                    using var effect = effects.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, amplitude);
                    _vibrator.Call("vibrate", effect);
                }
                else
                {
                    _vibrator.Call("vibrate", milliseconds);
                }
            }
            catch (AndroidJavaException)
            {
                // No vibrator, or the system refused: haptics are a nicety, never a crash.
            }
        }

        /// <summary>Never called. Referencing Handheld.Vibrate makes Unity add the VIBRATE permission to the manifest.</summary>
        static void KeepTheVibratePermission() => Handheld.Vibrate();
#endif
    }
}
