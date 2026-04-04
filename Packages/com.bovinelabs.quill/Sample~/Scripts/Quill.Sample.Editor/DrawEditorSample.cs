// <copyright file="DrawEditorSample.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Quill.Sample.Editor
{
    using BovineLabs.Quill;
    using Unity.Mathematics;
    using UnityEditor;
    using UnityEngine;

    [InitializeOnLoad]
    public class DrawEditorSample
    {
        static DrawEditorSample()
        {
            DrawEditor.Update += Update;
        }

        private static void Update()
        {
            GlobalDraw.Text128(new float3(0, 1, 0), "Drawing in editor", Color.green);
        }
    }
}
