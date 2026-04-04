// <copyright file="DrawToolbarViewModel.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if (UNITY_EDITOR || !APP_UI_EDITOR_ONLY) && UNITY_APPUI
namespace BovineLabs.Quill.Debug
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using BovineLabs.Anchor;
    using BovineLabs.Anchor.Binding;
    using BovineLabs.Core.Extensions;
    using Unity.AppUI.UI;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Properties;
    using UnityEngine;

    [Serializable]
    public partial class DrawToolbarViewModel : SystemObservableObject<DrawToolbarViewModel.Data>, ILoadable, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<string> categorySave = new();

        [SerializeField]
        private List<int> systemSave = new();

        public DrawToolbarViewModel()
        {
            this.PropertyChanged += this.OnPropertyChanged;
        }

        [CreateProperty]
        public bool Enabled
        {
            get => this.Value.Enabled.Value;
            set => this.Value.Enabled = value;
        }

        [CreateProperty]
        public UIArray<FixedString32Bytes> Categories => this.Value.Categories;

        [CreateProperty]
        public IEnumerable<int> CategoryValues
        {
            get => this.Value.CategoryValues.Value.AsArray();
            set => this.SetProperty(this.Value.CategoryValues, value);
        }

        [CreateProperty]
        public UIArray<int> Systems => this.Value.Systems;

        [CreateProperty]
        public IEnumerable<int> SystemValues
        {
            get => this.Value.SystemValues.Value.AsArray();
            set => this.SetProperty(this.Value.SystemValues, value);
        }

        public void BindCategoryItem(DropdownItem item, int index)
        {
            item.label = this.Value.Categories[index].ToString();
        }

        public void BindSystemItem(DropdownItem item, int index)
        {
            const string drawSystemPrefix = "DrawSystem";
            const string systemPrefix = "System";

            var type = this.Value.Systems[index];

            var name = TypeManager.GetSystemName(type).ToString();
            var nameIndex = name.LastIndexOf('.') + 1;
            name = nameIndex == 0 ? name : name.Substring(nameIndex, name.Length - nameIndex);

            var drawIndex = name.IndexOf(drawSystemPrefix, StringComparison.Ordinal);
            if (drawIndex != -1)
            {
                name = name.Remove(drawIndex, drawSystemPrefix.Length);
            }
            else
            {
                var systemIndex = name.IndexOf(systemPrefix, StringComparison.Ordinal);
                if (systemIndex != -1)
                {
                    name = name.Remove(systemIndex, systemPrefix.Length);
                }
            }

            item.label = name.ToSentence();
        }

        /// <inheritdoc/>
        void ILoadable.Load()
        {
            this.Value.Initialize();
        }

        /// <inheritdoc/>
        void ILoadable.Unload()
        {
            this.Value.Dispose();
        }

        public void OnBeforeSerialize()
        {
            this.categorySave.Clear();
            this.systemSave.Clear();
            foreach (var c in this.Value.CategoryValues.Value)
            {
                this.categorySave.Add(this.Value.Categories[c].ToString());
            }

            foreach (var c in this.Value.SystemValues.Value)
            {
                this.systemSave.Add(this.Value.Systems[c]);
            }
        }

        public void OnAfterDeserialize()
        {
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // When system/categories are added, try to check if it's state was saved and loaded it
            switch (e.PropertyName)
            {
                case nameof(this.Categories):
                {
                    var any = false;
                    for (var i = this.categorySave.Count - 1; i >= 0; i--)
                    {
                        var categoryName = this.categorySave[i];

                        var index = this.Value.Categories.AsArray().IndexOf(categoryName);

                        if (index != -1)
                        {
                            this.categorySave.RemoveAt(i);
                            this.Value.CategoryValues.Add(index);
                            any = true;
                        }
                    }

                    if (any)
                    {
                        this.Value.Notify(nameof(this.CategoryValues));
                    }

                    break;
                }

                case nameof(this.Systems):
                {
                    var any = false;
                    for (var i = this.systemSave.Count - 1; i >= 0; i--)
                    {
                        var systemHash = this.systemSave[i];

                        var index = this.Value.Systems.AsArray().IndexOf(systemHash);

                        if (index != -1)
                        {
                            this.systemSave.RemoveAt(i);
                            this.Value.SystemValues.Add(index);
                            any = true;
                        }
                    }

                    if (any)
                    {
                        this.Value.Notify(nameof(this.CategoryValues));
                    }

                    break;
                }
            }
        }

        private static void ConvertToDisplayNames(List<string> list, NativeArray<int> types)
        {
            const string drawSystemPrefix = "DrawSystem";
            const string systemPrefix = "System";

            foreach (var type in types)
            {
                var name = TypeManager.GetSystemName(type).ToString();
                var index = name.LastIndexOf('.') + 1;
                name = index == 0 ? name : name.Substring(index, name.Length - index);

                var drawIndex = name.IndexOf(drawSystemPrefix, StringComparison.Ordinal);
                if (drawIndex != -1)
                {
                    name = name.Remove(drawIndex, drawSystemPrefix.Length);
                }
                else
                {
                    var systemIndex = name.IndexOf(systemPrefix, StringComparison.Ordinal);
                    if (systemIndex != -1)
                    {
                        name = name.Remove(systemIndex, systemPrefix.Length);
                    }
                }

                list.Add(name.ToSentence());
            }
        }

        [Serializable]
        public partial struct Data
        {
            [SystemProperty]
            private Changed<bool> enabled;[SystemProperty]
            private NativeList<FixedString32Bytes> categories;

            [SystemProperty]
            private ChangedList<int> categoryValues;

            [SystemProperty]
            private NativeList<int> systems;

            [SystemProperty]
            private ChangedList<int> systemValues;

            internal void Initialize()
            {
                this.categories = new NativeList<FixedString32Bytes>(Allocator.Persistent);
                this.categoryValues = new NativeList<int>(Allocator.Persistent);
                this.systems = new NativeList<int>(Allocator.Persistent);
                this.systemValues = new NativeList<int>(Allocator.Persistent);
            }

            internal void Dispose()
            {
                this.categories.Dispose();
                this.categoryValues.Value.Dispose();
                this.systems.Dispose();
                this.systemValues.Value.Dispose();
            }
        }
    }
}
#endif