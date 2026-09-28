using System.Collections;
using TMPro;
using UnityEngine;

namespace Luar
{
    /// <summary>
    /// Recorrido: servir cada pedido en la mesa correcta. Muestra el progreso en la pizarra
    /// y al completar todo lleva a la escena de felicitaciones.
    /// </summary>
    public class OrderManager : MonoBehaviour
    {
        [System.Serializable]
        public class Order
        {
            public PlacementZone zone;
            [Tooltip("Globo del pedido sobre la mesa (se oculta al servir).")]
            public GameObject bubble;
            [Tooltip("Corazón/check que aparece al servir.")]
            public GameObject doneMark;
        }

        public Order[] orders;
        public TextMeshPro progressText;
        public TextMeshPro messageText;
        public GameObject messageBox;
        public float delayBeforeFinish = 2.5f;

        int served;
        Coroutine msgRoutine;

        void OnEnable()
        {
            PlacementZone.Placed += OnPlaced;
            PlacementZone.Rejected += OnRejected;
        }

        void OnDisable()
        {
            PlacementZone.Placed -= OnPlaced;
            PlacementZone.Rejected -= OnRejected;
        }

        void Start()
        {
            GameStats.Reset(Time.time);
            foreach (var o in orders) if (o.doneMark) o.doneMark.SetActive(false);
            Refresh();
            ShowMessage("Mira los globos sobre las mesas: cada cliente pidió algo distinto.", 6f);
        }

        void OnPlaced(PlacementZone zone, Grabbable item)
        {
            foreach (var o in orders)
            {
                if (o.zone != zone) continue;
                served++;
                GameStats.Served = served;
                if (o.bubble) o.bubble.SetActive(false);
                if (o.doneMark) o.doneMark.SetActive(true);
                // Evita que el pedido servido se vuelva a mover.
                item.enabled = false;
                Refresh();
                if (served >= orders.Length) StartCoroutine(Finish());
                else ShowMessage("¡Pedido entregado! Te faltan " + (orders.Length - served) + ".", 3f);
                return;
            }
        }

        void OnRejected(PlacementZone zone, Grabbable item)
        {
            GameStats.Mistakes++;
            ShowMessage("Ups, ese no es el pedido de esta mesa. Revisa el globo.", 3f);
        }

        void Refresh()
        {
            if (progressText != null)
                progressText.text = "Pedidos servidos  <b>" + served + " / " + orders.Length + "</b>";
        }

        void ShowMessage(string msg, float seconds)
        {
            if (messageText == null) return;
            if (msgRoutine != null) StopCoroutine(msgRoutine);
            msgRoutine = StartCoroutine(MessageRoutine(msg, seconds));
        }

        IEnumerator MessageRoutine(string msg, float seconds)
        {
            messageText.text = msg;
            if (messageBox) messageBox.SetActive(true);
            yield return new WaitForSeconds(seconds);
            messageText.text = "";
            if (messageBox) messageBox.SetActive(false);
        }

        IEnumerator Finish()
        {
            GameStats.Duration = Time.time - GameStats.StartTime;
            GameStats.Completed = true;
            ShowMessage("¡Todos los clientes están felices!", delayBeforeFinish + 1f);
            Sfx.Success();
            yield return new WaitForSeconds(delayBeforeFinish);
            SceneFlow.Load(SceneFlow.Felicitaciones);
        }
    }
}
