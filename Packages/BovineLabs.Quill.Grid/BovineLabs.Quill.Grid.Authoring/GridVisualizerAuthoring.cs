using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BovineLabs.Quill.Grid.Authoring
{
    public class GridVisualizerAuthoring : MonoBehaviour
    {
        public int2 Size = new int2(8, 8);
        public float3 BlockSize = new float3(1f, 1f, 1f);
        public float Spacing = 0.1f;
    
        public float HoverRadius = 3f;
        public float HoverDepth = 2f;
        public Color DefaultColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        public Color HoverColor = new Color(0.8f, 0.1f, 0.1f, 1f);
    
        public float TransitionSpeed = 10f;
    
        public bool RevealEnabled;
        public float RevealYOffset = -2f;
        public Color RevealTextColor = Color.white;
        public float RevealTextSize = 16f;

        public bool IsCenter;
    
        public class GridVisualizerBaker : Baker<GridVisualizerAuthoring>
        {
            public override void Bake(GridVisualizerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable);
        
                AddComponent(entity, new GridVisualizerConfig
                {
                    Size = authoring.Size,
                    BlockSize = authoring.BlockSize,
                    Spacing = authoring.Spacing,
                    HoverRadius = authoring.HoverRadius,
                    HoverDepth = authoring.HoverDepth,
                    DefaultColor = new float4(authoring.DefaultColor.r, authoring.DefaultColor.g, authoring.DefaultColor.b, authoring.DefaultColor.a),
                    HoverColor = new float4(authoring.HoverColor.r, authoring.HoverColor.g, authoring.HoverColor.b, authoring.HoverColor.a),
                    TransitionSpeed = authoring.TransitionSpeed,
                    RevealEnabled = authoring.RevealEnabled,
                    RevealYOffset = authoring.RevealYOffset,
                    RevealTextColor = new float4(authoring.RevealTextColor.r, authoring.RevealTextColor.g, authoring.RevealTextColor.b, authoring.RevealTextColor.a),
                    RevealTextSize = authoring.RevealTextSize
                });

                AddComponent<GridVisualizerInput>(entity);
                AddBuffer<GridCellVisualState>(entity);

                if (authoring.IsCenter)
                {
                    AddComponent<GridCenterTag>(entity);
                }
            }
        }
    }
}

