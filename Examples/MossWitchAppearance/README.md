# 青苔魔法套装外观包

[English](README_EN.md)

这个数据包把青苔魔法袍和帽子加入「外观库」。两件可以分开选，也可以和直立鼠等自定义身体一起穿。

1. 编译并安装[外观模组框架](../AppearanceFramework/README.md#编译和试用)。
2. 将本目录的 `mod.json`、`appearance.json`、`appearance-upgrades.json`、[封面图](preview.png)和 `Assets/witch-robe-win64.bundle` 复制到游戏的 `Mods/MossWitchAppearance`，保留 `Assets/` 这一层。随附资源包适用于 Windows x64。
3. 启用「外观模组框架」和「青苔魔法套装外观包」，完整重开游戏。进岛后按 **F10**，在「服装」中分别选择魔法袍和帽子。

在 [appearance.json](appearance.json) 修改对应条目的 `color` 就能改变布料颜色。`parts` 演示怎样把衣身和袖子挂到不同位置，并用原版装备的材质和摆放方式。可编辑模型与重新打包步骤见[青苔魔法套装物品范例](../WitchRobe/README.md#修改模型和配色)。

用外观窗口选穿不需要制作物品。启用同款物品模组后，正常穿上工作台制作的实物，也可以通过这个数据包同步外观。这由条目里的 `nativeOwner` 和 `nativeItem` 指定。复制本例做新服装时，如果不关联自制装备，删掉这两个字段。
