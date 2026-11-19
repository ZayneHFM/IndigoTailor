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

namespace IndigoTailor.Cards.Token;

/// <summary>
/// T10 - 解剖服·右 (Autopsy Coat: Right)
/// 对所有敌人造成等同于你当前格挡值 50%(75%) 的伤害。消耗。
/// 缝合：伤害增加当前格挡 50%。
/// </summary>
public sealed class AutopsyCoatRight() : IndigoTailorCard(
    baseCost: 1,
    type: CardType.Attack,
    rarity: CardRarity.Token,
    target: TargetType.AllEnemies)
{
    public override bool IsScrapToken => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_STITCH");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var combatState = Owner?.Creature?.CombatState;
        if (combatState == null) return;

        decimal fraction = IsUpgraded ? 0.75m : 0.50m;
        decimal currentBlock = Owner?.Creature?.Block ?? 0m;
        decimal baseDmg = Math.Floor(currentBlock * fraction);
        decimal totalDmg = baseDmg;

        await DamageCmd.Attack(totalDmg)
            .FromCard(this, play)
            .TargetingAllOpponents(combatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
    }
}
