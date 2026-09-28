using System.Collections;
using TMPro;
using UnityEngine;

namespace Luar
{
    /// <summary>Pantalla de felicitaciones: lanza confeti y muestra el resumen del recorrido.</summary>
    public class CelebrationController : MonoBehaviour
    {
        public ParticleSystem[] confetti;
        public TextMeshPro statsText;
        public float burstEvery = 4f;

        IEnumerator Start()
        {
            if (statsText != null)
            {
                if (GameStats.Completed)
                {
                    int m = Mathf.FloorToInt(GameStats.Duration / 60f);
                    int s = Mathf.FloorToInt(GameStats.Duration % 60f);
                    statsText.text = $"Serviste <b>{GameStats.Served}</b> pedidos en <b>{m}:{s:00}</b> min" +
                                     (GameStats.Mistakes == 0 ? "\nsin ningún error. ¡Barista estrella!" : $"\ncon {GameStats.Mistakes} intento(s) extra.");
                }
                else statsText.text = "¡Gracias por visitar Luar Café!";
                statsText.text += "\n\n<i><color=#C9738A>Ahora eres barista oficial de <b>Luar Café</b></color></i>";
            }
            yield return new WaitForSeconds(0.6f);
            Sfx.Success();
            while (true)
            {
                foreach (var c in confetti) if (c) c.Play();
                yield return new WaitForSeconds(burstEvery);
            }
        }
    }
}
