using UnityEngine;

namespace Luar
{
    /// <summary>
    /// Base para todo lo que responde a la mirada (Gaze). Se activa mirando fijamente
    /// durante <see cref="dwellTime"/> segundos o pulsando el botón del visor Cardboard.
    /// </summary>
    public abstract class GazeInteractable : MonoBehaviour
    {
        [Tooltip("Segundos de mirada fija para activar.")]
        public float dwellTime = 1.5f;
        public bool useDwell = true;
        [Tooltip("Texto corto que aparece junto al puntero.")]
        public string hint = "";
        [Tooltip("Escala al mirar (1 = sin cambio).")]
        public float hoverScale = 1.06f;
        [Tooltip("Transform que se escala al mirar. Si está vacío se usa este mismo.")]
        public Transform hoverTarget;

        protected bool hovered;
        Vector3 baseScale;
        float hoverT;
        Renderer[] renderers;
        Color[] baseColors;
        MaterialPropertyBlock mpb;
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        public virtual bool IsInteractable => isActiveAndEnabled;
        public bool IsHovered => hovered;

        protected virtual void Awake()
        {
            if (hoverTarget == null) hoverTarget = transform;
            baseScale = hoverTarget.localScale;
            renderers = System.Array.FindAll(hoverTarget.GetComponentsInChildren<Renderer>(),
                r => r.sharedMaterial != null && r.sharedMaterial.HasProperty(BaseColor));
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                baseColors[i] = renderers[i].sharedMaterial.GetColor(BaseColor);
            mpb = new MaterialPropertyBlock();
        }

        public virtual void GazeEnter()
        {
            hovered = true;
            Sfx.Hover();
        }

        public virtual void GazeExit() => hovered = false;

        public abstract void GazeClick(RaycastHit hit);

        protected virtual void Update()
        {
            float target = hovered && IsInteractable ? 1f : 0f;
            if (Mathf.Approximately(hoverT, target)) return;
            hoverT = Mathf.MoveTowards(hoverT, target, Time.deltaTime * 6f);
            float e = Mathf.SmoothStep(0, 1, hoverT);
            if (hoverScale != 1f) hoverTarget.localScale = baseScale * Mathf.Lerp(1f, hoverScale, e);
            // Brillo suave (más claro, no más saturado) para no cansar la vista.
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                renderers[i].GetPropertyBlock(mpb);
                mpb.SetColor(BaseColor, Color.Lerp(baseColors[i], Color.white, 0.28f * e));
                renderers[i].SetPropertyBlock(mpb);
                if (e <= 0f) renderers[i].SetPropertyBlock(null);
            }
        }

        /// <summary>Restaura escala base (útil cuando el objeto cambia de padre).</summary>
        protected void ResetHoverVisual()
        {
            hovered = false;
            hoverT = 0f;
            hoverTarget.localScale = baseScale;
            foreach (var r in renderers) if (r) r.SetPropertyBlock(null);
        }
    }
}
