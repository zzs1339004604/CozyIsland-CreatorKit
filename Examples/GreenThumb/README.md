# GreenThumb：作者自带的代码 Mod

作物生长与耗水调整。`Source/GreenThumb.cs` 使用 `context.Harmony` 在原版 `CozyFallowLand.Update` 前后计算变化；游戏农田源码不需要为这个 Mod 修改。只在房主执行，配置在 `garden.json`。额外生长进度在下一次正常 Update 参与成熟判断，最多延后一帧；与其他修改同一方法的 Mod 需要联合测试。

本文件夹的 `mod.json` 只声明身份、入口和文件位置，实际行为由 `Code/CozyMods.GreenThumb.dll` 实现。修改配置后完全退出当前世界，在主菜单重新协调或重启；修改已加载 DLL 必须重启进程。

在仓库根目录编译，将 `<Managed 路径>` 换成已安装游戏的 Managed 目录。 [编译方式](../../Tools/README.md#直接编译)

```text
dotnet build ./Examples/GreenThumb/Source/GreenThumb.csproj -c Release "-p:GameManagedPath=<Managed 路径>"
```

把 `mod.json`、`garden.json`、`preview.png` 和生成的 `Code/` 复制到本地 `Mods/GreenThumb`，开启并使用测试岛验证效果与关闭后的恢复。发布时选择这份试过的作品。 [发布流程](../../Docs/Workshop.md#发布和更新) · [补丁与清理](../../Docs/Reloading.md)
