using UnityEngine;

namespace Luar
{
    /// <summary>Parpadeo cálido de luz (velas, neón).</summary>
    public class LightFlicker : MonoBehaviour
    {
        public float baseIntensity = 1f;
        public float variation = 0.08f;
        Light l;
        float seed;
        void Awake() { l = GetComponent<Light>(); seed = Random.value * 10f; }
        void Update() { if (l) l.intensity = baseIntensity * (1f + (Mathf.PerlinNoise(seed, Time.time * 2f) - 0.5f) * 2f * variation); }
    }
}
