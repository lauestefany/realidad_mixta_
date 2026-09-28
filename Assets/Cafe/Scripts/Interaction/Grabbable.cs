using System;
using System.Collections;
using UnityEngine;

namespace Luar
{
    /// <summary>
    /// Objeto que se puede agarrar con la mirada. Mientras se sostiene flota delante de la
    /// cámara; luego se coloca mirando una zona de colocación (<see cref="PlacementZone"/>)
    /// o una superficie libre (<see cref="PlaceableSurface"/>).
    /// </summary>
    public class Grabbable : GazeInteractable
    {
        public static Grabbable Held { get; private set; }
        public static event Action<Grabbable> PickedUp;

        [Tooltip("Identificador para validar la zona correcta (ej: cafe, cupcake, macaron).")]
        public string itemId = "cafe";
        public string displayName = "Café";

        public PlacementZone CurrentZone { get; set; }

        Collider[] colliders;
        Rigidbody body;
        Vector3 heldScale;

        protected override void Awake()
        {
            base.Awake();
            hint = "Recoger " + displayName.ToLowerInvariant();
            colliders = GetComponentsInChildren<Collider>();
            body = GetComponent<Rigidbody>();
            heldScale = transform.localScale;
        }

        public override bool IsInteractable => base.IsInteractable && Held == null;

        public override void GazeClick(RaycastHit hit) => PickUp();

        public void PickUp()
        {
            if (Held != null) return;
            var rig = PlayerRig.Instance;
            if (rig == null || rig.holdAnchor == null) return;

            if (CurrentZone != null) { CurrentZone.Release(this); CurrentZone = null; }
            Held = this;
            ResetHoverVisual();
            SetPhysics(false);
            StopAllCoroutines();
            transform.SetParent(rig.holdAnchor, true);
            StartCoroutine(MoveLocal(Vector3.zero, Quaternion.identity, 0.3f));
            PickedUp?.Invoke(this);
        }

        /// <summary>Suelta el objeto en una posición del mundo con animación.</summary>
        public void PlaceAt(Vector3 worldPos, Quaternion worldRot, Transform parent)
        {
            if (Held == this) Held = null;
            StopAllCoroutines();
            transform.SetParent(parent, true);
            StartCoroutine(MoveWorld(worldPos, worldRot, 0.35f));
        }

        void SetPhysics(bool on)
        {
            foreach (var c in colliders) c.enabled = on;
            if (body != null) { body.isKinematic = true; body.useGravity = false; }
        }

        IEnumerator MoveLocal(Vector3 pos, Quaternion rot, float dur)
        {
            Vector3 p0 = transform.localPosition; Quaternion r0 = transform.localRotation;
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                float e = Mathf.SmoothStep(0, 1, t / dur);
                transform.localPosition = Vector3.Lerp(p0, pos, e);
                transform.localRotation = Quaternion.Slerp(r0, rot, e);
                yield return null;
            }
            transform.localPosition = pos; transform.localRotation = rot;
            // Balanceo suave mientras se sostiene
            while (Held == this)
            {
                transform.localPosition = pos + Vector3.up * Mathf.Sin(Time.time * 2f) * 0.008f;
                transform.localRotation = rot * Quaternion.Euler(0, Time.time * 25f, 0);
                yield return null;
            }
        }

        IEnumerator MoveWorld(Vector3 pos, Quaternion rot, float dur)
        {
            Vector3 p0 = transform.position; Quaternion r0 = transform.rotation;
            Vector3 lift = Vector3.up * 0.12f;
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                float k = t / dur;
                float e = Mathf.SmoothStep(0, 1, k);
                transform.position = Vector3.Lerp(p0, pos, e) + lift * Mathf.Sin(k * Mathf.PI);
                transform.rotation = Quaternion.Slerp(r0, rot, e);
                yield return null;
            }
            transform.position = pos; transform.rotation = rot;
            transform.localScale = heldScale;
            SetPhysics(true);
            StartCoroutine(Squash());
        }

        IEnumerator Squash()
        {
            for (float t = 0; t < 0.25f; t += Time.deltaTime)
            {
                float s = Mathf.Sin(t / 0.25f * Mathf.PI) * 0.12f;
                transform.localScale = new Vector3(heldScale.x * (1 + s), heldScale.y * (1 - s), heldScale.z * (1 + s));
                yield return null;
            }
            transform.localScale = heldScale;
        }

        void OnDestroy() { if (Held == this) Held = null; }
    }
}
