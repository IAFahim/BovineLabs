using Counter.Data.Components;
using Unity.Entities;
using UnityEngine;

namespace Counter.Authoring
{
    public class CounterAuthoring : MonoBehaviour
    {
        public int initialValue;

        public class Baker : Baker<CounterAuthoring>
        {
            public override void Bake(CounterAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new CounterComponent
                {
                    Value = authoring.initialValue
                });
            }
        }
    }
}