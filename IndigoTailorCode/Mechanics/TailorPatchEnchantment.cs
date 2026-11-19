using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using IndigoTailor.Cards;
using IndigoTailor.Powers;

namespace IndigoTailor.Mechanics;

/// <summary>
/// 靛染裁缝原生附魔模型：缝合补丁 (TailorPatchEnchantment)。
/// 复用原版 Slay the Spire 2 的 EnchantmentModel 架构，
/// 支持堆叠（IsStackable）、多槽位补丁维护、卡面动态文本注入与独立 HoverTip。
/// 严格对标 v2.7 设计规范：统称【补丁】，卡面采用【补丁】(功能说明) 格式。
/// </summary>
public class TailorPatchEnchantment : CustomEnchantmentModel
{
    public override bool IsStackable => true;
    public override bool HasExtraCardText => false;
    public override bool ShowAmount => true;
    public override int DisplayAmount => Patches.Count;

    protected override string? CustomIconPath => "res://IndigoTailor/images/powers/patchwork_stitch.png";

    /// <summary>
    /// 本张牌上挂着的补丁列表。
    ///
    /// ⚠ 必须是可重新赋值的（private set），不能写成只读 `{ get; } = new()`：
    /// <see cref="EnchantmentModel.ToMutable"/> 内部走的是 <c>MemberwiseClone</c>（浅拷贝），
    /// 引用类型字段会被**两个实例共享**。若不给 <see cref="DeepCloneFields"/> 换新 List，
    /// 那么每次 <c>ModelDb.Enchantment&lt;TailorPatchEnchantment&gt;().ToMutable()</c>
    /// 拿到的「新」附魔，其 Patches 仍指向全局 canonical 单例的同一个 List ——
    /// 结果是所有牌的补丁堆在一起、补丁数只增不减、任意牌打出都结算全场补丁
    /// （实机表现：打《打击》莫名获得 4 点格挡、补丁数从 4 涨到 5 永不归零）。
    /// </summary>
    public List<PatchworkSlotData> Patches { get; private set; } = new();

    [SavedProperty]
    public string SerializedPatches { get; set; } = string.Empty;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new StringVar("Summary", "")
    ];

    /// <summary>
    /// 克隆时把 <see cref="Patches"/> 换成一个全新的空列表。
    ///
    /// 注意**不能**用 <c>Patches.Clear()</c> —— 那样清掉的还是共享的那个 List，
    /// 会把 canonical 单例和所有其它卡牌的补丁一起清空。必须重新 new 一个实例。
    /// </summary>
    protected override void DeepCloneFields()
    {
        base.DeepCloneFields();

        Patches = new List<PatchworkSlotData>();
        SerializedPatches = string.Empty;
    }

    public void AddPatch(PatchworkSlotData patch)
    {
        Patches.Add(patch);
        SyncSerializedPatches();
        UpdateSummary();
    }

    public void RemovePatch(PatchworkSlotData patch)
    {
        Patches.Remove(patch);
        SyncSerializedPatches();
        UpdateSummary();
    }

    public void ClearPatches()
    {
        Patches.Clear();
        SyncSerializedPatches();
        UpdateSummary();
    }

    private void SyncSerializedPatches()
    {
        SerializedPatches = string.Join(";", Patches.Select(p => $"{(int)p.Kind}:{p.Magnitude}:{p.IsUpgraded}:{p.IsPermanent}:{p.SourceCardId}"));
    }

    private void RestoreFromSerialized()
    {
        if (string.IsNullOrEmpty(SerializedPatches)) return;
        Patches.Clear();
        var tokens = SerializedPatches.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var t in tokens)
        {
            var parts = t.Split(':');
            if (parts.Length >= 4 &&
                int.TryParse(parts[0], out var k) &&
                int.TryParse(parts[1], out var mag) &&
                bool.TryParse(parts[2], out var up) &&
                bool.TryParse(parts[3], out var perm))
            {
                Patches.Add(new PatchworkSlotData
                {
                    Kind = (PatchworkKind)k,
                    Magnitude = mag,
                    IsUpgraded = up,
                    IsPermanent = perm,
                    SourceCardId = parts.Length >= 5 ? parts[4] : string.Empty
                });
            }
        }
        UpdateSummary();
    }

    public void UpdateSummary()
    {
        if (DynamicVars.TryGetValue("Summary", out var dynVar) && dynVar is StringVar sVar)
        {
            sVar.StringValue = BuildSummaryString();
        }
    }

    public override void RecalculateValues()
    {
        base.RecalculateValues();
        if (Patches.Count == 0 && !string.IsNullOrEmpty(SerializedPatches))
        {
            RestoreFromSerialized();
        }
        else
        {
            UpdateSummary();
        }
    }

    public string BuildSummaryString()
    {
        if (Patches.Count == 0) return string.Empty;

        var descriptors = new List<string>();
        foreach (var p in Patches)
        {
            string desc = p.Kind switch
            {
                PatchworkKind.CrimsonNeedle => StitchLoc.Get("INDIGOTAILOR-PATCH_SUMMARY_DAMAGE", "【补丁】(伤害+{0})", p.Magnitude),
                PatchworkKind.IndigoLining => StitchLoc.Get("INDIGOTAILOR-PATCH_SUMMARY_BLOCK", "【补丁】(格挡+{0})", p.Magnitude),
                PatchworkKind.KintsugiGold => p.IsUpgraded
                    ? StitchLoc.Get("INDIGOTAILOR-PATCH_SUMMARY_DRAW2", "【补丁】(抽2张牌)")
                    : StitchLoc.Get("INDIGOTAILOR-PATCH_SUMMARY_DRAW1", "【补丁】(抽1张牌)"),
                PatchworkKind.MultiHitLock => StitchLoc.Get("INDIGOTAILOR-PATCH_SUMMARY_MULTIHIT", "【补丁】(段数+1)"),
                PatchworkKind.HiddenWeak => StitchLoc.Get("INDIGOTAILOR-PATCH_SUMMARY_WEAK", "【补丁】(虚弱+{0})", p.Magnitude),
                PatchworkKind.BarbedUnravel => StitchLoc.Get("INDIGOTAILOR-PATCH_SUMMARY_UNRAVEL", "【补丁】(脱线+{0})", p.Magnitude),
                PatchworkKind.KeyholeEnergy => StitchLoc.Get("INDIGOTAILOR-PATCH_SUMMARY_ENERGY", "【补丁】(能量+1)"),
                PatchworkKind.HorsehairRetain => StitchLoc.Get("INDIGOTAILOR-PATCH_SUMMARY_RETAIN", "【补丁】(保留与格挡+{0})", p.Magnitude),
                PatchworkKind.WoundHeal => StitchLoc.Get("INDIGOTAILOR-PATCH_SUMMARY_HEAL", "【补丁】(回复1生命)"),
                PatchworkKind.Cost => StitchLoc.Get("INDIGOTAILOR-PATCH_SUMMARY_COST", "【补丁】(每回合+1伤口)"),
                _ => StitchLoc.Get("INDIGOTAILOR-PATCH_SUMMARY_FALLBACK", "【补丁】(+{0})", p.Magnitude)
            };
            descriptors.Add(desc);
        }

        string header = StitchLoc.Get("INDIGOTAILOR-PATCH_SUMMARY_HEADER", "已缝合：");
        return header + string.Join("，", descriptors);
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            var kinds = Patches.Select(p => p.Kind).Distinct();
            foreach (var kind in kinds)
            {
                var matching = Patches.Where(p => p.Kind == kind).ToList();
                int totalMag = matching.Sum(p => p.Magnitude);
                string title = ResolveSourceCardTitle(matching.FirstOrDefault()?.SourceCardId);

                switch (kind)
                {
                    case PatchworkKind.CrimsonNeedle:
                    {
                        var icon = ResourceLoader.Load<Texture2D>("res://IndigoTailor/images/powers/crimson_needle_patch_64.png");
                        string body = StitchLoc.Get("INDIGOTAILOR-PATCH_TOOLTIP_DAMAGE", "【补丁】(伤害)：使此牌造成的伤害增加 {0} 点。", totalMag);
                        yield return new HoverTip(
                            new LocString("card_keywords", "INDIGOTAILOR-STITCH.title"),
                            string.IsNullOrEmpty(title) ? body : $"{title}{StitchLoc.Get("INDIGOTAILOR-PATCH_TOOLTIP_ARROW", " → ")}{body}",
                            icon
                        );
                        break;
                    }
                    case PatchworkKind.IndigoLining:
                    {
                        var icon = ResourceLoader.Load<Texture2D>("res://IndigoTailor/images/powers/indigo_lining_patch_64.png");
                        string body = StitchLoc.Get("INDIGOTAILOR-PATCH_TOOLTIP_BLOCK", "【补丁】(格挡)：使此牌获得的格挡增加 {0} 点。", totalMag);
                        yield return new HoverTip(
                            new LocString("card_keywords", "INDIGOTAILOR-STITCH.title"),
                            string.IsNullOrEmpty(title) ? body : $"{title}{StitchLoc.Get("INDIGOTAILOR-PATCH_TOOLTIP_ARROW", " → ")}{body}",
                            icon
                        );
                        break;
                    }
                    case PatchworkKind.KintsugiGold:
                    {
                        var icon = ResourceLoader.Load<Texture2D>("res://IndigoTailor/images/powers/03_kintsugi_patch_金缮贴_64.png");
                        string body = StitchLoc.Get("INDIGOTAILOR-PATCH_TOOLTIP_DRAW", "【补丁】(抽牌)：打出此牌时，抽牌。");
                        yield return new HoverTip(
                            new LocString("card_keywords", "INDIGOTAILOR-STITCH.title"),
                            string.IsNullOrEmpty(title) ? body : $"{title}{StitchLoc.Get("INDIGOTAILOR-PATCH_TOOLTIP_ARROW", " → ")}{body}",
                            icon
                        );
                        break;
                    }
                    case PatchworkKind.BarbedUnravel:
                    {
                        var icon = ResourceLoader.Load<Texture2D>("res://IndigoTailor/images/powers/unravel.png");
                        string body = StitchLoc.Get("INDIGOTAILOR-PATCH_TOOLTIP_UNRAVEL", "【补丁】(脱线)：打出此牌时，对目标施加 {0} 层脱线。", totalMag);
                        yield return new HoverTip(
                            new LocString("card_keywords", "INDIGOTAILOR-STITCH.title"),
                            string.IsNullOrEmpty(title) ? body : $"{title}{StitchLoc.Get("INDIGOTAILOR-PATCH_TOOLTIP_ARROW", " → ")}{body}",
                            icon
                        );
                        break;
                    }
                    case PatchworkKind.HiddenWeak:
                    {
                        var icon = ResourceLoader.Load<Texture2D>("res://IndigoTailor/images/powers/big/bespoke_tailoring.png");
                        string body = StitchLoc.Get("INDIGOTAILOR-PATCH_TOOLTIP_WEAK", "【补丁】(虚弱)：打出此牌时，对目标施加 {0} 层虚弱。", totalMag);
                        yield return new HoverTip(
                            new LocString("card_keywords", "INDIGOTAILOR-STITCH.title"),
                            string.IsNullOrEmpty(title) ? body : $"{title}{StitchLoc.Get("INDIGOTAILOR-PATCH_TOOLTIP_ARROW", " → ")}{body}",
                            icon
                        );
                        break;
                    }
                    case PatchworkKind.KeyholeEnergy:
                    {
                        var icon = ResourceLoader.Load<Texture2D>("res://IndigoTailor/images/powers/big/running_stitch.png");
                        string body = StitchLoc.Get("INDIGOTAILOR-PATCH_TOOLTIP_ENERGY", "【补丁】(能量)：打出此牌时，获得 1 点能量。");
                        yield return new HoverTip(
                            new LocString("card_keywords", "INDIGOTAILOR-STITCH.title"),
                            string.IsNullOrEmpty(title) ? body : $"{title}{StitchLoc.Get("INDIGOTAILOR-PATCH_TOOLTIP_ARROW", " → ")}{body}",
                            icon
                        );
                        break;
                    }
                    case PatchworkKind.Cost:
                    {
                        var icon = ResourceLoader.Load<Texture2D>("res://IndigoTailor/images/powers/big/raw_scrap_kintsugi.png");
                        string body = StitchLoc.Get("INDIGOTAILOR-PATCH_TOOLTIP_COST", "【补丁】(代价)：打出此牌时，将 1 张《伤口》加入手牌。");
                        yield return new HoverTip(
                            new LocString("card_keywords", "INDIGOTAILOR-STITCH.title"),
                            string.IsNullOrEmpty(title) ? body : $"{title}{StitchLoc.Get("INDIGOTAILOR-PATCH_TOOLTIP_ARROW", " → ")}{body}",
                            icon
                        );
                        break;
                    }
                }
            }
        }
    }

    private static string ResolveSourceCardTitle(string? sourceCardId)
    {
        if (string.IsNullOrEmpty(sourceCardId)) return string.Empty;
        try
        {
            var model = ModelDb.AllCards.FirstOrDefault(
                c => c.Id.Entry.Equals(sourceCardId, StringComparison.OrdinalIgnoreCase));
            return model?.Title ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    public override decimal EnchantDamageAdditive(decimal originalDamage, ValueProp props)
    {
        decimal total = 0m;
        int auraBonus = Card?.Owner?.Creature?.GetPower<HundredPatchMasterworkPower>()?.Amount ?? 0;
        int myriadMult = 1 + (Card?.Owner?.Creature?.GetPower<MyriadThreadsPower>()?.Amount ?? 0);
        int repeat = (1 + (Card?.Owner?.Creature?.GetPower<SelvedgeAmbushPower>()?.Amount ?? 0)) * myriadMult;

        foreach (var p in Patches)
        {
            if (p.Kind == PatchworkKind.CrimsonNeedle)
            {
                total += (p.Magnitude + auraBonus) * repeat;
            }
            else if (auraBonus > 0)
            {
                total += auraBonus * repeat;
            }
        }
        return total;
    }

    public override decimal EnchantBlockAdditive(decimal originalBlock)
    {
        decimal total = 0m;
        int auraBonus = Card?.Owner?.Creature?.GetPower<HundredPatchMasterworkPower>()?.Amount ?? 0;
        int myriadMult = 1 + (Card?.Owner?.Creature?.GetPower<MyriadThreadsPower>()?.Amount ?? 0);
        int repeat = (1 + (Card?.Owner?.Creature?.GetPower<SelvedgeAmbushPower>()?.Amount ?? 0)) * myriadMult;

        foreach (var p in Patches)
        {
            if (p.Kind is PatchworkKind.IndigoLining or PatchworkKind.HorsehairRetain)
            {
                total += (p.Magnitude + auraBonus) * repeat;
            }
            else if (auraBonus > 0)
            {
                total += auraBonus * repeat;
            }
        }
        return total;
    }

    public override int ModifyAttackHitCount(MegaCrit.Sts2.Core.Commands.Builders.AttackCommand attack, int hitCount)
    {
        if (attack.ModelSource == Card)
        {
            int extra = Patches.Count(p => p.Kind == PatchworkKind.MultiHitLock);
            int myriadMult = 1 + (Card?.Owner?.Creature?.GetPower<MyriadThreadsPower>()?.Amount ?? 0);
            return hitCount + (extra * myriadMult);
        }
        return hitCount;
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        if (Card?.Owner?.Creature == null || Patches.Count == 0) return;

        var owner = Card.Owner;
        int myriadMult = 1 + (owner.Creature.GetPower<MyriadThreadsPower>()?.Amount ?? 0);
        int repeat = (1 + (owner.Creature.GetPower<SelvedgeAmbushPower>()?.Amount ?? 0)) * myriadMult;

        // 若主牌本身不具备格挡属性（例如纯攻击牌），Hook.ModifyBlock 不会被触发，在此为玩家结算补丁格挡
        if (!Card.GainsBlock)
        {
            decimal totalBlock = EnchantBlockAdditive(0m);
            if (totalBlock > 0)
            {
                await CreatureCmd.GainBlock(owner.Creature, totalBlock, ValueProp.Unpowered, null);
            }
        }

        // 若主牌本身不是攻击牌（例如纯技能牌），Hook.ModifyDamage 不会被触发，在此对目标结算针胚伤害。
        // 注意：DamageCmd.Attack(...) 必须接 .FromCard(this, cardPlay) 来设置 Attacker，
        // 否则 AttackCommand.Execute 会抛 InvalidOperationException("No attacker set.")（sts2 反编译攻击命令构造器）。
        if (Card.Type != CardType.Attack && cardPlay?.Target != null && cardPlay.Target.IsAlive)
        {
            decimal totalDamage = EnchantDamageAdditive(0m, ValueProp.Unpowered);
            if (totalDamage > 0)
            {
                await DamageCmd.Attack(totalDamage)
                    .FromCard(Card, cardPlay)
                    .Targeting(cardPlay.Target)
                    .WithHitFx("vfx/vfx_attack_slash")
                    .Execute(choiceContext);
            }
        }

        for (int r = 0; r < repeat; r++)
        {
            foreach (var p in Patches)
            {
                switch (p.Kind)
                {
                    case PatchworkKind.KintsugiGold:
                        await CardPileCmd.Draw(choiceContext, p.IsUpgraded ? 2m : 1m, owner, false);
                        break;
                    case PatchworkKind.WoundHeal:
                        await CreatureCmd.Heal(owner.Creature, 1m, true);
                        break;
                    case PatchworkKind.HiddenWeak:
                        if (cardPlay?.Target != null && cardPlay.Target.IsAlive)
                            await PowerCmd.Apply<WeakPower>(choiceContext, cardPlay.Target, p.Magnitude, owner.Creature, Card, false);
                        break;
                    case PatchworkKind.BarbedUnravel:
                        if (cardPlay?.Target != null && cardPlay.Target.IsAlive)
                            await PowerCmd.Apply<UnravelPower>(choiceContext, cardPlay.Target, p.Magnitude, owner.Creature, Card, false);
                        break;
                    case PatchworkKind.KeyholeEnergy:
                        await PlayerCmd.GainEnergy(1m, owner);
                        break;
                    case PatchworkKind.Cost:
                        // 缝入后只塞《伤口》，**不扣血** —— 扣血是《代价》本体被打出时的效果。
                        await RawScrapHelper.AddWoundToHandSafe(owner);
                        break;
                }
            }
        }
    }
}
