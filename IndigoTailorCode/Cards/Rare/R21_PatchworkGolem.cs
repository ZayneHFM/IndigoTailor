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
/// R21 - 百衲魔像 (Patchwork Golem)
/// 造成 14(18) 点伤害，获得 14(18) 点格挡。本场战斗中每【缝合】过 1 次，伤害与格挡 +4(6)。
/// </summary>
public sealed class PatchworkGolem() : IndigoTailorCard(
    baseCost: 3,
    type: CardType.Attack,
    rarity: CardRarity.Rare,
    target: TargetType.AnyEnemy)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(14m, ValueProp.Move),
        new BlockVar(14m, ValueProp.Move),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_STITCH");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);

        int stitches = LoomSeamManager.StitchesThisCombat;
        decimal bonusPerStitch = IsUpgraded ? 6m : 4m;
        decimal bonus = stitches * bonusPerStitch;

        decimal totalDmg = DynamicVars.Damage.BaseValue + bonus;
        decimal totalBlock = DynamicVars.Block.BaseValue + bonus;

        await DamageCmd.Attack(totalDmg)
            .FromCard(this, play)
            .Targeting(play.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        if (Owner?.Creature != null)
        {
            await CreatureCmd.GainBlock(Owner.Creature, totalBlock, ValueProp.Move, play);
        }

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
        DynamicVars.Block.UpgradeValueBy(4m);
    }
}
