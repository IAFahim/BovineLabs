// <copyright file="CMActivateClipEditor.cs" company="BovineLabs">
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

    [CustomEditor(typeof(CMActivateClip))]
    public class CMActivateClipEditor : DOTSClipEditor
    {
        private PropertyField enabledField;
        private PropertyField priorityField;
        private PropertyField outputChannelField;
        private PropertyField blendHintField;

        private SerializedProperty setEnabledProperty;
        private SerializedProperty setPriorityProperty;
        private SerializedProperty setOutputChannelProperty;
        private SerializedProperty setBlendHintProperty;

        /// <inheritdoc/>
        protected override VisualElement CreateElement(SerializedProperty property)
        {
            switch (property.name)
            {
                case "setEnabled":
                    this.setEnabledProperty = property;
                    var setEnabledField = CreatePropertyField(property);
                    setEnabledField.RegisterValueChangeCallback(this.OnSettingsChanged);
                    return setEnabledField;

                case "enabled":
                    return this.enabledField = CreatePropertyField(property);

                case "setPriority":
                    this.setPriorityProperty = property;
                    var setPriorityField = CreatePropertyField(property);
                    setPriorityField.RegisterValueChangeCallback(this.OnSettingsChanged);
                    return setPriorityField;

                case "priority":
                    return this.priorityField = CreatePropertyField(property);

                case "setOutputChannel":
                    this.setOutputChannelProperty = property;
                    var setOutputChannelField = CreatePropertyField(property);
                    setOutputChannelField.RegisterValueChangeCallback(this.OnSettingsChanged);
                    return setOutputChannelField;

                case "outputChannel":
                    return this.outputChannelField = CreatePropertyField(property);

                case "setBlendHint":
                    this.setBlendHintProperty = property;
                    var setBlendHintField = CreatePropertyField(property);
                    setBlendHintField.RegisterValueChangeCallback(this.OnSettingsChanged);
                    return setBlendHintField;

                case "blendHint":
                    return this.blendHintField = CreatePropertyField(property);
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
            ElementUtility.SetVisible(this.enabledField, this.setEnabledProperty.boolValue);
            ElementUtility.SetVisible(this.priorityField, this.setPriorityProperty.boolValue);
            ElementUtility.SetVisible(this.outputChannelField, this.setOutputChannelProperty.boolValue);
            ElementUtility.SetVisible(this.blendHintField, this.setBlendHintProperty.boolValue);
        }
    }
}
#endif
