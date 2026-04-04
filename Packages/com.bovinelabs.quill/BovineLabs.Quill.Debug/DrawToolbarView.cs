// <copyright file="DrawToolbarView.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if (UNITY_EDITOR || !APP_UI_EDITOR_ONLY) && UNITY_APPUI
namespace BovineLabs.Quill.Debug
{
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Anchor;
    using Unity.AppUI.UI;
    using Toggle = Unity.AppUI.UI.Toggle;

    [Transient]
    public class DrawToolbarView : View<DrawToolbarViewModel>
    {
        public DrawToolbarView()
            : base(new DrawToolbarViewModel())
        {
            var toggle = new Toggle
            {
                label = "Enabled",
                dataSource = this.ViewModel,
            };

            toggle.SetBindingTwoWay(nameof(Toggle.value), nameof(DrawToolbarViewModel.Enabled));

            var categories = new Dropdown
            {
                dataSource = this.ViewModel,
                selectionType = PickerSelectionType.Multiple,
                closeOnSelection = false,
                defaultMessage = "Categories",
                bindTitle = (item, _) => item.labelElement.text = "Categories",
                bindItem = this.ViewModel.BindCategoryItem,
            };

            categories.SetBindingToUI(nameof(Dropdown.sourceItems), nameof(DrawToolbarViewModel.Categories));
            categories.SetBindingTwoWay(nameof(Dropdown.value), nameof(DrawToolbarViewModel.CategoryValues));
            categories.SetBindingToUI(nameof(Dropdown.defaultValue), nameof(DrawToolbarViewModel.CategoryValues),
                static (ref IEnumerable<int> value) => value.ToArray());

            var system = new Dropdown
            {
                dataSource = this.ViewModel,
                selectionType = PickerSelectionType.Multiple,
                closeOnSelection = false,
                defaultMessage = "Systems",
                bindTitle = (item, _) => item.labelElement.text = "Systems",
                bindItem = this.ViewModel.BindSystemItem,
            };

            system.SetBindingToUI(nameof(Dropdown.sourceItems), nameof(DrawToolbarViewModel.Systems));
            system.SetBindingTwoWay(nameof(Dropdown.value), nameof(DrawToolbarViewModel.SystemValues));
            system.SetBindingToUI(nameof(Dropdown.defaultValue), nameof(DrawToolbarViewModel.SystemValues),
                static (ref IEnumerable<int> value) => value.ToArray());

            this.Add(toggle);
            this.Add(categories);
            this.Add(system);
        }
    }
}
#endif