using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Luar
{
    /// <summary>Nombres de escena y cambios de escena con fundido suave (evita mareo).</summary>
    public static class SceneFlow
    {
        public const string Inicio = "Inicio";
        public const string Tutorial = "Tutorial";
        public const string Recorrido = "Recorrido";
        public const string Felicitaciones = "Felicitaciones";

        static bool loading;
        static Runner runner;

        /// <summary>Ejecuta la carga en un objeto persistente: la escena actual se destruye durante la carga.</summary>
        class Runner : MonoBehaviour { }

        public static void Load(string sceneName)
        {
            if (loading) return;
            loading = true;
            if (runner == null)
            {
                var go = new GameObject("[SceneFlow]");
                Object.DontDestroyOnLoad(go);
                runner = go.AddComponent<Runner>();
            }
            runner.StartCoroutine(LoadRoutine(sceneName));
        }

        static IEnumerator LoadRoutine(string sceneName)
        {
            var fader = ScreenFader.Instance;
            if (fader != null) yield return fader.FadeTo(1f, 0.45f);
            var op = SceneManager.LoadSceneAsync(sceneName);
            while (op != null && !op.isDone) yield return null;
            loading = false;
        }

        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
