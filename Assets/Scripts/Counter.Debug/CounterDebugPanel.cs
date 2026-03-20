using Counter.Data.Components;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace Counter.Debug
{
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
            var query = entityManager.CreateEntityQuery(typeof(CounterComponent));
            if (query.TryGetSingleton(out CounterComponent counter)) _counterLabel.text = $"Counter: {counter.Value}";
            query.Dispose();
        }
    }
}