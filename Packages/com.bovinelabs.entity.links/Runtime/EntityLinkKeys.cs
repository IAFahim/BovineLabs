using BovineLabs.Core.Keys;
using BovineLabs.Core.Settings;
using System.Collections.Generic;

namespace BovineLabs.EntityLinks
{
    [SettingsGroup("Core")]
    public class EntityLinkKeys : KSettings<EntityLinkKeys, byte>
    {
        protected override IEnumerable<NameValue<byte>> SetReset()
        {
            yield return new NameValue<byte>("Player", 0);
            yield return new NameValue<byte>("Inventory", 1);
            yield return new NameValue<byte>("Weapon", 2);
        }
    }
}
