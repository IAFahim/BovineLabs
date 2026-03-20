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
            if (query.TryGetSingleton(out CounterComponent counter)) SetLabelAsCounter(_counterLabel, counter);
            query.Dispose();
        }

        private void SetLabelAsCounter(Label label, CounterComponent counter)
        {
            label.text = $"Counter: {counter.Value}";
        }
    }
}