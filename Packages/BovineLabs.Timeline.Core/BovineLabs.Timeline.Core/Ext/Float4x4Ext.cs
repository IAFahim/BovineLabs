using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Unity.Transforms;

namespace BovineLabs.Timeline.Core
{
    public static class Float4x4Ext
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExtractLocalTransform(this float4x4 m, out LocalTransform localTransform)
        {
            var pos = m.c3.xyz;
            var scale3 = new float3(math.length(m.c0.xyz), math.length(m.c1.xyz), math.length(m.c2.xyz));
            var scale = scale3.x;

            if (math.lengthsq(scale3) > 1e-12f)
            {
                m.c0.xyz /= scale3.x;
                m.c1.xyz /= scale3.y;
                m.c2.xyz /= scale3.z;
            }

            localTransform = LocalTransform.FromPositionRotationScale(pos, new quaternion(m), scale);
        }
    }
}