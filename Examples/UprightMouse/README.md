# 直立鼠

[English](README_EN.md)

这个外观包给玩家加上一只可以穿衣服的直立鼠。进岛后在「外观库」中选用它，角色会换上鼠头、耳朵、躯干和会摆动的尾巴，仍然使用游戏的角色动作。

## 在游戏里试用

本范例附带 Windows x64 模型资源包。先按说明编译并安装[外观模组框架](../AppearanceFramework/README.md#编译和试用)，然后把本目录中的 `mod.json`、`appearance.json`、`appearance-upgrades.json`、[封面图](preview.png)和 `Assets/uprightmouse.bundle` 复制到游戏的 `Mods/UprightMouse` 目录，保留 `Assets/` 这一层。

在模组列表启用「外观模组框架」和「直立鼠」，完整重开游戏。进岛按 **F10**，在「身体」选择「直立鼠」。试试走动、转身和穿原版服装。和朋友试用时，双方都启用框架。缺少直立鼠的玩家可在允许自动接收房间外观时看到它。

## 改颜色

打开安装副本的 [appearance.json](appearance.json)。在 `materials` 里，`MouseFur` 是毛色，`EarPink` 是内耳，`TailPink` 是尾巴，`MouseNose` 是鼻尖，`EyeWhite` 和 `EyePupil` 是眼白和瞳孔。把对应的 `color` 换成 `"#C8875C"` 这样的颜色代码，保存后重开游戏，再选择这套外观。

框架用原版老鼠的材质保留游戏的光照和阴影。范例把各部位的颜色明确写在配置里。配置里的模型部件还能指定挂点、遮住原有外观，以及调整眨眼和尾巴的骨骼摆动。 [外观数据说明](../AppearanceFramework/README.md#做自己的外观包)

## 修改模型或做一套自己的角色

1. 在 Unity Hub 新建 **Unity 2022.3.62f3c1** 的 Universal 3D（URP）项目，安装 Windows Build Support。预览材质使用 URP 14.0.12。将 [Source/UnityAssets](Source/UnityAssets) **里面的内容**复制到新项目的 `Assets` 目录，包含随附的 `.meta` 文件。
2. 打开 `Assets/UprightMouse/Preview.unity` 看组合效果。在 `Assets/UprightMouse/UprightMouse.prefab` 修改并保存部件位置。需要修改网格时，可用 [Blender 原稿](Source/Models/UprightMouse.blend)导出新的 FBX。尾巴的可编辑骨骼在 [UprightMouseTail.fbx](Source/UnityAssets/UprightMouse/Models/UprightMouseTail.fbx) 中。
3. 在 Unity 的 Project 窗口选中要打包的预制件，使用 **Cozy Island → Build Selected Prefab Bundle (Windows)**。如果在改直立鼠，将打包结果命名为 `uprightmouse.bundle`，替换测试副本中 `Assets` 下的同名文件，然后重启游戏检查。
4. 如果要做一套全新的外观，先复制这个范例。在 `mod.json` 给新作品生成 UUID，并改作品名称。在 `appearance.json` 给外观包和条目改成自己的 `id` 和名称。把 `appearance.json` 中的 `bundle`、`prefab` 和 `parts` 改成新模型对应的资源包、预制件路径和部件。可以先做一颗新头。在预制件根节点下放一个名为 `Head` 的模型子节点，在配置里只留一个部件 `{"path":"Head","anchor":"Face","hideOriginal":true}`。先移除原来对应鼠身的 `materials`、`bodyMaterial` 和 `hideBody`，确认新头能显示，再加其他部位和配色。 [外观数据说明](../AppearanceFramework/README.md#做自己的外观包)
