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

namespace IndigoTailor.Cards.Uncommon;

/// <summary>
/// U15 - 双边裁 (Twin-Blade Shear)
/// 1费攻击：造成位于【幅边】的两张手牌费用之和 ×5(7) 点伤害。
/// </summary>
public sealed class TwinBladeShear() : IndigoTailorCard(
    baseCost: 1,
    type: CardType.Attack,
    rarity: CardRarity.Uncommon,
    target: TargetType.AnyEnemy)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_SELVEDGE");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);

        var hand = Owner?.PlayerCombatState?.Hand?.Cards;
        int leftCost = 0, rightCost = 0;

        if (hand != null && hand.Count == 1)
        {
            // 手牌只剩 1 张时，它既是最左端也是最右端 —— 但只能算一次，
            // 否则 `hand[0]` 与 `hand[^1]` 指向同一张牌，费用会被重复累加（数值翻倍）。
            leftCost = Math.Max(0, hand[0].EnergyCost.GetWithModifiers(CostModifiers.All));
        }
        else if (hand != null && hand.Count >= 2)
        {
            leftCost = Math.Max(0, hand[0].EnergyCost.GetWithModifiers(CostModifiers.All));
            rightCost = Math.Max(0, hand[^1].EnergyCost.GetWithModifiers(CostModifiers.All));
        }

        decimal mult = IsUpgraded ? 7m : 5m;
        decimal totalDmg = (leftCost + rightCost) * mult;

        await DamageCmd.Attack(totalDmg)
            .FromCard(this, play)
            .Targeting(play.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
    }
}
