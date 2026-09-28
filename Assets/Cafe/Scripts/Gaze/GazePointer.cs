using TMPro;
using UnityEngine;

namespace Luar
{
    /// <summary>
    /// Puntero Gaze centrado en la vista. Punto blanco con borde cacao (visible sobre fondos
    /// claros y oscuros). Al mirar algo interactivo aparece un anillo que se llena mientras
    /// se mantiene la mirada; al completarse (o al pulsar el botón del visor) se activa.
    /// </summary>
    public class GazePointer : MonoBehaviour
    {
        public float maxDistance = 15f;
        public float idleDistance = 3f;
        public LayerMask mask = ~0;

        [Header("Materiales (Unlit)")]
        public Material lightMat;
        public Material darkMat;
        public Material accentMat;

        [Header("Etiqueta de ayuda")]
        public TextMeshPro hintLabel;
        public GameObject hintBackdrop;

        public static GazePointer Instance { get; private set; }
        public GazeInteractable Current => current;

        Transform reticle, ring;
        Mesh progressMesh;
        GazeInteractable current;
        float dwell;
        bool fired;
        float ringT;
        float lastProgress = -1;
        float cooldownUntil;

        const float K = 0.012f; // escala angular del puntero

        void Awake()
        {
            Instance = this;
            reticle = new GameObject("Reticle").transform;
            reticle.SetParent(transform, false);

            AddMesh(reticle, "DotOutline", Ring(0f, 0.75f, 32, 1f), darkMat, 0.004f);
            AddMesh(reticle, "Dot", Ring(0f, 0.5f, 32, 1f), lightMat, 0.002f);

            ring = new GameObject("Ring").transform;
            ring.SetParent(reticle, false);
            AddMesh(ring, "RingOutline", Ring(1.35f, 2.45f, 48, 1f), darkMat, 0.004f);
            AddMesh(ring, "RingBase", Ring(1.55f, 2.25f, 48, 1f), lightMat, 0.003f);
            progressMesh = new Mesh { name = "Progress" };
            progressMesh.MarkDynamic();
            AddMesh(ring, "RingProgress", progressMesh, accentMat, 0.001f);
            ring.localScale = Vector3.zero;

            if (hintLabel != null)
            {
                hintLabel.transform.SetParent(reticle, false);
                hintLabel.transform.localPosition = new Vector3(0, -3.6f, 0);
                hintLabel.transform.localRotation = Quaternion.identity;
                hintLabel.transform.localScale = Vector3.one;
                hintLabel.text = "";
                if (hintBackdrop != null)
                {
                    hintBackdrop.transform.SetParent(reticle, false);
                    hintBackdrop.transform.localPosition = new Vector3(0, -3.6f, 0.05f);
                    hintBackdrop.transform.localRotation = Quaternion.identity;
                    hintBackdrop.transform.localScale = new Vector3(12f, 2.2f, 1f);
                    hintBackdrop.SetActive(false);
                }
            }
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void LateUpdate()
        {
            var ray = new Ray(transform.position, transform.forward);
            GazeInteractable target = null;
            float dist = idleDistance;
            RaycastHit hit;
            bool gotHit = Physics.Raycast(ray, out hit, maxDistance, mask, QueryTriggerInteraction.Collide);
            if (gotHit)
            {
                dist = hit.distance;
                target = hit.collider.GetComponentInParent<GazeInteractable>();
                if (target != null && !target.IsInteractable) target = null;
            }

            if (target != current)
            {
                if (current != null) current.GazeExit();
                current = target;
                dwell = 0f;
                fired = false;
                if (current != null) current.GazeEnter();
            }

            if (current != null)
            {
                bool fire = false;
                if (VRInput.TriggerPressed) fire = true;
                else if (current.useDwell && !fired && Time.time >= cooldownUntil)
                {
                    dwell += Time.deltaTime;
                    if (dwell >= current.dwellTime) fire = true;
                }
                if (fire)
                {
                    fired = true;
                    cooldownUntil = Time.time + 0.9f;
                    dwell = current.dwellTime;
                    Sfx.Click();
                    current.GazeClick(hit);
                }
            }

            // Visual
            float d = Mathf.Max(0.3f, dist * 0.97f);
            reticle.position = transform.position + transform.forward * d;
            reticle.rotation = transform.rotation;
            reticle.localScale = Vector3.one * d * K;

            bool active = current != null && current.IsInteractable;
            ringT = Mathf.MoveTowards(ringT, active ? 1f : 0f, Time.deltaTime * 8f);
            ring.localScale = Vector3.one * Mathf.SmoothStep(0, 1, ringT);

            float progress = active && current.useDwell ? Mathf.Clamp01(dwell / current.dwellTime) : 0f;
            if (!Mathf.Approximately(progress, lastProgress))
            {
                lastProgress = progress;
                FillRing(progressMesh, 1.55f, 2.25f, 48, progress);
            }

            if (hintLabel != null)
            {
                string h = active ? current.hint : "";
                if (hintLabel.text != h)
                {
                    hintLabel.text = h;
                    if (hintBackdrop != null) hintBackdrop.SetActive(!string.IsNullOrEmpty(h));
                }
            }
        }

        static void AddMesh(Transform parent, string name, Mesh mesh, Material mat, float z)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0, 0, z / K);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        static Mesh Ring(float inner, float outer, int seg, float fill)
        {
            var m = new Mesh();
            FillRing(m, inner, outer, seg, fill);
            return m;
        }

        /// <summary>Anillo (o disco si inner=0) que empieza arriba y avanza en sentido horario.</summary>
        static void FillRing(Mesh m, float inner, float outer, int seg, float fill)
        {
            m.Clear();
            int n = Mathf.Max(0, Mathf.RoundToInt(seg * fill));
            if (n == 0) return;
            var v = new Vector3[(n + 1) * 2];
            var t = new int[n * 6];
            for (int i = 0; i <= n; i++)
            {
                float a = Mathf.PI * 0.5f - (float)i / seg * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                v[i * 2] = dir * inner;
                v[i * 2 + 1] = dir * outer;
            }
            for (int i = 0; i < n; i++)
            {
                int b = i * 2;
                t[i * 6 + 0] = b; t[i * 6 + 1] = b + 1; t[i * 6 + 2] = b + 3;
                t[i * 6 + 3] = b; t[i * 6 + 4] = b + 3; t[i * 6 + 5] = b + 2;
            }
            m.vertices = v;
            m.triangles = t;
            m.RecalculateBounds();
        }
    }
}
