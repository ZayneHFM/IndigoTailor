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
/// R04 - 红梅针插 (CrimsonPlumPincushion)
/// </summary>
public sealed class CrimsonPlumPincushion : IndigoTailorRelic
{
    public override RelicRarity Rarity => RelicRarity.Common;
    public override bool ShouldReceiveCombatHooks => true;
    protected override string IconBaseName => "crimson_plum_pincushion";

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature?.CombatState == null) return;
        Flash();
        var ctx = new ThrowingPlayerChoiceContext();
        foreach (var enemy in Owner.Creature.CombatState.HittableEnemies)
            await PowerCmd.Apply<UnravelPower>(ctx, enemy, 2m, Owner.Creature, null, false);
    }
}
