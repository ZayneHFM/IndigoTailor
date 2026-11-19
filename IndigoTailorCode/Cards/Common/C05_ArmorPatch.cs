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
/// C05 - 剥痂 (Armor Patch / Scab Graft)
/// 移除敌人最多 8(12) 点格挡，将 1(2) 张【布胚(+)】置入手牌。
/// </summary>
public sealed class ArmorPatch() : IndigoTailorCard(
    baseCost: 1,
    type: CardType.Skill,
    rarity: CardRarity.Common,
    target: TargetType.AnyEnemy)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_STITCH");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (play.Target != null && play.Target.Block > 0 && Owner?.Creature != null)
        {
            decimal removeMax = IsUpgraded ? 12m : 8m;
            decimal toRemove = Math.Min(play.Target.Block, removeMax);
            if (toRemove > 0)
            {
                await CreatureCmd.LoseBlock(choiceContext, play.Target, toRemove, Owner.Creature);
            }
        }

        int count = IsUpgraded ? 2 : 1;
        for (int i = 0; i < count; i++)
        {
            await RawScrapHelper.AddScrapToHandSafe(Owner, "DYED_SCRAP_CRINOLINE", IsUpgraded);
        }

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
    }
}
