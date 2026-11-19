using System;
using System.Collections.Generic;
using System.Linq;
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
/// R10 - 金缮 (Kintsugi Permanence)
/// (保留。)选择 1 张手牌，将其上的第 1 枚【布贴】永久保存至牌库此牌上。消耗。
/// </summary>
public sealed class KintsugiPermanence() : IndigoTailorCard(
    baseCost: 1,
    type: CardType.Skill,
    rarity: CardRarity.Rare,
    target: TargetType.Self)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_STITCH");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var hand = Owner?.PlayerCombatState?.Hand?.Cards;
        if (hand != null)
        {
            var candidates = hand.OfType<IndigoTailorCard>().Where(c => c.AttachedPatches.Count > 0).ToList();
            if (candidates.Count > 0)
            {
                var chosen = await CommonActions.SelectSingleCard(
                    this,
                    RawScrapHelper.GetPrompt("INDIGOTAILOR-SELECT_TO_KINTSUGI", CardSelectorPrefs.EnchantSelectionPrompt),
                    choiceContext,
                    PileType.Hand,
                    c => c is IndigoTailorCard itc && itc.AttachedPatches.Count > 0);

                if (chosen is IndigoTailorCard itc && itc.AttachedPatches.Count > 0)
                {
                    var patch = itc.AttachedPatches[0];

                    // ⚠ 关键：不能把「被选中的那张手牌」上的补丁直接标记成 IsPermanent。
                    // 手牌是战斗内的可变克隆，它会随战斗结束消失；
                    // 真正要永久保存的是**牌库里那张同名卡**。
                    //
                    // 历史 Bug：原实现只给 deckCard 追加补丁，却把 itc 上那枚的 IsPermanent 也置了 true ——
                    // 导致手牌这张在战斗内被误判为永久补丁，卸不掉。
                    var deckCard = Owner?.Deck?.Cards.OfType<IndigoTailorCard>()
                                        .FirstOrDefault(c => c.Id == itc.Id);
                    if (deckCard != null)
                    {
                        // 追加前先 hydrate 已保存的旧永久补丁，避免 Sync 时覆盖旧数据。
                        if (deckCard.AttachedPatches.All(p => !p.IsPermanent))
                            Patches.PermanentPatchworkSavePatch.RestorePermanentPatchesFromString(deckCard);
                        deckCard.AttachedPatches.Add(new PatchworkSlotData
                        {
                            Kind = patch.Kind,
                            Magnitude = patch.Magnitude,
                            IsUpgraded = patch.IsUpgraded,
                            IsPermanent = true,
                            SourceCardId = patch.SourceCardId
                        });

                        // ⚠ 不能手写成 $"{Kind}:{Magnitude}:{IsUpgraded}"。
                        // PermanentPatchwerkSavePatch 的格式是
                        //   "{int Kind}:{Magnitude}:{0|1}:{SourceCardId}"，多条用 ";" 分隔。
                        // 手写会：
                        //   1. Kind 写成枚举名（"CrimsonNeedle"），读档时 int.TryParse 失败 → 补丁丢失；
                        //   2. 覆盖掉这张牌之前已经保存的其它永久补丁。
                        // 正确做法是统一走 SyncPermanentPatchesToString，它会从 AttachedPatches 全部重建。
                        Patches.PermanentPatchworkSavePatch.SyncPermanentPatchesToString(deckCard);
                    }
                }
            }
        }

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
