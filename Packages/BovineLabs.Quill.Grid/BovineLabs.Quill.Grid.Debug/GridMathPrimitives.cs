using BovineLabs.Grid;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace BovineLabs.Quill.Grid.Debug
{
    public static class GridMathPrimitives
    {
        public static bool TryGetLocalPosition(int2 xy, int2 size, float3 blockSize, float spacing, out float3 localPos)
        {
            float2 offset = (new float2(size.x, size.y) - 1f) * 0.5f;
            float2 p = (new float2(xy.x, xy.y) - offset) * (blockSize.xz + spacing);
            localPos = new float3(p.x, 0f, p.y);
            return true;
        }

        public static bool TryGetUV(int2 xy, int2 size, out float2 uv)
        {
            uv = new float2(xy.x, xy.y) / math.max(new float2(size.x - 1, size.y - 1), 1f);
            return true;
        }
    }

    public static class GridVisualPrimitives
    {
        public static bool TryCalculateHoverTarget(
            float3 worldPos,
            float3 hoverPos,
            bool isHovering,
            float radius,
            float maxDepth,
            float4 defaultColor,
            float4 hoverColor,
            out float targetDepth,
            out float4 targetColor)
        {
            if (!isHovering)
            {
                targetDepth = 0f;
                targetColor = defaultColor;
                return true;
            }

            float dist = math.distance(worldPos.xz, hoverPos.xz);
            float t = math.saturate(1f - (dist / radius));

            targetDepth = math.lerp(0f, maxDepth, t);
            targetColor = math.lerp(defaultColor, hoverColor, t);
            return true;
        }

        public static bool TryStepState(in GridCellVisualState current, float targetDepth, float4 targetColor, float dt,
            float speed, out GridCellVisualState next)
        {
            float t = math.saturate(dt * speed);
            next = new GridCellVisualState
            {
                TargetDepth = targetDepth,
                TargetColor = targetColor,
                CurrentDepth = math.lerp(current.CurrentDepth, targetDepth, t),
                CurrentColor = math.lerp(current.CurrentColor, targetColor, t)
            };
            return true;
        }
    }

    public static class GridQuillPrimitives
    {
        public static bool TryDrawBlock(this ref Drawer drawer, float3 center, float3 size, float depth, float4 color)
        {
            float3 pos = center + new float3(0f, -depth, 0f);
            var col = new UnityEngine.Color(color.x, color.y, color.z, color.w);

            drawer.Cuboid(pos, quaternion.identity, size, col);
            return true;
        }

        public static bool TryDrawReveal(this ref Drawer drawer, float3 center, float2 size, float yOffset,
            float4 color, float2 infoData, float textSize)
        {
            float3 pos = center + new float3(0f, yOffset, 0f);
            var col = new UnityEngine.Color(color.x, color.y, color.z, color.w);

            float3 p0 = pos + new float3(-size.x, 0f, -size.y) * 0.5f;
            float3 p1 = pos + new float3(-size.x, 0f, size.y) * 0.5f;
            float3 p2 = pos + new float3(size.x, 0f, size.y) * 0.5f;
            float3 p3 = pos + new float3(size.x, 0f, -size.y) * 0.5f;

            drawer.Line(p0, p1, col);
            drawer.Line(p1, p2, col);
            drawer.Line(p2, p3, col);
            drawer.Line(p3, p0, col);
            drawer.Line(p0, p2, col);

            FixedString32Bytes text = default;
            text.Append("u: ");
            FormatFloatFast(ref text, infoData.x);
            text.Append(", v: ");
            FormatFloatFast(ref text, infoData.y);

            drawer.Text32(pos, text, col, textSize);
            return true;
        }

        private static void FormatFloatFast(ref FixedString32Bytes str, float val)
        {
            if (val < 0)
            {
                str.Append('-');
                val = -val;
            }

            int whole = (int)val;
            int frac = (int)(math.abs(val - whole) * 100f);
            str.Append(whole);
            str.Append('.');
            if (frac < 10) str.Append('0');
            str.Append(frac);
        }
    }
    
    public static class GridVisualizerMath
    {
        public static int ToIndex(int2 cell, int width) =>
            cell.y * width + cell.x;

        public static int2 ToCell(int index, int width) =>
            new int2(index % width, index / width);

        public static float3 GetCellPosition(int2 cell, int2 size, float3 blockSize, float spacing, float3 center)
        {
            float2 step = new float2(blockSize.x + spacing, blockSize.z + spacing);
            float2 offset = (new float2(cell.x, cell.y) - (new float2(size.x, size.y) - 1f) * 0.5f) * step;
            return center + new float3(offset.x, 0f, offset.y);
        }

        public static float2 GetCellUV(int2 cell, int2 size) =>
            new float2(cell.x / math.max(1f, size.x - 1f), cell.y / math.max(1f, size.y - 1f));

        public static float GetHoverInfluence(float3 cellPos, float3 hoverPos, bool isHovering, float radius)
        {
            float dist = math.select(float.MaxValue, math.distance(cellPos.xz, hoverPos.xz), isHovering);
            return math.smoothstep(0f, 1f, math.saturate(1f - (dist / math.max(0.001f, radius))));
        }

        public static float GetTargetDepth(float influence, float maxDepth) =>
            influence * maxDepth;

        public static float4 GetTargetColor(float influence, float4 defaultColor, float4 hoverColor) =>
            math.lerp(defaultColor, hoverColor, influence);

        public static float Step(float current, float target, float speed, float dt) =>
            math.lerp(current, target, math.saturate(speed * dt));

        public static float4 Step(float4 current, float4 target, float speed, float dt) =>
            math.lerp(current, target, math.saturate(speed * dt));

        public static Color ToColor(this float4 c) => new(c.x, c.y, c.z, c.w);
    }
}