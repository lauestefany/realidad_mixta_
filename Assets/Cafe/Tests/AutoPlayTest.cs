#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Luar.Tests
{
    /// <summary>
    /// Prueba automática en Play Mode (solo editor): simula la mirada del usuario apuntando la
    /// cámara a cada objetivo y esperando el dwell. Recorre Tutorial → Recorrido → Felicitaciones.
    /// Ejecutar: Luar Café VR > Prueba automática.
    /// </summary>
    public class AutoPlayTest : MonoBehaviour
    {
        const string Flag = "Luar.AutoTest";
        int failures;

        [MenuItem("Luar Café VR/Prueba automática (Play Mode)")]
        public static void Run()
        {
            EditorPrefs.SetBool(Flag, true);
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Tutorial.unity");
            EditorApplication.EnterPlaymode();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!EditorPrefs.GetBool(Flag, false)) return;
            EditorPrefs.SetBool(Flag, false);
            var go = new GameObject("[AutoPlayTest]");
            DontDestroyOnLoad(go);
            go.AddComponent<AutoPlayTest>();
        }

        void Check(bool ok, string what)
        {
            Debug.Log((ok ? "[TEST OK] " : "[TEST FAIL] ") + what);
            if (!ok) failures++;
        }

        static T Find<T>(string name) where T : Component =>
            FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault(c => c.name == name);

        IEnumerator LookAt(Component target, float wait, Vector3 offset = default)
        {
            var rig = PlayerRig.Instance;
            var cam = rig.head;
            var col = target.GetComponentInChildren<Collider>();
            Vector3 point = (col != null && col.enabled ? col.bounds.center : target.transform.position) + offset;
            cam.parent.rotation = Quaternion.LookRotation(point - cam.position);
            yield return new WaitForSeconds(wait);
        }

        IEnumerator LookAway()
        {
            PlayerRig.Instance.head.parent.rotation = Quaternion.LookRotation(Vector3.up);
            yield return new WaitForSeconds(0.3f);
        }

        IEnumerator Start()
        {
            yield return new WaitForSeconds(1f);

            // ---------------- TUTORIAL ----------------
            var tut = FindAnyObjectByType<TutorialController>();
            Check(tut != null && tut.titleText.text.StartsWith("1."), "Tutorial inicia en paso 1: " + tut?.titleText.text);
            var startBtn = Find<GazeButton>("Boton_Comenzar");
            yield return LookAt(startBtn, 2.0f);
            Check(tut.titleText.text.StartsWith("2."), "Gaze sobre 'Comenzar' avanza a paso 2: " + tut.titleText.text);

            var tp = Find<TeleportPoint>("Salto_Barradepráctica");
            Vector3 before = PlayerRig.Instance.transform.position;
            yield return LookAt(tp, 2.2f);
            Check((PlayerRig.Instance.transform.position - before).magnitude > 1f, "Teletransporte mueve al jugador");
            Check(tut.titleText.text.StartsWith("3."), "Paso 3 tras teletransporte: " + tut.titleText.text);

            var cup = FindObjectsByType<Grabbable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).First();
            yield return LookAt(cup, 2.0f);
            Check(Grabbable.Held == cup, "Recoger taza con la mirada");
            Check(tut.titleText.text.StartsWith("4."), "Paso 4 tras recoger: " + tut.titleText.text);

            var zone = FindAnyObjectByType<PlacementZone>();
            yield return LookAt(zone, 2.4f);
            Check(zone.Occupant == cup && Grabbable.Held == null, "Colocar taza en el plato");
            Check(tut.titleText.text.Contains("lograste"), "Tutorial completado: " + tut.titleText.text);
            Check(tut.finishButtons.activeSelf, "Botones finales visibles");

            var goRecorrido = Find<GazeButton>("Boton_IralRecorrido");
            yield return LookAt(goRecorrido, 2.6f);
            yield return new WaitForSeconds(1f);
            Check(SceneManager.GetActiveScene().name == SceneFlow.Recorrido, "Botón lleva al Recorrido: " + SceneManager.GetActiveScene().name);

            // ---------------- RECORRIDO ----------------
            var mgr = FindAnyObjectByType<OrderManager>();
            yield return Serve("cafe", "Mesa 1", mgr);
            Check(mgr.progressText.text.Contains("1 / 3"), "Progreso 1/3: " + mgr.progressText.text);

            // Error intencional: llevar cupcake a la mesa 3 (pide macarons)
            yield return Fetch("cupcake");
            yield return Teleport("Mesa 3");
            var z3 = mgr.orders.First(o => o.zone.acceptedId == "macaron").zone;
            yield return LookAt(z3, 2.0f);
            Check(Grabbable.Held != null && z3.Occupant == null, "Pedido incorrecto es rechazado");
            yield return Teleport("Barra");
            yield return DeliverHeldTo("cupcake", "Mesa 2", mgr);
            yield return Serve("macaron", "Mesa 3", mgr);
            Check(GameStats.Served == 3 && GameStats.Mistakes == 1, $"Estadísticas: servidos {GameStats.Served}, errores {GameStats.Mistakes}");

            yield return new WaitForSeconds(4f);
            Check(SceneManager.GetActiveScene().name == SceneFlow.Felicitaciones, "Al completar va a Felicitaciones: " + SceneManager.GetActiveScene().name);
            yield return new WaitForSeconds(1f);
            var cel = FindAnyObjectByType<CelebrationController>();
            Check(cel != null && cel.statsText.text.Contains("3"), "Resumen en felicitaciones: " + cel?.statsText.text.Replace("\n", " | "));

            var menuBtn = Find<GazeButton>("Boton_Menúprincipal");
            yield return LookAt(menuBtn, 2.6f);
            yield return new WaitForSeconds(1f);
            Check(SceneManager.GetActiveScene().name == SceneFlow.Inicio, "Volver al Inicio: " + SceneManager.GetActiveScene().name);
            Check(Find<GazeButton>("Boton_Salir") != null, "Botón Salir presente en Inicio");

            Debug.Log($"[TEST] Terminado con {failures} fallo(s)");
            EditorApplication.ExitPlaymode();
            if (Application.isBatchMode) EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        IEnumerator Teleport(string label)
        {
            var tp = FindObjectsByType<TeleportPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).First(t => t.label == label);
            yield return LookAway();
            // Apuntar a la flecha flotante del punto (lo más visible a distancia)
            var marker = tp.visual != null ? tp.visual.Find("Flecha") : null;
            yield return LookAt(marker != null ? (Component)marker : tp, 2.0f);
            Check(PlayerRig.Instance.CurrentPoint == tp, "Salto a " + label + " (quedó en: " + PlayerRig.Instance.CurrentPoint?.label + ")");
        }

        IEnumerator Fetch(string id)
        {
            yield return Teleport("Barra");
            Grabbable item;
            if (id == "cafe")
            {
                var disp = FindAnyObjectByType<ItemDispenser>();
                yield return LookAt(disp, 3.2f);
                item = disp.spawnPoint.GetComponentInChildren<Grabbable>();
                Check(item != null, "Cafetera prepara un café");
                yield return LookAway();
            }
            else
            {
                item = FindObjectsByType<Grabbable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                    .First(g => g.itemId == id && g.enabled && g.CurrentZone == null);
            }
            yield return LookAt(item, 2.0f);
            Check(Grabbable.Held == item, "Recoger " + id);
        }

        IEnumerator DeliverHeldTo(string id, string table, OrderManager mgr)
        {
            yield return Teleport(table);
            var zone = mgr.orders.First(o => o.zone.acceptedId == id).zone;
            yield return LookAt(zone, 2.0f);
            Check(zone.Occupant != null && zone.IsCorrect, "Entregar " + id + " en " + table);
        }

        IEnumerator Serve(string id, string table, OrderManager mgr)
        {
            yield return Fetch(id);
            yield return DeliverHeldTo(id, table, mgr);
        }
    }
}
#endif