# 青苔魔法套装外观包

[English](README_EN.md)

在「外观库」选穿青苔魔法袍和魔法帽。两件可以分开穿，也能搭配直立鼠等自定义身体。

## 在游戏里试用

1. 安装并启用 [外观模组框架](../AppearanceFramework/README.md) 0.2.0 或更新版本，然后订阅并启用青苔魔法套装外观包。[Steam 创意工坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3845056621)
2. 重启游戏，点主菜单的「外观库」，或进岛按 **F10**。在「服装」中分别选择魔法袍和帽子。

从仓库手动安装时，把 `mod.json`、`appearance.json`、`appearance-upgrades.json`、`preview.png` 和 [Assets/moss-witch.glb](Assets/moss-witch.glb) 复制到 `Mods/MossWitchAppearance`，保留 `Assets/` 这一层。Windows x64 和 macOS 使用同一份模型。

## 改颜色或模型

在 [appearance.json](appearance.json) 找到对应条目，修改 `materials.ClothTeal.color`，即可改变布料颜色。帽子的饰边、内衬和金属扣分别使用原版材质。把 [moss-witch.glb](Assets/moss-witch.glb) 导入 Blender 可以编辑衣身、袖子和帽子。若更改节点名称，记得同步更新配置的 `parts`。[外观数据说明](../AppearanceFramework/README.md#做自己的外观包)

在外观库可以直接选穿。若同时启用 [青苔魔法套装物品模组](../WitchRobe/README.md)，正常穿上制作好的实物也会关联这套外观。这由 `nativeOwner` 和 `nativeItem` 指定。复制本例做新服装时，如果自己的外观没有绑定物品，删掉这两个字段，并给作品换上新的 UUID 和条目 ID。
