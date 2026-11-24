# 起始上衣换色

简体中文 · [English](README_EN.md)

进岛就能看到，本机角色的绿色起始上衣和两只袖子会变成玫红色。游戏照常使用原来的模型和材质，换上其他服装时照常显示对应的服装。这个示例只有一个 C# 文件，无需安装 Unity 编辑器。

## 编译和试穿

1. 安装 .NET SDK，打开终端并进入仓库根目录，也就是包含 `Examples` 和 `Tools` 的文件夹。下面用的是 Windows 示例路径，请替换为你安装的配套游戏目录下的 `CozyIsland_Data/Managed`。macOS 的对应位置是游戏 `.app` 内的 `Contents/Resources/Data/Managed`。 [游戏版本说明](../../README.md) · [.NET SDK](https://dotnet.microsoft.com/download)

   ```text
   dotnet build Examples/ClothingColor/Source/ClothingColor.csproj -c Release "-p:GameManagedPath=D:/SteamLibrary/steamapps/common/CozyIsland/CozyIsland_Data/Managed"
   ```

2. 在游戏打开 **模组 → 模组创作 → 打开作品文件夹**。找到 `Mods`，在下面新建 `ClothingColor`。从 `Examples/ClothingColor` 复制 `mod.json` 和编译生成的 `Code/CozyMods.ClothingColor.dll`，保留下列目录结构。上传时也只需要这两个文件。

   ```text
   Mods/
     ClothingColor/
       mod.json
       Code/
         CozyMods.ClothingColor.dll
   ```

3. 回到 **模组** 列表，启用 **起始上衣换色**，进岛看起始上衣的颜色。如果已经穿了其他服装，先换回起始装扮。
4. 在源码的 `ShirtColor` 那行试换一组数字。前三个数是红、绿、蓝，各为 0 到 255。最后的 `255` 表示完全不透明。完全退出游戏，重新编译并替换本地作品里的 DLL，再开游戏看效果。 [换色源码](Source/ClothingColor.cs)

## 发布到 Steam 创意工坊

发布自己的改版前，在 `mod.json` 里填写自己的 `displayName`、`author` 和 `name`。在 Windows PowerShell 运行 `[guid]::NewGuid()`，或在 macOS 终端运行 `uuidgen`，把生成的作品编号填入 `uuid`。以后更新同一作品时保留它。给项目的 `AssemblyName` 取一个独立的名字，重新编译，并将清单的 `code.assembly` 改为新 DLL 的路径。 [清单字段说明](../../Docs/Manifest.md)

把新清单和 DLL 放入本地作品目录，启用后进岛复测。登录 Steam 后打开 **模组 → 模组创作**，选中自己的作品并保存。首次发布时让工坊 ID 留空，点击 **发布到创意工坊**，默认使用私密可见性。如果 Steam 提示接受工坊协议，请在 Steam 页面完成。上传后订阅并启用工坊副本，停用本地副本，重开游戏检查上衣颜色。 [发布和更新](../../Docs/Workshop.md)
