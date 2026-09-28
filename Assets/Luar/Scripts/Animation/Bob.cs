using UnityEngine;

namespace Luar
{
    /// <summary>Flota suavemente arriba/abajo (globos, flechas, íconos).</summary>
    public class Bob : MonoBehaviour
    {
        public float amplitude = 0.05f;
        public float speed = 1.5f;
        public float phase;
        Vector3 start;
        void Awake() { start = transform.localPosition; if (phase == 0) phase = Random.value * 6f; }
        void Update() => transform.localPosition = start + Vector3.up * Mathf.Sin(Time.time * speed + phase) * amplitude;
    }
}
