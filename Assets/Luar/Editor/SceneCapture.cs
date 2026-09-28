using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Luar.EditorTools
{
    /// <summary>Renderiza vistas previas de cada escena a Captures/*.png (para revisar sin visor).</summary>
    public static class SceneCapture
    {
        [MenuItem("Luar Café VR/Capturas de las escenas")]
        public static void CaptureAll()
        {
            Directory.CreateDirectory("Captures");
            Shot(SceneFlow.Inicio, null, 0, 0, "a");
            Shot(SceneFlow.Inicio, null, -45, 5, "b_izq");
            Shot(SceneFlow.Inicio, null, 45, 5, "c_der");
            Shot(SceneFlow.Inicio, null, 180, 0, "d_atras");
            Shot(SceneFlow.Tutorial, null, 0, 5, "a");
            Shot(SceneFlow.Tutorial, new Vector3(1.0f, 1.6f, -0.2f), 10, 15, "b_barra");
            Shot(SceneFlow.Tutorial, new Vector3(1.0f, 1.6f, -0.2f), -45, 20, "c_mesa");
            Shot(SceneFlow.Recorrido, null, 0, 5, "a");
            Shot(SceneFlow.Recorrido, new Vector3(-3.4f, 1.6f, 0.5f), -90, 20, "b_barra");
            Shot(SceneFlow.Recorrido, new Vector3(2.0f, 1.6f, 2.1f), 30, 15, "c_mesa1");
            Shot(SceneFlow.Recorrido, new Vector3(0.2f, 1.6f, -1.0f), 60, 5, "d_centro");
            Shot(SceneFlow.Recorrido, new Vector3(0f, 3.1f, -5.5f), 0, 35, "e_aerea");
            Shot(SceneFlow.Felicitaciones, null, 0, 0, "a");
            Shot(SceneFlow.Felicitaciones, null, 50, 10, "b_der");
            Debug.Log("[Luar] Capturas listas en Captures/");
        }

        static void Shot(string scene, Vector3? pos, float yaw, float pitch, string suffix)
        {
            EditorSceneManager.OpenScene($"Assets/Scenes/{scene}.unity");
            DynamicGI.UpdateEnvironment();
            var cam = Camera.main;
            if (cam == null) { Debug.LogError("Sin cámara en " + scene); return; }
            var t = cam.transform;
            if (pos.HasValue) t.position = pos.Value;
            t.rotation = Quaternion.Euler(pitch, yaw, 0);
            // Ocultar el fundido para la foto
            var fader = t.Find("Fader");
            if (fader) fader.gameObject.SetActive(false);
            cam.fieldOfView = 80f;

            int w = 1280, h = 800;
            var rt = new RenderTexture(w, h, 24) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            File.WriteAllBytes($"Captures/{scene}_{suffix}.png", tex.EncodeToPNG());
            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
        }

        public static void BuildAndCapture()
        {
            ProjectSetup.Configure();
            SceneBuilder.BuildAll();
            CaptureAll();
        }
    }
}
