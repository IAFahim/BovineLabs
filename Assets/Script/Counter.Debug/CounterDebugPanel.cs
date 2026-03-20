using Examples._01_AssemblyArchitecture.Script.Counter.Data;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Examples._01_AssemblyArchitecture.Script.Counter.Debug
{
    public class CounterDebugPanel : MonoBehaviour
    {
        public Text CounterText;

        void Update()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

            var entityManager = world.EntityManager;
            var query = entityManager.CreateEntityQuery(typeof(CounterComponent));

            if (query.TryGetSingleton(out CounterComponent counter))
            {
                CounterText.text = $"Counter: {counter.Value}";
            }

            query.Dispose();
        }
    }
}
