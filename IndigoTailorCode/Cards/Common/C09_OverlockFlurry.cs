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
/// C09 - 锁边刺 (Overlock Flurry)
/// 造成 4(6) 点伤害 2 次。
/// 缝合：攻击段数 +1。
/// </summary>
public sealed class OverlockFlurry() : IndigoTailorCard(
    baseCost: 1,
    type: CardType.Attack,
    rarity: CardRarity.Common,
    target: TargetType.AnyEnemy)
{
    public override bool IsScrapToken => false;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(4m, ValueProp.Move),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_STITCH");

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        ArgumentNullException.ThrowIfNull(play.Target);

        // 基础 2 段。补丁带来的额外段数**不在这里手动加**：
        // 每次 DamageCmd.Attack(...).Execute() 都会走 Hook.ModifyAttackHitCount，
        // 由 IndigoTailorCard.ModifyAttackHitCount（无附魔）或
        // TailorPatchEnchantment.ModifyAttackHitCount（有附魔）统一叠加。
        // 早期版本这里又写了一遍 `2 + GetTotalPatchExtraHits()`，导致 1 枚锁边贴算出 6 段而不是 3 段。
        const int BaseHits = 2;
        for (int i = 0; i < BaseHits; i++)
        {
            if (play.Target == null || !play.Target.IsAlive) break;
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, play)
                .Targeting(play.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }
        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}
