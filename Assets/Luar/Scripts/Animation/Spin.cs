using UnityEngine;

namespace Luar
{
    /// <summary>Gira continuamente (letreros, ventilador, pastel giratorio).</summary>
    public class Spin : MonoBehaviour
    {
        public Vector3 degreesPerSecond = new Vector3(0, 30, 0);
        void Update() => transform.Rotate(degreesPerSecond * Time.deltaTime, Space.Self);
    }
}
