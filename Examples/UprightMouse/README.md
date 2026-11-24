# 直立鼠

[English](README_EN.md)

在「外观库」换上直立鼠，角色会有鼠头、会眨眼的眼睛、耳朵和走动时会摆动的尾巴。可以继续穿游戏里的服装。

## 在游戏里试用

1. 安装并启用 [外观模组框架](../AppearanceFramework/README.md) 0.2.0 或更新版本，再订阅并启用直立鼠。[Steam 创意工坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3844597180)
2. 重启游戏，点主菜单的「外观库」，或进岛按 **F10**。在「身体」选择「直立鼠」。

从仓库手动安装时，把 `mod.json`、`appearance.json`、`appearance-upgrades.json`、`preview.png` 和 [Assets/upright-mouse.glb](Assets/upright-mouse.glb) 复制到 `Mods/UprightMouse`，保留 `Assets/` 这一层。Windows x64 和 macOS 使用同一份模型。和朋友联机时，双方启用同版本的框架，即可接收房间分享的外观。

## 改颜色

打开 [appearance.json](appearance.json)。在 `materials` 中，`MouseFur` 是毛色，`EarPink` 是内耳，`TailPink` 是尾巴，`MouseNose` 是鼻尖，`EyeWhite` 和 `EyePupil` 是眼白和瞳孔。把对应的 `color` 改成 `"#C8875C"` 这样的颜色代码。保存后重开游戏，在外观库选穿检查效果。

这些颜色用在游戏原有的鼠材质上，保留原版光照和阴影。配置还指定了各部位的挂点、眼睛的眨眼形状以及尾巴的五节骨骼。

## 改模型或做自己的身体外观

1. 在 Blender 导入 [upright-mouse.glb](Assets/upright-mouse.glb)，即可看到游戏实际使用的模型组合。鼠的网格原稿见 [UprightMouse.blend](Source/Models/UprightMouse.blend)，尾巴骨骼也保存在 [UprightMouseTail.fbx](Source/UnityAssets/UprightMouse/Models/UprightMouseTail.fbx) 中。
2. 导出单个 GLB 时保留网格法线。保留眼睛的 `Close` 形状键、尾巴骨骼和顶点权重。模型改名或移动节点后，同时更新 `appearance.json` 的 `prefab`、`parts` 或 `followThrough` 路径。替换测试副本中的模型，重开游戏看眨眼、走动和穿衣效果。
3. 做全新外观时，先用自己的作品名称、UUID 和条目 ID，并移除范例的 `appearance-upgrades.json`。可以从一颗名为 `Head` 的头开始，只保留 `{"path":"Head","anchor":"Face","hideOriginal":true}` 这个部件。按新模型更新材质和隐藏设置。[外观数据说明](../AppearanceFramework/README.md#做自己的外观包)
