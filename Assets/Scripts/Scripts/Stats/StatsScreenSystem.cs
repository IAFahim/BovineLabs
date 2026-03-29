using System;
using System.Collections.Generic;
using Scripts.Data.Stats;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace Scripts.Stats
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class StatsScreenSystem : SystemBase
    {
        private const string ResourcePath = "UI/StatsScreen";
        private const string RowTemplatePath = "UI/StatsScreenRow";
        private const string RootName = "stats-screen-root";
        private const string PanelName = "stats-screen-panel";
        private const string TabsName = "stats-screen-tabs";
        private const string ColumnsName = "stats-screen-columns";
        private const string EmptyName = "stats-screen-empty";
        private const string FloatTabName = "stats-screen-tab-float";
        private const string IntTabName = "stats-screen-tab-int";
        private const string BoolTabName = "stats-screen-tab-bool";
        private const float WidthFactor = 0.2f;
        private const float MinPanelWidth = 280f;
        private const float MaxPanelWidth = 380f;
        private const float ProgressThreshold = 0.05f;

        private readonly List<RowView> floatRows = new();
        private readonly List<RowView> intRows = new();
        private readonly List<RowView> boolRows = new();
        private VisualTreeAsset rowTemplateAsset;

        private UIDocument document;
        private VisualTreeAsset screenAsset;
        private VisualElement root;
        private VisualElement panel;
        private VisualElement tabs;
        private VisualElement columns;
        private Label empty;
        private Button floatTab;
        private Button intTab;
        private Button boolTab;
        private ColumnView floatColumn;
        private ColumnView intColumn;
        private ColumnView boolColumn;
        private bool treeReady;
        private bool showFloat = true;
        private bool showInt = true;
        private bool showBool;
        private EntityQuery query;

        protected override void OnCreate()
        {
            this.query = this.GetEntityQuery(
                ComponentType.ReadOnly<StatFloatCatalogSingleton>(),
                ComponentType.ReadOnly<StatIntCatalogSingleton>(),
                ComponentType.ReadOnly<StatBoolCatalogSingleton>(),
                ComponentType.ReadOnly<StatFloatElement>(),
                ComponentType.ReadOnly<StatIntElement>(),
                ComponentType.ReadOnly<StatBoolElement>());
            this.RequireForUpdate(this.query);
        }

        protected override void OnUpdate()
        {
            if (!this.TryEnsureDocument())
            {
                return;
            }

            if (!this.TryEnsureTree())
            {
                return;
            }

            if (this.rowTemplateAsset == null)
            {
                this.rowTemplateAsset = Resources.Load<VisualTreeAsset>(RowTemplatePath);
            }

            this.UpdatePanelWidth();
            this.UpdateToggles();
            this.UpdateColumns();
        }

        private void UpdateColumns()
        {
            var entity = this.query.GetSingletonEntity();
            var floatCatalog = this.EntityManager.GetComponentData<StatFloatCatalogSingleton>(entity).Value;
            var intCatalog = this.EntityManager.GetComponentData<StatIntCatalogSingleton>(entity).Value;
            var boolCatalog = this.EntityManager.GetComponentData<StatBoolCatalogSingleton>(entity).Value;
            var floatBuffer = this.EntityManager.GetBuffer<StatFloatElement>(entity, true);
            var intBuffer = this.EntityManager.GetBuffer<StatIntElement>(entity, true);
            var boolBuffer = this.EntityManager.GetBuffer<StatBoolElement>(entity, true);

            var visibleCount = CountVisibleColumns(this.showFloat, this.showInt, this.showBool);
            this.empty.style.display = visibleCount == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            this.columns.style.display = visibleCount == 0 ? DisplayStyle.None : DisplayStyle.Flex;

            this.ApplyColumn(this.floatColumn.Root, this.showFloat, visibleCount);
            this.ApplyColumn(this.intColumn.Root, this.showInt, visibleCount);
            this.ApplyColumn(this.boolColumn.Root, this.showBool, visibleCount);

            if (this.showFloat)
            {
                this.EnsureFloatRows(floatBuffer.Length);
                this.UpdateFloatRows(floatBuffer, floatCatalog, visibleCount);
            }

            if (this.showInt)
            {
                this.EnsureIntRows(intBuffer.Length);
                this.UpdateIntRows(intBuffer, intCatalog, visibleCount);
            }

            if (this.showBool)
            {
                this.EnsureBoolRows(boolBuffer.Length);
                this.UpdateBoolRows(boolBuffer, boolCatalog, visibleCount);
            }
        }

        private static int CountVisibleColumns(bool showFloat, bool showInt, bool showBool)
        {
            var count = 0;
            if (showFloat)
            {
                count++;
            }

            if (showInt)
            {
                count++;
            }

            if (showBool)
            {
                count++;
            }

            return count;
        }

        private void ApplyColumn(VisualElement element, bool visible, int visibleCount)
        {
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible || visibleCount == 0)
            {
                return;
            }

            var widthPercent = 100f / visibleCount;
            element.style.width = Length.Percent(widthPercent);
            element.style.minWidth = 0f;
            element.style.maxWidth = StyleKeyword.None;
            element.style.flexGrow = 0f;
            element.style.flexShrink = 0f;
        }

        private void UpdatePanelWidth()
        {
            var width = this.document.rootVisualElement.resolvedStyle.width;
            if (width <= 0f)
            {
                return;
            }

            var panelWidth = Mathf.Clamp(width * WidthFactor, MinPanelWidth, MaxPanelWidth);
            this.panel.style.width = panelWidth;
            this.panel.style.minWidth = panelWidth;
            this.panel.style.maxWidth = panelWidth;
        }

        private void UpdateToggles()
        {
            this.ApplyToggleClass(this.floatTab, this.showFloat);
            this.ApplyToggleClass(this.intTab, this.showInt);
            this.ApplyToggleClass(this.boolTab, this.showBool);
            this.floatTab.text = this.showFloat ? "● Stats" : "Stats";
            this.intTab.text = this.showInt ? "● Intrinsic" : "Intrinsic";
            this.boolTab.text = this.showBool ? "● Events" : "Events";
        }

        private void ApplyToggleClass(Button button, bool enabled)
        {
            if (enabled)
            {
                button.AddToClassList("stats-screen__tab--active");
                return;
            }

            button.RemoveFromClassList("stats-screen__tab--active");
        }

        private bool TryEnsureDocument()
        {
            if (this.document != null)
            {
                return true;
            }

            this.document = UnityEngine.Object.FindFirstObjectByType<UIDocument>();
            return this.document != null;
        }

        private bool TryEnsureTree()
        {
            if (this.treeReady)
            {
                return true;
            }

            this.screenAsset ??= Resources.Load<VisualTreeAsset>(ResourcePath);
            if (this.screenAsset == null)
            {
                return false;
            }

            this.root = this.document.rootVisualElement;
            if (this.root.Q<VisualElement>(RootName) == null)
            {
                this.root.Clear();
                this.screenAsset.CloneTree(this.root);
            }

            this.panel = this.root.Q<VisualElement>(PanelName);
            this.tabs = this.root.Q<VisualElement>(TabsName);
            this.columns = this.root.Q<VisualElement>(ColumnsName);
            this.empty = this.root.Q<Label>(EmptyName);
            this.floatTab = this.root.Q<Button>(FloatTabName);
            this.intTab = this.root.Q<Button>(IntTabName);
            this.boolTab = this.root.Q<Button>(BoolTabName);
            this.floatColumn = CreateColumn(this.root, "stats-screen-column-float", "stats-screen-list-float", "Stats");
            this.intColumn = CreateColumn(this.root, "stats-screen-column-int", "stats-screen-list-int", "Intrinsic");
            this.boolColumn = CreateColumn(this.root, "stats-screen-column-bool", "stats-screen-list-bool", "Events");
            if (this.panel == null || this.tabs == null || this.columns == null || this.empty == null || this.floatTab == null || this.intTab == null || this.boolTab == null)
            {
                return false;
            }

            this.floatTab.clicked += this.ToggleFloat;
            this.intTab.clicked += this.ToggleInt;
            this.boolTab.clicked += this.ToggleBool;
            this.treeReady = true;
            return true;
        }

        private static ColumnView CreateColumn(VisualElement root, string rootName, string listName, string title)
        {
            var columnRoot = root.Q<VisualElement>(rootName);
            var list = root.Q<ScrollView>(listName);
            var heading = columnRoot.Q<Label>();
            heading.text = title;
            return new ColumnView(columnRoot, list, heading);
        }

        private void ToggleFloat()
        {
            this.showFloat = !this.showFloat;
        }

        private void ToggleInt()
        {
            this.showInt = !this.showInt;
        }

        private void ToggleBool()
        {
            this.showBool = !this.showBool;
        }

        private void EnsureFloatRows(int count)
        {
            this.EnsureRows(this.floatRows, this.floatColumn.List, count);
        }

        private void EnsureIntRows(int count)
        {
            this.EnsureRows(this.intRows, this.intColumn.List, count);
        }

        private void EnsureBoolRows(int count)
        {
            this.EnsureRows(this.boolRows, this.boolColumn.List, count);
        }

        private void EnsureRows(List<RowView> rows, ScrollView list, int count)
        {
            while (rows.Count < count)
            {
                var row = new RowView(this.rowTemplateAsset);
                list.Add(row.Root);
                rows.Add(row);
            }

            for (var i = 0; i < rows.Count; i++)
            {
                rows[i].Root.style.display = i < count ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }



        private void UpdateFloatRows(DynamicBuffer<StatFloatElement> buffer, BlobAssetReference<StatFloatCatalogBlob> catalog, int visibleCount)
        {
            for (var i = 0; i < buffer.Length; i++)
            {
                var stat = buffer[i];
                var row = this.floatRows[i];
                var hasLink = TryGetFloatLinkValue(buffer, stat.LinkId, out var linkValue);
                row.Name.text = StatNameUtility.Compact(GetShortName(catalog.Value.Entries[i].ShortName, catalog.Value.Entries[i].Name), visibleCount);
                row.Value.text = StatFloatFormatter.Format(stat, linkValue, hasLink);
                UpdateProgressBar(row, stat.View, stat.Value, (int)linkValue, hasLink);
                ApplyBoolValueClass(row.Value, false);
            }
        }

        private void UpdateIntRows(DynamicBuffer<StatIntElement> buffer, BlobAssetReference<StatIntCatalogBlob> catalog, int visibleCount)
        {
            for (var i = 0; i < buffer.Length; i++)
            {
                var stat = buffer[i];
                var row = this.intRows[i];
                var hasLink = TryGetIntLinkValue(buffer, stat.LinkId, out var linkValue);
                row.Name.text = StatNameUtility.Compact(GetShortName(catalog.Value.Entries[i].ShortName, catalog.Value.Entries[i].Name), visibleCount);
                row.Value.text = StatIntFormatter.Format(stat, linkValue, hasLink);
                UpdateProgressBar(row, stat.View, (float)stat.Value, linkValue, hasLink);
                ApplyBoolValueClass(row.Value, false);
            }
        }

        private void UpdateBoolRows(DynamicBuffer<StatBoolElement> buffer, BlobAssetReference<StatBoolCatalogBlob> catalog, int visibleCount)
        {
            for (var i = 0; i < buffer.Length; i++)
            {
                var stat = buffer[i];
                var row = this.boolRows[i];
                row.Name.text = StatNameUtility.Compact(GetShortName(catalog.Value.Entries[i].ShortName, catalog.Value.Entries[i].Name), visibleCount);
                row.Value.text = StatBoolFormatter.Format(stat);
                ApplyBoolValueClass(row.Value, stat.Value != 0);
            }
        }

        private static string GetShortName(in Unity.Collections.FixedString32Bytes shortName, in Unity.Collections.FixedString64Bytes name)
        {
            return shortName.Length > 0 ? shortName.ToString() : name.ToString();
        }

        private static void ApplyBoolValueClass(Label label, bool active)
        {
            label.RemoveFromClassList("stats-screen__row-value--active");
            label.RemoveFromClassList("stats-screen__row-value--inactive");
            if (label.text == "ON" || label.text == "ACTIVE" || label.text == "READY" || label.text == "OPEN" || label.text == "DONE" || label.text == "SEEN")
            {
                label.AddToClassList(active ? "stats-screen__row-value--active" : "stats-screen__row-value--inactive");
                return;
            }

            if (label.text == "OFF" || label.text == "IDLE" || label.text == "USED" || label.text == "LOCK" || label.text == "TODO" || label.text == "HIDE")
            {
                label.AddToClassList(active ? "stats-screen__row-value--active" : "stats-screen__row-value--inactive");
            }
        }

        private static bool TryGetFloatLinkValue(DynamicBuffer<StatFloatElement> buffer, ushort linkId, out float value)
        {
            if (linkId == StatConstants.NoLink || linkId >= buffer.Length)
            {
                value = 0f;
                return false;
            }

            value = buffer[linkId].Value;
            return true;
        }

        private static bool TryGetIntLinkValue(DynamicBuffer<StatIntElement> buffer, ushort linkId, out int value)
        {
            if (linkId == StatConstants.NoLink || linkId >= buffer.Length)
            {
                value = 0;
                return false;
            }

            value = buffer[linkId].Value;
            return true;
        }

        private static void UpdateProgressBar(RowView row, StatFloatView floatView, float floatValue, int intValue, bool hasLink)
        {
            var showProgress = false;
            if (hasLink)
            {
                showProgress = floatView == StatFloatView.CurrentOfLink || floatView == StatFloatView.PercentOfLink || floatView == StatFloatView.MissingFromLink;
            }

            if (!showProgress)
            {
                row.ProgressContainer.style.display = DisplayStyle.None;
                row.Root.RemoveFromClassList("stats-screen__row--with-progress");
                return;
            }

            row.ProgressContainer.style.display = DisplayStyle.Flex;
            row.Root.AddToClassList("stats-screen__row--with-progress");

            var current = floatValue;
            var max = (float)intValue;

            if (floatView == StatFloatView.PercentOfLink)
            {
                current = floatValue * max * 0.01f;
            }

            var percent = max > float.Epsilon ? Mathf.Clamp01(current / max) : 0f;
            row.ProgressBar.style.width = Length.Percent(percent * 100f);

            row.ProgressBar.RemoveFromClassList("stats-screen__progress-bar--primary");
            row.ProgressBar.RemoveFromClassList("stats-screen__progress-bar--success");
            row.ProgressBar.RemoveFromClassList("stats-screen__progress-bar--warning");
            row.ProgressBar.RemoveFromClassList("stats-screen__progress-bar--danger");
            row.ProgressBar.RemoveFromClassList("stats-screen__progress-bar--info");
            row.ProgressBar.RemoveFromClassList("stats-screen__progress-bar--muted");

            if (percent <= ProgressThreshold)
            {
                row.ProgressBar.AddToClassList("stats-screen__progress-bar--danger");
            }
            else if (percent < 0.25f)
            {
                row.ProgressBar.AddToClassList("stats-screen__progress-bar--warning");
            }
            else if (percent < 0.5f)
            {
                row.ProgressBar.AddToClassList("stats-screen__progress-bar--primary");
            }
            else if (percent < 0.75f)
            {
                row.ProgressBar.AddToClassList("stats-screen__progress-bar--info");
            }
            else
            {
                row.ProgressBar.AddToClassList("stats-screen__progress-bar--success");
            }
        }

        private static void UpdateProgressBar(RowView row, StatIntView intView, float floatValue, int intValue, bool hasLink)
        {
            var showProgress = false;
            if (hasLink)
            {
                showProgress = intView == StatIntView.CurrentOfLink || intView == StatIntView.PercentOfLink || intView == StatIntView.MissingFromLink;
            }

            if (!showProgress)
            {
                row.ProgressContainer.style.display = DisplayStyle.None;
                row.Root.RemoveFromClassList("stats-screen__row--with-progress");
                return;
            }

            row.ProgressContainer.style.display = DisplayStyle.Flex;
            row.Root.AddToClassList("stats-screen__row--with-progress");

            var current = floatValue;
            var max = (float)intValue;

            if (intView == StatIntView.PercentOfLink)
            {
                current = floatValue * max * 0.01f;
            }

            var percent = max > float.Epsilon ? Mathf.Clamp01(current / max) : 0f;
            row.ProgressBar.style.width = Length.Percent(percent * 100f);

            row.ProgressBar.RemoveFromClassList("stats-screen__progress-bar--primary");
            row.ProgressBar.RemoveFromClassList("stats-screen__progress-bar--success");
            row.ProgressBar.RemoveFromClassList("stats-screen__progress-bar--warning");
            row.ProgressBar.RemoveFromClassList("stats-screen__progress-bar--danger");
            row.ProgressBar.RemoveFromClassList("stats-screen__progress-bar--info");
            row.ProgressBar.RemoveFromClassList("stats-screen__progress-bar--muted");

            if (percent <= ProgressThreshold)
            {
                row.ProgressBar.AddToClassList("stats-screen__progress-bar--danger");
            }
            else if (percent < 0.25f)
            {
                row.ProgressBar.AddToClassList("stats-screen__progress-bar--warning");
            }
            else if (percent < 0.5f)
            {
                row.ProgressBar.AddToClassList("stats-screen__progress-bar--primary");
            }
            else if (percent < 0.75f)
            {
                row.ProgressBar.AddToClassList("stats-screen__progress-bar--info");
            }
            else
            {
                row.ProgressBar.AddToClassList("stats-screen__progress-bar--success");
            }
        }

        private sealed class ColumnView
        {
            public ColumnView(VisualElement root, ScrollView list, Label title)
            {
                this.Root = root;
                this.List = list;
                this.Title = title;
            }

            public VisualElement Root { get; }

            public ScrollView List { get; }

            public Label Title { get; }
        }

        private sealed class RowView
        {
            public RowView(VisualTreeAsset templateAsset)
            {
                templateAsset.CloneTree(this.Root = new VisualElement());
                this.ProgressContainer = this.Root.Q(className: "stats-screen__progress");
                this.ProgressBar = this.Root.Q(className: "stats-screen__progress-bar");
                this.Content = this.Root.Q(className: "stats-screen__row-content");
                this.Name = this.Content.Q<Label>(className: "stats-screen__row-name");
                this.Value = this.Content.Q<Label>(className: "stats-screen__row-value");
            }

            public VisualElement Root { get; }

            public Label Name { get; }

            public Label Value { get; }

            public VisualElement ProgressContainer { get; }

            public VisualElement ProgressBar { get; }

            public VisualElement Content { get; }
        }
    }
}
