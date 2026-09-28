using System;
using System.Collections;
using UnityEngine;

namespace Luar
{
    /// <summary>
    /// Raíz del jugador (Rig > CameraOffset > Main Camera). Mueve al jugador con un fundido
    /// breve (teletransportación sin mareo).
    /// </summary>
    public class PlayerRig : MonoBehaviour
    {
        public static PlayerRig Instance { get; private set; }
        public static event Action<TeleportPoint> Teleported;

        public Transform head;
        [Tooltip("Objetos sostenidos se ubican aquí (hijo de la cámara).")]
        public Transform holdAnchor;

        public TeleportPoint CurrentPoint { get; private set; }
        bool busy;

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        public void TeleportTo(TeleportPoint point)
        {
            if (busy || point == null) return;
            StartCoroutine(TeleportRoutine(point));
        }

        IEnumerator TeleportRoutine(TeleportPoint point)
        {
            busy = true;
            Sfx.Teleport();
            var fader = ScreenFader.Instance;
            if (fader != null) yield return fader.FadeTo(1f, 0.18f);

            // Compensar el desplazamiento de la cabeza para quedar justo sobre el punto.
            Vector3 headOffset = head != null ? head.position - transform.position : Vector3.zero;
            headOffset.y = 0f;
            var p = point.transform.position;
            transform.position = new Vector3(p.x, transform.position.y, p.z) - headOffset;
            SetCurrent(point);

            if (fader != null) yield return fader.FadeTo(0f, 0.25f);
            busy = false;
            Teleported?.Invoke(point);
        }

        public void SetCurrent(TeleportPoint point)
        {
            if (CurrentPoint != null) CurrentPoint.SetOccupied(false);
            CurrentPoint = point;
            if (CurrentPoint != null) CurrentPoint.SetOccupied(true);
        }
    }
}
