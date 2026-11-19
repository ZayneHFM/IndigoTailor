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

namespace IndigoTailor.Cards.Common;

/// <summary>
/// C20 - 合捻 (Ply Merge)
/// 1费：【缝合】1 对相邻手牌，抽 2(3) 张牌。
/// </summary>
public sealed class PlyMerge() : IndigoTailorCard(
    baseCost: 1,
    type: CardType.Skill,
    rarity: CardRarity.Common,
    target: TargetType.Self)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_STITCH", "INDIGOTAILOR_ADJACENT");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await LoomSeamManager.StitchFirstAvailableSeamPair(choiceContext, Owner, this);
        if (Owner != null)
        {
            await CardPileCmd.Draw(choiceContext, IsUpgraded ? 3m : 2m, Owner, false);
        }
        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
    }
}
