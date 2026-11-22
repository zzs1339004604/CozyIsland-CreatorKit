# MinimalCodeMod：加载、Harmony 与上下文生命周期

这是《舒舒服服小岛时光》的最小外部 C# 示例。它实现 `ICozyMod`，通过游戏提供的 `context.Harmony` 修补自己 DLL 内的 `Probe.ReadValue()`，把返回值从 21 改为 42。目标方法标记 `NoInlining`，避免编译器内联后绕过补丁。它不访问玩家或玩法类型，也不添加界面。

## 构建与安装

在工具包根目录打开 PowerShell，按[主指南](../../README.md#第一个代码-mod)为配套 Mono Player 设置 `$game` 和 `$player`，再导入、编译。

```powershell
./Tools/Import-GameReferences.ps1 -GameDirectory $game
./Tools/Build-CodeMod.ps1 -ProjectPath './Examples/MinimalCodeMod/Source/MinimalCodeMod.csproj'
```

生成文件为 `Code/CozyMods.MinimalCodeMod.dll`。把示例的 `mod.json` 和 `Code/` 复制到游戏 `Mods/MinimalCodeMod`，关闭游戏后用下面的命令校验这一份。`Source` 留在作者目录。

```powershell
$runtime = '<Mods 根目录的绝对路径>/MinimalCodeMod'
./Tools/Validate-Mod.ps1 -GameExe $player -ModDirectory $runtime
```

校验成功后启动游戏，在主菜单模组列表中启用该副本并核对下方日志。

## 如何判断结果

Windows 的默认日志通常位于 `%USERPROFILE%\AppData\LocalLow\LazyFish Studio\CozyIsland\Player.log`，Mac 通常位于 `~/Library/Logs/LazyFish Studio/CozyIsland/Player.log`。用启动参数 `-logFile` 时，以指定位置为准。在日志中查找 `[Mod cozy.example.minimal-code]`。

1. 启用时出现 `Loaded; Harmony probe before=21, after=42`，证明入口执行、真实方法补丁改变了返回值。
2. 首帧出现一次 `First update callback; Harmony probe=42`，证明上下文更新回调运行且补丁仍生效。它不会逐帧刷日志。
3. 进入岛屿、返回菜单等实际加载场景时出现 `Scene callback: <场景名>`。
4. 在菜单停用时出现 `OnUnload` 和 `Cleanup callback; observed updates=...`。停用后不应继续收到此 Mod 的场景回调。
5. 同一进程重新启用，应再次得到 `before=21, after=42`；若停用未撤销补丁，加载会明确失败。重复检查不会用强制全局撤销掩盖残留。

上下文在 `OnUnload` 返回后执行清理并移除该 owner 的 Harmony 补丁；`OnUnload` 日志本身不是撤销成功证明。上述停用/重启用与场景切换才是相应验收步骤。修改 DLL 后完整退出进程，再启动测试新版本。

这里给出可重复的预期观察方法，不代表随包源码已经在你的游戏版本完成 Player、上传或订阅回测。发布流程见[主指南](../../README.md)，接口与清理契约见[基础 API](../../Docs/API.md)和[生命周期](../../Docs/Reloading.md)。
