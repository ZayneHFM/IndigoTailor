using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using IndigoTailor.Character;
using IndigoTailor.Mechanics;
using IndigoTailor.Powers;

namespace IndigoTailor.Relics;

/// <summary>
/// R12 - 旧衣翻新台 (TreadleRefurbishTable)
/// </summary>
public sealed class TreadleRefurbishTable : IndigoTailorRelic
{
    public override RelicRarity Rarity => RelicRarity.Shop;
    public override bool ShouldReceiveCombatHooks => true;
    protected override string IconBaseName => "treadle_refurbish_table";

    [SavedProperty]
    public string PermanentRefurbishLedger { get; set; } = string.Empty;

    public void RecordPermanentRefurbish(string targetCardUid, string patchKind)
    {
        PermanentRefurbishLedger = string.IsNullOrEmpty(PermanentRefurbishLedger)
            ? $"{targetCardUid}:{patchKind}"
            : $"{PermanentRefurbishLedger};{targetCardUid}:{patchKind}";
    }
}
