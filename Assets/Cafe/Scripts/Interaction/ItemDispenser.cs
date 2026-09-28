using System.Collections;
using UnityEngine;

namespace Luar
{
    /// <summary>
    /// Máquina (ej: cafetera) que al activarla con la mirada prepara un objeto agarrable
    /// y lo deja en la bandeja con vapor y una pequeña animación.
    /// </summary>
    public class ItemDispenser : GazeInteractable
    {
        [Tooltip("Objeto plantilla (desactivado en la escena) que se clona.")]
        public Grabbable template;
        public Transform spawnPoint;
        public ParticleSystem steam;
        public Light glow;
        public int maxWaiting = 1;
        public float brewTime = 1.2f;

        bool brewing;
        Grabbable waiting;

        protected override void Awake()
        {
            base.Awake();
            if (string.IsNullOrEmpty(hint)) hint = "Preparar café";
        }

        public override bool IsInteractable => base.IsInteractable && !brewing && Grabbable.Held == null && WaitingCount() < maxWaiting;

        int WaitingCount()
        {
            // El vaso sigue "esperando" mientras no lo recojan de la bandeja.
            if (waiting != null && waiting.transform.parent == spawnPoint) return 1;
            waiting = null;
            return 0;
        }

        public override void GazeClick(RaycastHit hit)
        {
            if (!brewing) StartCoroutine(Brew());
        }

        IEnumerator Brew()
        {
            brewing = true;
            if (steam != null) steam.Play();
            float t = 0;
            while (t < brewTime)
            {
                t += Time.deltaTime;
                if (glow != null) glow.intensity = Mathf.Lerp(0f, 0.8f, Mathf.PingPong(t * 3f, 1f));
                yield return null;
            }
            if (glow != null) glow.intensity = 0f;

            var clone = Instantiate(template, spawnPoint.position, spawnPoint.rotation, spawnPoint);
            clone.gameObject.SetActive(true);
            clone.name = template.name;
            waiting = clone;
            Vector3 s = clone.transform.localScale;
            for (float k = 0; k < 1f; k += Time.deltaTime * 4f)
            {
                clone.transform.localScale = s * Mathf.SmoothStep(0, 1, k);
                yield return null;
            }
            clone.transform.localScale = s;
            Sfx.Success();
            brewing = false;
        }
    }
}
