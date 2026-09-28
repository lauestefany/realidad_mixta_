using UnityEngine;
using UnityEngine.SceneManagement;

namespace Luar
{
    /// <summary>
    /// Mantiene la sesión Cardboard viva entre escenas: pantalla siempre encendida,
    /// escaneo del visor (engranaje), botón X para salir y actualización de parámetros.
    /// Se crea sola al arrancar la app.
    /// </summary>
    public class CardboardSession : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            var go = new GameObject("[CardboardSession]");
            DontDestroyOnLoad(go);
            go.AddComponent<CardboardSession>();
            SceneManager.sceneLoaded += (s, m) => Recenter();
        }

        /// <summary>Al entrar a cada escena el jugador queda mirando al contenido principal.</summary>
        public static void Recenter()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (VRInput.XRActive) Google.XR.Cardboard.Api.Recenter();
#endif
        }

        void Start()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.brightness = 1f;
            Application.targetFrameRate = 60;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Google.XR.Cardboard.Api.HasDeviceParams())
                Google.XR.Cardboard.Api.ScanDeviceParams();
#endif
        }

        void Update()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!VRInput.XRActive) return;
            if (Google.XR.Cardboard.Api.IsCloseButtonPressed)
                SceneFlow.Quit();
            if (Google.XR.Cardboard.Api.IsGearButtonPressed)
                Google.XR.Cardboard.Api.ScanDeviceParams();
            if (Google.XR.Cardboard.Api.HasNewDeviceParams())
                Google.XR.Cardboard.Api.ReloadDeviceParams();
            Google.XR.Cardboard.Api.UpdateScreenParams();
#endif
        }
    }
}
