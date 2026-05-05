namespace BovineLabs.Timeline.UI.Data.ViewModel
{
    using BovineLabs.Anchor;
    using Unity.Properties;

    [IsService]
    public partial class EssenceUIViewModel : SystemObservableObject<EssenceUIViewModel.Data>
    {
        [CreateProperty(ReadOnly = true)] public float StatValue => this.Value.StatValue;
        [CreateProperty(ReadOnly = true)] public int IntrinsicValue => this.Value.IntrinsicValue;
        [CreateProperty(ReadOnly = true)] public int EventValue => this.Value.EventValue;
        [CreateProperty(ReadOnly = true)] public bool HasEvent => this.Value.HasEvent;
        [CreateProperty(ReadOnly = true)] public bool IsVisible => this.Value.IsVisible;

        public partial struct Data
        {
            [SystemProperty] private float statValue;
            [SystemProperty] private int intrinsicValue;
            [SystemProperty] private int eventValue;
            [SystemProperty] private bool hasEvent;
            [SystemProperty] private bool isVisible;
        }
    }
}