using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace IndigoTailor.Powers;

/// <summary>
/// 替身偶 (Straw Mannequin - R09):
/// 每当你【缝合】或消耗状态牌时，获得 4(6) 点格挡并对随机敌人造成 4(6) 点伤害。
/// </summary>
public sealed class StrawMannequinPower : IndigoTailorPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card.Type == CardType.Status || card.Type == CardType.Curse)
        {
            await TriggerMannequinEffect(choiceContext);
        }
    }

    public async Task OnCardExhaustedOrStitched(PlayerChoiceContext choiceContext)
    {
        await TriggerMannequinEffect(choiceContext);
    }

    private async Task TriggerMannequinEffect(PlayerChoiceContext choiceContext)
    {
        if (Owner == null || Amount <= 0) return;
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);

        var enemies = Owner.CombatState?.HittableEnemies.ToList();
        if (enemies != null && enemies.Count > 0)
        {
            var target = enemies[Random.Shared.Next(enemies.Count)];
            await CreatureCmd.Damage(choiceContext, target, Amount, ValueProp.Unpowered, Owner);
        }
    }
}
