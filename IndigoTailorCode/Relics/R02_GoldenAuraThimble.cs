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
/// R02 - 黄金顶针 (GoldenAuraThimble)
/// </summary>
public sealed class GoldenAuraThimble : IndigoTailorRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    public override bool ShouldReceiveCombatHooks => true;
    protected override string IconBaseName => "golden_aura_thimble";

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner) return;
        Flash();
        await RawScrapHelper.AddRandomUpgradedScrapToHand(Owner);
        LoomSeamManager.GrantRetainToAllHandScraps(Owner);
    }
}
