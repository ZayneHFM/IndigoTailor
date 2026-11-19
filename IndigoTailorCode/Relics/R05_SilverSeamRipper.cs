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
/// R05 - 银质拆线刀 (SilverSeamRipper)
/// </summary>
public sealed class SilverSeamRipper : IndigoTailorRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    public override bool ShouldReceiveCombatHooks => true;
    protected override string IconBaseName => "silver_seam_ripper";

    public async Task OnUnpickTriggered(PlayerChoiceContext choiceContext)
    {
        if (Owner?.Creature?.CombatState == null) return;
        Flash();
        await CardPileCmd.Draw(choiceContext, 1m, Owner);
        foreach (var enemy in Owner.Creature.CombatState.HittableEnemies)
            await CreatureCmd.Damage(choiceContext, enemy, 4m, ValueProp.Unpowered, Owner.Creature);
    }
}
