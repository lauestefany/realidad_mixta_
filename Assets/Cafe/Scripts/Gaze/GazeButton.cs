using UnityEngine;
using UnityEngine.Events;

namespace Luar
{
    /// <summary>Botón 3D activable con la mirada. Puede cambiar de escena, salir o lanzar un evento.</summary>
    public class GazeButton : GazeInteractable
    {
        public enum Action { None, LoadScene, Quit }

        public Action action = Action.None;
        public string sceneName;
        public UnityEvent onClick = new UnityEvent();

        [Tooltip("Pequeño 'hundimiento' al pulsar.")]
        public Transform pressVisual;

        float pressT;
        Vector3 pressBase;

        protected override void Awake()
        {
            base.Awake();
            if (string.IsNullOrEmpty(hint)) hint = "Mira o toca";
            if (pressVisual != null) pressBase = pressVisual.localPosition;
        }

        public override void GazeClick(RaycastHit hit)
        {
            pressT = 1f;
            onClick.Invoke();
            switch (action)
            {
                case Action.LoadScene: SceneFlow.Load(sceneName); break;
                case Action.Quit: SceneFlow.Quit(); break;
            }
        }

        protected override void Update()
        {
            base.Update();
            if (pressVisual == null || pressT <= 0f) return;
            pressT = Mathf.MoveTowards(pressT, 0f, Time.deltaTime * 4f);
            pressVisual.localPosition = pressBase + Vector3.forward * 0.02f * Mathf.Sin(pressT * Mathf.PI);
        }
    }
}
