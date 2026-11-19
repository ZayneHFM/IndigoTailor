using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using IndigoTailor.Character;
using IndigoTailor.Mechanics;
using IndigoTailor.Powers;

namespace IndigoTailor.Relics;

/// <summary>
/// R10 - 双生银针 (TwinSilverNeedles)
/// 每当你施加【脱线】或【虚弱】时，施加的层数 +1。
/// </summary>
public sealed class TwinSilverNeedles : IndigoTailorRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;
    public override bool ShouldReceiveCombatHooks => true;
    protected override string IconBaseName => "twin_silver_needles";

    public override decimal ModifyPowerAmountGivenAdditive(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)
    {
        if (giver == Owner?.Creature && (power is UnravelPower || power is MegaCrit.Sts2.Core.Models.Powers.WeakPower))
        {
            Flash();
            return 1m;
        }
        return 0m;
    }
}
