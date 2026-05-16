// <copyright file="RukhankaAnimatorSpeedClipEditor.cs" company="BovineLabs">
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

    [CustomEditor(typeof(RukhankaAnimatorSpeedClip))]
    public class RukhankaAnimatorSpeedClipEditor : DOTSClipEditor
    {
        private PropertyField maxSpeedField;
        private PropertyField curveField;
        private PropertyField remapCurveField;
        private PropertyField seedField;

        private SerializedProperty modeProperty;

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

                case "maxSpeed":
                    return this.maxSpeedField = CreatePropertyField(property);

                case "curve":
                    return this.curveField = CreatePropertyField(property);

                case "remapCurveToClipLength":
                    return this.remapCurveField = CreatePropertyField(property);

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
            var mode = (RukhankaAnimatorSpeedMode)this.modeProperty.enumValueIndex;
            var showCurve = mode == RukhankaAnimatorSpeedMode.Curve;
            var showMaxSpeed = mode != RukhankaAnimatorSpeedMode.Constant;

            ElementUtility.SetVisible(this.maxSpeedField, showMaxSpeed);
            ElementUtility.SetVisible(this.curveField, showCurve);
            ElementUtility.SetVisible(this.remapCurveField, showCurve);
            ElementUtility.SetVisible(this.seedField, mode == RukhankaAnimatorSpeedMode.Random);
        }
    }
}

#endif
