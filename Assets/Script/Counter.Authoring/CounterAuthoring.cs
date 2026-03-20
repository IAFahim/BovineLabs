using Examples._01_AssemblyArchitecture.Script.Counter.Data;
using Unity.Entities;
using UnityEngine;

namespace Examples._01_AssemblyArchitecture.Script.Counter.Authoring
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
