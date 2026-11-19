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

namespace IndigoTailor.Cards.Basic;

/// <summary>
/// B03 - 斜裁 (Bias Cut)
/// 1 费技能：【裁剪】1 张手牌。升级后 0 费，不再额外抽牌。
///
/// 平衡调整（2026-11-15）：
/// 原为 0 费起手，升级后额外抽 1 张。改成 1(0) 费、升级只降费。
/// 理由：0 费起手的裁剪让开局节奏过快，「升级多抽一张」的强度也远超其它基础牌。
/// </summary>
public sealed class BiasCut() : IndigoTailorCard(
    baseCost: 1,
    type: CardType.Skill,
    rarity: CardRarity.Basic,
    target: TargetType.Self)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_CUT");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await RawScrapHelper.CutHandCardAndSpawnScraps(choiceContext, Owner, this, isUpgraded: IsUpgraded);
        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    /// <summary>
    /// 升级只降费：1 → 0。不再额外抽牌。
    /// </summary>
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
