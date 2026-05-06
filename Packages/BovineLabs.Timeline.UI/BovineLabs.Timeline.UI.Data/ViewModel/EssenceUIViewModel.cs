using BovineLabs.Anchor;
using Unity.Properties;

namespace BovineLabs.Timeline.UI.Data.ViewModel
{
    [IsService]
    public partial class EssenceUIViewModel : SystemObservableObject<EssenceUIViewModel.Data>
    {
        [CreateProperty(ReadOnly = true)] public float StatValue => Value.StatValue;
        [CreateProperty(ReadOnly = true)] public int IntrinsicValue => Value.IntrinsicValue;
        [CreateProperty(ReadOnly = true)] public int EventValue => Value.EventValue;
        [CreateProperty(ReadOnly = true)] public bool HasEvent => Value.HasEvent;
        [CreateProperty(ReadOnly = true)] public bool IsVisible => Value.IsVisible;

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