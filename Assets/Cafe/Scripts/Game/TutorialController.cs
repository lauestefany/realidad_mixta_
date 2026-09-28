using TMPro;
using UnityEngine;

namespace Luar
{
    /// <summary>
    /// Tutorial guiado paso a paso: mirar un botón, teletransportarse, recoger y colocar.
    /// Cada paso muestra su texto y una flecha sobre el objetivo.
    /// </summary>
    public class TutorialController : MonoBehaviour
    {
        [System.Serializable]
        public class Step
        {
            public string title;
            [TextArea] public string body;
            [Tooltip("Flecha/resalte que se activa en este paso.")]
            public GameObject highlight;
        }

        public Step[] steps;
        public TextMeshPro titleText;
        public TextMeshPro bodyText;
        public TextMeshPro stepCounter;
        [Tooltip("Botones que aparecen al terminar (ir al recorrido / menú).")]
        public GameObject finishButtons;
        [Tooltip("Botón 'Comenzar' del primer paso.")]
        public GameObject startButton;

        public const int StepLook = 0, StepTeleport = 1, StepGrab = 2, StepPlace = 3, StepDone = 4;
        int index = -1;

        void OnEnable()
        {
            PlayerRig.Teleported += OnTeleported;
            Grabbable.PickedUp += OnPicked;
            PlacementZone.Placed += OnPlaced;
        }

        void OnDisable()
        {
            PlayerRig.Teleported -= OnTeleported;
            Grabbable.PickedUp -= OnPicked;
            PlacementZone.Placed -= OnPlaced;
        }

        void Start()
        {
            if (finishButtons) finishButtons.SetActive(false);
            GoTo(0);
        }

        /// <summary>Llamado por el botón "Comenzar".</summary>
        public void CompleteLookStep() { if (index == StepLook) Next(); }

        void OnTeleported(TeleportPoint p) { if (index == StepTeleport) Next(); }
        void OnPicked(Grabbable g) { if (index == StepGrab) Next(); }
        void OnPlaced(PlacementZone z, Grabbable g) { if (index == StepPlace) Next(); }

        void Next()
        {
            Sfx.Success();
            GoTo(index + 1);
        }

        void GoTo(int i)
        {
            index = Mathf.Clamp(i, 0, steps.Length - 1);
            for (int k = 0; k < steps.Length; k++)
                if (steps[k].highlight) steps[k].highlight.SetActive(k == index);
            var s = steps[index];
            if (titleText) titleText.text = s.title;
            if (bodyText) bodyText.text = s.body;
            if (stepCounter) stepCounter.text = index < steps.Length - 1 ? "Paso " + (index + 1) + " de " + (steps.Length - 1) : "¡Listo!";
            if (startButton) startButton.SetActive(index == StepLook);
            if (finishButtons) finishButtons.SetActive(index == steps.Length - 1);
        }
    }
}
