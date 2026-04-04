// <copyright file="QuillSettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Quill
{
    using System.Collections.Generic;
    using BovineLabs.Core.Settings;
    using UnityEngine;

    public class QuillSettings : SettingsSingleton<QuillSettings>
    {
        [SerializeField]
        private string[] camerasToIgnore = { "Preview Scene Camera" };

        public IReadOnlyList<string> CamerasToIgnore => this.camerasToIgnore;
    }
}
