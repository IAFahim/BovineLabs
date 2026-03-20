using Counter.Data.Components;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace Counter.Debug
{
    [RequireComponent(typeof(UIDocument))]
    public class CounterDebugPanel : MonoBehaviour
    {
        public UIDocument uiDocument;
        private Label _counterLabel;

        private void OnValidate()
        {
            uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            _counterLabel = new Label();
            uiDocument.rootVisualElement.Add(_counterLabel);
        }

        private void Update()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            var entityManager = world.EntityManager;
            entityManager.CompleteDependencyBeforeRO<CounterComponent>();
            var query = entityManager.CreateEntityQuery(typeof(CounterComponent));
            if (query.TryGetSingleton(out CounterComponent counter)) SetLabel(_counterLabel, counter, "Counter: ", "");
            query.Dispose();
        }

        private void SetLabel(Label label, CounterComponent counter, FixedString128Bytes start, FixedString128Bytes end)
        {
            label.text = start + counter.Value.ToString() + end;
        }
    }
}