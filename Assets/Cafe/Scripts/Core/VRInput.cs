using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace Luar
{
    /// <summary>
    /// Entrada unificada: botón del visor Cardboard (toque de pantalla), clic de ratón o barra espaciadora en el editor.
    /// </summary>
    public static class VRInput
    {
        public static bool XRActive => XRSettings.isDeviceActive;

        public static bool TriggerPressed
        {
            get
            {
#if !UNITY_EDITOR
                if (XRActive)
                    return Google.XR.Cardboard.Api.IsTriggerPressed;
#endif
                var touch = Touchscreen.current;
                if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
                    return true;
                var mouse = Mouse.current;
                if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                    return true;
                var kb = Keyboard.current;
                return kb != null && kb.spaceKey.wasPressedThisFrame;
            }
        }
    }
}
