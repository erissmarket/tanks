using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TankGame.Presentation
{
    public sealed class IntegratedHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, ICancelHandler
    {
        [SerializeField] private IntegratedInput input = null;
        [SerializeField] private int command = 0;
        private readonly HashSet<int> pointers = new HashSet<int>();

        public void OnPointerDown(PointerEventData data)
        {
            if (input.CanAct && data.button == PointerEventData.InputButton.Left && pointers.Add(data.pointerId))
                input.Hold(command, true);
        }

        public void OnPointerUp(PointerEventData data)
        {
            pointers.Remove(data.pointerId);
            if (pointers.Count == 0) input.Hold(command, false);
        }

        private void OnDisable()
        {
            pointers.Clear();
            if (input != null) input.Hold(command, false);
        }
        public void OnPointerExit(PointerEventData data) => OnPointerUp(data);
        public void OnCancel(BaseEventData data) => OnDisable();
        private void OnApplicationFocus(bool focused) { if (!focused) OnDisable(); }
        private void OnApplicationPause(bool paused) { if (paused) OnDisable(); }
    }
}
