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
/// U01 - 一剪两断 (Sever in Twain)
/// 造成 8(10) 点伤害，给予 1(2) 层易伤；获得 8(10) 点格挡，给予 1(2) 层虚弱。
/// 【裁剪】
/// </summary>
public sealed class SeverInTwain() : IndigoTailorCard(
    baseCost: 2,
    type: CardType.Attack,
    rarity: CardRarity.Uncommon,
    target: TargetType.AnyEnemy)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Move),
        new BlockVar(8m, ValueProp.Move),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_CUT");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, play)
            .Targeting(play.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        if (play.Target != null && play.Target.IsAlive)
        {
            await PowerCmd.Apply<MegaCrit.Sts2.Core.Models.Powers.VulnerablePower>(choiceContext, play.Target, IsUpgraded ? 2m : 1m, Owner.Creature, this, false);
        }

        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);

        if (play.Target != null && play.Target.IsAlive)
        {
            await PowerCmd.Apply<MegaCrit.Sts2.Core.Models.Powers.WeakPower>(choiceContext, play.Target, IsUpgraded ? 2m : 1m, Owner.Creature, this, false);
        }

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}
