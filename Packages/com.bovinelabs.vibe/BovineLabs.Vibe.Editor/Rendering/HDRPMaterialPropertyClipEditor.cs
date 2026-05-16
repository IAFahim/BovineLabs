// <copyright file="HDRPMaterialPropertyClipEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_HDRP
namespace BovineLabs.Vibe.Editor.Rendering
{
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Timeline.Editor;
    using BovineLabs.Vibe.Authoring.Rendering;
    using BovineLabs.Vibe.Data.Rendering;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(HDRPMaterialPropertyClip))]
    public class HDRPMaterialPropertyClipEditor : DOTSClipEditor
    {
        private PropertyField alphaCutoffField;
        private PropertyField aoRemapMaxField;
        private PropertyField aoRemapMinField;
        private PropertyField detailAlbedoScaleField;
        private PropertyField detailNormalScaleField;
        private PropertyField detailSmoothnessScaleField;
        private PropertyField diffusionProfileHashField;
        private PropertyField metallicField;
        private PropertyField smoothnessField;
        private PropertyField smoothnessRemapMaxField;
        private PropertyField smoothnessRemapMinField;
        private PropertyField thicknessField;
        private PropertyField emissiveColorField;
        private PropertyField baseColorField;
        private PropertyField specularColorField;
        private PropertyField thicknessRemapField;
        private PropertyField unlitColorField;

        private SerializedProperty propertiesProperty;

        /// <inheritdoc/>
        protected override VisualElement CreateElement(SerializedProperty property)
        {
            switch (property.name)
            {
                case "properties":
                    this.propertiesProperty = property;
                    var propertiesField = CreatePropertyField(property);
                    propertiesField.RegisterValueChangeCallback(this.OnSettingsChanged);
                    return propertiesField;
                case "alphaCutoff":
                    return this.alphaCutoffField = CreatePropertyField(property);
                case "aoRemapMax":
                    return this.aoRemapMaxField = CreatePropertyField(property);
                case "aoRemapMin":
                    return this.aoRemapMinField = CreatePropertyField(property);
                case "detailAlbedoScale":
                    return this.detailAlbedoScaleField = CreatePropertyField(property);
                case "detailNormalScale":
                    return this.detailNormalScaleField = CreatePropertyField(property);
                case "detailSmoothnessScale":
                    return this.detailSmoothnessScaleField = CreatePropertyField(property);
                case "diffusionProfileHash":
                    return this.diffusionProfileHashField = CreatePropertyField(property);
                case "metallic":
                    return this.metallicField = CreatePropertyField(property);
                case "smoothness":
                    return this.smoothnessField = CreatePropertyField(property);
                case "smoothnessRemapMax":
                    return this.smoothnessRemapMaxField = CreatePropertyField(property);
                case "smoothnessRemapMin":
                    return this.smoothnessRemapMinField = CreatePropertyField(property);
                case "thickness":
                    return this.thicknessField = CreatePropertyField(property);
                case "emissiveColor":
                    return this.emissiveColorField = CreatePropertyField(property);
                case "baseColor":
                    return this.baseColorField = CreatePropertyField(property);
                case "specularColor":
                    return this.specularColorField = CreatePropertyField(property);
                case "thicknessRemap":
                    return this.thicknessRemapField = CreatePropertyField(property);
                case "unlitColor":
                    return this.unlitColorField = CreatePropertyField(property);
            }

            return base.CreateElement(property);
        }

        /// <inheritdoc/>
        protected override void PostElementCreation(VisualElement root, bool createdElements)
        {
            this.UpdateVisibility();
        }

        private void OnSettingsChanged(SerializedPropertyChangeEvent evt)
        {
            this.UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            var flags = (HDRPMaterialPropertyFlags)this.propertiesProperty.intValue;

            ElementUtility.SetVisible(this.alphaCutoffField, (flags & HDRPMaterialPropertyFlags.AlphaCutoff) != 0);
            ElementUtility.SetVisible(this.aoRemapMaxField, (flags & HDRPMaterialPropertyFlags.AORemapMax) != 0);
            ElementUtility.SetVisible(this.aoRemapMinField, (flags & HDRPMaterialPropertyFlags.AORemapMin) != 0);
            ElementUtility.SetVisible(this.detailAlbedoScaleField, (flags & HDRPMaterialPropertyFlags.DetailAlbedoScale) != 0);
            ElementUtility.SetVisible(this.detailNormalScaleField, (flags & HDRPMaterialPropertyFlags.DetailNormalScale) != 0);
            ElementUtility.SetVisible(this.detailSmoothnessScaleField, (flags & HDRPMaterialPropertyFlags.DetailSmoothnessScale) != 0);
            ElementUtility.SetVisible(this.diffusionProfileHashField, (flags & HDRPMaterialPropertyFlags.DiffusionProfileHash) != 0);
            ElementUtility.SetVisible(this.metallicField, (flags & HDRPMaterialPropertyFlags.Metallic) != 0);
            ElementUtility.SetVisible(this.smoothnessField, (flags & HDRPMaterialPropertyFlags.Smoothness) != 0);
            ElementUtility.SetVisible(this.smoothnessRemapMaxField, (flags & HDRPMaterialPropertyFlags.SmoothnessRemapMax) != 0);
            ElementUtility.SetVisible(this.smoothnessRemapMinField, (flags & HDRPMaterialPropertyFlags.SmoothnessRemapMin) != 0);
            ElementUtility.SetVisible(this.thicknessField, (flags & HDRPMaterialPropertyFlags.Thickness) != 0);
            ElementUtility.SetVisible(this.emissiveColorField, (flags & HDRPMaterialPropertyFlags.EmissiveColor) != 0);
            ElementUtility.SetVisible(this.baseColorField, (flags & HDRPMaterialPropertyFlags.BaseColor) != 0);
            ElementUtility.SetVisible(this.specularColorField, (flags & HDRPMaterialPropertyFlags.SpecularColor) != 0);
            ElementUtility.SetVisible(this.thicknessRemapField, (flags & HDRPMaterialPropertyFlags.ThicknessRemap) != 0);
            ElementUtility.SetVisible(this.unlitColorField, (flags & HDRPMaterialPropertyFlags.UnlitColor) != 0);
        }
    }
}
#endif
