using System.Collections;
using UnityEngine;

namespace Luar
{
    /// <summary>
    /// Panel de instrucciones que, tras cada teletransporte, se reubica suavemente delante
    /// del jugador (según hacia dónde mira), para que las instrucciones nunca se pierdan.
    /// No está pegado a la cabeza: así no genera mareo.
    /// </summary>
    public class LazyFollowPanel : MonoBehaviour
    {
        [Tooltip("Desplazamiento relativo a la cabeza: x = derecha, y = arriba, z = adelante.")]
        public Vector3 offset = new Vector3(-0.8f, 0.1f, 2.1f);
        public float moveTime = 0.6f;

        void OnEnable() => PlayerRig.Teleported += OnTeleported;
        void OnDisable() => PlayerRig.Teleported -= OnTeleported;

        void OnTeleported(TeleportPoint p)
        {
            StopAllCoroutines();
            StartCoroutine(MoveRoutine());
        }

        IEnumerator MoveRoutine()
        {
            var rig = PlayerRig.Instance;
            if (rig == null || rig.head == null) yield break;
            var head = rig.head;
            Vector3 fwd = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.ProjectOnPlane(head.up, Vector3.up);
            fwd.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            Vector3 target = new Vector3(head.position.x, rig.transform.position.y + 1.6f, head.position.z)
                             + right * offset.x + Vector3.up * offset.y + fwd * offset.z;
            Vector3 look = target - new Vector3(head.position.x, target.y, head.position.z);
            Quaternion rot = Quaternion.LookRotation(look, Vector3.up);

            Vector3 p0 = transform.position; Quaternion r0 = transform.rotation;
            for (float t = 0; t < moveTime; t += Time.deltaTime)
            {
                float e = Mathf.SmoothStep(0, 1, t / moveTime);
                transform.position = Vector3.Lerp(p0, target, e);
                transform.rotation = Quaternion.Slerp(r0, rot, e);
                yield return null;
            }
            transform.SetPositionAndRotation(target, rot);
        }
    }
}
