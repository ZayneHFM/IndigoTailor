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
/// U18 - 皮瓣转位 (Offset Slash / Flap Transposition)
/// 0费技能：本回合中，交换位于【幅边】的卡牌的费用。（升级：保留）
/// </summary>
public sealed class OffsetSlash() : IndigoTailorCard(
    baseCost: 0,
    type: CardType.Skill,
    rarity: CardRarity.Uncommon,
    target: TargetType.Self)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_SELVEDGE");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var hand = Owner?.PlayerCombatState?.Hand?.Cards;
        if (hand != null && hand.Count >= 2)
        {
            CardModel left = hand[0];
            CardModel right = hand[^1];
            int costLeft = left.EnergyCost.GetWithModifiers(CostModifiers.All);
            int costRight = right.EnergyCost.GetWithModifiers(CostModifiers.All);
            left.EnergyCost.SetThisTurn(costRight, false);
            right.EnergyCost.SetThisTurn(costLeft, false);
        }

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
