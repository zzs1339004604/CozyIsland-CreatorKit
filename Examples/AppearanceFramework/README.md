# 外观模组框架

[English](README_EN.md)

外观模组框架为直立鼠、青苔魔法套装等模组提供角色皮肤和服装支持。启用框架和外观模组后，就可以在「外观库」选择外观。普通皮肤、帽子和服装外观用 GLB 模型与配置制作。同一份文件可用于 Windows 和 macOS。

## 安装和试用

安装并启用支持 GLB 的外观模组框架 0.2.0，然后安装一个外观包。重开游戏，在主菜单打开「外观库」，或进岛按 F10。第一次可以直接使用[芽苗帽](../SproutCap/README.md)。

### 编译和试用

以下步骤供维护外观框架自身代码时使用。同一份框架 DLL 支持 Windows 和 macOS。

1. 准备支持 [UUID 模组清单](../../Docs/Manifest.md)的游戏和 .NET SDK。在本目录运行下面的命令。将路径替换为游戏安装目录中的 `CozyIsland_Data/Managed`（Windows）或 `CozyIsland.app/Contents/Resources/Data/Managed`（macOS）。

   ```powershell
   dotnet build Source/AppearanceFramework.csproj -c Release "-p:GameManagedPath=D:/SteamLibrary/steamapps/common/CozyIsland/CozyIsland_Data/Managed"
   ```

2. 在游戏中打开 **模组 → 模组创作 → 打开作品文件夹**。如果打开的是某个作品，回到上一级 `Mods`。在其中新建 `AppearanceFramework`，把 `mod.json`、[封面图](preview.png)和刚生成的 `Code/CozyMods.AppearanceFramework.dll` 放进去，保留原有目录结构。
3. 把你想试用的外观包放入 `Mods` 下另一个文件夹。在模组列表开启框架和外观包，完整重开游戏。在主菜单点 **外观库**，可以先选好要穿的身体和服装，进岛后就会显示。游戏中按 **F10** 也能打开窗口，可以在窗口里更改快捷键或恢复默认外观。

在房间里分享外观时，房主和客机都需启用同一版本的框架。收到的外观先放入临时缓存。想下次继续使用时，可以在「房间外观」选中作者允许保存的外观，点击 **保存并穿上**。关闭「自动接收房间外观」后，本机停止获取缺少的资源，房主也停止为客机获取缺少的资源。已安装或缓存的外观仍可使用。

## 做自己的外观包

复制[芽苗帽](../SproutCap/README.md)的三个运行文件，在一个新的模组目录里替换模型和配置。

| 文件 | 要写什么 |
| --- | --- |
| `mod.json` | 作品清单继续使用 `schemaVersion: 1`。生成新作品 UUID，填写名称，并在 `dependencies` 声明框架 UUID `a2f648bf-1066-45da-91ea-1c7ca2f2511e` 和最低版本 `0.2.0`。 [模组清单](../../Docs/Manifest.md) |
| `appearance.json` | 使用 `schemaVersion: 2`，填写 `id`、`name`、`author`、`model` 和 `entries`。例如将 `model` 设为 `Assets/hat.glb`。 |
| `Assets/hat.glb` | 从 Blender 等建模工具导出的自包含 glTF Binary。导出单个场景，保留模型节点名称，把使用的 PNG 或 JPEG 贴图嵌入文件。 |

在 Blender 中选择 **File → Export → glTF 2.0**，格式选 **glTF Binary (.glb)**，导出需要的模型节点。圆弧曲面可使用平滑着色并保留法线。导出皮肤时保留需要的形状键和骨骼权重，并关闭动画和压缩。FBX 可以作为可编辑的源文件保存，导入 Blender 后再导出供游戏读取的 GLB。 [Blender 导出说明](https://docs.blender.org/manual/en/latest/addons/scene_gltf2.html)

将三个文件放进游戏 `Mods/<作品目录>`，启用框架和外观包。用「外观库 → 我的外观」试戴并进岛检查尺寸、颜色和挂点。用支持打包 `.glb` 的游戏版本发布到工坊。 [试用和发布](../../Docs/Workshop.md#发布和更新)

`mod.json` 的 `author` 显示在「模组」页。`appearance.json` 的 `author` 会随外观传给房间里的玩家，显示在「外观库」。同一位作者制作模组和外观时，两处填同一个署名。

更新已发布的外观包时，可以附带 `appearance-upgrades.json`，用 JSON 字符串数组列出被替换的旧版 SHA-256。计算旧版摘要时，依次使用 4 字节小端序的目录字节数、旧 `appearance.json` 原始字节和旧模型文件字节（GLB 或 AssetBundle）。安装新版外观包后，框架会沿用相同条目 ID 和穿戴部位的选择。复制范例做新外观包时，删除范例附带的升级文件。

每个 `entries` 条目用 `id` 和 `name` 标识外观，用 `slot` 指定穿戴位置，再用 `parts` 列出模型部件。部件的 `path` 是 GLB 中区分大小写的节点路径，例如 `Head/Ears`，`anchor` 是游戏角色上的挂点。GLB 默认从模型根查找部件。要以某个子节点为起点，可以在条目里用 `prefab` 填它的相对路径。蒙皮部件的网格和骨骼放在同一个部件节点下。

- 身体条目使用 `Body`。头部用 `Face`，跟随发型显隐的部件用 `Hair`，躯干用 `Torso`。需要先按一个挂点摆好位置，再跟随另一个挂点时，填写 `followAnchor`。
- 服装可用 `Hat`、`Cloth`、`Shoe`、`Glove` 或 `Knee`。衣身和袖子分别使用 `Cloth` 和 `Sleeve` 挂点。部件可填写 `position`、四元数 `rotation` 和 `scale`。需要遮住挂点上的原有外观时，设置 `hideOriginal`。
- GLB 的基础色与嵌入贴图可用于原版材质。要覆盖基础色，可在条目或部件填写 `color`，例如 `"#C8875C"`。要为不同模型材质槽指定游戏原版材质，在条目的 `materials` 中按槽名填写原版预制件 ID、材质名和可选颜色。部件可以用 `materialOverrides` 把自己的材质槽重定向到同一份配色中的另一项。
- 身体条目可用 `bodyMaterial` 给原角色露出的皮肤指定配色，用 `hideBody` 另行遮住颈部等原有部件。眼睛可通过 `blinkBlendShape` 指定眨眼使用的形变。带骨骼的尾巴等部件可以用 `followThrough` 配置跟随角色移动的摆动。
- 服装部件可用 `nativeEquipment` 选一个游戏原版装备作为位置和材质的参考。要让穿上自制实物时自动应用外观，填入条目的 `nativeOwner`（注册物品的模组 UUID）和 `nativeItem`（物品 ID）。

将 `share` 设为 `true` 可以在房间里分享 GLB 外观。也将 `allowSave` 设为 `true` 时，收到外观的玩家可以保存并穿上。模型和外观清单合计上限 8 MiB。框架使用自带的挂接、材质和动画行为。 [数据字段](Source/AppearanceData.cs) · [部件字段](Source/PlacementVisuals.cs)

## 只隐藏原版部件

外观框架 0.2.2 起，可以只用配置制作身体皮肤。例如要隐藏原版头部，使用 `schemaVersion: 2` 并省略 `model`。每个条目使用 `slot: "Body"`、`hideBody: ["Face", "Hair"]` 和空的 `parts: []`。在 `mod.json` 中将框架最低版本设为 `0.2.2`。这类皮肤也可通过 `share` 和 `allowSave` 在房间里分享和保存。

## 现有 AssetBundle 外观

框架继续支持 `schemaVersion: 1`。这一格式使用 `platform`（`win64` 或 `macos`）、`bundle` 和各条目的 Unity Prefab 路径。这里的 `share` 和 `allowSave` 也控制房间分享和保存。维护 Unity 专用的 AssetBundle 时，为每个目标平台分别构建。 [原生资源制作](../../Docs/Native-Authoring.md)
