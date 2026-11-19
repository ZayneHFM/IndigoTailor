using System;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using IndigoTailor.Cards;
using IndigoTailor.Cards.Token;

namespace IndigoTailor.Mechanics;

/// <summary>
/// 【裁剪】(Cut)、胚件产出与手牌溢出保护。
/// </summary>
public static class RawScrapHelper
{
    public const int MaxHandSize = 10;

    public static LocString GetPrompt(string key, LocString fallback)
    {
        try
        {
            var loc = LocString.KeyPathToLocString($"cards/{key}");
            if (loc.Exists()) return loc;
        }
        catch
        {
            // fallback
        }
        return fallback;
    }

    public static async Task AddScrapToHandSafe(Player? owner, string scrapKey, bool isUpgraded, bool descContainsPlus = false)
    {
        var creature = owner?.Creature;
        var state = creature?.CombatState;
        if (owner == null || creature == null || state == null || owner.PlayerCombatState == null) return;

        CardModel canonical = scrapKey switch
        {
            "DYED_SCRAP_NEEDLE" => ModelDb.Card<DyedScrapNeedle>(),
            "DYED_SCRAP_CRINOLINE" => ModelDb.Card<DyedScrapCrinoline>(),
            "HALF_CUT_EDGE" => ModelDb.Card<HalfCutEdge>(),
            "HALF_CUT_LINING" => ModelDb.Card<HalfCutLining>(),
            "RIBSPREADER_LEFT" => ModelDb.Card<RibSpreaderLeft>(),
            "RIBSPREADER_RIGHT" => ModelDb.Card<RibSpreaderRight>(),
            "AUTOPSY_COAT_LEFT" => ModelDb.Card<AutopsyCoatLeft>(),
            "AUTOPSY_COAT_RIGHT" => ModelDb.Card<AutopsyCoatRight>(),
            "AMPUTATION_SAW_LEFT" => ModelDb.Card<AmputationSawLeft>(),
            "AMPUTATION_SAW_RIGHT" => ModelDb.Card<AmputationSawRight>(),
            "TWIN_FATE_SHEARS_LEFT" => ModelDb.Card<TwinFateShearsLeft>(),
            "TWIN_FATE_SHEARS_RIGHT" => ModelDb.Card<TwinFateShearsRight>(),
            "THORACIC_CAGE_LEFT" => ModelDb.Card<ThoracicCageLeft>(),
            "THORACIC_CAGE_RIGHT" => ModelDb.Card<ThoracicCageRight>(),
            _ => ModelDb.Card<RawScrapKintsugi>(),
        };

        var scrap = state.CreateCard(canonical, owner);
        if (isUpgraded || descContainsPlus)
            CardCmd.Upgrade(scrap);

        int handCount = owner.PlayerCombatState.Hand.Cards.Count;
        PileType targetPile = handCount >= MaxHandSize ? PileType.Discard : PileType.Hand;
        await CardPileCmd.AddGeneratedCardToCombat(scrap, targetPile, owner, CardPilePosition.Top);
    }

    public static async Task AddTangledThreadToHandSafe(Player? owner)
    {
        var creature = owner?.Creature;
        var state = creature?.CombatState;
        if (owner == null || creature == null || state == null || owner.PlayerCombatState == null) return;

        CardModel tangled = state.CreateCard<TangledThread>(owner);
        int handCount = owner.PlayerCombatState.Hand.Cards.Count;
        PileType targetPile = handCount >= MaxHandSize ? PileType.Discard : PileType.Hand;
        await CardPileCmd.AddGeneratedCardToCombat(tangled, targetPile, owner, CardPilePosition.Top);
    }

    public static async Task AddRandomUpgradedScrapToHand(Player owner)
    {
        await AddScrapToHandSafe(owner, "DYED_SCRAP_NEEDLE", true);
    }

    public static async Task AddNeedleBlankToHand(Player owner, bool isUpgraded)
    {
        await AddScrapToHandSafe(owner, "DYED_SCRAP_NEEDLE", isUpgraded);
    }

    public static async Task AddClothBlankToHand(Player owner, bool isUpgraded)
    {
        await AddScrapToHandSafe(owner, "DYED_SCRAP_CRINOLINE", isUpgraded);
    }

    public static async Task AddThreadBlankToHand(Player owner, bool isUpgraded)
    {
        await AddScrapToHandSafe(owner, "RAW_SCRAP_KINTSUGI", isUpgraded);
    }

    /// <summary>
    /// 往手牌塞 1 张原版《伤口》(Wound)。
    /// 手牌已满时退到弃牌堆，避免生成失败。
    /// </summary>
    public static async Task AddWoundToHandSafe(Player? owner)
    {
        var creature = owner?.Creature;
        var state = creature?.CombatState;
        if (owner == null || state == null || owner.PlayerCombatState == null) return;

        var wound = state.CreateCard<MegaCrit.Sts2.Core.Models.Cards.Wound>(owner);
        int handCount = owner.PlayerCombatState.Hand.Cards.Count;
        PileType targetPile = handCount >= MaxHandSize ? PileType.Discard : PileType.Hand;
        await CardPileCmd.AddGeneratedCardToCombat(wound, targetPile, owner, CardPilePosition.Top);
    }

    /// <summary>
    /// 往手牌塞 1 张《代价》(BloodPrice)。
    /// </summary>
    public static async Task AddBloodPriceToHandSafe(Player? owner)
    {
        var creature = owner?.Creature;
        var state = creature?.CombatState;
        if (owner == null || state == null || owner.PlayerCombatState == null) return;

        var price = state.CreateCard<BloodPrice>(owner);
        int handCount = owner.PlayerCombatState.Hand.Cards.Count;
        PileType targetPile = handCount >= MaxHandSize ? PileType.Discard : PileType.Hand;
        await CardPileCmd.AddGeneratedCardToCombat(price, targetPile, owner, CardPilePosition.Top);
    }

    /// <summary>
    /// 往手牌塞 1 张《结痂》(ScabToken)。
    /// </summary>
    public static async Task AddScabTokenToHandSafe(Player? owner)
    {
        var creature = owner?.Creature;
        var state = creature?.CombatState;
        if (owner == null || state == null || owner.PlayerCombatState == null) return;

        var scab = state.CreateCard<ScabToken>(owner);
        int handCount = owner.PlayerCombatState.Hand.Cards.Count;
        PileType targetPile = handCount >= MaxHandSize ? PileType.Discard : PileType.Hand;
        await CardPileCmd.AddGeneratedCardToCombat(scab, targetPile, owner, CardPilePosition.Top);
    }

    /// <summary>
    /// 浮窗用：这张牌**被裁剪**时产出什么。
    ///
    /// 规则与 <see cref="CutSpecificCardAndSpawnScraps"/> 的分发保持一致：
    /// - 伤口 → 《结痂》
    /// - 乱麻 → 线胚
    /// - 其它诅咒 / 状态牌 → 《代价》
    /// - 攻击牌 → 针胚、技能牌 → 布胚、其它 → 线胚
    /// </summary>
    public static string CutProductTipFor(string entry)
    {
        if (entry.Contains("WOUND"))
            return StitchLoc.Get("INDIGOTAILOR-MATERIAL_CUT_WOUND", "获得 1 张《结痂》");
        if (entry.Contains("SCAB_TOKEN"))
            return StitchLoc.Get("INDIGOTAILOR-MATERIAL_CUT_SCAB", "获得 1 张《结痂》");
        if (entry.Contains("BLOOD_PRICE"))
            return StitchLoc.Get("INDIGOTAILOR-MATERIAL_CUT_PRICE", "获得 1 张《代价》");
        if (entry.Contains("TANGLED_THREAD"))
            return StitchLoc.Get("INDIGOTAILOR-MATERIAL_CUT_TANGLED", "获得 1 张【线胚】");
        // 其它诅咒 / 状态牌
        return StitchLoc.Get("INDIGOTAILOR-MATERIAL_CUT_CURSE", "获得 1 张《代价》");
    }

    /// <summary>
    /// 浮窗用：这张牌**作为副牌被缝合**时，给主牌什么补丁效果。
    ///
    /// 规则与 <see cref="LoomSeamManager.ConvertCardToPatchwork"/> 保持一致。
    /// </summary>
    public static string StitchProductTipFor(string entry)
    {
        if (entry.Contains("WOUND"))
            return StitchLoc.Get("INDIGOTAILOR-MATERIAL_STITCH_WOUND", "打出主牌时回复 1 点生命");
        if (entry.Contains("SCAB_TOKEN"))
            return StitchLoc.Get("INDIGOTAILOR-MATERIAL_STITCH_SCAB", "打出主牌时回复 1 点生命");
        if (entry.Contains("BLOOD_PRICE"))
            return StitchLoc.Get("INDIGOTAILOR-MATERIAL_STITCH_PRICE", "打出主牌时将 1 张《伤口》加入手牌");
        if (entry.Contains("TANGLED_THREAD"))
            return StitchLoc.Get("INDIGOTAILOR-MATERIAL_STITCH_TANGLED", "打出主牌时抽 1 张牌");
        return StitchLoc.Get("INDIGOTAILOR-MATERIAL_STITCH_DEFAULT", "按副牌类型获得对应【补丁】");
    }

    /// <summary>
    /// 播放原版「删牌」的**划开**动画，替代默认的【消耗】燃烧动画。
    ///
    /// 为什么：裁剪剪断一张牌，用燃烧动画完全不合人设。
    /// 原版删牌有一套专门的横切 VFX（`NCardRemoveVfx`，场景 `vfx/ui/card/vfx_card_remove`），
    /// 带三段粒子：`_anticipationParticles`（预备）→ `_slashStartParticles`（起刀）→ `_slashEndParticles`（落刀），
    /// 视觉上就是「一道白光划过、牌被切开」。这正是裁剪该有的表现。
    ///
    /// 用法：在真正移除卡牌**之前**调用，让 VFX 从卡牌当前位置起播。
    /// 找不到卡牌节点（例如牌在牌库里）时静默跳过，不影响流程。
    /// </summary>
    public static void PlayCardSlashVfx(CardModel card)
    {
        try
        {
            var cardNode = MegaCrit.Sts2.Core.Nodes.Cards.NCard.FindOnTable(card);
            if (cardNode == null) return;

            var vfx = MegaCrit.Sts2.Core.Nodes.Vfx.Cards.NCardRemoveVfx.Create(cardNode);
            if (vfx == null) return;

            var container = MegaCrit.Sts2.Core.Nodes.NRun.Instance?.GlobalUi?.AboveTopBarVfxContainer;
            if (container == null)
            {
                // 没有全局容器时退回到卡牌自己的父节点，至少能播出来
                cardNode.GetParent()?.AddChildSafely(vfx);
                return;
            }
            container.AddChildSafely(vfx);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[PlayCardSlashVfx] 划开动画播放失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 精准裁剪指定目标卡牌：消耗目标卡牌，产出对应 0 费胚件/分件，并触发 OnCardCut 事件。
    /// </summary>
    public static async Task CutSpecificCardAndSpawnScraps(
        PlayerChoiceContext choiceContext,
        Player? owner,
        CardModel? cutterCard,
        CardModel targetCard,
        bool isUpgraded)
    {
        if (owner == null || targetCard == null) return;

        // 消耗被裁剪的目标牌。
        // 先播原版的划开 VFX，再走消耗流程 —— 消耗自带的燃烧粒子仍会放，
        // 但划开动画在视觉上占主导，且持续时间更长（pre 0.25s + 分层落刀）。
        PlayCardSlashVfx(targetCard);
        await CardCmd.Exhaust(choiceContext, targetCard);

        // 分发裁剪事件给遗物与能力。
        // 注意：CutsPerformedThisCombat 只由 R08 自己的 OnCardCut 递增，
        // 这里不能再手动 ++，否则每次裁剪会被记 2 次（U03 碎锦刃变 +6 而非 +3）。
        await LoomSeamManager.OnCardCut(choiceContext, owner, cutterCard, targetCard);

        // 特殊分支：6组母牌严格拆分为《·左》与《·右》
        if (targetCard.Id.Entry.Contains("SEVER_IN_TWAIN"))
        {
            bool splitUpgraded = targetCard.IsUpgraded || isUpgraded;
            await AddScrapToHandSafe(owner, "HALF_CUT_EDGE", splitUpgraded);
            await AddScrapToHandSafe(owner, "HALF_CUT_LINING", splitUpgraded);
            return;
        }

        if (targetCard.Id.Entry.Contains("REVERSIBLE_GREATCOAT") || targetCard.Id.Entry.Contains("AUTOPSY_COAT"))
        {
            bool splitUpgraded = targetCard.IsUpgraded || isUpgraded;
            await AddScrapToHandSafe(owner, "AUTOPSY_COAT_LEFT", splitUpgraded);
            await AddScrapToHandSafe(owner, "AUTOPSY_COAT_RIGHT", splitUpgraded);
            return;
        }

        if (targetCard.Id.Entry.Contains("RIB_SPREADER"))
        {
            bool splitUpgraded = targetCard.IsUpgraded || isUpgraded;
            await AddScrapToHandSafe(owner, "RIBSPREADER_LEFT", splitUpgraded);
            await AddScrapToHandSafe(owner, "RIBSPREADER_RIGHT", splitUpgraded);
            return;
        }

        if (targetCard.Id.Entry.Contains("SCRAP_HURRICANE") || targetCard.Id.Entry.Contains("TWIN_FATE_SHEARS"))
        {
            bool splitUpgraded = targetCard.IsUpgraded || isUpgraded;
            await AddScrapToHandSafe(owner, "TWIN_FATE_SHEARS_LEFT", splitUpgraded);
            await AddScrapToHandSafe(owner, "TWIN_FATE_SHEARS_RIGHT", splitUpgraded);
            return;
        }

        if (targetCard.Id.Entry.Contains("AMPUTATION_SAW"))
        {
            bool splitUpgraded = targetCard.IsUpgraded || isUpgraded;
            await AddScrapToHandSafe(owner, "AMPUTATION_SAW_LEFT", splitUpgraded);
            await AddScrapToHandSafe(owner, "AMPUTATION_SAW_RIGHT", splitUpgraded);
            return;
        }

        if (targetCard.Id.Entry.Contains("THORACIC_CAGE"))
        {
            bool splitUpgraded = targetCard.IsUpgraded || isUpgraded;
            await AddScrapToHandSafe(owner, "THORACIC_CAGE_LEFT", splitUpgraded);
            await AddScrapToHandSafe(owner, "THORACIC_CAGE_RIGHT", splitUpgraded);
            return;
        }

        // 乱麻被裁剪时分解为【线胚】
        if (targetCard.Id.Entry.Contains("TANGLED_THREAD"))
        {
            await AddScrapToHandSafe(owner, "RAW_SCRAP_KINTSUGI", isUpgraded || targetCard.IsUpgraded);
            return;
        }

        // 《伤口》被裁剪时产出【结痂】：0 费技能，回复 1 点生命、获得 1 点能量，消耗。
        // 这是唯一的「裁负面牌有正面回报」分支，回报刻意做小，且消耗掉避免循环回费。
        if (targetCard.Id.Entry.Contains("WOUND"))
        {
            await AddScabTokenToHandSafe(owner);
            return;
        }

        // 其它诅咒 / 状态牌被裁剪时，一律产出《代价》。
        //
        // 设计：裁掉一张负面牌不是白赚 —— 负担被换成一张更直接的代价。
        // 《代价》本身也能被裁剪，会再产出一张《代价》，形成自指但不无限（因为裁掉后仍只有 1 张）。
        if (targetCard.Type is CardType.Status or CardType.Curse)
        {
            await AddBloodPriceToHandSafe(owner);
            return;
        }

        // 通用裁剪：攻击牌变针胚，技能牌变布胚，其他牌变线胚
        string scrapKey = targetCard.Type switch
        {
            CardType.Attack => "DYED_SCRAP_NEEDLE",
            CardType.Skill => "DYED_SCRAP_CRINOLINE",
            _ => "RAW_SCRAP_KINTSUGI",
        };

        bool scrapUpgraded = isUpgraded || targetCard.IsUpgraded;
        await AddScrapToHandSafe(owner, scrapKey, scrapUpgraded);
    }

    public static Task CutHandCardAndSpawnScraps(PlayerChoiceContext choiceContext, Player? owner, IndigoTailorCard sourceCard, bool isUpgraded)
        => SelectAndCutHandCard(choiceContext, owner, sourceCard, selvedgeOnly: false, isUpgraded: isUpgraded);

    public static async Task SelectAndCutHandCard(
        PlayerChoiceContext choiceContext,
        Player? owner,
        IndigoTailorCard sourceCard,
        bool selvedgeOnly,
        bool isUpgraded)
    {
        var hand = owner?.PlayerCombatState?.Hand?.Cards;
        if (owner == null || hand == null || hand.Count <= 1) return;

        Func<CardModel, bool> filter = c =>
        {
            if (ReferenceEquals(c, sourceCard) || c == sourceCard) return false;
            if (selvedgeOnly && c is IndigoTailorCard tc)
            {
                return LoomSeamManager.IsCardAtSelvedge(tc);
            }
            return true;
        };

        var candidates = hand.Where(filter).ToList();
        if (candidates.Count == 0)
        {
            candidates = hand.Where(c => !ReferenceEquals(c, sourceCard) && c != sourceCard).ToList();
            if (candidates.Count == 0) return;
        }

        CardModel? chosen = null;
        if (candidates.Count == 1)
        {
            chosen = candidates[0];
        }
        else
        {
            var prompt = GetPrompt("INDIGOTAILOR-SELECT_TO_CUT", CardSelectorPrefs.ExhaustSelectionPrompt);
            chosen = await CommonActions.SelectSingleCard(sourceCard, prompt, choiceContext, PileType.Hand, filter);
        }

        if (chosen != null)
        {
            await CutSpecificCardAndSpawnScraps(choiceContext, owner, sourceCard, chosen, isUpgraded);
        }
    }

    public static async Task SpawnUpgradedScrapFromEnemyIntent(Player? owner, Creature enemy)
    {
        await AddScrapToHandSafe(owner, "DYED_SCRAP_NEEDLE", true);
    }

    public static async Task ShuffleTangledThreadsIntoDrawPile(Player owner, int count)
    {
        var creature = owner?.Creature;
        var state = creature?.CombatState;
        if (owner == null || creature == null || state == null) return;

        for (int i = 0; i < count; i++)
        {
            CardModel tangled = state.CreateCard<TangledThread>(owner);
            await CardPileCmd.AddGeneratedCardToCombat(tangled, PileType.Draw, owner, CardPilePosition.Random);
        }
    }
}
