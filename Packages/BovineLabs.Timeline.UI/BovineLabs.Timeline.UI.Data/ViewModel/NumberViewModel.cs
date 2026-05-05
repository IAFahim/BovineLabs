namespace BovineLabs.Timeline.UI.Data.ViewModel
{
    using BovineLabs.Anchor;
    using Unity.Properties;

    [IsService]
    public partial class NumberViewModel : SystemObservableObject<NumberViewModel.Data>
    {
        [CreateProperty(ReadOnly = true)] public int Number => this.Value.Number;
        [CreateProperty(ReadOnly = true)] public bool IsVisible => this.Value.IsVisible;

        public partial struct Data
        {
            [SystemProperty] private int number;
            [SystemProperty] private bool isVisible;
        }
    }
}
