using UnityEngine;
using UnityEngine.UIElements;

namespace Scripts.Stats
{
    public class SimpleDragManipulator : PointerManipulator
    {
        private bool isDragging;
        private Vector2 pointerStartPosition;
        private Vector2 targetStartPosition;

        public SimpleDragManipulator(VisualElement target)
        {
            this.target = target;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0) return;

            if (evt.target is Button) return;

            target.style.position = Position.Absolute;
            isDragging = true;
            pointerStartPosition = (Vector2)evt.position;
            targetStartPosition = target.worldBound.position;

            target.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!isDragging || !target.HasPointerCapture(evt.pointerId)) return;

            Vector2 pointerCurrent = (Vector2)evt.position;
            Vector2 pointerDelta = pointerCurrent - pointerStartPosition;
            Vector2 newWorldPosition = targetStartPosition + pointerDelta;
            
            newWorldPosition = ClampToScreenBounds(newWorldPosition);
            
            Vector2 newLocalPosition = target.parent != null ? target.parent.WorldToLocal(newWorldPosition) : newWorldPosition;

            target.style.left = newLocalPosition.x;
            target.style.top = newLocalPosition.y;

            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!isDragging || !target.HasPointerCapture(evt.pointerId)) return;

            isDragging = false;
            target.ReleasePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            isDragging = false;
        }
        
        private Vector2 ClampToScreenBounds(Vector2 position)
        {
            var panelRect = target.worldBound;
            var screenW = Screen.width;
            var screenH = Screen.height;
            
            float minX = 0;
            float minY = 0;
            float maxX = screenW - panelRect.width;
            float maxY = screenH - panelRect.height;
            
            float clampedX = Mathf.Clamp(position.x, minX, maxX);
            float clampedY = Mathf.Clamp(position.y, minY, maxY);
            
            return new Vector2(clampedX, clampedY);
        }
    }
}
