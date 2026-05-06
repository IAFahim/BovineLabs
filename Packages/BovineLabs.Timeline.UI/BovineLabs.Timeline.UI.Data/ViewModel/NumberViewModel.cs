using BovineLabs.Anchor;
using Unity.Properties;

namespace BovineLabs.Timeline.UI.Data.ViewModel
{
    [IsService]
    public partial class NumberViewModel : SystemObservableObject<NumberViewModel.Data>
    {
        [CreateProperty(ReadOnly = true)] public int Number => Value.Number;
        [CreateProperty(ReadOnly = true)] public bool IsVisible => Value.IsVisible;

        public partial struct Data
        {
            [SystemProperty] private int number;
            [SystemProperty] private bool isVisible;
        }
    }
}