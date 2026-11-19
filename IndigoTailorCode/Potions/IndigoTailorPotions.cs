using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using IndigoTailor.Character;
using IndigoTailor.Mechanics;
using IndigoTailor.Powers;

namespace IndigoTailor.Potions;

[Pool(typeof(IndigoTailorPotionPool))]
public abstract class IndigoTailorPotion : CustomPotionModel
{
    protected abstract string ImageStem { get; }
    public override string? CustomPackedImagePath => $"res://IndigoTailor/images/potions/{ImageStem}.png";
    public override string? CustomPackedOutlinePath => $"res://IndigoTailor/images/potions/outline/{ImageStem}.png";
}

/// <summary>
/// P01 - 靛蓝染液 (Indigo Dye Vat): Add 1 upgraded Needle, Crinoline, and Kintsugi scrap to hand.
/// </summary>
public sealed class PotionIndigoDye : IndigoTailorPotion
{
    public override PotionRarity Rarity => PotionRarity.Common;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;
    protected override string ImageStem => "potion_indigo_dye";

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (Owner == null) return;
        await RawScrapHelper.AddScrapToHandSafe(Owner, "DYED_SCRAP_NEEDLE", true);
        await RawScrapHelper.AddScrapToHandSafe(Owner, "DYED_SCRAP_CRINOLINE", true);
        await RawScrapHelper.AddScrapToHandSafe(Owner, "RAW_SCRAP_KINTSUGI", true);
    }
}

/// <summary>
/// P02 - 顶针圣油 (Consecrated Thimble Oil): All hand cards enter Loom Seam this turn and gain 1 Indigo Lining Patch (+4 Block).
/// </summary>
public sealed class PotionThimbleOil : IndigoTailorPotion
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.Self;
    protected override string ImageStem => "potion_thimble_oil";

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (Owner == null) return;
        LoomSeamManager.ForceAllHandSeamAndGrantLiningPatch(Owner, 4);
        await Task.CompletedTask;
    }
}

/// <summary>
/// P03 - 阿特洛波斯金剪 (Atropos Golden Shears): Apply 5 Unravel to ALL enemies and snip 1 upgraded intent Scrap from each.
/// </summary>
public sealed class PotionAtroposShears : IndigoTailorPotion
{
    public override PotionRarity Rarity => PotionRarity.Rare;
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override TargetType TargetType => TargetType.AllEnemies;
    protected override string ImageStem => "potion_atropos_shears";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Unravel", 5m)];

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (Owner?.Creature?.CombatState == null) return;
        foreach (var enemy in Owner.Creature.CombatState.HittableEnemies)
        {
            await PowerCmd.Apply<UnravelPower>(choiceContext, enemy, 5m, Owner.Creature, null, false);
            await RawScrapHelper.SpawnUpgradedScrapFromEnemyIntent(Owner, enemy);
        }
    }
}
