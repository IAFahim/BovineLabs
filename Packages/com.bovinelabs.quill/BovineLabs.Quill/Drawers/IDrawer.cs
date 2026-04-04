// <copyright file="IDrawer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Quill.Drawers
{
    /// <summary> The shared drawer interface. </summary>
    internal interface IDrawer
    {
        /// <summary> Builds a mesh from the drawer. </summary>
        /// <param name="builder"> The mesh builder to use. </param>
        void Draw(ref DrawBuilder builder);
    }
}
