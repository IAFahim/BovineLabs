// <copyright file="LightExtendedConstantClipEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BOVINELABS_BRIDGE
namespace BovineLabs.Vibe.Editor.Light
{
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Timeline.Editor;
    using BovineLabs.Vibe.Authoring.Light;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(LightExtendedConstantClip))]
    public class LightExtendedConstantClipEditor : DOTSClipEditor
    {
        private VisualElement overrideSpotAngleElement;
        private VisualElement overrideInnerSpotAngleElement;

        private SerializedProperty overrideTypeProperty;
        private SerializedProperty typeProperty;

        /// <inheritdoc/>
        protected override VisualElement CreateElement(SerializedProperty property)
        {
            switch (property.name)
            {
                case "overrideSpotAngle":
                    return this.overrideSpotAngleElement = base.CreateElement(property);

                case "overrideInnerSpotAngle":
                    return this.overrideInnerSpotAngleElement = base.CreateElement(property);

                case "overrideType":
                    this.overrideTypeProperty = property;
                    return base.CreateElement(property);

                case "type":
                    this.typeProperty = property;
                    return base.CreateElement(property);

                default:
                    return base.CreateElement(property);
            }
        }

        /// <inheritdoc/>
        protected override void PostElementCreation(VisualElement root, bool createdElements)
        {
            base.PostElementCreation(root, createdElements);

            if (this.overrideTypeProperty != null)
            {
                root.TrackPropertyValue(this.overrideTypeProperty, _ => this.UpdateVisibility());
            }

            if (this.typeProperty != null)
            {
                root.TrackPropertyValue(this.typeProperty, _ => this.UpdateVisibility());
            }

            this.UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            var showSpotSettings = true;

            if (this.overrideTypeProperty.boolValue)
            {
                var lightType = (LightType)this.typeProperty.enumValueIndex;
                showSpotSettings = lightType == LightType.Spot;
            }

            ElementUtility.SetVisible(this.overrideSpotAngleElement, showSpotSettings);
            ElementUtility.SetVisible(this.overrideInnerSpotAngleElement, showSpotSettings);
        }
    }
}
#endif
