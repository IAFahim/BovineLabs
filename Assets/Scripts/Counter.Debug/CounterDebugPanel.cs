using Counter.Data.Components;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Counter.Debug
{
    public class CounterDebugPanel : MonoBehaviour
    {
        public Text CounterText;

        private void Update()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

            var entityManager = world.EntityManager;
            var query = entityManager.CreateEntityQuery(typeof(CounterComponent));

            if (query.TryGetSingleton(out CounterComponent counter)) CounterText.text = $"Counter: {counter.Value}";

            query.Dispose();
        }
    }
}