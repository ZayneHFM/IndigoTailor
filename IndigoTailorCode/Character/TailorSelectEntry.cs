using System;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace IndigoTailor.Character;

/// <summary>
/// 靛染裁缝选人界面入口。
/// </summary>
public class TailorSelectEntry : CustomCharacterSelectEntry
{
    public override string EntryId => "INDIGOTAILOR";

    public override string ButtonIconPath => "res://IndigoTailor/images/charui/char_select_tailor.png";

    public override string EntryTitle =>
        SafeLoc("characters/INDIGOTAILOR-INDIGO_TAILOR_CHARACTER.title", "靛染裁缝");

    public override string EntryDescription =>
        SafeLoc("characters/INDIGOTAILOR-INDIGO_TAILOR_CHARACTER.description", "暗港底层织坊的孤僻匠人，背负着比身躯更庞大的双刃铁剪。");

    public override int SortOrder => 101;

    public override bool VisibleInCharacterSelect => true;
    public override bool UnlockedInCharacterSelect => true;
    public override CharacterModel? AvailabilitySourceCharacter => null;

    public override CharacterModel? InitialCharacter => ModelDb.Character<IndigoTailorCharacter>();

    public override string LockedTitle => EntryTitle;
    public override string LockedDescription =>
        SafeLoc("characters/INDIGOTAILOR-INDIGO_TAILOR_CHARACTER.lockedDescription",
                "用其他角色完成一局游戏后解锁。");

    public override bool ShowVanillaInfoPanelWhenUnresolved => false;
    public override bool ShowVanillaInfoPanelWhenResolved => false;

    public override string? CharacterSelectScenePath => "res://IndigoTailor/scenes/tailor_select_bg.tscn";

    private static string SafeLoc(string path, string fallback)
    {
        try
        {
            var loc = LocString.KeyPathToLocString(path);
            return loc.Exists() ? loc.GetFormattedText() : fallback;
        }
        catch
        {
            return fallback;
        }
    }
}
