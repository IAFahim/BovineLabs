// <copyright file="RukhankaAnimatorPlayStateClipEditor.cs" company="BovineLabs">
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

    [CustomEditor(typeof(RukhankaAnimatorPlayStateClip))]
    public class RukhankaAnimatorPlayStateClipEditor : DOTSClipEditor
    {
        private PropertyField normalizedTimeField;
        private PropertyField fixedTimeSecondsField;
        private PropertyField layerIndexField;
        private PropertyField weightLayerNameField;
        private PropertyField weightLayerIndexField;
        private PropertyField layerWeightField;

        private SerializedProperty modeProperty;
        private SerializedProperty layerNameProperty;
        private SerializedProperty setLayerWeightProperty;
        private SerializedProperty weightLayerNameProperty;

        /// <inheritdoc/>
        protected override VisualElement CreateElement(SerializedProperty property)
        {
            switch (property.name)
            {
                case "mode":
                    this.modeProperty = property;
                    var modeField = CreatePropertyField(property);
                    modeField.RegisterValueChangeCallback(this.OnSettingsChanged);
                    return modeField;

                case "normalizedTime":
                    return this.normalizedTimeField = CreatePropertyField(property);

                case "fixedTimeSeconds":
                    return this.fixedTimeSecondsField = CreatePropertyField(property);

                case "layerName":
                    this.layerNameProperty = property;
                    var layerNameField = CreatePropertyField(property);
                    layerNameField.RegisterValueChangeCallback(this.OnSettingsChanged);
                    return layerNameField;

                case "layerIndex":
                    return this.layerIndexField = CreatePropertyField(property);

                case "setLayerWeight":
                    this.setLayerWeightProperty = property;
                    var setLayerWeightField = CreatePropertyField(property);
                    setLayerWeightField.RegisterValueChangeCallback(this.OnSettingsChanged);
                    return setLayerWeightField;

                case "weightLayerName":
                    this.weightLayerNameProperty = property;
                    this.weightLayerNameField = CreatePropertyField(property);
                    this.weightLayerNameField.RegisterValueChangeCallback(this.OnSettingsChanged);
                    return this.weightLayerNameField;

                case "weightLayerIndex":
                    return this.weightLayerIndexField = CreatePropertyField(property);

                case "layerWeight":
                    return this.layerWeightField = CreatePropertyField(property);
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
            var useNormalizedTime = (RukhankaAnimatorPlayStateMode)this.modeProperty.enumValueIndex
                == RukhankaAnimatorPlayStateMode.NormalizedTime;
            ElementUtility.SetVisible(this.normalizedTimeField, useNormalizedTime);
            ElementUtility.SetVisible(this.fixedTimeSecondsField, !useNormalizedTime);

            var hasLayerName = !string.IsNullOrEmpty(this.layerNameProperty.stringValue);
            ElementUtility.SetVisible(this.layerIndexField, !hasLayerName);

            var setLayerWeight = this.setLayerWeightProperty.boolValue;
            ElementUtility.SetVisible(this.weightLayerNameField, setLayerWeight);

            var hasWeightLayerName = !string.IsNullOrEmpty(this.weightLayerNameProperty.stringValue);
            ElementUtility.SetVisible(this.weightLayerIndexField, setLayerWeight && !hasWeightLayerName);
            ElementUtility.SetVisible(this.layerWeightField, setLayerWeight);
        }
    }
}

#endif
