using System;
using System.Globalization;
using MegaCrit.Sts2.Core.Localization;

namespace IndigoTailor.Mechanics;

/// <summary>
/// 缝合相关的所有玩家可见文案统一入口。
///
/// 历史问题一：这些文案原先硬编码在 C# 字符串里，导致
/// 1. 英文玩家看到中文；
/// 2. 术语改「布贴 → 补丁」时要改多处 C#，容易漏。
/// 现在全部走 cards.json 的 INDIGOTAILOR-STITCH_* / INDIGOTAILOR-PATCH_* 键。
///
/// 历史问题二（2026-11-15 修复）：本类原先用 <c>GetFormattedText()</c> 取模板再做
/// <c>string.Format</c>。但 <c>GetFormattedText()</c> 内部走的是 SmartFormat，
/// 它会把模板里的 <c>{0}</c> 当作「变量 0」去 _variables 字典里查；查不到时
/// 直接把整个 Dictionary 对象 ToString() 填进去，界面上就出现
/// <c>System.Collections.Generic.Dictionary`2[System.String,System.Object]</c>。
///
/// 正确做法：用 <see cref="LocString.GetRawText"/> 拿 JSON 原文（保留 <c>{0}</c>），
/// 再用 <c>string.Format</c> 填充。这样占位符由 .NET 处理，不经过 SmartFormat。
/// </summary>
public static class StitchLoc
{
    /// <summary>
    /// 读取 cards 表里的键的**原始文本**（不做 SmartFormat 展开）。
    /// 失败返回 <paramref name="fallback"/>。
    /// </summary>
    public static string Get(string key, string fallback)
    {
        try
        {
            var loc = LocString.KeyPathToLocString($"cards/{key}");
            if (loc.Exists())
            {
                string raw = loc.GetRawText();
                if (!string.IsNullOrEmpty(raw)) return raw;
            }
        }
        catch
        {
            // fall through
        }
        return fallback;
    }

    /// <summary>
    /// 读取带一个数值参数的模板（模板里用 <c>{0}</c>）。
    ///
    /// 注意：这里**必须**从原始文本取，不能用 GetFormattedText()，
    /// 否则 <c>{0}</c> 会被 SmartFormat 当成变量名去查字典（见类注释）。
    /// </summary>
    public static string Get(string key, string fallback, decimal arg)
    {
        string template = Get(key, fallback);
        try
        {
            return string.Format(CultureInfo.InvariantCulture, template, arg);
        }
        catch (FormatException)
        {
            // 模板里没有合法占位符时，原样返回，不抛给上层
            return template;
        }
    }

    /// <summary>
    /// 读取带两个字符串参数的模板（模板里用 <c>{0}</c> / <c>{1}</c>）。
    /// 用于「副牌名（效果）」这种拼接。
    /// </summary>
    public static string Get(string key, string fallback, string arg0, string arg1)
    {
        string template = Get(key, fallback);
        try
        {
            return string.Format(CultureInfo.InvariantCulture, template, arg0, arg1);
        }
        catch (FormatException)
        {
            return template;
        }
    }
}
