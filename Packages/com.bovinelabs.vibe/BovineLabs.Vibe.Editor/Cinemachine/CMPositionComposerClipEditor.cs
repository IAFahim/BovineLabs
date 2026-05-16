// <copyright file="CMPositionComposerClipEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_CINEMACHINE && BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Editor.Cinemachine
{
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Timeline.Editor;
    using BovineLabs.Vibe.Authoring.Cinemachine;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(CMPositionComposerClip))]
    public class CMPositionComposerClipEditor : DOTSClipEditor
    {
        private PropertyField lookaheadTimeField;
        private PropertyField lookaheadSmoothingField;
        private PropertyField lookaheadIgnoreYField;
        private PropertyField deadZoneDepthField;
        private PropertyField deadZoneSizeField;
        private PropertyField hardLimitSizeField;
        private PropertyField hardLimitOffsetField;

        private SerializedProperty lookaheadEnabledProperty;
        private SerializedProperty deadZoneEnabledProperty;
        private SerializedProperty hardLimitsEnabledProperty;

        /// <inheritdoc/>
        protected override VisualElement CreateElement(SerializedProperty property)
        {
            switch (property.name)
            {
                case "lookaheadEnabled":
                    this.lookaheadEnabledProperty = property;
                    var lookaheadEnabledField = CreatePropertyField(property);
                    lookaheadEnabledField.RegisterValueChangeCallback(this.OnSettingsChanged);
                    return lookaheadEnabledField;

                case "lookaheadTime":
                    return this.lookaheadTimeField = CreatePropertyField(property);

                case "lookaheadSmoothing":
                    return this.lookaheadSmoothingField = CreatePropertyField(property);

                case "lookaheadIgnoreY":
                    return this.lookaheadIgnoreYField = CreatePropertyField(property);

                case "deadZoneEnabled":
                    this.deadZoneEnabledProperty = property;
                    var deadZoneEnabledField = CreatePropertyField(property);
                    deadZoneEnabledField.RegisterValueChangeCallback(this.OnSettingsChanged);
                    return deadZoneEnabledField;

                case "deadZoneDepth":
                    return this.deadZoneDepthField = CreatePropertyField(property);

                case "deadZoneSize":
                    return this.deadZoneSizeField = CreatePropertyField(property);

                case "hardLimitsEnabled":
                    this.hardLimitsEnabledProperty = property;
                    var hardLimitsEnabledField = CreatePropertyField(property);
                    hardLimitsEnabledField.RegisterValueChangeCallback(this.OnSettingsChanged);
                    return hardLimitsEnabledField;

                case "hardLimitSize":
                    return this.hardLimitSizeField = CreatePropertyField(property);

                case "hardLimitOffset":
                    return this.hardLimitOffsetField = CreatePropertyField(property);
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
            var lookaheadEnabled = this.lookaheadEnabledProperty.boolValue;
            ElementUtility.SetVisible(this.lookaheadTimeField, lookaheadEnabled);
            ElementUtility.SetVisible(this.lookaheadSmoothingField, lookaheadEnabled);
            ElementUtility.SetVisible(this.lookaheadIgnoreYField, lookaheadEnabled);

            var deadZoneEnabled = this.deadZoneEnabledProperty.boolValue;
            ElementUtility.SetVisible(this.deadZoneDepthField, deadZoneEnabled);
            ElementUtility.SetVisible(this.deadZoneSizeField, deadZoneEnabled);

            var hardLimitsEnabled = this.hardLimitsEnabledProperty.boolValue;
            ElementUtility.SetVisible(this.hardLimitSizeField, hardLimitsEnabled);
            ElementUtility.SetVisible(this.hardLimitOffsetField, hardLimitsEnabled);
        }
    }
}
#endif
