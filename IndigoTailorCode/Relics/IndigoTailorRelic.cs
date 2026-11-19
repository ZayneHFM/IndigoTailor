using BaseLib.Abstracts;
using BaseLib.Utils;
using IndigoTailor.Character;
using MegaCrit.Sts2.Core.Models;

namespace IndigoTailor.Relics;

[Pool(typeof(IndigoTailorRelicPool))]
public abstract class IndigoTailorRelic : CustomRelicModel
{
    protected override string IconBaseName => string.Empty;
    public override string PackedIconPath => $"res://IndigoTailor/images/relics/{IconBaseName}.png";
    protected override string PackedIconOutlinePath => $"res://IndigoTailor/images/relics/{IconBaseName}.png";
    protected override string BigIconPath => $"res://IndigoTailor/images/relics/big/{IconBaseName}.png";
}
