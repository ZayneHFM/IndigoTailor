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
/// R08 - 暗港裁衣剪 (UnderdocksTailoringShears)
/// </summary>
public sealed class UnderdocksTailoringShears : IndigoTailorRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;
    public override bool ShouldReceiveCombatHooks => true;
    protected override string IconBaseName => "underdocks_tailoring_shears";

    public int CutsPerformedThisCombat { get; set; }

    public override Task BeforeCombatStart()
    {
        CutsPerformedThisCombat = 0;
        return Task.CompletedTask;
    }

    public async Task OnCardCut(PlayerChoiceContext choiceContext)
    {
        CutsPerformedThisCombat++;
        if (Owner?.Creature?.CombatState == null) return;
        Flash();
        foreach (var enemy in Owner.Creature.CombatState.HittableEnemies)
        {
            await CreatureCmd.Damage(choiceContext, enemy, 5m, ValueProp.Unpowered, Owner.Creature);
            var ctx = new ThrowingPlayerChoiceContext(); await PowerCmd.Apply<UnravelPower>(ctx, enemy, 1m, Owner.Creature, null, false);
        }
    }
}
