using UnityEngine;

namespace Luar
{
    /// <summary>Late suavemente (corazones, letrero neón).</summary>
    public class Pulse : MonoBehaviour
    {
        public float amount = 0.06f;
        public float speed = 2f;
        Vector3 start;
        void Awake() => start = transform.localScale;
        void Update() => transform.localScale = start * (1f + Mathf.Sin(Time.time * speed) * amount);
    }
}
