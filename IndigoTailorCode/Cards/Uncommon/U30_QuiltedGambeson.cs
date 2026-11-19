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

namespace IndigoTailor.Cards.Uncommon;

/// <summary>
/// U30 - 蜡纱护 (Quilted Gambeson / Waxed Gauze Guard)
/// 获得 5(7) 点格挡。幅边：改为获得等量层数【金属化】。
/// </summary>
public sealed class QuiltedGambeson() : IndigoTailorCard(
    baseCost: 1,
    type: CardType.Skill,
    rarity: CardRarity.Uncommon,
    target: TargetType.Self)
{
    public override bool IsScrapToken => false;
    public override bool HasSelvedgeEffect => true;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(5m, ValueProp.Move),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        GetDetectedKeywordTips("INDIGOTAILOR_SELVEDGE");

    /// <summary>
    /// 幅边分支只把「本体格挡」换成【金属化】，**补丁提供的格挡必须照常结算**。
    ///
    /// 历史 Bug：原实现两个分支互斥，幅边时直接 `PowerCmd.Apply&lt;MetallicizePower&gt;` 然后
    /// `TriggerAttachedPatchesOnPlay` 里因为 `Card.GainsBlock` 为真、以为原生 Hook 会处理补丁格挡，
    /// 结果幅边分支根本没走 `CreatureCmd.GainBlock`，Hook 也就没触发 → 布胚补丁的格挡被吞了。
    ///
    /// 修法：先按幅边与否结算本体效果，再**手动**把补丁格挡补上（还在幅边分支时才补，避免重复）。
    /// </summary>
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        bool atSelvedge = LoomSeamManager.IsCardAtSelvedge(this);

        if (atSelvedge)
        {
            await PowerCmd.Apply<MetallicizePower>(choiceContext, Owner.Creature, DynamicVars.Block.BaseValue, Owner.Creature, this, false);

            // 幅边分支换掉了 GainBlock，补丁格挡不会被 Hook 触发，这里手动补。
            int patchBlock = GetTotalPatchBlockBonus();
            if (patchBlock > 0)
            {
                await CreatureCmd.GainBlock(Owner.Creature, patchBlock, ValueProp.Unpowered, null);
            }
        }
        else
        {
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        }

        await TriggerAttachedPatchesOnPlay(choiceContext, play.Target);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}
