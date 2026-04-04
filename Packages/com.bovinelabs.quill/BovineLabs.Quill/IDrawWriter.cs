// <copyright file="IDrawWriter.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Quill
{
    using Unity.Collections;

    public interface IDrawWriter
    {
#if UNITY_EDITOR || BL_DEBUG
        public bool IsEnabled { get; }

        /// <summary> Write data. </summary>
        /// <typeparam name="T"> The type of value. </typeparam>
        /// <param name="value"> Value to write. </param>
        void Write<T>(T value)
            where T : unmanaged;

        void WriteLarge<T>(NativeArray<T> array)
            where T : unmanaged;
#endif
    }
}
