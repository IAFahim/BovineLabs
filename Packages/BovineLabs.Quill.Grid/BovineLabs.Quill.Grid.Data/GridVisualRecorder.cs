using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Quill.Grid.Data
{
    public static class GridVisualRecorder
    {
        public static void RecordBlockedCells(
            ref DynamicBuffer<GridBlockedData> buffer,
            in NativeArray<byte> blocked,
            int width,
            int height)
        {
            var count = math.min(blocked.Length, width * height);
            buffer.ResizeUninitialized(count);
            for (var i = 0; i < count; i++)
                buffer[i] = new GridBlockedData(blocked[i]);
        }

        public static void RecordPath(
            ref DynamicBuffer<GridPathVisual> buffer,
            in NativeList<int2> path,
            int width,
            int frame = 0)
        {
            buffer.Clear();
            for (var i = 0; i < path.Length - 1; i++)
                buffer.Add(new GridPathVisual(ToIndex(path[i], width), ToIndex(path[i + 1], width), frame));
        }

        public static void RecordPath(
            ref DynamicBuffer<GridPathVisual> buffer,
            in NativeList<int> path,
            int width,
            int frame = 0)
        {
            _ = width;
            buffer.Clear();
            for (var i = 0; i < path.Length - 1; i++)
                buffer.Add(new GridPathVisual(path[i], path[i + 1], frame));
        }

        public static void RecordCellLayer(
            ref DynamicBuffer<GridCellVisual> buffer,
            in NativeArray<byte> state,
            byte layer,
            int count,
            int frame = 0)
        {
            for (var i = 0; i < count; i++)
                if (state[i] != 0)
                    buffer.Add(new GridCellVisual(i, state[i], layer, frame));
        }

        public static void RecordScalarField(
            ref DynamicBuffer<GridCellVisual> buffer,
            in NativeArray<float> values,
            int count,
            byte layer,
            int frame = 0)
        {
            var length = math.min(values.Length, count);
            var min = float.PositiveInfinity;
            var max = float.NegativeInfinity;

            for (var i = 0; i < length; i++)
            {
                var value = values[i];
                if (!math.isfinite(value))
                    continue;

                min = math.min(min, value);
                max = math.max(max, value);
            }

            var range = max - min;
            for (var i = 0; i < length; i++)
            {
                var value = values[i];
                if (!math.isfinite(value))
                    continue;

                var normalized = range > math.EPSILON ? math.saturate((value - min) / range) : 0f;
                buffer.Add(new GridCellVisual(i, normalized, layer, frame));
            }
        }

        public static void RecordScalarField(
            ref DynamicBuffer<GridCellVisual> buffer,
            in NativeArray<float> values,
            int count,
            byte layer,
            float minValue,
            float maxValue,
            int frame = 0)
        {
            var length = math.min(values.Length, count);
            var range = maxValue - minValue;

            for (var i = 0; i < length; i++)
            {
                var value = values[i];
                if (!math.isfinite(value))
                    continue;

                var normalized = range > math.EPSILON ? math.saturate((value - minValue) / range) : 0f;
                buffer.Add(new GridCellVisual(i, normalized, layer, frame));
            }
        }

        private static int ToIndex(int2 cell, int width)
        {
            return cell.y * width + cell.x;
        }
    }
}
