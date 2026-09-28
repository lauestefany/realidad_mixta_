using System;
using UnityEngine;

namespace Luar
{
    /// <summary>
    /// Lugar donde se coloca un objeto (ej: un plato en la mesa del cliente). Solo se puede
    /// mirar cuando el jugador sostiene algo. Si <see cref="acceptedId"/> no está vacío,
    /// valida que sea el objeto correcto.
    /// </summary>
    public class PlacementZone : GazeInteractable
    {
        public static event Action<PlacementZone, Grabbable> Placed;
        public static event Action<PlacementZone, Grabbable> Rejected;

        public string acceptedId = "";
        public Transform snapPoint;
        [Tooltip("Aro/indicador que se muestra mientras se sostiene un objeto.")]
        public GameObject indicator;

        public Grabbable Occupant { get; private set; }
        public bool IsCorrect => Occupant != null && (string.IsNullOrEmpty(acceptedId) || Occupant.itemId == acceptedId);

        protected override void Awake()
        {
            hoverScale = 1f;
            base.Awake();
            if (snapPoint == null) snapPoint = transform;
        }

        public override bool IsInteractable => base.IsInteractable && Grabbable.Held != null && Occupant == null;

        protected override void Update()
        {
            base.Update();
            if (indicator != null)
            {
                bool show = Grabbable.Held != null && Occupant == null;
                if (indicator.activeSelf != show) indicator.SetActive(show);
                if (show)
                {
                    float s = 1f + Mathf.Sin(Time.time * 4f) * 0.06f + (hovered ? 0.15f : 0f);
                    indicator.transform.localScale = new Vector3(s, 1f, s);
                }
            }
        }

        public override void GazeClick(RaycastHit hit)
        {
            var item = Grabbable.Held;
            if (item == null || Occupant != null) return;
            if (!string.IsNullOrEmpty(acceptedId) && item.itemId != acceptedId)
            {
                Sfx.Error();
                Rejected?.Invoke(this, item);
                return;
            }
            Occupant = item;
            item.CurrentZone = this;
            item.PlaceAt(snapPoint.position, snapPoint.rotation, snapPoint);
            Sfx.Success();
            Placed?.Invoke(this, item);
        }

        public void Release(Grabbable item)
        {
            if (Occupant == item) Occupant = null;
        }
    }
}
