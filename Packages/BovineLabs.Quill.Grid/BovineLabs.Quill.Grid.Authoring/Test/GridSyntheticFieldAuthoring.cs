using BovineLabs.Core.Authoring.EntityCommands;
using BovineLabs.Quill.Grid.Data;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BovineLabs.Quill.Grid.Authoring.Test
{
    public class GridSyntheticFieldAuthoring : MonoBehaviour
    {
        [Header("Grid")]
        public int gridWidth = 32;
        public int gridHeight = 32;
        public float cellSize = 1f;
        public Vector3 origin = Vector3.zero;

        [Header("Visualizer")]
        public bool enabledByDefault = true;
        public string algorithmName = "Synthetic Field";
        public string category = "Grid/Test";

        [Header("Draw Toggles")]
        public bool drawGrid = true;
        public bool drawHeatmap = true;
        public bool drawVectorField = true;
        public bool drawLabels = false;
        public bool drawPath = false;
        public bool drawObstacles = false;

        [Header("Synthetic Field")]
        public float baseWaveStrength = 0.15f;
        public float memberFalloffPower = 2f;
        public float memberMotionSpeed = 1f;
        public bool animateMembers = true;
        public bool writeVectorField = true;
        public bool writeLabels = false;

        [Header("Smooth Pillar Style")]
        public float3 blockSize = new float3(0.95f, 1f, 0.95f);
        public float spacing = 0.05f;
        public float baseHeight = 1.5f;
        public float maxDepth = 1.25f;
        public float transitionSpeed = 10f;

        public Color baseColor = new Color(0.12f, 0.11f, 0.11f, 1f);
        public Color outlineColor = new Color(0.25f, 0.22f, 0.22f, 1f);
        public Color coldColor = new Color(0.1f, 0.8f, 0.2f, 0.65f);
        public Color hotColor = new Color(0.95f, 0.15f, 0.08f, 0.9f);

        [Header("Members")]
        public SyntheticMember[] members =
        {
            new()
            {
                name = "Player Fear",
                position = new Vector2(8, 8),
                radius = 7f,
                strength = 1f,
                motionRadius = 3f,
                phase = 0f,
                color = new Color(1f, 0.1f, 0.05f, 1f)
            },
            new()
            {
                name = "Weapon Pressure",
                position = new Vector2(22, 18),
                radius = 5f,
                strength = 0.75f,
                motionRadius = 2f,
                phase = 2.1f,
                color = new Color(1f, 0.55f, 0.05f, 1f)
            }
        };

        [System.Serializable]
        public class SyntheticMember
        {
            public string name = "Member";
            public Vector2 position;
            public float radius = 5f;
            public float strength = 1f;
            public float motionRadius = 2f;
            public float phase;
            public Color color = Color.red;
        }
    }

    public class GridSyntheticFieldBaker : Baker<GridSyntheticFieldAuthoring>
    {
        public override void Bake(GridSyntheticFieldAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            var commands = new BakerCommands(this, entity);
            var visualizerAuthoring = authoring.GetComponent<GridVisualizerAuthoring>();
            var hasEnabledVisualizerAuthoring = visualizerAuthoring != null && visualizerAuthoring.enabled;

            if (!hasEnabledVisualizerAuthoring)
            {
                var builder = GridSyntheticFieldAuthoringBuilderExtensions.FromAuthoring(authoring);
                builder.ApplyTo(ref commands);
            }

            AddComponent(entity, new GridSyntheticFieldConfig
            {
                BaseWaveStrength = authoring.baseWaveStrength,
                MemberFalloffPower = authoring.memberFalloffPower,
                MemberMotionSpeed = authoring.memberMotionSpeed,
                AnimateMembers = authoring.animateMembers,
                WriteVectorField = authoring.writeVectorField,
                WriteLabels = authoring.writeLabels
            });

            AddComponent(entity, new GridPillarStyleConfig
            {
                BlockSize = authoring.blockSize,
                Spacing = authoring.spacing,
                BaseHeight = authoring.baseHeight,
                MaxDepth = authoring.maxDepth,
                TransitionSpeed = authoring.transitionSpeed,
                BaseColor = ToFloat4(authoring.baseColor),
                OutlineColor = ToFloat4(authoring.outlineColor),
                ColdColor = ToFloat4(authoring.coldColor),
                HotColor = ToFloat4(authoring.hotColor)
            });

            AddComponent(entity, new GridVisualizerStyle
            {
                Type = GridVisualStyleType.Pillar
            });

            AddBuffer<GridCellVisualState>(entity);

            var memberBuffer = AddBuffer<GridSyntheticMember>(entity);
            foreach (var member in authoring.members)
            {
                memberBuffer.Add(new GridSyntheticMember
                {
                    BasePosition = member.position,
                    Radius = math.max(0.001f, member.radius),
                    Strength = member.strength,
                    MotionRadius = member.motionRadius,
                    Phase = member.phase,
                    Color = ToFloat4(member.color)
                });
            }
        }

        private static float4 ToFloat4(Color color)
        {
            return new float4(color.r, color.g, color.b, color.a);
        }
    }
}
