# 芽苗帽

[English](README_EN.md)

这是一份由两个 JSON 文件和一个 Blender 导出的 GLB 模型组成的帽子范例。配套游戏和外观模组框架 0.2.0 即可试用，Windows 和 macOS 使用同一份文件。

1. 启用[外观模组框架](../AppearanceFramework/README.md)，在游戏里打开 **模组 → 模组创作 → 打开作品文件夹**。
2. 在 `Mods` 下新建 `SproutCap`，复制 `mod.json`、`appearance.json` 和 `Assets/sprout-cap.glb`，保留 `Assets/` 这层目录。
3. 启用「芽苗帽」，重开游戏。在主菜单打开「外观库」，选择 **服装 → 帽子 → 我的外观 → 芽苗帽**。穿上后进岛查看，也可以在岛上按 F10 选择。

先在测试副本的 `appearance.json` 修改 `Crown` 和 `Sprout` 的 `color`，重开后重新选择。想换自己的模型，可以把范例 GLB 导入 Blender 编辑，或替换成自己导出的 GLB。保留这两个对象名，或者同步修改配置中的 `parts[].path`。发布为新作品时，更换 `mod.json` 的 UUID 以及两个清单中作品和外观的标识。 [完整外观制作指南](../AppearanceFramework/README.md#做自己的外观包)
