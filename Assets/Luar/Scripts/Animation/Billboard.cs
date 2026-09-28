using UnityEngine;

namespace Luar
{
    /// <summary>Gira (solo en Y) para quedar de frente a la cámara. Para etiquetas flotantes.</summary>
    public class Billboard : MonoBehaviour
    {
        Transform cam;
        void LateUpdate()
        {
            if (cam == null) { var c = Camera.main; if (c == null) return; cam = c.transform; }
            Vector3 dir = transform.position - cam.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }
    }
}
