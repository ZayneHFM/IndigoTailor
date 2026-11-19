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
/// U13 - 解剖服 (Autopsy Coat / Reversible Greatcoat)
/// 3费技能：获得 7(10) 点格挡，将 1(2) 张【针胚(+)】置入手牌；对所有敌人造成等同于你当前格挡值 50%(75%) 的伤害。
/// 【裁剪】
/// </summary>
public sealed class ReversibleGreatcoat() : IndigoTailorCard(
    baseCost: 3,
    type: CardType.Skill,
    rarity: CardRarity.Uncommon,
    target: TargetType.AllEnemies)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(7m, ValueProp.Move),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_CUT");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // 获得 7(10) 点格挡
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);

        // 将 1(2) 张【针胚(+)】置入手牌
        int needleCount = IsUpgraded ? 2 : 1;
        for (int i = 0; i < needleCount; i++)
        {
            await RawScrapHelper.AddScrapToHandSafe(Owner, "DYED_SCRAP_NEEDLE", IsUpgraded);
        }

        // 对所有敌人造成等同于你当前格挡值 50%(75%) 的伤害
        var combatState = Owner?.Creature?.CombatState;
        if (combatState != null && Owner?.Creature != null)
        {
            decimal ratio = IsUpgraded ? 0.75m : 0.50m;
            decimal dmg = Math.Floor(Owner.Creature.Block * ratio);
            await DamageCmd.Attack(dmg)
                .FromCard(this, play)
                .TargetingAllOpponents(combatState)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}
