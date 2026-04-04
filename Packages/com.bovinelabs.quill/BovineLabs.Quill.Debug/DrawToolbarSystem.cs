// <copyright file="DrawToolbarSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if (UNITY_EDITOR || !APP_UI_EDITOR_ONLY) && UNITY_APPUI
namespace BovineLabs.Quill.Debug
{
    using BovineLabs.Anchor.Debug.Toolbar;
    using Unity.Burst;
    using Unity.Entities;[UpdateInGroup(typeof(ToolbarSystemGroup))]
    public partial struct DrawToolbarSystem : ISystem, ISystemStartStop
    {
        private ToolbarHelper<DrawToolbarView, DrawToolbarViewModel, DrawToolbarViewModel.Data> toolbar;

        /// <inheritdoc />
        public void OnCreate(ref SystemState state)
        {
            this.toolbar = new ToolbarHelper<DrawToolbarView, DrawToolbarViewModel, DrawToolbarViewModel.Data>(ref state, "Draw");
        }

        /// <inheritdoc />
        public void OnStartRunning(ref SystemState state)
        {
            this.toolbar.Load();
            this.Update(ref state); // So we can apply save data even if not visible
        }

        /// <inheritdoc />
        public void OnStopRunning(ref SystemState state)
        {
            this.toolbar.Unload();
        }

        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!this.toolbar.IsVisible())
            {
                return;
            }

            this.Update(ref state);
        }

        private void Update(ref SystemState state)
        {
            ref var data = ref this.toolbar.Binding;
            var singleton = SystemAPI.GetSingleton<DrawSystem.Singleton>();

            this.UpdateEnabled(ref data, singleton);
            data.Categories = singleton.KnownCategorySet;
            data.Systems = singleton.KnownSystemSet;

            this.UpdateCategoryValue(ref data, singleton);
            this.UpdateSystemValue(ref data, singleton);
        }

        private void UpdateEnabled(ref DrawToolbarViewModel.Data data, DrawSystem.Singleton singleton)
        {
            if (data.EnabledChanged(out var enabled))
            {
                singleton.Enabled.Value = enabled;
            }
            else
            {
                data.Enabled = singleton.Enabled.Value;
            }
        }

        private void UpdateCategoryValue(ref DrawToolbarViewModel.Data data, DrawSystem.Singleton singleton)
        {
            if (!data.CategoryValues.GetIfChanged(out var categoryValues))
            {
                return;
            }

            singleton.CategoryFilterSet.Clear();
            foreach (var index in categoryValues)
            {
                singleton.CategoryFilterSet.Add(data.Categories[index]);
            }
        }

        private void UpdateSystemValue(ref DrawToolbarViewModel.Data data, DrawSystem.Singleton singleton)
        {
            if (!data.SystemValues.GetIfChanged(out var systemValues))
            {
                return;
            }

            singleton.SystemFilterSet.Clear();
            foreach (var index in systemValues)
            {
                singleton.SystemFilterSet.Add(data.Systems[index]);
            }
        }
    }
}
#endif