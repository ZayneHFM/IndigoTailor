using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using IndigoTailor.Mechanics;
using IndigoTailor.Powers;

namespace IndigoTailor.Cards.Uncommon;

/// <summary>
/// U08 - 钉合 (Emergency Baste)
/// 获得 15(20) 点格挡。将 2 张《伤口》分别置入手牌两端。
/// </summary>
public sealed class EmergencyBaste() : IndigoTailorCard(
    baseCost: 1,
    type: CardType.Skill,
    rarity: CardRarity.Uncommon,
    target: TargetType.Self)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(15m, ValueProp.Move),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_SELVEDGE");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);

        if (Owner?.PlayerCombatState != null && Owner.Creature?.CombatState != null)
        {
            var hand = Owner.PlayerCombatState.Hand;
            CardModel wLeft = Owner.Creature.CombatState.CreateCard<MegaCrit.Sts2.Core.Models.Cards.Wound>(Owner);
            CardModel wRight = Owner.Creature.CombatState.CreateCard<MegaCrit.Sts2.Core.Models.Cards.Wound>(Owner);

            // 两张都先塞进手牌，再分别摆到两端。
            // 早期实现只把 wLeft 移到 index 0，wRight 留在 Top，结果两张都在左端，
            // 与卡面「分别置入手牌两端」不符。
            await CardPileCmd.AddGeneratedCardToCombat(wLeft, PileType.Hand, Owner, CardPilePosition.Top);
            await CardPileCmd.AddGeneratedCardToCombat(wRight, PileType.Hand, Owner, CardPilePosition.Top);

            // 左端：wLeft 插到 index 0
            hand.RemoveInternal(wLeft, true);
            hand.AddInternal(wLeft, 0, true);

            // 右端：wRight 插到最后一个位置
            hand.RemoveInternal(wRight, true);
            hand.AddInternal(wRight, Math.Max(0, hand.Cards.Count), true);

            hand.InvokeContentsChanged();
        }

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(5m);
    }
}
