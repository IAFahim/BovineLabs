// <copyright file="InGameUI.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace Scripts.Debug
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public class InGameUI : MonoBehaviour
    {
        private static InGameUI instance;

        private class UIText
        {
            public Func<string> GetText { get; set; }
            public float YOffset { get; set; }
            public Color Color { get; set; }
        }

        private List<UIText> uiTexts = new List<UIText>();
        private GUIStyle textStyle;

        public static void RegisterText(Func<string> getTextFunc, float yOffset = 10)
        {
            if (instance == null)
            {
                var mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    instance = mainCamera.gameObject.GetComponent<InGameUI>();
                    if (instance == null)
                    {
                        instance = mainCamera.gameObject.AddComponent<InGameUI>();
                    }
                }
            }

            if (instance != null)
            {
                instance.AddText(getTextFunc, yOffset);
            }
        }

        private void AddText(Func<string> getTextFunc, float yOffset)
        {
            uiTexts.Add(new UIText
            {
                GetText = getTextFunc,
                YOffset = yOffset,
                Color = GetColorForOffset(yOffset)
            });
        }

        private void OnGUI()
        {
            if (textStyle == null)
            {
                InitializeTextStyle();
            }

            foreach (var uiText in uiTexts)
            {
                var text = uiText.GetText();
                var rect = new Rect(10, Screen.height - uiText.YOffset - 10, 300, 100);

                textStyle.normal.textColor = uiText.Color;
                GUI.Label(rect, text, textStyle);
            }
        }

        private void InitializeTextStyle()
        {
            textStyle = new GUIStyle(GUI.skin.label);
            textStyle.fontSize = 18;
            textStyle.fontStyle = FontStyle.Bold;
            textStyle.alignment = TextAnchor.UpperLeft;
            textStyle.wordWrap = false;
        }

        private static Color GetColorForOffset(float offset)
        {
            if (offset < 100) return Color.cyan;
            if (offset < 220) return Color.yellow;
            if (offset < 340) return Color.green;
            if (offset < 460) return new Color(1f, 0.6f, 0f);
            return new Color(0.5f, 0.8f, 1f);
        }
    }
}
