// <copyright file="LightFlickerClipEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Editor.Light
{
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Timeline.Editor;
    using BovineLabs.Vibe.Authoring.Light;
    using BovineLabs.Vibe.Data.Light;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(LightFlickerClip))]
    public class LightFlickerClipEditor : DOTSClipEditor
    {
        private PropertyField presetField;
        private PropertyField dutyCycleField;
        private PropertyField loopField;
        private PropertyField customCurveField;
        private PropertyField remapCurveField;
        private PropertyField colorAField;
        private PropertyField colorBField;
        private PropertyField temperatureAField;
        private PropertyField temperatureBField;

        private SerializedProperty presetProperty;
        private SerializedProperty useCustomCurveProperty;
        private SerializedProperty overrideColorProperty;
        private SerializedProperty overrideColorTemperatureProperty;

        /// <inheritdoc/>
        protected override VisualElement CreateElement(SerializedProperty property)
        {
            switch (property.name)
            {
                case "preset":
                    this.presetProperty = property;
                    this.presetField = CreatePropertyField(property);
                    this.presetField.RegisterValueChangeCallback(this.OnSettingsChanged);
                    return this.presetField;

                case "useCustomCurve":
                    this.useCustomCurveProperty = property;
                    var useCustomCurveField = CreatePropertyField(property);
                    useCustomCurveField.RegisterValueChangeCallback(this.OnSettingsChanged);
                    return useCustomCurveField;

                case "overrideColor":
                    this.overrideColorProperty = property;
                    var overrideColorField = CreatePropertyField(property);
                    overrideColorField.RegisterValueChangeCallback(this.OnSettingsChanged);
                    return overrideColorField;

                case "dutyCycle":
                    return this.dutyCycleField = CreatePropertyField(property);

                case "loop":
                    return this.loopField = CreatePropertyField(property);

                case "customCurve":
                    return this.customCurveField = CreatePropertyField(property);

                case "remapCurveToClipLength":
                    return this.remapCurveField = CreatePropertyField(property);

                case "colorA":
                    return this.colorAField = CreatePropertyField(property);

                case "colorB":
                    return this.colorBField = CreatePropertyField(property);

                case "overrideColorTemperature":
                    this.overrideColorTemperatureProperty = property;
                    var overrideTemperatureField = CreatePropertyField(property);
                    overrideTemperatureField.RegisterValueChangeCallback(this.OnSettingsChanged);
                    return overrideTemperatureField;

                case "temperatureA":
                    return this.temperatureAField = CreatePropertyField(property);

                case "temperatureB":
                    return this.temperatureBField = CreatePropertyField(property);
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
            var preset = (LightFlickerPreset)this.presetProperty.enumValueIndex;
            var useCustomCurve = this.useCustomCurveProperty.boolValue;
            ElementUtility.SetVisible(this.presetField, !useCustomCurve);
            ElementUtility.SetVisible(this.dutyCycleField, !useCustomCurve && preset == LightFlickerPreset.Strobe);
            ElementUtility.SetVisible(this.loopField, !useCustomCurve && preset == LightFlickerPreset.Strobe);
            ElementUtility.SetVisible(this.customCurveField, useCustomCurve);
            ElementUtility.SetVisible(this.remapCurveField, useCustomCurve);

            var showColor = this.overrideColorProperty.boolValue;
            ElementUtility.SetVisible(this.colorAField, showColor);
            ElementUtility.SetVisible(this.colorBField, showColor);

            var showTemperature = this.overrideColorTemperatureProperty.boolValue;
            ElementUtility.SetVisible(this.temperatureAField, showTemperature);
            ElementUtility.SetVisible(this.temperatureBField, showTemperature);
        }
    }
}
#endif
