using System.Collections;
using UnityEngine;

namespace Luar
{
    /// <summary>
    /// Fundido a color crema delante de la cámara (funciona en estéreo porque es geometría 3D).
    /// Se usa al teletransportarse y al cambiar de escena.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class ScreenFader : MonoBehaviour
    {
        public static ScreenFader Instance { get; private set; }
        public Color color = new Color(1f, 0.96f, 0.92f, 1f);

        Material mat;
        float alpha = 1f;

        void Awake()
        {
            Instance = this;
            mat = GetComponent<MeshRenderer>().material;
            Apply();
        }

        void Start() => StartCoroutine(FadeTo(0f, 0.6f));

        void OnDestroy() { if (Instance == this) Instance = null; }

        public IEnumerator FadeTo(float target, float duration)
        {
            float start = alpha;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                alpha = Mathf.Lerp(start, target, Mathf.SmoothStep(0, 1, t / duration));
                Apply();
                yield return null;
            }
            alpha = target;
            Apply();
        }

        void Apply()
        {
            var c = color; c.a = alpha;
            mat.SetColor("_BaseColor", c);
            GetComponent<MeshRenderer>().enabled = alpha > 0.001f;
        }
    }
}
