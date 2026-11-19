using MegaCrit.Sts2.Core.ValueProps;
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using BaseLib.Abstracts;
using BaseLib.Utils;
using IndigoTailor.Character;
using IndigoTailor.Mechanics;
using IndigoTailor.Powers;
using IndigoTailor.Patches;

namespace IndigoTailor.Cards;

public enum PatchworkKind
{
    CrimsonNeedle,   // 针胚 / 攻击牌: +4(+6) 伤害
    IndigoLining,    // 布胚 / 技能牌: +4(+6) 格挡
    KintsugiGold,    // 线胚 / 衍生牌: 抽 1 张牌 (升级抽 2)
    WoundHeal,       // 《伤口》: 回复 1 点生命
    MultiHitLock,    // 锁边刺 (C09): 攻击段数 +1
    HiddenWeak,      // 暗扣 (C10): 给予 1(2) 层虚弱
    BarbedUnravel,   // 倒刺针 (U21): 给予 2(3) 层脱线
    KeyholeEnergy,   // 扣眼 (U22): 获得 1 点能量
    HorsehairRetain, // 鬃衬 (U23): 获得保留
    Cost             // 《代价》: 失去 3 点生命，将 1 张《伤口》加入手牌
}

[Serializable]
public sealed class PatchworkSlotData
{
    public PatchworkKind Kind { get; set; }
    public int Magnitude { get; set; }
    public bool IsUpgraded { get; set; }
    public bool IsPermanent { get; set; }
    public string SourceCardId { get; set; } = string.Empty;
}

[Pool(typeof(IndigoTailorCardPool))]
public abstract class IndigoTailorCard(
    int baseCost,
    CardType type,
    CardRarity rarity,
    TargetType target) : CustomCardModel(baseCost, type, rarity, target)
{
    public const int DefaultMaxPatches = 3;

    /// <summary>
    /// 本张牌上挂着的补丁列表。
    ///
    /// ⚠ 与 <see cref="Mechanics.TailorPatchEnchantment.Patches"/> 完全同类的陷阱：
    /// <c>AbstractModel.MutableClone()</c> 走的是 <c>MemberwiseClone</c>（浅拷贝），
    /// 字段初始化器 <c>= new()</c> 在浅拷贝时**不会重新执行**，
    /// 所以克隆出来的卡牌，其 AttachedPatches 会指向原卡牌（甚至 canonical 原型）的同一个 List。
    /// 结果就是：一张牌加补丁，牌库里所有同名复制品、乃至其它路径克隆出的牌全部"看到"这个补丁。
    ///
    /// 必须给可赋值（private set），并在 <see cref="DeepCloneFields"/> 里换新 List。
    /// </summary>
    public List<PatchworkSlotData> AttachedPatches { get; private set; } = new();

    /// <summary>
    /// 克隆时把补丁列表换成全新实例。
    /// 注意不能 <c>Clear()</c> —— 那会清掉共享的那个 List，连原卡一起清空。
    /// </summary>
    protected override void DeepCloneFields()
    {
        base.DeepCloneFields();
        AttachedPatches = new List<PatchworkSlotData>();
    }

    [SavedProperty]
    public string PermanentPatchSerialized { get; set; } = string.Empty;

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        // 统一入口：恢复永久补丁并接进正常附魔（伤害/格挡/UI 生效），重复入场不叠加。
        if (card == this)
            PermanentPatchworkSavePatch.RestorePermanentPatchesFromString(this, applyInCombat: true);
        return base.AfterCardEnteredCombat(card);
    }

    public virtual bool IsScrapToken => false;
    public override bool ShouldReceiveCombatHooks => true;

    public bool IsInLoomSeam() => LoomSeamManager.IsCardInLoomSeam(this);
    public bool IsAtSelvedge() => LoomSeamManager.IsCardAtSelvedge(this);

    public bool WasAtSelvedgeWhenPlayed { get; set; }

    /// <summary>
    /// 带【针胚】补丁的牌，即使本体是技能牌，也需要能选中敌人。
    ///
    /// 问题：技能牌 <see cref="TargetType"/> 是 <c>Self</c>，
    /// 打出去时 <c>CardPlay.Target</c> 是 null，补丁的针胚伤害无处可打 ——
    /// 表现就是「防御缝了针胚，打不出伤害」。
    ///
    /// 修法：只要身上有针胚（CrimsonNeedle）或任何需要目标的效果，就把目标类型改成 <c>AnyEnemy</c>。
    /// 这是原版就支持的机制（<c>TargetType</c> 是 virtual 属性），不需要改卡牌构造参数。
    /// </summary>
    public override TargetType TargetType
    {
        get
        {
            if (AttachedPatches != null && AttachedPatches.Any(p => p.Kind == PatchworkKind.CrimsonNeedle))
                return TargetType.AnyEnemy;
            return base.TargetType;
        }
    }

    /// <summary>
    /// 打出瞬间在手牌中的位置索引（由 <see cref="Patches.SelvedgePlayPatches"/> 在出牌前快照）。
    /// -1 表示未记录或未在手牌中。用于需要「自己前后是谁」的卡牌逻辑，
    /// 因为 OnPlay 执行时该牌已被移出手牌，再查索引必然失败。
    /// </summary>
    public int HandIndexWhenPlayed { get; set; } = -1;

    /// <summary>
    /// 打出瞬间的手牌总张数（与 <see cref="HandIndexWhenPlayed"/> 同一个快照）。
    /// </summary>
    public int HandCountWhenPlayed { get; set; } = -1;

    public virtual bool HasSelvedgeEffect => false;

    protected override bool ShouldGlowGoldInternal
    {
        get
        {
            if (HasSelvedgeEffect && IsAtSelvedge())
                return true;
            return base.ShouldGlowGoldInternal;
        }
    }

    private const string FrameMaterialPath = "res://IndigoTailor/materials/card_frame_tailor_mat.tres";

    public override Material? CreateCustomFrameMaterial
    {
        get
        {
            if (!ResourceLoader.Exists(FrameMaterialPath)) return null;
            return GD.Load<Material>(FrameMaterialPath);
        }
    }

    public override string CustomPortraitPath
    {
        get
        {
            string id = Id.Entry.ToLowerInvariant().Replace("indigotailor-", "");
            string bigPath = $"res://IndigoTailor/images/card_portraits/big/{id}.png";
            if (ResourceLoader.Exists(bigPath)) return bigPath;
            string path = $"res://IndigoTailor/images/card_portraits/{id}.png";
            return ResourceLoader.Exists(path) ? path : "res://IndigoTailor/images/card_portraits/strike_indigo.png";
        }
    }

    public override string PortraitPath
    {
        get
        {
            string id = Id.Entry.ToLowerInvariant().Replace("indigotailor-", "");
            string path = $"res://IndigoTailor/images/card_portraits/{id}.png";
            return ResourceLoader.Exists(path) ? path : "res://IndigoTailor/images/card_portraits/strike_indigo.png";
        }
    }

    public int GetTotalPatchDamageBonus()
    {
        int bonus = 0;
        int auraBonus = Owner?.Creature?.GetPower<HundredPatchMasterworkPower>()?.Amount ?? 0;
        int myriadMultiplier = 1 + (Owner?.Creature?.GetPower<MyriadThreadsPower>()?.Amount ?? 0);
        int multiplier = (1 + (Owner?.Creature?.GetPower<SelvedgeAmbushPower>()?.Amount ?? 0)) * myriadMultiplier;
        foreach (var p in AttachedPatches)
        {
            if (p.Kind == PatchworkKind.CrimsonNeedle)
                bonus += (p.Magnitude + auraBonus) * multiplier;
            else if (auraBonus > 0)
                bonus += auraBonus * multiplier;
        }
        return bonus;
    }

    public int GetTotalPatchBlockBonus()
    {
        int bonus = 0;
        int auraBonus = Owner?.Creature?.GetPower<HundredPatchMasterworkPower>()?.Amount ?? 0;
        int myriadMultiplier = 1 + (Owner?.Creature?.GetPower<MyriadThreadsPower>()?.Amount ?? 0);
        int multiplier = (1 + (Owner?.Creature?.GetPower<SelvedgeAmbushPower>()?.Amount ?? 0)) * myriadMultiplier;
        foreach (var p in AttachedPatches)
        {
            if (p.Kind is PatchworkKind.IndigoLining or PatchworkKind.HorsehairRetain)
                bonus += (p.Magnitude + auraBonus) * multiplier;
            else if (auraBonus > 0)
                bonus += auraBonus * multiplier;
        }
        return bonus;
    }

    public int GetTotalPatchExtraHits()
    {
        int extra = AttachedPatches.Count(p => p.Kind == PatchworkKind.MultiHitLock);
        int myriadMultiplier = 1 + (Owner?.Creature?.GetPower<MyriadThreadsPower>()?.Amount ?? 0);
        return extra * myriadMultiplier;
    }

    public override int ModifyAttackHitCount(MegaCrit.Sts2.Core.Commands.Builders.AttackCommand attack, int hitCount)
    {
        if (attack.ModelSource == this && Enchantment == null)
        {
            return hitCount + GetTotalPatchExtraHits();
        }
        return hitCount;
    }

    public async Task TriggerAttachedPatchesOnPlay(MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceContext choiceContext, Creature? target)
    {
        if (IsScrapToken && Owner != null)
        {
            await LoomSeamManager.OnScrapPlayed(choiceContext, Owner, this);
        }

        // When Enchantment (TailorPatchEnchantment) is attached, STS2's CardModel.OnPlayWrapper
        // natively invokes Enchantment.OnPlay() right after Card.OnPlay(), and Hook.ModifyDamage /
        // Hook.ModifyBlock natively handles damage/block additives. We only run manual fallback if Enchantment is null.
        if (Enchantment == null && AttachedPatches.Count > 0 && Owner?.Creature != null)
        {
            int myriadMultiplier = 1 + (Owner.Creature.GetPower<MyriadThreadsPower>()?.Amount ?? 0);
            int repeat = (1 + (Owner.Creature.GetPower<SelvedgeAmbushPower>()?.Amount ?? 0)) * myriadMultiplier;
            for (int r = 0; r < repeat; r++)
            {
                int patchBlock = GetTotalPatchBlockBonus() / repeat;
                if (patchBlock > 0)
                    await CreatureCmd.GainBlock(Owner.Creature, patchBlock, ValueProp.Unpowered, null);

                foreach (var p in AttachedPatches.ToList())
                {
                    switch (p.Kind)
                    {
                        case PatchworkKind.KintsugiGold:
                            await CardPileCmd.Draw(choiceContext, p.IsUpgraded ? 2m : 1m, Owner, false);
                            break;
                        case PatchworkKind.WoundHeal:
                            await CreatureCmd.Heal(Owner.Creature, 1m, true);
                            break;
                        case PatchworkKind.HiddenWeak:
                            if (target != null && target.IsAlive)
                                await PowerCmd.Apply<MegaCrit.Sts2.Core.Models.Powers.WeakPower>(choiceContext, target, p.Magnitude, Owner.Creature, this, false);
                            break;
                        case PatchworkKind.BarbedUnravel:
                            if (target != null && target.IsAlive)
                                await PowerCmd.Apply<UnravelPower>(choiceContext, target, p.Magnitude, Owner.Creature, this, false);
                            break;
                        case PatchworkKind.KeyholeEnergy:
                            await PlayerCmd.GainEnergy(1m, Owner);
                            break;
                        case PatchworkKind.Cost:
                            // 缝入后只塞《伤口》，不扣血。
                            await RawScrapHelper.AddWoundToHandSafe(Owner);
                            break;
                    }
                }
            }
        }
    }

    protected static IHoverTip MakeKeywordTip(string key)
    {
        LocString title = LocString.KeyPathToLocString($"card_keywords/{key}.title");
        LocString desc = LocString.KeyPathToLocString($"card_keywords/{key}.description");
        return new HoverTip(title, desc);
    }

    protected IEnumerable<IHoverTip> GetDetectedKeywordTips(params string[] keys)
    {
        foreach (string k in keys)
            yield return MakeKeywordTip(k);
    }
}
