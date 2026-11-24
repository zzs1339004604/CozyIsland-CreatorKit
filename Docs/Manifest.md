# Mod 清单与运行包

本文定义《舒舒服服小岛时光》加载基础使用的 `mod.json`。每个作品独占一个目录，清单直接放在根部，UTF-8 编码。它声明身份、作者信息、依赖和代码入口；玩法配置由作者自己的代码解析。

```json
{
  "schemaVersion": 1,
  "uuid": "3b95f7ab-4ea1-4d86-bd88-93bc2c712e45",
  "displayName": "园艺扩展",
  "name": "alice.garden-tools",
  "author": "作者名",
  "version": "1.0.0",
  "description": "向玩家说明实际效果、触发方式和限制。",
  "requiresRestart": false,
  "dependencies": [],
  "code": {
    "assembly": "Code/MyMods.MyMod.dll",
    "type": "MyMods.MyMod"
  }
}
```

## 字段

| 字段 | 规则 |
| --- | --- |
| `schemaVersion` | 必填整数 `1`，是清单格式版本，不是作品版本、Steam 版本或功能级别 |
| `displayName` | 必填，玩家在游戏和 Steam 工坊看到的显示名，最多 100 字符 |
| `name` | 必填，作者使用的标识名。3–64 个小写字母、数字、点、横线或下划线，首字符为字母或数字 |
| `uuid` | 必填，作品的永久身份。用带连字符的非空 UUID，读取时统一为小写 |
| `author` | 必填非空文本，最多 100 字符 |
| `version` | 必填非空文本，最多 40 字符；建议数字形式 `1.0.0`，以便依赖比较 |
| `description` | 可选，最多 4000 字符 |
| `preview` | 可选，Mod 内 PNG/JPEG 的相对路径，文件须存在且小于 1 MiB |
| `requiresRestart` | 布尔值，默认 `false`；无法完整撤销效果时设为 `true` |
| `dependencies` | 必要依赖数组，默认空，最多 64 项；结构见下节 |
| `code.assembly` | 相对作品根部的 `.dll` 路径，文件须存在 |
| `code.type` | 实现 `CozyIsland.Modding.ICozyMod` 的完整入口类名，最多 256 字符；公开、非抽象、有可公开调用的无参构造函数 |

数据包可以只提供清单与资源，并在 `dependencies` 中声明负责读取它的模组。例如，外观模组框架读取直立鼠外观包的 `appearance.json`。清单负责说明身份、依赖和代码入口。自定义配置可以随包发布，由代码通过 `context.ReadPath` 定位并校验。未知清单字段和未知格式版本会报错。 [直立鼠外观数据](../Examples/UprightMouse/appearance.json)

没有 `preview` 字段时，上传器依次尝试 `preview.png`、`icon.png`，均不存在则生成默认封面。显示名与标识名可以修改。`uuid` 用于副本识别、依赖匹配、发布关联、Harmony owner 和注册归属。更新时保留它。

## 身份与副本

| 标识 | 示例 | 生命周期 |
| --- | --- | --- |
| 永久身份 `uuid` | `3b95f7ab-4ea1-4d86-bd88-93bc2c712e45` | 新建时生成一次，更新、发布及改名时保留 |
| 显示名 `displayName` | `园艺扩展` | 可以重名、修改或翻译 |
| 标识名 `name` | `alice.garden-tools` | 可以重名或修改，显示在作者页和日志中 |
| 物品局部 ID | `flowerpot` | 同一作品内的物品定义名称，与永久身份共同确定稳定物品 ID |
| 作品版本 `version` | `1.2.0` | 随作品更新递增 |
| 工坊数字 ID | Steam 创建作品时返回 | 用于下载和更新某个工坊发布项 |
| 本地目录名 | `garden` | 本机工程或安装位置，与永久身份分别记录 |

创建新作品时，生成一次随机 UUID 并写入 `mod.json`。C# 项目还需要独立的程序集名，用 `code.type` 指定入口类名。Windows 可以在系统自带的 PowerShell 运行 `[guid]::NewGuid()`，macOS 可在终端运行 `uuidgen`。同一作品的本地版、工坊版和更新版本共用该 UUID。 [示例中的操作](../Examples/ClothingColor/README.md#发布到-steam-创意工坊) · [可选的工程生成工具](../Tools/README.md#新建自己的作品)

复制工程做一个独立作品时，生成新的 UUID 并更新作品信息。复制 C# 项目时，同时更新程序集名和入口，检查源码里的身份引用。复制运行包作为同一作品的测试副本时，保留原 UUID。

启用和排序使用安装键，本地为 `local:<目录名>`，工坊为 `workshop:<数字 ID>`。同一作品可以安装多份。启用一份时，游戏会同时停用已读出相同作品身份的其他副本。需要重启时，按提示退出整个进程。若多个同身份副本同时开启，先在列表保留一份。改本地安装目录名、换账号或切换副本后，先核对副本的版本与安装路径，确认这一份已启用并生效，再读含该作品物件的存档。停用或未识别的副本可能触发[缺失内容清理](Save-Data.md#缺失内容与失败是不同情况)。

## 必要依赖与顺序

```json
"dependencies": [
  { "uuid": "64ea60f6-a1e8-4c68-9f39-8dd5b3d6d627", "minimumVersion": "1.0.0" }
]
```

每项填写目标作品清单中的 `uuid`。UUID 统一为小写后比较，禁止重复或依赖自己。`minimumVersion` 可省略；填写时必须可由 `System.Version` 解析，例如 `1.0.0`，不支持版本范围或预发布后缀。依赖比较也要求被依赖作品的 `version` 是可解析数字版本。

加载器在玩家首选顺序中让必要依赖优先；缺失、版本过低、循环或依赖加载失败会使依赖它的作品无法正常加载。依赖不会自动订阅、下载或开启。当前没有可选依赖或 `loadAfter` 字段。反序卸载用于让使用者先清理；顺序不能自动解决两个补丁的逻辑冲突。

## 文件和发布边界

```text
MyMod/
  mod.json
  preview.png
  Code/MyMods.MyMod.dll
  settings.json
  Source/MyMod.cs
  Source/MyMod.csproj
```

清单最大 64 KiB；运行包最大 256 MiB、4096 个文件。路径必须位于作品根部，不允许路径逃逸、符号链接或目录联接。运行文件允许 `.json`、`.png`、`.jpg`、`.jpeg`、`.dll`、`.bundle`、`.assetbundle`、`.wav`、`.ogg`、`.mp3`、`.csv`、`.txt`、`.bytes`、`.xml`、`.md`。允许上传某扩展名不代表加载器会自动解析它。

上传工具排除 `Source`、`References`、`Tools`、`bin`、`obj` 及版本控制/IDE 目录。游戏、Unity、Mirror、Harmony、MonoMod、Cecil、系统和其他共享运行时程序集不得混入运行包；游戏的运行包校验按 DLL 元数据中的程序集身份检查共享库，改文件名不会改变程序集身份。作者从自己安装的游戏导入引用，仅供本机编译。

在本地 `Mods` 的直接子目录放入 `mod.json`、`Code/` 和所需运行资源。先在游戏里启用并试用，再选择这份安装副本发布。需要单独查看机器可读的包校验回执时，可以运行 `Validate-Mod.ps1`。 [游戏内发布](Workshop.md#发布和更新) · [命令行校验](../Tools/README.md#编译并验证运行包)
