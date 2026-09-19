using UnityEngine;

namespace Project51.UIV2.Core
{
    /// <summary>
    /// Condivisione testo: su Android apre il selettore di sistema (Intent.ACTION_SEND), altrove
    /// copia il testo negli appunti. Ritorna true se e' stato aperto il selettore nativo.
    /// </summary>
    public static class NativeShare
    {
        public static bool ShareText(string text, string chooserTitle = "Invita un amico")
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var intentClass = new AndroidJavaClass("android.content.Intent"))
                using (var intent = new AndroidJavaObject("android.content.Intent"))
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    intent.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
                    intent.Call<AndroidJavaObject>("setType", "text/plain");
                    intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), text);
                    using (var chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intent, chooserTitle))
                        activity.Call("startActivity", chooser);
                }
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[NativeShare] Share sheet unavailable, copying to clipboard: {ex.Message}");
            }
#endif
            GUIUtility.systemCopyBuffer = text;
            return false;
        }
    }
}
