// <copyright file="RukhankaAnimatorParameterClipEditor.cs" company="BovineLabs">
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

    [CustomEditor(typeof(RukhankaAnimatorParameterClip))]
    public class RukhankaAnimatorParameterClipEditor : DOTSClipEditor
    {
        private PropertyField triggerParameterField;
        private PropertyField triggerModeField;
        private PropertyField boolParameterField;
        private PropertyField boolValueField;
        private PropertyField intModeField;
        private PropertyField intValueField;
        private PropertyField intMinField;
        private PropertyField intMaxField;
        private PropertyField intIncrementField;
        private PropertyField floatModeField;
        private PropertyField floatValueField;
        private PropertyField floatMinField;
        private PropertyField floatMaxField;
        private PropertyField floatIncrementField;
        private PropertyField randomParametersField;
        private PropertyField layerNameField;
        private PropertyField layerIndexField;
        private PropertyField layerWeightField;
        private PropertyField seedField;

        private SerializedProperty updateTriggerProperty;
        private SerializedProperty updateRandomTriggerProperty;
        private SerializedProperty updateBoolProperty;
        private SerializedProperty updateRandomBoolProperty;
        private SerializedProperty intModeProperty;
        private SerializedProperty floatModeProperty;
        private SerializedProperty intParameterProperty;
        private SerializedProperty floatParameterProperty;
        private SerializedProperty setLayerWeightProperty;
        private SerializedProperty layerNameProperty;

        /// <inheritdoc/>
        protected override VisualElement CreateElement(SerializedProperty property)
        {
            switch (property.name)
            {
                case "updateTrigger":
                    this.updateTriggerProperty = property;
                    var updateTriggerField = CreatePropertyField(property);
                    updateTriggerField.RegisterValueChangeCallback(this.OnSettingsChanged);
                    return updateTriggerField;

                case "triggerParameter":
                    return this.triggerParameterField = CreatePropertyField(property);

                case "triggerMode":
                    return this.triggerModeField = CreatePropertyField(property);

                case "updateRandomTrigger":
                    this.updateRandomTriggerProperty = property;
                    var updateRandomTriggerField = CreatePropertyField(property);
                    updateRandomTriggerField.RegisterValueChangeCallback(this.OnSettingsChanged);
                    return updateRandomTriggerField;

                case "updateBool":
                    this.updateBoolProperty = property;
                    var updateBoolField = CreatePropertyField(property);
                    updateBoolField.RegisterValueChangeCallback(this.OnSettingsChanged);
                    return updateBoolField;

                case "boolParameter":
                    return this.boolParameterField = CreatePropertyField(property);

                case "boolValue":
                    return this.boolValueField = CreatePropertyField(property);

                case "updateRandomBool":
                    this.updateRandomBoolProperty = property;
                    var updateRandomBoolField = CreatePropertyField(property);
                    updateRandomBoolField.RegisterValueChangeCallback(this.OnSettingsChanged);
                    return updateRandomBoolField;

                case "intParameter":
                    this.intParameterProperty = property;
                    var intParameterField = CreatePropertyField(property);
                    intParameterField.RegisterValueChangeCallback(this.OnSettingsChanged);
                    return intParameterField;

                case "intMode":
                    this.intModeProperty = property;
                    var intModeField = CreatePropertyField(property);
                    intModeField.RegisterValueChangeCallback(this.OnSettingsChanged);
                    return this.intModeField = intModeField;

                case "intValue":
                    return this.intValueField = CreatePropertyField(property);

                case "intMin":
                    return this.intMinField = CreatePropertyField(property);

                case "intMax":
                    return this.intMaxField = CreatePropertyField(property);

                case "intIncrement":
                    return this.intIncrementField = CreatePropertyField(property);

                case "floatParameter":
                    this.floatParameterProperty = property;
                    var floatParameterField = CreatePropertyField(property);
                    floatParameterField.RegisterValueChangeCallback(this.OnSettingsChanged);
                    return floatParameterField;

                case "floatMode":
                    this.floatModeProperty = property;
                    var floatModeField = CreatePropertyField(property);
                    floatModeField.RegisterValueChangeCallback(this.OnSettingsChanged);
                    return this.floatModeField = floatModeField;

                case "floatValue":
                    return this.floatValueField = CreatePropertyField(property);

                case "floatMin":
                    return this.floatMinField = CreatePropertyField(property);

                case "floatMax":
                    return this.floatMaxField = CreatePropertyField(property);

                case "floatIncrement":
                    return this.floatIncrementField = CreatePropertyField(property);

                case "randomParameters":
                    return this.randomParametersField = CreatePropertyField(property);

                case "setLayerWeight":
                    this.setLayerWeightProperty = property;
                    var setLayerWeightField = CreatePropertyField(property);
                    setLayerWeightField.RegisterValueChangeCallback(this.OnSettingsChanged);
                    return setLayerWeightField;

                case "layerName":
                    this.layerNameProperty = property;
                    var layerNameField = CreatePropertyField(property);
                    layerNameField.RegisterValueChangeCallback(this.OnSettingsChanged);
                    return this.layerNameField = layerNameField;

                case "layerIndex":
                    return this.layerIndexField = CreatePropertyField(property);

                case "layerWeight":
                    return this.layerWeightField = CreatePropertyField(property);

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
            var updateTrigger = this.updateTriggerProperty.boolValue;
            var updateRandomTrigger = this.updateRandomTriggerProperty.boolValue;
            ElementUtility.SetVisible(this.triggerParameterField, updateTrigger);
            ElementUtility.SetVisible(this.triggerModeField, updateTrigger || updateRandomTrigger);

            var updateBool = this.updateBoolProperty.boolValue;
            var updateRandomBool = this.updateRandomBoolProperty.boolValue;
            ElementUtility.SetVisible(this.boolParameterField, updateBool);
            ElementUtility.SetVisible(this.boolValueField, updateBool || updateRandomBool);

            ElementUtility.SetVisible(this.randomParametersField, updateRandomTrigger || updateRandomBool);

            var hasIntParameter = !string.IsNullOrEmpty(this.intParameterProperty.stringValue);
            ElementUtility.SetVisible(this.intModeField, hasIntParameter);
            if (hasIntParameter)
            {
                var intMode = (RukhankaAnimatorParameterValueMode)this.intModeProperty.enumValueIndex;
                ElementUtility.SetVisible(this.intValueField, intMode == RukhankaAnimatorParameterValueMode.Constant);
                ElementUtility.SetVisible(this.intMinField, intMode == RukhankaAnimatorParameterValueMode.Random);
                ElementUtility.SetVisible(this.intMaxField, intMode == RukhankaAnimatorParameterValueMode.Random);
                ElementUtility.SetVisible(this.intIncrementField, intMode == RukhankaAnimatorParameterValueMode.Increment);
            }
            else
            {
                ElementUtility.SetVisible(this.intValueField, false);
                ElementUtility.SetVisible(this.intMinField, false);
                ElementUtility.SetVisible(this.intMaxField, false);
                ElementUtility.SetVisible(this.intIncrementField, false);
            }

            var hasFloatParameter = !string.IsNullOrEmpty(this.floatParameterProperty.stringValue);
            ElementUtility.SetVisible(this.floatModeField, hasFloatParameter);
            if (hasFloatParameter)
            {
                var floatMode = (RukhankaAnimatorParameterValueMode)this.floatModeProperty.enumValueIndex;
                ElementUtility.SetVisible(this.floatValueField, floatMode == RukhankaAnimatorParameterValueMode.Constant);
                ElementUtility.SetVisible(this.floatMinField, floatMode == RukhankaAnimatorParameterValueMode.Random);
                ElementUtility.SetVisible(this.floatMaxField, floatMode == RukhankaAnimatorParameterValueMode.Random);
                ElementUtility.SetVisible(this.floatIncrementField, floatMode == RukhankaAnimatorParameterValueMode.Increment);
            }
            else
            {
                ElementUtility.SetVisible(this.floatValueField, false);
                ElementUtility.SetVisible(this.floatMinField, false);
                ElementUtility.SetVisible(this.floatMaxField, false);
                ElementUtility.SetVisible(this.floatIncrementField, false);
            }

            var intRandom = hasIntParameter
                && (RukhankaAnimatorParameterValueMode)this.intModeProperty.enumValueIndex == RukhankaAnimatorParameterValueMode.Random;
            var floatRandom = hasFloatParameter
                && (RukhankaAnimatorParameterValueMode)this.floatModeProperty.enumValueIndex == RukhankaAnimatorParameterValueMode.Random;
            ElementUtility.SetVisible(this.seedField, updateRandomTrigger || updateRandomBool || intRandom || floatRandom);

            var setLayerWeight = this.setLayerWeightProperty.boolValue;
            ElementUtility.SetVisible(this.layerNameField, setLayerWeight);
            ElementUtility.SetVisible(this.layerWeightField, setLayerWeight);

            var hasLayerName = !string.IsNullOrEmpty(this.layerNameProperty.stringValue);
            ElementUtility.SetVisible(this.layerIndexField, setLayerWeight && !hasLayerName);
        }
    }
}

#endif
