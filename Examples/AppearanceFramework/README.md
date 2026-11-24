# 联机外观框架

[English](README_EN.md)

「联机外观」让玩家在游戏里选择自定义身体和服装。在同一个房间启用框架后，玩家可以看见彼此的外观。作者允许分享时，框架还能从房间玩家那里接收缺少的模型。每套可穿戴外观由创作者制作的数据包提供。

## 编译和试用

1. 准备支持 [UUID 模组清单](../../Docs/Manifest.md)的游戏和 .NET SDK。在本目录运行下面的命令。将路径替换为游戏安装目录中的 `CozyIsland_Data/Managed`。

   ```powershell
   dotnet build Source/AppearanceFramework.csproj -c Release "-p:GameManagedPath=D:/SteamLibrary/steamapps/common/CozyIsland/CozyIsland_Data/Managed"
   ```

2. 在游戏中打开 **模组 → 模组创作 → 打开作品文件夹**。如果打开的是某个作品，回到上一级 `Mods`。在其中新建 `AppearanceFramework`，把 `mod.json` 和刚生成的 `Code/CozyMods.AppearanceFramework.dll` 放进去，保留原有目录结构。
3. 把你想试用的外观包放入 `Mods` 下另一个文件夹。在模组列表开启框架和外观包，完整重开游戏。进岛后按 **F10**，在「身体」或「服装」选择外观。可以在窗口里更改快捷键或恢复默认外观。

房主和客机都需要启用同一版本的框架。收到的外观先放入临时缓存。想下次继续使用时，可以在「房间外观」选中作者允许保存的外观，点击 **保存并穿上**。关闭「自动接收房间外观」后，本机停止获取缺少的资源，房主也停止为客机获取缺少的资源。已安装或缓存的外观仍可使用。

## 做自己的外观包

一个可试用的外观包包含三个文件。把它们放入同一个模组目录，保留资源包相对于清单的路径。

| 文件 | 要写什么 |
| --- | --- |
| `mod.json` | 生成自己的作品 UUID，填写名字，并在 `dependencies` 中声明框架的 UUID `a2f648bf-1066-45da-91ea-1c7ca2f2511e` 和最低版本 `0.1.0`。 [模组清单](../../Docs/Manifest.md) |
| `appearance.json` | 使用 `schemaVersion: 1`。填写 `id`、`name`、`author`、`platform`、`bundle` 和 `entries`。 |
| `Assets/模型名.bundle` | 用 Unity 打包一个或多个预制件，供框架安装到角色身上。资源包要为玩家使用的平台构建。 |

`platform` 可以是 `win64` 或 `macos`。每个 `entries` 条目用 `id` 和 `name` 标识外观，用 `slot` 指定穿戴位置，用 `prefab` 填写资源包里的预制件路径，再用 `parts` 列出要挂到角色身上的模型部件。部件的 `path` 是预制件中相对根节点的路径，`anchor` 是游戏角色上的挂点。

- 身体条目使用 `Body`。头部用 `Face`，跟随发型显隐的部件用 `Hair`，躯干用 `Torso`。需要先按一个挂点摆好位置，再跟随另一个挂点时，填写 `followAnchor`。
- 服装可用 `Hat`、`Cloth`、`Shoe`、`Glove` 或 `Knee`。衣身和袖子分别使用 `Cloth` 和 `Sleeve` 挂点。部件可填写 `position`、四元数 `rotation` 和 `scale`。需要遮住挂点上的原有外观时，设置 `hideOriginal`。
- 改色可在条目或部件填写 `color`，例如 `"#C8875C"`。要为不同模型材质槽指定游戏原版材质，在条目的 `materials` 中按槽名填写原版预制件 ID、材质名和可选颜色。部件可以用 `materialOverrides` 把自己的材质槽重定向到同一份配色中的另一项。
- 身体条目可用 `bodyMaterial` 给原角色露出的皮肤指定配色，用 `hideBody` 另行遮住颈部等原有部件。眼睛可通过 `blinkBlendShape` 指定眨眼使用的形变。带骨骼的尾巴等部件可以用 `followThrough` 配置跟随角色移动的摆动。
- 服装部件可用 `nativeEquipment` 选一个游戏原版装备作为位置和材质的参考。要让穿上自制实物时也同步外观，填入条目的 `nativeOwner`（注册物品的模组 UUID）和 `nativeItem`（物品 ID）。

`share` 控制外观能否在房间内分享，`allowSave` 控制收到的外观能否保存到本地。每包上限 8 MiB。框架读取网格、骨骼和贴图，使用自身的挂接与动画。预制件中的脚本、动画控制器等组件不会随外观在游戏中执行。 [数据字段](Source/AppearanceData.cs) · [部件字段](Source/PlacementVisuals.cs)
