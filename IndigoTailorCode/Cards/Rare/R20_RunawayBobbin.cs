using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
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

namespace IndigoTailor.Cards.Rare;

/// <summary>
/// R20 - 异体植皮 (Skin Grafting / Runaway Bobbin)
/// (保留。)选择 1 张手牌使其费用 -1 并赋予【布贴】(获得 6(8) 点格挡)。消耗。
/// </summary>
public sealed class RunawayBobbin() : IndigoTailorCard(
    baseCost: 1,
    type: CardType.Skill,
    rarity: CardRarity.Rare,
    target: TargetType.Self)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(6m, ValueProp.Move),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_STITCH");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var hand = Owner?.PlayerCombatState?.Hand?.Cards;
        if (hand != null && hand.Count > 1)
        {
            var chosen = await CommonActions.SelectSingleCard(
                this,
                RawScrapHelper.GetPrompt("INDIGOTAILOR-SELECT_TO_STITCH", CardSelectorPrefs.DiscardSelectionPrompt),
                choiceContext,
                PileType.Hand,
                c => !ReferenceEquals(c, this));

            if (chosen != null)
            {
                chosen.EnergyCost.AddThisTurn(-1, true);

                // ⚠ 必须走 LoomSeamManager.AddPatchDirectly，不能只往 AttachedPatches 里塞。
                // 后者只更新「卡牌侧」的数据，不会创建 TailorPatchEnchantment 附魔 ——
                // 而补丁的实际效果（伤害/格挡加成、OnPlay 附加效果）全在附魔里结算。
                // 历史 Bug：4 条「裸补丁」路径都犯这个错（R20 异体植皮、R13 逆裁、
                // 沉船织坊事件、顶针油），补丁显示在卡上但完全不生效。
                LoomSeamManager.AddPatchDirectly(chosen, new PatchworkSlotData
                {
                    Kind = PatchworkKind.IndigoLining,
                    Magnitude = (int)DynamicVars.Block.BaseValue,
                    IsUpgraded = IsUpgraded
                });
            }
        }

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}
