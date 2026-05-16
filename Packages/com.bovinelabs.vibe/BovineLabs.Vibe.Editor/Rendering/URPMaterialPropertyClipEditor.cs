// <copyright file="URPMaterialPropertyClipEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_URP
namespace BovineLabs.Vibe.Editor.Rendering
{
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Timeline.Editor;
    using BovineLabs.Vibe.Authoring.Rendering;
    using BovineLabs.Vibe.Data.Rendering;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(URPMaterialPropertyClip))]
    public class URPMaterialPropertyClipEditor : DOTSClipEditor
    {
        private PropertyField bumpScaleField;
        private PropertyField cutoffField;
        private PropertyField metallicField;
        private PropertyField occlusionStrengthField;
        private PropertyField smoothnessField;
        private PropertyField baseColorField;
        private PropertyField emissionColorField;
        private PropertyField specColorField;

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
                case "bumpScale":
                    return this.bumpScaleField = CreatePropertyField(property);
                case "cutoff":
                    return this.cutoffField = CreatePropertyField(property);
                case "metallic":
                    return this.metallicField = CreatePropertyField(property);
                case "occlusionStrength":
                    return this.occlusionStrengthField = CreatePropertyField(property);
                case "smoothness":
                    return this.smoothnessField = CreatePropertyField(property);
                case "baseColor":
                    return this.baseColorField = CreatePropertyField(property);
                case "emissionColor":
                    return this.emissionColorField = CreatePropertyField(property);
                case "specColor":
                    return this.specColorField = CreatePropertyField(property);
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
            var flags = (URPMaterialPropertyFlags)this.propertiesProperty.intValue;

            ElementUtility.SetVisible(this.bumpScaleField, (flags & URPMaterialPropertyFlags.BumpScale) != 0);
            ElementUtility.SetVisible(this.cutoffField, (flags & URPMaterialPropertyFlags.Cutoff) != 0);
            ElementUtility.SetVisible(this.metallicField, (flags & URPMaterialPropertyFlags.Metallic) != 0);
            ElementUtility.SetVisible(this.occlusionStrengthField, (flags & URPMaterialPropertyFlags.OcclusionStrength) != 0);
            ElementUtility.SetVisible(this.smoothnessField, (flags & URPMaterialPropertyFlags.Smoothness) != 0);
            ElementUtility.SetVisible(this.baseColorField, (flags & URPMaterialPropertyFlags.BaseColor) != 0);
            ElementUtility.SetVisible(this.emissionColorField, (flags & URPMaterialPropertyFlags.EmissionColor) != 0);
            ElementUtility.SetVisible(this.specColorField, (flags & URPMaterialPropertyFlags.SpecColor) != 0);
        }
    }
}
#endif
