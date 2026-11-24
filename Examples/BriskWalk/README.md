# BriskWalk：作者自带的代码 Mod

步行与奔跑字段倍率示例。`Source/BriskWalk.cs` 在房主找到玩家后记录并修改原有字段，通过上下文清理恢复原值；游戏加载器不执行移动规则。配置在 `walking.json`。具体控制器是否使用奔跑字段需实测，不据字段存在就承诺所有形态生效。

本文件夹的 `mod.json` 只声明身份、入口和文件位置，实际行为由 `Code/CozyMods.BriskWalk.dll` 实现。修改配置后完全退出当前世界，在主菜单重新协调或重启；修改已加载 DLL 必须重启进程。

在仓库根目录编译，将 `<Managed 路径>` 换成已安装游戏的 Managed 目录。 [编译方式](../../Tools/README.md#直接编译)

```text
dotnet build ./Examples/BriskWalk/Source/BriskWalk.csproj -c Release "-p:GameManagedPath=<Managed 路径>"
```

把 `mod.json`、`walking.json`、`preview.png` 和生成的 `Code/` 复制到本地 `Mods/BriskWalk`，开启并使用测试岛验证效果与关闭后的恢复。发布时选择这份试过的作品。 [发布流程](../../Docs/Workshop.md#发布和更新) · [补丁与清理](../../Docs/Reloading.md)
