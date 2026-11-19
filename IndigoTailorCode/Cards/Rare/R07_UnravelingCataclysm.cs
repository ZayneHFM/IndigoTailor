using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using IndigoTailor.Mechanics;
using IndigoTailor.Powers;

namespace IndigoTailor.Cards.Rare;

/// <summary>
/// R07 - 崩线 (Unraveling Cataclysm)
/// 对所有敌人造成 14(18) 点伤害。引爆所有敌人的【脱线】，每层造成 5(7) 点伤害。
/// </summary>
public sealed class UnravelingCataclysm() : IndigoTailorCard(
    baseCost: 2,
    type: CardType.Attack,
    rarity: CardRarity.Rare,
    target: TargetType.AllEnemies)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(14m, ValueProp.Move),
        new IntVar("ExtraDmg", 5m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_UNRAVEL");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var combatState = Owner?.Creature?.CombatState;
        if (combatState == null) return;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, play)
            .TargetingAllOpponents(combatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        int totalUnravel = 0;
        foreach (var enemy in combatState.HittableEnemies)
        {
            var up = enemy.GetPower<UnravelPower>();
            if (up != null && up.Amount > 0)
            {
                totalUnravel += (int)up.Amount;
                await PowerCmd.Remove(up);
            }
        }

        if (totalUnravel > 0)
        {
            decimal burstDmg = totalUnravel * DynamicVars["ExtraDmg"].BaseValue;
            await DamageCmd.Attack(burstDmg)
                .FromCard(this, play)
                .TargetingAllOpponents(combatState)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
        DynamicVars["ExtraDmg"].UpgradeValueBy(2m);
    }
}
