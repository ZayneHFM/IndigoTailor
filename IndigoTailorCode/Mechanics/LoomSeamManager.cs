using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using IndigoTailor.Cards;
using IndigoTailor.Powers;
using IndigoTailor.Relics;

namespace IndigoTailor.Mechanics;

/// <summary>
/// 手牌空间拓扑与核心机制管理器：
/// - 【相邻】(Adjacent): 紧挨着的左右手牌（飞梭引纬生效时可跨越任意两张手牌）。
/// - 【幅边】(Selvedge): 手牌最左端 (Index 0) 或最右端 (Index Count - 1)。
/// - 统筹调度【裁剪】、【缝合】、【拆线】向遗物与能力分发战斗事件。
/// </summary>
public static class LoomSeamManager
{
    public static bool KeyholeEnergyUsedThisTurn { get; set; }
    public static int ScrapsSavedByHurricaneThisTurn { get; set; }
    public static int StitchesThisCombat { get; set; }
    public static bool IsInStitchSelectionMode { get; set; }
    public static CardModel? StitchSourceCard { get; set; }

    /// <summary>
    /// 顶针圣油：喝下当刻的手牌在本回合被强制视为「幅边」。
    /// 用引用身份比较，避免依赖 CardModel 的值相等语义；
    /// 只由 ResetTurnState / ResetCombatState 清理，不写入 WasAtSelvedgeWhenPlayed 缓存。
    /// </summary>
    private static readonly HashSet<CardModel> ForcedSelvedgeCards =
        new(ReferenceEqualityComparer.Instance);

    // ── 战斗内跨回合状态 ──────────────────────────────────────────────
    // 这几个字段是 static，如果不显式重置会跨战斗泄漏：
    //   StitchesThisCombat 会让《百衲魔像》从第 2 场战斗起就吃到上一场的加成；
    //   KeyholeEnergyUsedThisTurn / ScrapsSavedByHurricaneThisTurn 会让「每回合限一次」
    //   变成「整局限一次」。
    // 现在由 CombatLifecyclePatches 在战斗开始时和玩家回合开始时重置。
    // 注意：InitializeHooks() 只在 Mod 加载时跑一次，不能当战斗重置用。

    /// <summary>战斗开始时调用：清空跨战斗累计值。</summary>
    public static void ResetCombatState()
    {
        StitchesThisCombat = 0;
        KeyholeEnergyUsedThisTurn = false;
        ScrapsSavedByHurricaneThisTurn = 0;
        IsInStitchSelectionMode = false;
        StitchSourceCard = null;
        ForcedSelvedgeCards.Clear();
    }

    /// <summary>玩家回合开始时调用：清空「本回合一次」类标记。</summary>
    public static void ResetTurnState()
    {
        KeyholeEnergyUsedThisTurn = false;
        ScrapsSavedByHurricaneThisTurn = 0;
        ForcedSelvedgeCards.Clear();
    }

    public static void InitializeHooks()
    {
        ResetCombatState();
    }

    public static bool AreCardsSeamConnected(CardModel left, CardModel right)
    {
        if (left == null || right == null) return false;
        return true; // 所有相邻卡牌均可自然对合缝合
    }

    public static int FindHandCardIndex(IReadOnlyList<CardModel>? hand, CardModel? card)
    {
        if (hand == null || card == null) return -1;
        for (int i = 0; i < hand.Count; i++)
            if (ReferenceEquals(hand[i], card) || hand[i] == card) return i;
        return -1;
    }

    public static bool IsCardInLoomSeam(IndigoTailorCard card)
    {
        var hand = card.Owner?.PlayerCombatState?.Hand?.Cards;
        if (hand == null || hand.Count < 2) return false;
        if (card.Owner?.Creature != null && card.Owner.Creature.HasPower<FlyingShuttlePower>())
            return true;

        int idx = FindHandCardIndex(hand, card);
        if (idx < 0) return false;

        if (idx > 0 && AreCardsSeamConnected(hand[idx - 1], card))
            return true;
        if (idx < hand.Count - 1 && AreCardsSeamConnected(card, hand[idx + 1]))
            return true;

        if ((idx == 0 || idx == hand.Count - 1) &&
            card.Owner?.Creature != null &&
            card.Owner.Creature.HasPower<TubularWeavePower>() &&
            AreCardsSeamConnected(hand[0], hand[^1]))
        {
            return true;
        }
        return false;
    }

    /// <summary>顶针圣油：把「喝下当刻的手牌」标记为强制幅边（引用身份，不落缓存）。</summary>
    public static void MarkForcedSelvedge(CardModel card)
    {
        if (card != null) ForcedSelvedgeCards.Add(card);
    }

    /// <summary>该牌本回合是否被强制视为幅边（顶针圣油）。</summary>
    public static bool IsForcedSelvedge(CardModel? card)
        => card != null && ForcedSelvedgeCards.Contains(card);

    public static bool IsCardAtSelvedge(IndigoTailorCard card)
    {
        if (IsForcedSelvedge(card))
            return true;

        var hand = card.Owner?.PlayerCombatState?.Hand?.Cards;
        if (hand == null || hand.Count == 0)
        {
            return card.WasAtSelvedgeWhenPlayed;
        }

        int idx = FindHandCardIndex(hand, card);
        if (idx < 0)
        {
            return card.WasAtSelvedgeWhenPlayed;
        }

        return idx == 0 || idx == hand.Count - 1;
    }

    public static (PatchworkSlotData patch, bool generatesTangledHemp) ConvertCardToPatchwork(CardModel subCard)
    {
        string entry = subCard.Id.Entry;
        PatchworkSlotData patch;
        bool generatesTangled = false;

        if (entry.Contains("OVERLOCK_FLURRY"))
            patch = new PatchworkSlotData { Kind = PatchworkKind.MultiHitLock, Magnitude = 1, IsUpgraded = subCard.IsUpgraded };
        else if (entry.Contains("HIDDEN_SNAP"))
            patch = new PatchworkSlotData { Kind = PatchworkKind.HiddenWeak, Magnitude = subCard.IsUpgraded ? 2 : 1, IsUpgraded = subCard.IsUpgraded };
        else if (entry.Contains("BARBED_HOOK_NEEDLE"))
            patch = new PatchworkSlotData { Kind = PatchworkKind.BarbedUnravel, Magnitude = subCard.IsUpgraded ? 3 : 2, IsUpgraded = subCard.IsUpgraded };
        else if (entry.Contains("KEYHOLE_BUTTONHOLE"))
            patch = new PatchworkSlotData { Kind = PatchworkKind.KeyholeEnergy, Magnitude = 1, IsUpgraded = subCard.IsUpgraded };
        else if (entry.Contains("HORSEHAIR_INTERLINING"))
            patch = new PatchworkSlotData { Kind = PatchworkKind.HorsehairRetain, Magnitude = subCard.IsUpgraded ? 6 : 4, IsUpgraded = subCard.IsUpgraded };
        else if (entry.Contains("WOUND"))
            patch = new PatchworkSlotData { Kind = PatchworkKind.WoundHeal, Magnitude = 1, IsUpgraded = false };
        else if (entry.Contains("SCAB_TOKEN"))
            patch = new PatchworkSlotData { Kind = PatchworkKind.WoundHeal, Magnitude = 1, IsUpgraded = false };
        else if (entry.Contains("BLOOD_PRICE"))
            patch = new PatchworkSlotData { Kind = PatchworkKind.Cost, Magnitude = 3, IsUpgraded = false };
        else if (entry.Contains("TANGLED_THREAD"))
            patch = new PatchworkSlotData { Kind = PatchworkKind.KintsugiGold, Magnitude = 1, IsUpgraded = false };
        else if (entry.Contains("DYED_SCRAP_NEEDLE"))
            patch = new PatchworkSlotData { Kind = PatchworkKind.CrimsonNeedle, Magnitude = subCard.IsUpgraded ? 6 : 4, IsUpgraded = subCard.IsUpgraded };
        else if (entry.Contains("DYED_SCRAP_CRINOLINE"))
            patch = new PatchworkSlotData { Kind = PatchworkKind.IndigoLining, Magnitude = subCard.IsUpgraded ? 6 : 4, IsUpgraded = subCard.IsUpgraded };
        else if (entry.Contains("RAW_SCRAP_KINTSUGI"))
            patch = new PatchworkSlotData { Kind = PatchworkKind.KintsugiGold, Magnitude = 1, IsUpgraded = subCard.IsUpgraded };
        // 衍生分件专属缝合补丁
        else if (entry.Contains("HALF_CUT_EDGE"))
            patch = new PatchworkSlotData { Kind = PatchworkKind.CrimsonNeedle, Magnitude = subCard.IsUpgraded ? 8 : 6, IsUpgraded = subCard.IsUpgraded };
        else if (entry.Contains("HALF_CUT_LINING"))
            patch = new PatchworkSlotData { Kind = PatchworkKind.IndigoLining, Magnitude = subCard.IsUpgraded ? 8 : 6, IsUpgraded = subCard.IsUpgraded };
        else if (entry.Contains("AUTOPSY_COAT_LEFT"))
            patch = new PatchworkSlotData { Kind = PatchworkKind.IndigoLining, Magnitude = subCard.IsUpgraded ? 8 : 6, IsUpgraded = subCard.IsUpgraded };
        else if (entry.Contains("AUTOPSY_COAT_RIGHT"))
            patch = new PatchworkSlotData { Kind = PatchworkKind.CrimsonNeedle, Magnitude = subCard.IsUpgraded ? 8 : 6, IsUpgraded = subCard.IsUpgraded };
        else if (entry.Contains("TWIN_FATE_SHEARS_LEFT"))
            patch = new PatchworkSlotData { Kind = PatchworkKind.BarbedUnravel, Magnitude = subCard.IsUpgraded ? 3 : 2, IsUpgraded = subCard.IsUpgraded };
        else if (entry.Contains("TWIN_FATE_SHEARS_RIGHT"))
            patch = new PatchworkSlotData { Kind = PatchworkKind.MultiHitLock, Magnitude = 1, IsUpgraded = subCard.IsUpgraded };
        else if (entry.Contains("RIB_SPREADER_LEFT"))
            patch = new PatchworkSlotData { Kind = PatchworkKind.KeyholeEnergy, Magnitude = 1, IsUpgraded = subCard.IsUpgraded };
        else if (entry.Contains("RIB_SPREADER_RIGHT"))
            patch = new PatchworkSlotData { Kind = PatchworkKind.IndigoLining, Magnitude = subCard.IsUpgraded ? 8 : 6, IsUpgraded = subCard.IsUpgraded };
        else if (entry.Contains("AMPUTATION_SAW_LEFT"))
            patch = new PatchworkSlotData { Kind = PatchworkKind.CrimsonNeedle, Magnitude = subCard.IsUpgraded ? 12 : 9, IsUpgraded = subCard.IsUpgraded };
        else if (entry.Contains("AMPUTATION_SAW_RIGHT"))
            patch = new PatchworkSlotData { Kind = PatchworkKind.KeyholeEnergy, Magnitude = 1, IsUpgraded = subCard.IsUpgraded };
        else if (entry.Contains("THORACIC_CAGE_LEFT"))
            patch = new PatchworkSlotData { Kind = PatchworkKind.IndigoLining, Magnitude = subCard.IsUpgraded ? 16 : 12, IsUpgraded = subCard.IsUpgraded };
        else if (entry.Contains("THORACIC_CAGE_RIGHT"))
            patch = new PatchworkSlotData { Kind = PatchworkKind.IndigoLining, Magnitude = subCard.IsUpgraded ? 6 : 4, IsUpgraded = subCard.IsUpgraded };
        else
        {
            generatesTangled = true;
            patch = subCard.Type switch
            {
                CardType.Attack => new PatchworkSlotData
                {
                    Kind = PatchworkKind.CrimsonNeedle,
                    Magnitude = subCard.IsUpgraded ? 6 : 4,
                    IsUpgraded = subCard.IsUpgraded
                },
                CardType.Skill => new PatchworkSlotData
                {
                    Kind = PatchworkKind.IndigoLining,
                    Magnitude = subCard.IsUpgraded ? 6 : 4,
                    IsUpgraded = subCard.IsUpgraded
                },
                _ => new PatchworkSlotData
                {
                    Kind = PatchworkKind.KintsugiGold,
                    Magnitude = 1,
                    IsUpgraded = subCard.IsUpgraded
                }
            };
        }

        patch.SourceCardId = subCard.Id.Entry;
        return (patch, generatesTangled);
    }

    /// <summary>
    /// 缝合结果浮窗的文本。
    ///
    /// 格式：`副牌名（效果）`，外层由 <c>STITCH_UI_RESULT</c> = 「缝合：{0}」包住，
    /// 最终显示成「缝合：乱麻（抽 1 张牌）」。
    ///
    /// 为什么带副牌名：玩家在选择副牌时最需要确认的是「我选的这张是谁」，
    /// 只写效果（「抽 1 张牌」）会让人分不清自己选中的是乱麻还是线胚 —— 两者效果相同但来源不同。
    /// </summary>
    public static (string effectText, bool generatesTangled) GetStitchPreviewText(CardModel subCard)
    {
        var (patch, generates) = ConvertCardToPatchwork(subCard);
        string effect = patch.Kind switch
        {
            PatchworkKind.MultiHitLock => StitchLoc.Get("INDIGOTAILOR-STITCH_PREVIEW_MULTIHIT", "攻击段数 +1"),
            PatchworkKind.HiddenWeak => StitchLoc.Get("INDIGOTAILOR-STITCH_PREVIEW_WEAK", "给予 {0} 层虚弱", patch.Magnitude),
            PatchworkKind.BarbedUnravel => StitchLoc.Get("INDIGOTAILOR-STITCH_PREVIEW_UNRAVEL", "给予 {0} 层脱线", patch.Magnitude),
            PatchworkKind.KeyholeEnergy => StitchLoc.Get("INDIGOTAILOR-STITCH_PREVIEW_ENERGY", "获得 1 点能量"),
            PatchworkKind.HorsehairRetain => StitchLoc.Get("INDIGOTAILOR-STITCH_PREVIEW_RETAIN", "获得保留"),
            PatchworkKind.WoundHeal => StitchLoc.Get("INDIGOTAILOR-STITCH_PREVIEW_HEAL", "回复 1 点生命"),
            PatchworkKind.CrimsonNeedle => StitchLoc.Get("INDIGOTAILOR-STITCH_PREVIEW_DAMAGE", "造成 {0} 点额外伤害", patch.Magnitude),
            PatchworkKind.IndigoLining => StitchLoc.Get("INDIGOTAILOR-STITCH_PREVIEW_BLOCK", "获得 {0} 点额外格挡", patch.Magnitude),
            PatchworkKind.KintsugiGold => StitchLoc.Get("INDIGOTAILOR-STITCH_PREVIEW_DRAW", "抽 1 张牌"),
            PatchworkKind.Cost => StitchLoc.Get("INDIGOTAILOR-STITCH_PREVIEW_COST", "获得 1 张《伤口》"),
            _ => StitchLoc.Get("INDIGOTAILOR-STITCH_PREVIEW_NONE", "无特殊效果")
        };

        string cardName = subCard.Title ?? string.Empty;
        string text = string.IsNullOrEmpty(cardName)
            ? effect
            : StitchLoc.Get("INDIGOTAILOR-STITCH_PREVIEW_FORMAT", "{0}（{1}）", cardName, effect);

        return (text, generates);
    }

    #region Central Event Handlers

    public static async Task OnCardCut(PlayerChoiceContext choiceContext, Player? player, CardModel? cutter, CardModel targetCard)
    {
        if (player == null) return;

        var shears = player.GetRelic<UnderdocksTailoringShears>();
        if (shears != null) await shears.OnCardCut(choiceContext);

        if (player.Creature != null)
        {
            var masterCutter = player.Creature.GetPower<MasterCutterPower>();
            if (masterCutter != null) await masterCutter.OnCutOrUnpick(choiceContext);

            var mannequin = player.Creature.GetPower<StrawMannequinPower>();
            if (mannequin != null) await mannequin.OnCardExhaustedOrStitched(choiceContext);
        }
    }

    public static async Task OnCardsStitched(PlayerChoiceContext choiceContext, Player? player, CardModel hostCard, CardModel subCard)
    {
        StitchesThisCombat++;
        if (player == null) return;

        var chalk = player.GetRelic<NotchedTailorChalk>();
        if (chalk != null) await chalk.OnSeamOrStitchTriggered();

        var button = player.GetRelic<TwoHoleBrassButton>();
        if (button != null) await button.OnScrapConsumedOrStitched();

        if (player.Creature != null)
        {
            var seamstress = player.Creature.GetPower<ArtisanSeamstressPower>();
            if (seamstress != null) await seamstress.OnStitched(choiceContext);

            var running = player.Creature.GetPower<RunningStitchPower>();
            if (running != null) await running.OnSeamOrStitch(choiceContext);

            var bobbin = player.Creature.GetPower<RunawayBobbinPower>();
            if (bobbin != null) await bobbin.OnScrapOrStitched(choiceContext);

            var mannequin = player.Creature.GetPower<StrawMannequinPower>();
            if (mannequin != null) await mannequin.OnCardExhaustedOrStitched(choiceContext);

            if (subCard is IndigoTailorCard tc && tc.IsScrapToken)
            {
                var offcuts = player.Creature.GetPower<CrinolineOffcutsPower>();
                if (offcuts != null) await offcuts.OnScrapStitched(choiceContext);
            }
        }
    }

    public static async Task OnUnpicked(PlayerChoiceContext choiceContext, Player? player, CardModel hostCard, int count)
    {
        if (player == null) return;

        var ripper = player.GetRelic<SilverSeamRipper>();
        if (ripper != null) await ripper.OnUnpickTriggered(choiceContext);

        if (player.Creature != null)
        {
            var masterCutter = player.Creature.GetPower<MasterCutterPower>();
            if (masterCutter != null) await masterCutter.OnCutOrUnpick(choiceContext);
        }
    }

    public static async Task OnScrapPlayed(PlayerChoiceContext choiceContext, Player? player, CardModel scrapCard)
    {
        if (player == null) return;

        var button = player.GetRelic<TwoHoleBrassButton>();
        if (button != null) await button.OnScrapConsumedOrStitched();

        if (player.Creature != null)
        {
            var hurricane = player.Creature.GetPower<ScrapHurricanePower>();
            if (hurricane != null) await hurricane.OnScrapPlayed(choiceContext);

            var bobbin = player.Creature.GetPower<RunawayBobbinPower>();
            if (bobbin != null) await bobbin.OnScrapOrStitched(choiceContext);
        }
    }

    #endregion

    /// <summary>
    /// 判断某张牌能否作为【缝合】的**主牌**接受补丁。
    ///
    /// 关键约束：必须排除 Status / Curse / Quest 类型。
    /// <c>EnchantmentModel.CanEnchant</c> 里有这么一段（sts2 反编译）：
    /// <code>
    /// CardType type = card.Type;
    /// if ((uint)(type - 4) &lt;= 2u) return false;   // 4/5/6 = Status/Curse/Quest
    /// </code>
    /// 而 <see cref="MegaCrit.Sts2.Core.Commands.CardCmd.Enchant"/> 在 CanEnchant 返回 false 时
    /// 会直接 <c>throw new InvalidOperationException("Cannot enchant ...")</c>，
    /// 打牌流程会被中断。
    ///
    /// 历史 Bug：这里原先不查卡牌类型，导致手牌里有《伤口》时
    /// （C03 扩创斩 / C12 飞针雨 / R24 截骨锯 都会产）把它选成主牌缝合会直接崩。
    /// </summary>
    public static bool CanCardAcceptPatch(CardModel card, Player? owner)
    {
        if (card == null) return false;

        // Status / Curse / Quest 不能被附魔（原版 EnchantmentModel.CanEnchant 的硬约束）
        if (card.Type is CardType.Status or CardType.Curse or CardType.Quest)
            return false;

        const int maxCapacity = IndigoTailorCard.DefaultMaxPatches;

        if (card is IndigoTailorCard itc && itc.AttachedPatches.Count >= maxCapacity)
            return false;

        if (card.Enchantment is TailorPatchEnchantment patchEnchant && patchEnchant.Patches.Count >= maxCapacity)
            return false;

        if (card.Enchantment != null && card.Enchantment is not TailorPatchEnchantment)
            return false;

        return true;
    }

    /// <summary>
    /// 取「玩家眼睛看到的」手牌顺序。
    ///
    /// 为什么需要这个：相邻判定原本用 <c>PlayerCombatState.Hand.Cards</c> 的下标（数据顺序），
    /// 但玩家看到的排列由 <c>NPlayerHand.CardHolderContainer</c> 的子节点顺序决定。
    /// 这两个顺序**会永久错位**，因为：
    ///
    /// 1. 原版 <c>NPlayerHand.OnSelectModeSourceFinished</c> 里，
    ///    选牌结束后每张牌走的是 <c>Add(cardNode)</c>（**没有 index，追加到末尾**），
    ///    而 <c>CardPile.Cards</c> 里的位置没变；
    /// 2. 本 Mod 的 C07/C16/U08/U11/U14/R07 直接 <c>RemoveInternal + AddInternal</c> 改
    ///    <c>Hand.Cards</c> 顺序，也没同步 holder 节点顺序。
    ///
    /// 结果：两视觉相邻的牌在数据里可能差 2 位甚至更多 → 触发「未处于相邻缝位」。
    /// 修法：判定优先按视觉顺序（<c>ActiveHolders</c>），拿不到再退回数据顺序。
    /// </summary>
    public static IReadOnlyList<CardModel> GetVisualHandOrder(IReadOnlyList<CardModel> fallback)
    {
        var handNode = MegaCrit.Sts2.Core.Nodes.Combat.NPlayerHand.Instance;
        if (handNode == null) return fallback;

        var holders = handNode.ActiveHolders;
        if (holders == null || holders.Count == 0) return fallback;

        var order = new List<CardModel>(holders.Count);
        foreach (var h in holders)
        {
            var m = h?.CardModel;
            if (m != null) order.Add(m);
        }

        return order.Count >= 2 ? order : fallback;
    }

    /// <summary>
    /// 在给定顺序里找牌的索引；找不到返回 -1。
    /// </summary>
    private static int IndexIn(IReadOnlyList<CardModel> order, CardModel card)
    {
        for (int i = 0; i < order.Count; i++)
        {
            if (ReferenceEquals(order[i], card)) return i;
        }
        return -1;
    }

    /// <summary>
    /// 判断 material 能否作为 host 的【缝合】副牌。
    ///
    /// 「相邻」优先按**玩家视觉顺序**判定，而不是数据顺序 —— 理由见 <see cref="GetVisualHandOrder"/>。
    ///
    /// ⚠ 关键补充（2026-11-15 实机反馈修复）：
    /// 选牌阶段被选中的牌会被 `NPlayerHand.SelectCardInSimpleMode` 从 `CardHolderContainer`
    /// 移到 `_selectedHandCardContainer`，因此**不在 ActiveHolders 里**。
    /// 如果此时只用视觉顺序，两张牌都会查成 -1 → 明明相邻也提示「未处于相邻缝位」。
    /// 所以视觉顺序查不到时必须退回数据顺序（hand.Cards），不能直接判 false。
    /// </summary>
    public static bool IsValidMaterialForHost(IReadOnlyList<CardModel> hand, CardModel host, CardModel material, Player? owner, bool anyPair)
    {
        if (material == host) return false;
        if (anyPair) return true;

        IReadOnlyList<CardModel> order = GetVisualHandOrder(hand);

        int hostIdx = IndexIn(order, host);
        int matIdx = IndexIn(order, material);

        // 视觉顺序里查不到（选牌中被移出 holder 容器）→ 退回数据顺序
        if (hostIdx < 0 || matIdx < 0)
        {
            order = hand;
            hostIdx = FindHandCardIndex(hand, host);
            matIdx = FindHandCardIndex(hand, material);
            if (hostIdx < 0 || matIdx < 0) return false;
        }

        if (Math.Abs(hostIdx - matIdx) == 1) return true;

        // U12 环状吻合：首尾视为相邻
        if (owner?.Creature?.HasPower<TubularWeavePower>() == true &&
            ((hostIdx == 0 && matIdx == order.Count - 1) || (hostIdx == order.Count - 1 && matIdx == 0)))
        {
            return true;
        }

        return false;
    }

    public static async Task ApplyStitch(
        PlayerChoiceContext choiceContext,
        Player owner,
        CardModel host,
        CardModel material,
        int costReductionForCombat = 0,
        int magnitudeMultiplier = 1)
    {
        var (patchData, generatesTangled) = ConvertCardToPatchwork(material);
        if (magnitudeMultiplier > 1)
        {
            patchData.Magnitude *= magnitudeMultiplier;
        }

        TailorPatchEnchantment? enchantment = host.Enchantment as TailorPatchEnchantment;
        if (enchantment == null)
        {
            enchantment = (TailorPatchEnchantment)ModelDb.Enchantment<TailorPatchEnchantment>().ToMutable();
            enchantment.AddPatch(patchData);
            CardCmd.Enchant(enchantment, host, 1m);
        }
        else
        {
            enchantment.AddPatch(patchData);
            enchantment.Amount++;
            enchantment.ModifyCard();
        }

        if (host is IndigoTailorCard itc)
        {
            itc.AttachedPatches.Add(patchData);
        }

        if (costReductionForCombat > 0)
        {
            host.EnergyCost.AddThisCombat(-costReductionForCombat, true);
        }

        if (patchData.Kind == PatchworkKind.HorsehairRetain)
        {
            host.AddKeyword(CardKeyword.Retain);
        }

        await CardCmd.Exhaust(choiceContext, material);

        if (generatesTangled)
        {
            await RawScrapHelper.AddTangledThreadToHandSafe(owner);
        }

        await OnCardsStitched(choiceContext, owner, host, material);
    }

    public static void AddPatchDirectly(CardModel target, PatchworkSlotData patchData)
    {
        TailorPatchEnchantment? enchantment = target.Enchantment as TailorPatchEnchantment;
        if (enchantment == null)
        {
            enchantment = (TailorPatchEnchantment)ModelDb.Enchantment<TailorPatchEnchantment>().ToMutable();
            enchantment.AddPatch(patchData);
            CardCmd.Enchant(enchantment, target, 1m);
        }
        else
        {
            enchantment.AddPatch(patchData);
            enchantment.Amount++;
            enchantment.ModifyCard();
        }

        if (target is IndigoTailorCard itc && !itc.AttachedPatches.Contains(patchData))
        {
            itc.AttachedPatches.Add(patchData);
        }

        if (patchData.Kind == PatchworkKind.HorsehairRetain)
        {
            target.AddKeyword(CardKeyword.Retain);
        }
    }

    public static int RemoveAllPatches(CardModel target)
    {
        int count = 0;
        if (target.Enchantment is TailorPatchEnchantment pe)
        {
            count = pe.Patches.Count;
            pe.ClearPatches();
            CardCmd.ClearEnchantment(target);
        }
        if (target is IndigoTailorCard itc)
        {
            if (count == 0) count = itc.AttachedPatches.Count;
            itc.AttachedPatches.Clear();
        }
        return count;
    }

    public static int GetPatchCount(CardModel? target)
    {
        if (target == null) return 0;
        if (target.Enchantment is TailorPatchEnchantment pe && pe.Patches.Count > 0)
            return pe.Patches.Count;
        if (target is IndigoTailorCard itc)
            return itc.AttachedPatches.Count;
        return 0;
    }

    public static int GetTotalBlockFromAttachedPatches(CardModel target)
    {
        if (target.Enchantment is TailorPatchEnchantment pe)
            return (int)pe.EnchantBlockAdditive(0m);
        if (target is IndigoTailorCard itc)
            return itc.GetTotalPatchBlockBonus();
        return 0;
    }

    /// <summary>
    /// 把目标牌上的补丁数值翻倍（U29《压明线》）。
    ///
    /// 注意：<see cref="ApplyStitch"/> 会把**同一个 PatchworkSlotData 实例引用**
    /// 同时塞进 <c>Enchantment.Patches</c> 和 <c>Card.AttachedPatches</c>，
    /// 所以这里不能对两个列表各遍历一次 —— 那会让同一枚补丁被翻倍两次（×4 而非 ×2）。
    /// 修法：以附魔为主数据源只翻一次；卡对象那边只在「该补丁不在附魔里」时才单独处理。
    /// </summary>
    public static void DoublePatches(CardModel target)
    {
        if (target.Enchantment is TailorPatchEnchantment pe)
        {
            // 附魔存在时，以它为唯一权威来源
            foreach (var p in pe.Patches)
            {
                if (p.Magnitude > 0) p.Magnitude *= 2;
            }
            pe.ModifyCard();
            return;
        }

        // 无附魔时才回退到卡对象上的副本（例如顶针油直接写入的补丁）
        if (target is IndigoTailorCard itc)
        {
            foreach (var p in itc.AttachedPatches)
            {
                if (p.Magnitude > 0) p.Magnitude *= 2;
            }
        }
    }

    public static async Task StitchFirstAvailableSeamPair(
        PlayerChoiceContext choiceContext,
        Player? owner,
        CardModel sourceCard,
        int costReductionForCombat = 0)
    {
        var hand = owner?.PlayerCombatState?.Hand?.Cards;
        if (hand == null || hand.Count < 2 || owner == null) return;

        bool anyPair = owner.Creature?.HasPower<FlyingShuttlePower>() == true;

        var validHosts = hand.Where(c => c != sourceCard &&
                                         CanCardAcceptPatch(c, owner) &&
                                         hand.Any(m => m != sourceCard && IsValidMaterialForHost(hand, c, m, owner, anyPair)))
                             .ToList();

        if (validHosts.Count == 0) return;

        IsInStitchSelectionMode = true;
        StitchSourceCard = sourceCard;
        try
        {
            if (MegaCrit.Sts2.Core.Nodes.Combat.NPlayerHand.Instance != null)
            {
                UI.StitchingNativeOverlay.EnsureAttached(MegaCrit.Sts2.Core.Nodes.Combat.NPlayerHand.Instance);
            }

            var prefs = new CardSelectorPrefs(new LocString("card_selection", "TO_ENCHANT"), 2, 2)
            {
                RequireManualConfirmation = true,
                ShouldGlowGold = c => c != sourceCard && CanCardAcceptPatch(c, owner)
            };

            var chosen = await CardSelectCmd.FromHand(
                choiceContext,
                owner,
                prefs,
                c => c != sourceCard,
                sourceCard);

            var list = chosen?.ToList();
            if (list != null && list.Count == 2)
            {
                CardModel host = list[0];
                CardModel material = list[1];

                // 如果玩家先选了副牌后选了主牌，自动校准
                if (!CanCardAcceptPatch(host, owner) && CanCardAcceptPatch(material, owner))
                {
                    (host, material) = (material, host);
                }

                if (CanCardAcceptPatch(host, owner) && IsValidMaterialForHost(hand, host, material, owner, anyPair))
                {
                    await ApplyStitch(choiceContext, owner, host, material, costReductionForCombat);
                }
            }
        }
        finally
        {
            IsInStitchSelectionMode = false;
            StitchSourceCard = null;
            UI.StitchingNativeOverlay.Instance?.Deactivate();
        }
    }

    /// <summary>
    /// R01 对折缝 (OrigamiFold): 【缝合】位于【幅边】的两张手牌，使获得的【布贴】效果翻倍。消耗。
    /// </summary>
    public static async Task StitchSelvedgePairDoubled(PlayerChoiceContext choiceContext, Player? owner)
    {
        var hand = owner?.PlayerCombatState?.Hand?.Cards;
        if (hand == null || hand.Count < 2 || owner == null) return;

        var left = hand[0];
        var right = hand[^1];
        var candidates = new List<CardModel>();
        if (CanCardAcceptPatch(left, owner)) candidates.Add(left);
        if (CanCardAcceptPatch(right, owner)) candidates.Add(right);

        if (candidates.Count == 0) return;

        CardModel host;
        CardModel material;

        if (candidates.Count == 1)
        {
            host = candidates[0];
            material = host == left ? right : left;
        }
        else
        {
            IsInStitchSelectionMode = true;
            try
            {
                if (MegaCrit.Sts2.Core.Nodes.Combat.NPlayerHand.Instance != null)
                {
                    UI.StitchingNativeOverlay.EnsureAttached(MegaCrit.Sts2.Core.Nodes.Combat.NPlayerHand.Instance);
                }
                var hostPrefs = new CardSelectorPrefs(new LocString("card_selection", "TO_ENCHANT"), 1)
                {
                    ShouldGlowGold = c => candidates.Contains(c)
                };
                var chosen = await CardSelectCmd.FromHand(choiceContext, owner, hostPrefs, c => candidates.Contains(c), left);
                host = chosen?.FirstOrDefault() ?? left;
                material = host == left ? right : left;
            }
            finally
            {
                IsInStitchSelectionMode = false;
                UI.StitchingNativeOverlay.Instance?.Deactivate();
            }
        }

        await ApplyStitch(choiceContext, owner, host, material, magnitudeMultiplier: 2);
    }

    public static async Task UnpickTargetOrAllHandCards(PlayerChoiceContext choiceContext, Player? owner, IndigoTailorCard sourceCard, bool upgraded)
    {
        var hand = owner?.PlayerCombatState?.Hand?.Cards;
        if (hand == null || owner == null) return;

        var cardsWithPatches = hand.Where(c => (c is IndigoTailorCard itc && itc.AttachedPatches.Count > 0) ||
                                               (c.Enchantment is TailorPatchEnchantment pe && pe.Patches.Count > 0))
                                   .ToList();

        if (cardsWithPatches.Count == 0) return;

        CardModel? targetCard = null;
        if (cardsWithPatches.Count == 1)
        {
            targetCard = cardsWithPatches[0];
        }
        else
        {
            var prefs = new CardSelectorPrefs(new LocString("card_selection", "TO_UNPICK"), 1);
            var chosen = await CardSelectCmd.FromHand(choiceContext, owner, prefs, c => cardsWithPatches.Contains(c), sourceCard);
            targetCard = chosen?.FirstOrDefault() ?? cardsWithPatches[0];
        }

        if (targetCard == null) return;

        List<PatchworkSlotData> patchesToStrip = new();
        if (targetCard.Enchantment is TailorPatchEnchantment pe)
        {
            patchesToStrip.AddRange(pe.Patches);
            pe.ClearPatches();
            CardCmd.ClearEnchantment(targetCard);
        }
        if (targetCard is IndigoTailorCard itc)
        {
            if (patchesToStrip.Count == 0) patchesToStrip.AddRange(itc.AttachedPatches);
            itc.AttachedPatches.Clear();
        }

        int count = patchesToStrip.Count;
        for (int k = 0; k < count; k++)
        {
            var p = patchesToStrip[k];
            switch (p.Kind)
            {
                case PatchworkKind.CrimsonNeedle:
                    await RawScrapHelper.AddNeedleBlankToHand(owner, true);
                    break;
                case PatchworkKind.IndigoLining:
                    await RawScrapHelper.AddClothBlankToHand(owner, true);
                    break;
                default:
                    await RawScrapHelper.AddRandomUpgradedScrapToHand(owner);
                    break;
            }
        }

        await OnUnpicked(choiceContext, owner, targetCard, count);
    }

    public static async Task ExecuteUnravelingCataclysmBurst(PlayerChoiceContext choiceContext, Player? owner, IndigoTailorCard sourceCard, int extraDmgPerStack)
    {
        if (owner?.Creature?.CombatState == null) return;
        int totalStripped = 0;
        foreach (var enemy in owner.Creature.CombatState.HittableEnemies)
        {
            var up = enemy.GetPower<UnravelPower>();
            if (up != null && up.Amount > 0)
            {
                totalStripped += (int)up.Amount;
                await PowerCmd.Remove(up);
            }
        }
        if (totalStripped > 0)
        {
            decimal burst = totalStripped * extraDmgPerStack;
            foreach (var enemy in owner.Creature.CombatState.HittableEnemies)
                await CreatureCmd.Damage(choiceContext, enemy, burst, ValueProp.Move, owner.Creature);
        }
    }

    public static int CountAllHandPatches(Player? owner)
        => owner?.PlayerCombatState?.Hand?.Cards.OfType<IndigoTailorCard>().Sum(c => c.AttachedPatches.Count) ?? 0;

    public static int CountActiveSeamsAndHandScraps(Player? owner)
    {
        var hand = owner?.PlayerCombatState?.Hand?.Cards;
        if (hand == null) return 0;
        int count = 0;
        foreach (var c in hand)
        {
            if (c is IndigoTailorCard itc)
            {
                count += itc.AttachedPatches.Count;
                if (itc.IsScrapToken) count++;
            }
        }
        return count;
    }

    /// <summary>
    /// 顶针圣油：喝下当刻的手牌全部标记为「本回合视为幅边」，
    /// 其中可附魔者各加 1 枚【补丁】(布胚 +格挡)；不可附魔者只标记、跳过加补丁。
    ///
    /// ⚠ 必须走 <see cref="AddPatchDirectly"/>，不能只写 <c>AttachedPatches</c>。
    /// 补丁的实际效果（格挡加成）由 <see cref="TailorPatchEnchantment"/> 结算，
    /// 只写卡牌侧数据会出现「卡上显示有补丁、但打出去没加成」。
    /// 历史 Bug：这是 4 条裸补丁路径之一。
    /// </summary>
    public static void ForceAllHandSeamAndGrantLiningPatch(Player owner, int blockBonus)
    {
        var hand = owner?.PlayerCombatState?.Hand?.Cards;
        if (hand == null) return;
        foreach (var c in hand.ToList())
        {
            // 先无条件标记强制幅边：即使这张牌不能被附魔（诅咒/状态/已占用外部附魔），
            // 只要喝药当刻在手牌里，本回合就该被视为幅边。
            MarkForcedSelvedge(c);

            // 只放宽到「原版附魔合法性」这一层，不用 CanCardAcceptPatch：
            // 顶针油属于特殊发放，允许超过普通 3 枚缝合上限；
            // 已有 TailorPatchEnchantment 的牌继续走追加分支。
            bool canAcceptPatch = c.Enchantment is TailorPatchEnchantment
                || ModelDb.Enchantment<TailorPatchEnchantment>().CanEnchant(c);
            if (!canAcceptPatch) continue;

            AddPatchDirectly(c, new PatchworkSlotData
            {
                Kind = PatchworkKind.IndigoLining,
                Magnitude = blockBonus
            });
        }
    }

    /// <summary>
    /// 战斗开始时把指定牌移到手牌最左端（幅边）。
    ///
    /// 历史实现用非泛型 <c>System.Collections.IList</c> 直接 <c>RemoveAt</c> + <c>Insert</c>，
    /// 那条路**绕过了 <see cref="CardPile"/> 自己的内部方法**：
    /// - 不会触发 <c>InvokeCardRemoved</c> / <c>InvokeCardAddFinished</c>，
    ///   手牌 UI 不知道牌动过，视觉顺序不刷新；
    /// - 也不会触发 <c>InvokeContentsChanged</c>，依赖该事件的逻辑（相邻判定缓存等）全部失联。
    ///
    /// 现在改用 <c>RemoveInternal</c> + <c>AddInternal</c>（两者内部都会调 <c>InvokeContentsChanged</c>），
    /// 传 <c>silent: true</c> 避免播放「牌飞出去又飞回来」的动画，
    /// 最后手动补一次 <c>InvokeContentsChanged</c> 保证 UI 一定刷新。
    /// </summary>
    public static void MoveBoundCardToLeftSelvedgeOnCombatStart(Player owner, string cardId)
    {
        var combat = owner?.PlayerCombatState;
        var hand = combat?.Hand;
        if (combat == null || hand == null) return;
        if (hand.Cards == null || hand.Cards.Count <= 1) return;

        var card = hand.Cards.FirstOrDefault(c => c.Id.Entry == cardId);
        if (card == null) return;

        int idx = FindHandCardIndex(hand.Cards, card);
        if (idx <= 0) return;

        hand.RemoveInternal(card, silent: true);
        hand.AddInternal(card, 0, silent: true);
        hand.InvokeContentsChanged();
    }

    public static Task OrigamiFoldSymmetricHand(PlayerChoiceContext choiceContext, Player? owner)
        => StitchSelvedgePairDoubled(choiceContext, owner);

    public static int CountAllEnemyUnravelStacks(ICombatState? state)
        => (int)(state?.HittableEnemies.Sum(e => e.GetPower<UnravelPower>()?.Amount ?? 0m) ?? 0m);

    public static void GrantRetainToAllHandScraps(Player owner)
    {
        var hand = owner.PlayerCombatState?.Hand?.Cards;
        if (hand == null) return;
        foreach (var c in hand.OfType<IndigoTailorCard>().Where(c => c.IsScrapToken))
            c.AddKeyword(CardKeyword.Retain);
    }
}
