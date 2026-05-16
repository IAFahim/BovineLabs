// <copyright file="SpringClipEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Vibe.Editor
{
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Timeline.Editor;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    public abstract class SpringClipEditor : DOTSClipEditor
    {
        private PropertyField dampingField;
        private PropertyField settleToleranceField;

        private SerializedProperty matchClipDurationProperty;

        /// <inheritdoc/>
        protected override VisualElement CreateElement(SerializedProperty property)
        {
            switch (property.name)
            {
                case "damping":
                    this.dampingField = CreatePropertyField(property);
                    return this.dampingField;

                case "matchClipDuration":
                    this.matchClipDurationProperty = property;
                    var matchClipDurationField = CreatePropertyField(property);
                    matchClipDurationField.RegisterValueChangeCallback(this.MatchClipValueChanged);
                    return matchClipDurationField;

                case "settleTolerance":
                    this.settleToleranceField = CreatePropertyField(property);
                    return this.settleToleranceField;
            }

            return base.CreateElement(property);
        }

        /// <inheritdoc/>
        protected override void PostElementCreation(VisualElement root, bool createdElements)
        {
            this.UpdateVisibility();
        }

        private void MatchClipValueChanged(SerializedPropertyChangeEvent evt)
        {
            this.UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            ElementUtility.SetVisible(this.dampingField, !this.matchClipDurationProperty.boolValue);
            ElementUtility.SetVisible(this.settleToleranceField, this.matchClipDurationProperty.boolValue);
        }
    }
}
