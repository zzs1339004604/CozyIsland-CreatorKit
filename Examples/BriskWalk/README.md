# BriskWalk：作者自带的代码 Mod

步行与奔跑字段倍率示例。`Source/BriskWalk.cs` 在房主找到玩家后记录并修改原有字段，通过上下文清理恢复原值；游戏加载器不执行移动规则。配置在 `walking.json`。具体控制器是否使用奔跑字段需实测，不据字段存在就承诺所有形态生效。

本文件夹的 `mod.json` 只声明身份、入口和文件位置，实际行为由 `Code/CozyMods.BriskWalk.dll` 实现。修改配置后完全退出当前世界，在主菜单重新协调或重启；修改已加载 DLL 必须重启进程。

在工具包根目录导入本机引用并编译：

```powershell
./Tools/Import-GameReferences.ps1 -GameDirectory 'D:\SteamLibrary\steamapps\common\Cozy Island'
./Tools/Build-CodeMod.ps1 -ProjectPath ./Examples/BriskWalk/Source/BriskWalk.csproj
```

把完整文件夹放入本地 Mod 目录，开启并使用测试岛验证效果与关闭后的恢复。只上传自己的运行 DLL、清单、图片和配置，不携带游戏或 Harmony DLL。发布流程见[主指南](../../README.md)，补丁与清理约定见[生命周期](../../Docs/Reloading.md)。
