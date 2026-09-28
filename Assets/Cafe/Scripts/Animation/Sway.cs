using UnityEngine;

namespace Luar
{
    /// <summary>Balanceo tipo péndulo (lámparas colgantes, plantas).</summary>
    public class Sway : MonoBehaviour
    {
        public float angle = 2f;
        public float speed = 0.8f;
        public Vector3 axis = Vector3.forward;
        Quaternion start;
        float phase;
        void Awake() { start = transform.localRotation; phase = Random.value * 6f; }
        void Update() => transform.localRotation = start * Quaternion.AngleAxis(Mathf.Sin(Time.time * speed + phase) * angle, axis);
    }
}
