# GreenThumb：作者自带的代码 Mod

作物生长与耗水调整。`Source/GreenThumb.cs` 使用 `context.Harmony` 在原版 `CozyFallowLand.Update` 前后计算变化；游戏农田源码不需要为这个 Mod 修改。只在房主执行，配置在 `garden.json`。额外生长进度在下一次正常 Update 参与成熟判断，最多延后一帧；与其他修改同一方法的 Mod 需要联合测试。

本文件夹的 `mod.json` 只声明身份、入口和文件位置，实际行为由 `Code/CozyMods.GreenThumb.dll` 实现。修改配置后完全退出当前世界，在主菜单重新协调或重启；修改已加载 DLL 必须重启进程。

在工具包根目录导入本机引用并编译：

```powershell
./Tools/Import-GameReferences.ps1 -GameDirectory 'D:\SteamLibrary\steamapps\common\Cozy Island'
./Tools/Build-CodeMod.ps1 -ProjectPath ./Examples/GreenThumb/Source/GreenThumb.csproj
```

把完整文件夹放入本地 Mod 目录，开启并使用测试岛验证效果与关闭后的恢复。只上传自己的运行 DLL、清单、图片和配置，不携带游戏或 Harmony DLL。发布流程见[主指南](../../README.md)，补丁与清理约定见[生命周期](../../Docs/Reloading.md)。
