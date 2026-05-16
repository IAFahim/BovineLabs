// <copyright file="RukhankaAnimatorCrossfadeClipEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if RUKHANKA

namespace BovineLabs.Vibe.Editor.Animation
{
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Timeline.Editor;
    using BovineLabs.Vibe.Authoring.Animation;
    using BovineLabs.Vibe.Data.Animation;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(RukhankaAnimatorCrossfadeClip))]
    public class RukhankaAnimatorCrossfadeClipEditor : DOTSClipEditor
    {
        private PropertyField randomStatesField;
        private PropertyField normalizedTransitionDurationField;
        private PropertyField normalizedTimeOffsetField;
        private PropertyField transitionDurationField;
        private PropertyField timeOffsetField;
        private PropertyField layerIndexField;
        private PropertyField seedField;

        private SerializedProperty modeProperty;
        private SerializedProperty useRandomStateProperty;
        private SerializedProperty layerNameProperty;

        /// <inheritdoc/>
        protected override VisualElement CreateElement(SerializedProperty property)
        {
            switch (property.name)
            {
                case "useRandomState":
                    this.useRandomStateProperty = property;
                    var useRandomStateField = CreatePropertyField(property);
                    useRandomStateField.RegisterValueChangeCallback(this.OnSettingsChanged);
                    return useRandomStateField;

                case "randomStates":
                    return this.randomStatesField = CreatePropertyField(property);

                case "mode":
                    this.modeProperty = property;
                    var modeField = CreatePropertyField(property);
                    modeField.RegisterValueChangeCallback(this.OnSettingsChanged);
                    return modeField;

                case "normalizedTransitionDuration":
                    return this.normalizedTransitionDurationField = CreatePropertyField(property);

                case "normalizedTimeOffset":
                    return this.normalizedTimeOffsetField = CreatePropertyField(property);

                case "transitionDuration":
                    return this.transitionDurationField = CreatePropertyField(property);

                case "timeOffset":
                    return this.timeOffsetField = CreatePropertyField(property);

                case "layerName":
                    this.layerNameProperty = property;
                    var layerNameField = CreatePropertyField(property);
                    layerNameField.RegisterValueChangeCallback(this.OnSettingsChanged);
                    return layerNameField;

                case "layerIndex":
                    return this.layerIndexField = CreatePropertyField(property);

                case "seed":
                    return this.seedField = CreatePropertyField(property);
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
            var useRandomState = this.useRandomStateProperty.boolValue;
            ElementUtility.SetVisible(this.randomStatesField, useRandomState);
            ElementUtility.SetVisible(this.seedField, useRandomState);

            var mode = (RukhankaAnimatorCrossfadeMode)this.modeProperty.enumValueIndex;
            var useNormalized = mode == RukhankaAnimatorCrossfadeMode.Normalized;
            ElementUtility.SetVisible(this.normalizedTransitionDurationField, useNormalized);
            ElementUtility.SetVisible(this.normalizedTimeOffsetField, useNormalized);
            ElementUtility.SetVisible(this.transitionDurationField, !useNormalized);
            ElementUtility.SetVisible(this.timeOffsetField, !useNormalized);

            var hasLayerName = !string.IsNullOrEmpty(this.layerNameProperty.stringValue);
            ElementUtility.SetVisible(this.layerIndexField, !hasLayerName);
        }
    }
}

#endif
