// <copyright file="DrawerUnsafe.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_EDITOR
namespace BovineLabs.Quill
{
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Collections;
    using Unity.Collections;

    internal readonly struct DrawerUnsafe : IDrawWriter
    {
        private readonly UnsafeThreadStream.Writer writer;

        internal DrawerUnsafe(bool isEnabled, UnsafeThreadStream.Writer writer)
        {
            this.IsEnabled = isEnabled;
            this.writer = writer;
        }

        public bool IsEnabled { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IDrawWriter.Write<T>(T value)
        {
            this.writer.Write(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IDrawWriter.WriteLarge<T>(NativeArray<T> array)
        {
            this.writer.WriteLarge(array);
        }
    }
}
#endif
