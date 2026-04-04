// <copyright file="SingletonCollectionExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_EDITOR || BL_DEBUG
namespace BovineLabs.Quill
{
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.SingletonCollection;

    internal static class SingletonCollectionExtensions
    {
        internal static unsafe NativeThreadStream.Writer CreateEnabledThreadStream<TS>(this TS eventSingleton, bool isEnabled)
            where TS : unmanaged, ISingletonCollection<DrawSystem.Singleton.EnabledStream>
        {
            var stream = new NativeThreadStream(eventSingleton.Allocator);
            eventSingleton.Collections->Add(new DrawSystem.Singleton.EnabledStream
            {
                Stream = stream,
                Enabled = isEnabled,
            });

            return stream.AsWriter();
        }
    }
}
#endif
