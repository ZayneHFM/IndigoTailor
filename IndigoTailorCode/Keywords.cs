using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace IndigoTailor;

/// <summary>
/// 靛染裁缝 v2.0 五大空间织造关键词注册。
/// </summary>
public static class IndigoTailorKeywords
{
    [CustomEnum("CUT"), KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword Cut;

    [CustomEnum("LOOM_SEAM"), KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword LoomSeam;

    [CustomEnum("STITCH"), KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword Stitch;

    [CustomEnum("UNRAVEL"), KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword Unravel;

    [CustomEnum("UNPICK"), KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword Unpick;
}
