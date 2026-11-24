# Cozy Island Modding 工具说明

制作普通外观可以直接用 GLB 和两份 JSON。自定义游戏逻辑用 .NET 编译，Unity 专用 Prefab 或 Shader 才需要 Unity 构建。 [外观制作指南](../Examples/AppearanceFramework/README.md#做自己的外观包)

## 直接编译

编译 C# 模组时，准备 .NET SDK 和带「模组」菜单、支持 UUID 清单的游戏版本。第一次制作可以从上衣换色开始，跟着范例编译、在游戏里试用和发布。 [起始上衣换色](../Examples/ClothingColor/README.md) · [模组清单](../Docs/Manifest.md)

在仓库根目录打开终端，把命令里的路径换成你的游戏程序集目录。Windows 可以通过 Steam 的 **管理 → 浏览本地文件** 找到游戏，再打开 `CozyIsland_Data/Managed`。macOS 使用 `CozyIsland.app/Contents/Resources/Data/Managed`。

```text
dotnet build ./Examples/ClothingColor/Source/ClothingColor.csproj -c Release "-p:GameManagedPath=D:/SteamLibrary/steamapps/common/CozyIsland/CozyIsland_Data/Managed"
```

[CozyMod.props](CozyMod.props) 提供共用的编译设置。范例生成的模组 DLL 在 `Code/` 下。将 `mod.json`、模组 DLL 和所需资源装进本地作品目录，再在游戏里启用和试用。游戏程序集只用于本机编译。游戏更新后，用新版程序集重新编译和试用。 [游戏内试用和发布](../Docs/Workshop.md)

要重建模型或 Shader 的 AssetBundle，请用配套的 Unity 编辑器和目标平台构建模块。 [范例与资源平台](../Examples/README.md)

## 添加主菜单按钮

要在主菜单打开模组界面，可以把 [ModMainMenuEntry.cs](ModMainMenuEntry.cs) 编译进模组。传入自己的按钮名称、显示文字和点击操作，每次更新时调用 `Refresh(panelIsOpen)`，在停用或卸载时调用 `Dispose()`。使用这份代码的模组会共用按钮栏，可以同时启用两个示例检查效果。 [外观模组框架](../Examples/AppearanceFramework/Source/Wardrobe.cs) · [复古画面](../Examples/RetroFilters/Source/RetroControls.cs)

## 命令行校验与发布

需要 JSON 校验回执或自动发布时，可在仓库根目录使用 PowerShell 7。先关闭目标游戏。示例中的 `$player` 指向 Windows 的 `CozyIsland.exe` 或 macOS 的完整 `.app`，`$mod` 指向含 `mod.json` 的本地作品目录。

```powershell
$player = 'D:/SteamLibrary/steamapps/common/CozyIsland/CozyIsland.exe'
$mod = 'D:/Path/To/Mods/MyMod'
./Tools/Validate-Mod.ps1 -GameExe $player -ModDirectory $mod
```

校验由游戏检查清单和包结构。校验通过后，进游戏确认模组实际生效。

发布时先登录 Steam，并选用已试过的运行包。示例显式设为私密。省略 `-Visibility` 时，新作品默认私密，更新保留原可见性。 [发布与更新](../Docs/Workshop.md#发布和更新)

```powershell
./Tools/Publish-Mod.ps1 -GameExe $player -ModDirectory $mod -Visibility private
```

结果和游戏日志默认写入 `Results/`。可用 `-ResultPath` 指定新的结果文件。Apple 芯片 Mac 可用 `-Rosetta` 启动 Universal 应用中的 Intel 版本。
