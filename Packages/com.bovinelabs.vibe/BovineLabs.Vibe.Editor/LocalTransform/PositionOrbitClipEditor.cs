// <copyright file="PositionOrbitClipEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Editor.LocalTransform
{
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Timeline.Editor;
    using BovineLabs.Vibe.Authoring.LocalTransform;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(PositionOrbitClip))]
    public class PositionOrbitClipEditor : DOTSClipEditor
    {
        private PropertyField radiusField;
        private PropertyField initialOffsetField;
        private PropertyField angleCurveField;
        private PropertyField remapAngleField;
        private PropertyField radiusCurveField;
        private PropertyField remapRadiusField;

        private SerializedProperty useCustomInitialOffsetProperty;
        private SerializedProperty animateAngleProperty;
        private SerializedProperty animateRadiusProperty;

        /// <inheritdoc/>
        protected override VisualElement CreateElement(SerializedProperty property)
        {
            switch (property.name)
            {
                case "useCustomInitialOffset":
                    this.useCustomInitialOffsetProperty = property;
                    var initialOffsetToggle = CreatePropertyField(property);
                    initialOffsetToggle.RegisterValueChangeCallback(this.OnOrbitSettingsChanged);
                    return initialOffsetToggle;

                case "initialOffset":
                    return this.initialOffsetField = CreatePropertyField(property);

                case "radius":
                    return this.radiusField = CreatePropertyField(property);

                case "animateAngle":
                    this.animateAngleProperty = property;
                    var animateAngleField = CreatePropertyField(property);
                    animateAngleField.RegisterValueChangeCallback(this.OnOrbitSettingsChanged);
                    return animateAngleField;

                case "angleCurve":
                    return this.angleCurveField = CreatePropertyField(property);

                case "remapAngleToClipLength":
                    return this.remapAngleField = CreatePropertyField(property);

                case "animateRadius":
                    this.animateRadiusProperty = property;
                    var animateRadiusField = CreatePropertyField(property);
                    animateRadiusField.RegisterValueChangeCallback(this.OnOrbitSettingsChanged);
                    return animateRadiusField;

                case "radiusCurve":
                    return this.radiusCurveField = CreatePropertyField(property);

                case "remapRadiusToClipLength":
                    return this.remapRadiusField = CreatePropertyField(property);
            }

            return base.CreateElement(property);
        }

        /// <inheritdoc/>
        protected override void PostElementCreation(VisualElement root, bool createdElements)
        {
            this.UpdateVisibility();
        }

        private void OnOrbitSettingsChanged(SerializedPropertyChangeEvent evt)
        {
            this.UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            var useInitialOffset = this.useCustomInitialOffsetProperty.boolValue;
            ElementUtility.SetVisible(this.initialOffsetField, useInitialOffset);
            ElementUtility.SetVisible(this.radiusField, !useInitialOffset);

            var animateAngle = this.animateAngleProperty.boolValue;
            ElementUtility.SetVisible(this.angleCurveField, animateAngle);
            ElementUtility.SetVisible(this.remapAngleField, animateAngle);

            var animateRadius = this.animateRadiusProperty.boolValue;
            ElementUtility.SetVisible(this.radiusCurveField, animateRadius);
            ElementUtility.SetVisible(this.remapRadiusField, animateRadius);
        }
    }
}
