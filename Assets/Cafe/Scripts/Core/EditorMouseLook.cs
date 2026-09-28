using UnityEngine;
using UnityEngine.InputSystem;

namespace Luar
{
    /// <summary>
    /// Solo para probar sin visor: mantener clic derecho (o Alt) y mover el ratón para mirar alrededor.
    /// Se desactiva automáticamente cuando Cardboard está activo.
    /// </summary>
    public class EditorMouseLook : MonoBehaviour
    {
        public float sensitivity = 0.15f;
        float yaw, pitch;

        void Start()
        {
            var e = transform.localEulerAngles;
            yaw = e.y;
            pitch = e.x > 180 ? e.x - 360 : e.x;
        }

        void Update()
        {
            if (VRInput.XRActive) return;
            var mouse = Mouse.current;
            var kb = Keyboard.current;
            if (mouse == null) return;
            bool look = mouse.rightButton.isPressed || (kb != null && kb.leftAltKey.isPressed);
            if (!look) return;
            Vector2 d = mouse.delta.ReadValue() * sensitivity;
            yaw += d.x;
            pitch = Mathf.Clamp(pitch - d.y, -80f, 80f);
            transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }
}
