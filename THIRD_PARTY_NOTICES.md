# 第三方与来源声明 / THIRD_PARTY_NOTICES

本仓库**不包含**游戏本体文件，也不分发任何第三方 DLL。

## 许可范围
- 本人（Zayne）有权授权的**源代码**采用 MIT 许可，见 `LICENSE`。
- 《杀戮尖塔 2》游戏本体、其运行库，以及下列依赖与单独注明来源的素材，仍适用各自条款，不受本仓库 MIT 许可覆盖。

## 构建期/运行期依赖（未随仓库分发）
- `BaseLib`（NuGet: `Alchyr.Sts2.BaseLib`，作者 Alchyr）— 编译与运行期依赖，遵循其自身许可。
- `Alchyr.Sts2.ModAnalyzers` — 编译期 Roslyn 分析器，遵循其自身许可。
- `sts2.dll` / `0Harmony.dll` / `GodotSharp` — 来自《杀戮尖塔 2》游戏目录，仅在本机构建时以 `Private=false` 方式引用，不随仓库分发。

## 美术与音频素材
- 本 Mod 的图像等视觉素材**主要由 Gemini 生成**，其归属与最终许可由作者（Zayne）确认，不必然适用 MIT。
- 如需重用素材，请先获得作者许可。