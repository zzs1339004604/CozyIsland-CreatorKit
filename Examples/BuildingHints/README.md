# 建筑名称提示（BuildingHints）

这是《舒舒服服小岛时光》的 C# / Harmony 示例。启用后，拿着“小木锤”聚焦可拆除建筑或家具，交互提示会从“拆除”变成“拆除 · 建筑名称”，其中名称来自当前目标的原生字段。它只修改本机显示，不改变拆除条件、动作或产物。

## 构建和试用

1. 在工具包根目录打开 PowerShell，按[主指南](../../README.md#第一个代码-mod)为配套 Mono Player 设置 `$game` 和 `$player`。运行 `./Tools/Import-GameReferences.ps1 -GameDirectory $game`。
2. 运行 `./Tools/Build-CodeMod.ps1 -ProjectPath 'Examples/BuildingHints/Source/BuildingHints.csproj'`。需要本机安装 .NET SDK；产物是 `Code/CozyMods.BuildingHints.dll`。
3. 把示例的 `mod.json` 和 `Code/` 复制到游戏 `Mods/BuildingHints`。退出游戏后运行 `./Tools/Validate-Mod.ps1 -GameExe $player -ModDirectory '<Mods 根目录的绝对路径>/BuildingHints'` 校验该副本。启动游戏，在主菜单的“模组”界面启用它，再进入世界。
4. 拿起“小木锤”，把焦点移到可拆除的原版建筑或家具上，确认提示包含该物体的名称。不必实际执行拆除操作。
5. 返回主菜单停用，再进入世界，确认提示恢复原文。游戏会自动撤销此 Mod owner 的 Harmony 补丁。

发布时选择实际测试过的 `Mods/BuildingHints`。上传包需要 `mod.json` 和 `Code/CozyMods.BuildingHints.dll`。`Source` 是可编辑源码，游戏上传器会自动排除；不要随 Mod 携带 `References`、Unity DLL、`0Harmony.dll` 或 MonoMod DLL。

## 代码入口

`Source/BuildingHints.cs` 实现 `ICozyMod`，通过 `context.Harmony` 使用游戏统一提供的 Harmony。Postfix 修改现有 `PCDBuilding.interactDesc` 属性 getter 的返回值，保留原有本地化交互描述，再追加本地化 `buildingName`。没有建筑名称的物体保持原提示。

这个方法属于游戏现有建筑交互实现，游戏更新后需要重新导入引用、构建并核对其签名与用途。示例没有扫描场景或缓存交互文本；每次读取提示时都会执行补丁，因此上下文停用并撤销补丁后即可恢复原逻辑。
