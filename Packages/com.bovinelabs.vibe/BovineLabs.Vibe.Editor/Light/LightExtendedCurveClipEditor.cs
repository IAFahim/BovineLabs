// <copyright file="LightExtendedCurveClipEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Editor.Light
{
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Timeline.Editor;
    using BovineLabs.Vibe.Authoring.Light;
    using JetBrains.Annotations;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(LightExtendedCurveClip))]
    public class LightExtendedCurveClipEditor : DOTSClipEditor
    {
        private PropertyField rangeCurveField;
        private PropertyField rangeMinField;
        private PropertyField rangeMaxField;
        private PropertyField rangeRelativeField;
        private PropertyField rangeUseInitialField;

        private PropertyField spotAngleCurveField;
        private PropertyField spotAngleMinField;
        private PropertyField spotAngleMaxField;
        private PropertyField spotAngleRelativeField;
        private PropertyField spotAngleUseInitialField;

        private PropertyField innerSpotAngleCurveField;
        private PropertyField innerSpotAngleMinField;
        private PropertyField innerSpotAngleMaxField;
        private PropertyField innerSpotAngleRelativeField;
        private PropertyField innerSpotAngleUseInitialField;

        private PropertyField shadowStrengthCurveField;
        private PropertyField shadowStrengthMinField;
        private PropertyField shadowStrengthMaxField;
        private PropertyField shadowStrengthRelativeField;
        private PropertyField shadowStrengthUseInitialField;

        private PropertyField remapCurveField;

        private SerializedProperty animateRangeProperty;
        private SerializedProperty rangeRelativeProperty;
        private SerializedProperty animateSpotAngleProperty;
        private SerializedProperty spotAngleRelativeProperty;
        private SerializedProperty animateInnerSpotAngleProperty;
        private SerializedProperty innerSpotAngleRelativeProperty;
        private SerializedProperty animateShadowStrengthProperty;
        private SerializedProperty shadowStrengthRelativeProperty;

        /// <inheritdoc/>
        [CanBeNull]
        protected override VisualElement CreateElement(SerializedProperty property)
        {
            switch (property.name)
            {
                case "animateRange":
                    this.animateRangeProperty = property;
                    var animateRangeField = CreatePropertyField(property);
                    animateRangeField.RegisterValueChangeCallback(this.OnSettingsChanged);
                    return animateRangeField;

                case "rangeCurve":
                    return this.rangeCurveField = CreatePropertyField(property);

                case "rangeMin":
                    return this.rangeMinField = CreatePropertyField(property);

                case "rangeMax":
                    return this.rangeMaxField = CreatePropertyField(property);

                case "rangeRelative":
                    this.rangeRelativeProperty = property;
                    this.rangeRelativeField = CreatePropertyField(property);
                    this.rangeRelativeField.RegisterValueChangeCallback(this.OnSettingsChanged);
                    return this.rangeRelativeField;

                case "rangeUseInitial":
                    return this.rangeUseInitialField = CreatePropertyField(property);

                case "animateSpotAngle":
                    this.animateSpotAngleProperty = property;
                    var animateSpotAngleField = CreatePropertyField(property);
                    animateSpotAngleField.RegisterValueChangeCallback(this.OnSettingsChanged);
                    return animateSpotAngleField;

                case "spotAngleCurve":
                    return this.spotAngleCurveField = CreatePropertyField(property);

                case "spotAngleMin":
                    return this.spotAngleMinField = CreatePropertyField(property);

                case "spotAngleMax":
                    return this.spotAngleMaxField = CreatePropertyField(property);

                case "spotAngleRelative":
                    this.spotAngleRelativeProperty = property;
                    this.spotAngleRelativeField = CreatePropertyField(property);
                    this.spotAngleRelativeField.RegisterValueChangeCallback(this.OnSettingsChanged);
                    return this.spotAngleRelativeField;

                case "spotAngleUseInitial":
                    return this.spotAngleUseInitialField = CreatePropertyField(property);

                case "animateInnerSpotAngle":
                    this.animateInnerSpotAngleProperty = property;
                    var animateInnerSpotAngleField = CreatePropertyField(property);
                    animateInnerSpotAngleField.RegisterValueChangeCallback(this.OnSettingsChanged);
                    return animateInnerSpotAngleField;

                case "innerSpotAngleCurve":
                    return this.innerSpotAngleCurveField = CreatePropertyField(property);

                case "innerSpotAngleMin":
                    return this.innerSpotAngleMinField = CreatePropertyField(property);

                case "innerSpotAngleMax":
                    return this.innerSpotAngleMaxField = CreatePropertyField(property);

                case "innerSpotAngleRelative":
                    this.innerSpotAngleRelativeProperty = property;
                    this.innerSpotAngleRelativeField = CreatePropertyField(property);
                    this.innerSpotAngleRelativeField.RegisterValueChangeCallback(this.OnSettingsChanged);
                    return this.innerSpotAngleRelativeField;

                case "innerSpotAngleUseInitial":
                    return this.innerSpotAngleUseInitialField = CreatePropertyField(property);

                case "animateShadowStrength":
                    this.animateShadowStrengthProperty = property;
                    var animateShadowStrengthField = CreatePropertyField(property);
                    animateShadowStrengthField.RegisterValueChangeCallback(this.OnSettingsChanged);
                    return animateShadowStrengthField;

                case "shadowStrengthCurve":
                    return this.shadowStrengthCurveField = CreatePropertyField(property);

                case "shadowStrengthMin":
                    return this.shadowStrengthMinField = CreatePropertyField(property);

                case "shadowStrengthMax":
                    return this.shadowStrengthMaxField = CreatePropertyField(property);

                case "shadowStrengthRelative":
                    this.shadowStrengthRelativeProperty = property;
                    this.shadowStrengthRelativeField = CreatePropertyField(property);
                    this.shadowStrengthRelativeField.RegisterValueChangeCallback(this.OnSettingsChanged);
                    return this.shadowStrengthRelativeField;

                case "shadowStrengthUseInitial":
                    return this.shadowStrengthUseInitialField = CreatePropertyField(property);

                case "remapCurveToClipLength":
                    return this.remapCurveField = CreatePropertyField(property);
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
            var showRange = this.animateRangeProperty.boolValue;
            ElementUtility.SetVisible(this.rangeCurveField, showRange);
            ElementUtility.SetVisible(this.rangeMinField, showRange);
            ElementUtility.SetVisible(this.rangeMaxField, showRange);
            ElementUtility.SetVisible(this.rangeRelativeField, showRange);
            ElementUtility.SetVisible(this.rangeUseInitialField, showRange && this.rangeRelativeProperty.boolValue);

            var showSpot = this.animateSpotAngleProperty.boolValue;
            ElementUtility.SetVisible(this.spotAngleCurveField, showSpot);
            ElementUtility.SetVisible(this.spotAngleMinField, showSpot);
            ElementUtility.SetVisible(this.spotAngleMaxField, showSpot);
            ElementUtility.SetVisible(this.spotAngleRelativeField, showSpot);
            ElementUtility.SetVisible(this.spotAngleUseInitialField, showSpot && this.spotAngleRelativeProperty.boolValue);

            var showInner = this.animateInnerSpotAngleProperty.boolValue;
            ElementUtility.SetVisible(this.innerSpotAngleCurveField, showInner);
            ElementUtility.SetVisible(this.innerSpotAngleMinField, showInner);
            ElementUtility.SetVisible(this.innerSpotAngleMaxField, showInner);
            ElementUtility.SetVisible(this.innerSpotAngleRelativeField, showInner);
            ElementUtility.SetVisible(this.innerSpotAngleUseInitialField, showInner && this.innerSpotAngleRelativeProperty.boolValue);

            var showShadowStrength = this.animateShadowStrengthProperty.boolValue;
            ElementUtility.SetVisible(this.shadowStrengthCurveField, showShadowStrength);
            ElementUtility.SetVisible(this.shadowStrengthMinField, showShadowStrength);
            ElementUtility.SetVisible(this.shadowStrengthMaxField, showShadowStrength);
            ElementUtility.SetVisible(this.shadowStrengthRelativeField, showShadowStrength);
            ElementUtility.SetVisible(this.shadowStrengthUseInitialField, showShadowStrength && this.shadowStrengthRelativeProperty.boolValue);

            ElementUtility.SetVisible(this.remapCurveField, showRange || showSpot || showInner || showShadowStrength);
        }
    }
}
#endif
