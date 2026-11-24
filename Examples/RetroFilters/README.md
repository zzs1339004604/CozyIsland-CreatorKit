# 复古画面

[English](README_EN.md)

给《舒舒服服小岛时光》的画面加一点老游戏的像素感。四种效果可以单独开启，也可以叠加。主菜单能实时预览。滤镜只处理本机主摄像机的画面，屏幕叠加层的菜单按钮和文字仍然清晰。 [效果预览](preview.png)

## 在游戏里试用

准备支持 UUID 模组清单的 Windows x64 游戏版本和 .NET SDK。范例已经带有 Windows 资源包，试用时直接编译 C# 即可。 [模组清单](../../Docs/Manifest.md) · [配套版本](../../PlayerIdentity.json)

如果旧工坊副本 2.0.0 提示「缺少字段：displayName」，请使用本节步骤编译并试用带 UUID 清单的 2.1.0。更新已有工坊作品时，在游戏的作者页将新版关联到原工坊数字 ID。 [工坊更新说明](../../Docs/Workshop.md#发布和更新)

1. 在仓库根目录打开终端，把命令里的路径换成游戏安装目录中的 `CozyIsland_Data/Managed`，编译后会得到 `Examples/RetroFilters/Code/CozyMods.ColorDither.dll`。

   ```text
   dotnet build Examples/RetroFilters/Source/ColorDither.csproj -c Release "-p:GameManagedPath=D:/SteamLibrary/steamapps/common/CozyIsland/CozyIsland_Data/Managed"
   ```

2. 在游戏打开 **模组 → 模组创作 → 打开作品文件夹**。如果打开的是已有作品，返回上一级 `Mods`。在 `Mods` 里建一个 `RetroFilters` 文件夹，把编译好的 DLL 和示例里的清单、资源包、封面按下面的结构放好。

   ```text
   Mods/
     RetroFilters/
       mod.json
       preview.png
       Assets/colordither.bundle
       Code/CozyMods.ColorDither.dll
   ```

3. 回到 **模组** 列表启用 **复古画面**，按提示重启。点主菜单右上角的 **复古画面**，或进岛后按 **F9**。拖动滑块可以立即看到变化。点击 **完成** 会保存设置，**全部关闭** 可以切回原画，**恢复默认** 会应用范例的推荐搭配。

| 效果 | 主要设置 |
| --- | --- |
| 彩色抖动 | 强度、颗粒大小、彩点强度、抖动色阶 |
| 低分辨率 | 画面像素高度。数值越低，画面上的像素块越大。 |
| 色阶分层 | 明暗档数。数值越低，颜色的过渡越分明。 |
| 轮廓描边 | 根据物体的深度差描出轮廓，强度可调。 |

## 代码和资源

- `ColorDither.cs` 创建滤镜材质，通过 Harmony 把全屏效果挂到主摄像机的后处理之后。描边开启时才请求深度图，所有效果关闭时跳过这次绘制。 [滤镜入口](Source/ColorDither.cs)
- `RetroControls.cs` 使用游戏现有的滑块、按钮、开关和字体搭建面板，把参数交给滤镜材质。调整保存在本机游戏设置中。想换一套默认参数，可从同一文件的 `RetroSettings` 入手。 [面板和参数](Source/RetroControls.cs)
- `CozyColorDither.shader` 定义画面如何像素化、分层、描边和加抖动。低分辨率在最终画面上模拟像素块，游戏的渲染分辨率保持原值。 [Shader 源码](Source/UnityProject/Assets/CozyColorDither.shader)

## 重建资源包

只改 C# 或默认参数时，重新编译 DLL 即可。改动 Shader 后，用 Unity Hub 添加 `Source/UnityProject`，以 **Unity 2022.3.62f3c1** 打开。该项目已配好 **URP 14.0.12**。保存修改，点 **Cozy Island → Build Windows Retro Filter Bundle** 重建范例的 `Assets/colordither.bundle`。把生成的资源包复制到本地测试副本，完整退出并重开游戏。 [资源包构建代码](Source/UnityProject/Assets/Editor/BuildColorDitherBundle.cs)

从范例制作自己的作品时，使用新的 UUID、作品信息和程序集名，在清单里填入新 DLL 的路径。在 `RetroControls.cs` 里给设置键换一个前缀，两份作品就可以各自保存设置。发布时使用已在游戏里试过的运行文件。 [清单身份与副本](../../Docs/Manifest.md#身份与副本) · [发布到创意工坊](../../Docs/Workshop.md#发布和更新)
