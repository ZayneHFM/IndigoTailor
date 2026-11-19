using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
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
/// R01 - 锈铜顶针 (RustedBrassThimble)
/// </summary>
public sealed class RustedBrassThimble : IndigoTailorRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    public override RelicModel? GetUpgradeReplacement() => ModelDb.Relic<GoldenAuraThimble>();
    public override bool ShouldReceiveCombatHooks => true;
    protected override string IconBaseName => "rusted_brass_thimble";

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || player.Creature.CombatState?.RoundNumber != 1) return;
        Flash();
        await RawScrapHelper.AddScrapToHandSafe(Owner, "DYED_SCRAP_NEEDLE", false);
        await RawScrapHelper.AddScrapToHandSafe(Owner, "DYED_SCRAP_CRINOLINE", false);
    }
}
