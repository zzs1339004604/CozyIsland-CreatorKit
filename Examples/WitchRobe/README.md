# 青苔魔法套装

[English](README_EN.md)

这个范例添加青苔魔法袍和魔法帽。袍身、袖子和帽子使用模组附带的独立网格，布料主色为青绿色 `#267B73`。帽饰和光照沿用原版材质，穿脱使用游戏已有的装备流程。

启用联机外观框架和配套外观包后，也可以从 F10 外观菜单选择同款造型。 [青苔魔法套装外观包](../MossWitchAppearance/README.md)

## 在游戏里试穿

准备支持 [UUID 模组清单](../../Docs/Manifest.md)的 Windows x64 游戏版本和 .NET SDK。范例已包含模型资源包，试穿前只需编译 C#。

1. 在本范例目录打开终端，将下方路径换成游戏安装目录下的 `CozyIsland_Data/Managed`。命令会生成 `Code/CozyMods.WitchRobe.dll`。

   ```powershell
   dotnet build Source/WitchRobe.csproj -c Release "-p:GameManagedPath=D:/SteamLibrary/steamapps/common/CozyIsland/CozyIsland_Data/Managed"
   ```

2. 在游戏中打开 **模组 → 模组创作 → 打开作品文件夹**。如果打开了其他作品的文件夹，先回到上一级 `Mods`。新建 `WitchRobe`，将 `mod.json`、`Assets/witch-robe-win64.bundle` 和生成的 `Code/CozyMods.WitchRobe.dll` 按原有目录结构复制进去。
3. 启用 **青苔魔法套装** 并完整重启游戏。进入测试岛，在**基础工作台**制作青苔魔法袍和青苔魔法帽，每件消耗一份原木。对制作出的衣物互动即可穿戴。

## 修改模型和配色

1. 用 Unity Hub 添加 `Source/UnityProject`，以 **Unity 2022.3.62f3c1** 打开，并安装 Windows Build Support。三份 FBX 可独立编辑。 [模型文件](Source/UnityProject/Assets/WitchRobe/Models)
2. 如需重新从 FBX 生成预制件，选 **Cozy Island → Create Witch Robe Prefab**。选 **Cozy Island → Build Witch Robe Bundle (Windows)** 可重新打包到 `Assets/witch-robe-win64.bundle`。进游戏时沿用原版装备用的挂点变换。
3. 修改 `WitchRobe.cs` 中的 `ClothColor`，重新编译即可调整布料颜色。运行时复制原版材质，并修改主色 `_AlbedoColor`。帽子的其他材质槽保持原配色。将更新后的 DLL 或资源包复制到测试副本，再完整重启游戏。 [运行时代码](Source/WitchRobe.cs)

## 联机试穿

房主和客机安装并启用同一版本的范例。客机进岛后，房主穿上或脱下套装，客机可观察同步效果。范例通过 `CozyModRegistry` 登记服装物品，游戏联机传递装备 ID，各端从本机资源包加载模型。

使用原型清单在两个独立的 Windows 游戏进程中做过本机回环测试，确认两条配方已登记，并验证实时穿脱。客机晚加入时，房主已穿上的装备可能不会出现在首次同步中。待客机就绪后，房主重新穿戴可同步装备。客机需要安装此范例才能显示自定义装备。Steam 远程联机和跨平台联机仍待验证。
