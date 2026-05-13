using Unity.Mathematics;
using UnityEngine;

namespace BovineLabs.Quill.Grid.Data
{
    public readonly struct GridPalette
    {
        public static readonly Color Obstacle = new(0.2f, 0.2f, 0.2f, 0.8f);
        public static readonly Color ObstacleOutline = new(0.4f, 0.4f, 0.4f, 1f);
        public static readonly Color Frontier = new(1f, 1f, 0f, 0.6f);
        public static readonly Color Closed = new(0.5f, 0.5f, 0.5f, 0.3f);
        public static readonly Color Path = new(0f, 1f, 0f, 1f);
        public static readonly Color PathOutline = new(0f, 0.7f, 0f, 1f);
        public static readonly Color HeatmapCold = new(0f, 0f, 1f, 0.5f);
        public static readonly Color HeatmapHot = new(1f, 0f, 0f, 0.8f);
        public static readonly Color Conflict = new(1f, 0f, 0f, 0.8f);
        public static readonly Color Constraint = new(1f, 0.5f, 0f, 0.6f);
        public static readonly Color Interval = new(0f, 0.8f, 1f, 0.7f);
        public static readonly Color IntervalExpanded = new(0f, 1f, 0.8f, 0.7f);
        public static readonly Color RootPoint = new(1f, 1f, 0f, 1f);
        public static readonly Color GridLine = new(0.3f, 0.3f, 0.3f, 0.3f);
        public static readonly Color GridBorder = new(0.6f, 0.6f, 0.6f, 0.6f);
        public static readonly Color Label = new(1f, 1f, 1f, 1f);
        public static readonly Color Arrow = new(0.8f, 0.8f, 0.2f, 0.8f);
        public static readonly Color VectorField = new(0.2f, 0.6f, 1f, 0.7f);

        public static Color AgentColor(int index)
        {
            if (index < 0) return Color.white;

            switch (index % 8)
            {
                case 0: return new Color(1f, 0.3f, 0.3f, 1f);
                case 1: return new Color(0.3f, 0.3f, 1f, 1f);
                case 2: return new Color(0.3f, 1f, 0.3f, 1f);
                case 3: return new Color(1f, 1f, 0.3f, 1f);
                case 4: return new Color(1f, 0.3f, 1f, 1f);
                case 5: return new Color(0.3f, 1f, 1f, 1f);
                case 6: return new Color(1f, 0.6f, 0.3f, 1f);
                default: return new Color(0.6f, 0.3f, 1f, 1f);
            }
        }

        public static Color LerpHeatmap(float t)
        {
            t = math.saturate(t);
            var r = HeatmapHot.r * t + HeatmapCold.r * (1f - t);
            var g = HeatmapHot.g * t + HeatmapCold.g * (1f - t);
            var b = HeatmapHot.b * t + HeatmapCold.b * (1f - t);
            var a = HeatmapHot.a * t + HeatmapCold.a * (1f - t);
            return new Color(r, g, b, a);
        }
    }
}