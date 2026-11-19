using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using IndigoTailor.Cards;
using IndigoTailor.Character;
using IndigoTailor.Mechanics;
using IndigoTailor.Patches;
using IndigoTailor.Potions;
using IndigoTailor.Relics;

namespace IndigoTailor.Events;

/// <summary>
/// 专属剧情事件：《暗港底层织坊 (The Underdocks Loom-House)》
/// </summary>
public sealed class UnderdocksLoomHouseEvent : EventModel
{
    public override bool IsAllowed(IRunState runState)
    {
        return runState.Players.Count == 1 && runState.Players[0].Character is IndigoTailorCharacter;
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("HpCost", 6m),
        new IntVar("GoldGain", 75m)
    ];

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            new EventOption(this, OnSalvageDyedBolt, "INDIGOTAILOR-UNDERDOCKS_LOOM_HOUSE.pages.INITIAL.options.SALVAGE_BOLT"),
            new EventOption(this, OnDrinkIndigoVat, "INDIGOTAILOR-UNDERDOCKS_LOOM_HOUSE.pages.INITIAL.options.DRINK_VAT"),
            new EventOption(this, OnDismantleLoom, "INDIGOTAILOR-UNDERDOCKS_LOOM_HOUSE.pages.INITIAL.options.DISMANTLE_LOOM")
        ];
    }

    private async Task OnSalvageDyedBolt()
    {
        var ctx = new ThrowingPlayerChoiceContext();
        if (Owner?.Creature != null)
            await CreatureCmd.Damage(ctx, Owner.Creature, 6m, MegaCrit.Sts2.Core.ValueProps.ValueProp.Unblockable, null, null);
        if (Owner != null)
            await RelicCmd.Obtain(ModelDb.Relic<TwoHoleBrassButton>().ToMutable(), Owner);
        SetEventFinished(L10NLookup("INDIGOTAILOR-UNDERDOCKS_LOOM_HOUSE.pages.SALVAGE_BOLT_DONE.description"));
    }

    private async Task OnDrinkIndigoVat()
    {
        if (Owner != null)
        {
            var prefs = new CardSelectorPrefs(
                RawScrapHelper.GetPrompt("INDIGOTAILOR-SELECT_TO_STITCH", CardSelectorPrefs.EnchantSelectionPrompt),
                1);
            var selected = await CardSelectCmd.FromDeckGeneric(Owner, prefs, c => c is IndigoTailorCard, null);
            var chosen = selected?.FirstOrDefault() as IndigoTailorCard;
            if (chosen != null)
            {
                // 继续追加永久补丁前，先把已保存的旧永久补丁 hydrate 回来，
                // 否则随后的 SyncPermanentPatchesToString 会用不完整的列表覆盖旧数据。
                if (chosen.AttachedPatches.All(p => !p.IsPermanent))
                    PermanentPatchworkSavePatch.RestorePermanentPatchesFromString(chosen);
                chosen.AttachedPatches.Add(new PatchworkSlotData
                {
                    Kind = PatchworkKind.CrimsonNeedle,
                    Magnitude = 6,
                    IsUpgraded = true,
                    IsPermanent = true
                });
                chosen.AttachedPatches.Add(new PatchworkSlotData
                {
                    Kind = PatchworkKind.IndigoLining,
                    Magnitude = 6,
                    IsUpgraded = true,
                    IsPermanent = true
                });
                PermanentPatchworkSavePatch.SyncPermanentPatchesToString(chosen);
            }
        }
        SetEventFinished(L10NLookup("INDIGOTAILOR-UNDERDOCKS_LOOM_HOUSE.pages.DRINK_VAT_DONE.description"));
    }

    private async Task OnDismantleLoom()
    {
        if (Owner != null)
            await PlayerCmd.GainGold(75m, Owner);
        SetEventFinished(L10NLookup("INDIGOTAILOR-UNDERDOCKS_LOOM_HOUSE.pages.DISMANTLE_LOOM_DONE.description"));
    }
}
