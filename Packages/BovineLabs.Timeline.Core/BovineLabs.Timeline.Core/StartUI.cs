using BovineLabs.Timeline.Data;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace BovineLabs.Timeline.Core
{
    /// <summary>
    ///     Simple MonoBehaviour helper that activates all Timeline-referenced entities
    ///     on the first Update. Disables itself once triggered.
    /// </summary>
    public class StartUI : MonoBehaviour
    {
        private void Update()
        {
            TriggerTimeline();
        }

        public void TriggerTimeline()
        {
            if (World.DefaultGameObjectInjectionWorld == null)
                return;

            var em = World.DefaultGameObjectInjectionWorld.EntityManager;

            using var query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<TimelineReference>()
                .WithPresent<TimelineActive>()
                .Build(em);

            foreach (var e in query.ToEntityArray(Allocator.Temp))
                em.SetComponentEnabled<TimelineActive>(e, true);

            if (!query.IsEmpty)
                enabled = false;
        }
    }
}