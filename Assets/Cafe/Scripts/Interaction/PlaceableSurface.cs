using UnityEngine;

namespace Luar
{
    /// <summary>
    /// Superficie libre (mesa, mostrador, estante) donde se puede dejar el objeto sostenido
    /// justo en el punto que se mira.
    /// </summary>
    public class PlaceableSurface : GazeInteractable
    {
        protected override void Awake()
        {
            hoverScale = 1f;
            dwellTime = 2f;
            base.Awake();
            if (string.IsNullOrEmpty(hint)) hint = "Dejar aquí";
        }

        public override bool IsInteractable => base.IsInteractable && Grabbable.Held != null;

        public override void GazeClick(RaycastHit hit)
        {
            var item = Grabbable.Held;
            if (item == null || hit.collider == null) return;
            if (Vector3.Dot(hit.normal, Vector3.up) < 0.7f) { Sfx.Error(); return; }
            var cam = PlayerRig.Instance != null ? PlayerRig.Instance.head : Camera.main.transform;
            Vector3 fwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up);
            if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
            item.PlaceAt(hit.point, Quaternion.LookRotation(-fwd, Vector3.up), null);
        }

        protected override void Update() { }
    }
}
