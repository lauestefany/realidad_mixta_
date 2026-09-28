using UnityEngine;

namespace Luar
{
    /// <summary>
    /// Punto de salto en el piso. Mirarlo 1.2 s (o tocar) teletransporta al jugador.
    /// El punto donde está el jugador se oculta para no confundir.
    /// </summary>
    public class TeleportPoint : GazeInteractable
    {
        public string label = "Ir aquí";
        [Tooltip("Parte visual que late suavemente.")]
        public Transform visual;
        [Tooltip("Marcar si el jugador empieza aquí.")]
        public bool startHere;

        bool occupied;
        Collider col;

        protected override void Awake()
        {
            hoverScale = 1f;
            base.Awake();
            hint = label;
            col = GetComponent<Collider>();
        }

        void Start()
        {
            if (startHere && PlayerRig.Instance != null) PlayerRig.Instance.SetCurrent(this);
        }

        public override bool IsInteractable => base.IsInteractable && !occupied;

        public void SetOccupied(bool value)
        {
            occupied = value;
            if (visual != null) visual.gameObject.SetActive(!value);
            if (col != null) col.enabled = !value;
        }

        public override void GazeClick(RaycastHit hit)
        {
            if (PlayerRig.Instance != null) PlayerRig.Instance.TeleportTo(this);
        }

        protected override void Update()
        {
            base.Update();
            if (visual == null || occupied) return;
            float s = 1f + Mathf.Sin(Time.time * 3f) * 0.05f + (hovered ? 0.18f : 0f);
            visual.localScale = Vector3.Lerp(visual.localScale, new Vector3(s, 1f, s), Time.deltaTime * 10f);
        }
    }
}
