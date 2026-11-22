# 外部 C# Mod 基础 API

本文说明《舒舒服服小岛时光》配套 Mono Player 在 `CozyIsland.Modding` 命名空间提供的加载与注册底座。作者从自己的完整游戏 Build 导入程序集编译，不需要游戏 Unity 工程。玩法通过[原生 API 创作指南](Native-Authoring.md)中的方法实现；清单规则见 [Manifest.md](Manifest.md)。

## 取得接口与代码入口

运行 `Tools/Import-GameReferences.ps1 -GameDirectory '<游戏安装目录>'`，再用 `Tools/Build-CodeMod.ps1` 构建项目。引用位于 `References/Managed`，仅供本机使用。工具使用已安装 Player 的 Unity Mono 框架和 API，不把引用 DLL 复制进作品。游戏更新后重新导入、编译并运行验证。

```csharp
using CozyIsland.Modding;

namespace MyMods {
    public sealed class MyMod : ICozyMod {
        public void OnLoad(CozyModContext context) {
            context.Log("Loaded");
            context.OnSceneLoaded(scene => context.Log("Scene: " + scene.name));
            context.AddCleanup(() => context.Log("Cleanup"));
        }
        public void OnUnload() { }
    }
}
```

`ICozyMod` 定义 `void OnLoad(CozyModContext context)` 和 `void OnUnload()`。入口是公开、非抽象、有公开无参构造函数的类，完整类型名填入 `mod.json.code.type`。构造函数只初始化本 Mod 数据；事件、资源和补丁在 `OnLoad` 中建立。

`OnLoad` 可在主菜单执行，此时世界、玩家和管理器可能不存在。只有成功完成 `OnLoad` 的实例才会收到 `OnUnload`。加载到一半抛异常时，上下文仍会执行已登记的清理。开关、顺序或可热切换包变化会重建上下文；同一进程可能多次启停，不能假定只加载一次。加载按依赖和玩家顺序执行，卸载反序执行。更新已加载程序集需完整退出进程，详见 [Reloading.md](Reloading.md)。

## CozyModContext

| 成员 | 契约 |
| --- | --- |
| `Id` / `Title` / `Version` | 此上下文对应的清单身份、标题和版本；`Id` 为小写 UUID，历史作品则保留原 `id` 字符串 |
| `Directory` | Mod 安装目录的绝对路径；本地与工坊位置可能不同 |
| `Root` | 托管的持久 `GameObject`；将自建组件、Canvas 和临时对象放在其下，生命周期结束时一起释放 |
| `IsHost` | 当前 `Mirror.NetworkServer.active` 的值；不是清单声明，世界修改前必须检查实际权限 |
| `IsDisposed` | 上下文是否已释放；异步继续执行前检查，但它不替作者停止线程或任务 |
| `ReadPath(string relativePath)` | 定位 Mod 内已有文件，拒绝路径逃逸、链接与不允许的文件类型 |
| `LoadTexture(string relativePath)` | 读取 PNG/JPEG，单文件最大 8 MiB、最大 4096×4096；释放上下文时销毁纹理 |
| `LoadSprite(string relativePath, float pixelsPerUnit = 100)` | 用上述纹理创建 Sprite，并登记释放 |
| `AddCleanup(Action action)` | 登记逆序执行的清理，用于取消事件、恢复原值和释放自有资源 |
| `OnUpdate(Action<float> callback)` | 每帧回调，参数为 `Time.deltaTime`；异常会报告给加载器并停止失败作品的托管行为 |
| `OnSceneLoaded(Action<Scene> callback)` | 场景加载通知；上下文释放时退订。它不保证联网玩家已生成，也不代替内容注册关口 |
| `OnContentReady(Action callback)` | 在世界内容必须可用前同步准备，可能多次调用，必须幂等；在 `OnLoad` 中登记 |
| `RegisterSaveData(int dataVersion, Func<string> capture, Action<string, int> restore)` | 在 OnLoad 登记本 Mod 的房主世界 JSON 状态；与主存档同版本提交，具体时序、限制与迁移见 [存档契约](Save-Data.md) |
| `Harmony` | 游戏共享 HarmonyX 运行时创建的本 Mod 专属 owner；只应用和撤销自己的补丁 |
| `Log(string message)` | 输出带 Mod ID 的 Player 日志 |

直接订阅原生对象事件时用 `AddCleanup` 登记相反操作。场景对象可能销毁，清理应容许 Unity 对象为空；跨场景重新查找管理器，不能永久保留旧实例。作者自行创建的静态状态、线程、协程、材质替换和外部资源不会自动恢复。

## 内容准备与稳定注册

`OnContentReady` 是在读档反序列化和网络生成前准备模板的同步关口。把登记放进 `OnLoad`，在回调内查找当前场景需要的原生模板/管理器，准备作品固定的类型集合。回调必须完成实际工作后返回；不能启动异步工作后声称准备完成，也不能把注册拖到普通首帧或玩家已经出现以后。

世界运行时类型集合冻结，不能临时注册新类型或新增内容准备回调。玩法可以生成已经注册类型的实例。回调可能在不同准备步骤及重入世界时再次运行；每次先 `FindPrefab` 检查，避免重复克隆。原生配方等场景管理器对象则按当前实例检查，不能因为模板已存在就漏掉新场景的配方接入。

这个关口保证登记时机，不保证所有原生管理器都已执行 `Awake/Start` 或网络启动。模板必须同步准备；配方、HUD、任务等不参与类型登记的工作可以在场景回调或 `OnUpdate` 中等待其真实管理器出现后幂等接入。不要因为此时玩家或配方管理器尚不存在，就把已经有效的模板准备判为失败。

### CozyModRegistry

| 接口 | 契约 |
| --- | --- |
| `GameObject RegisterPrefab(CozyModContext owner, string localId, GameObject source, Action<GameObject> configure = null)` | 从 `source` 建立本 Mod 拥有的非活动模板副本，在副本上执行配置，分配稳定身份并登记归属；不改写源模板 |
| `GameObject FindPrefab(string modId, string localId)` | 查找已登记模板；不存在返回 `null`，用于幂等准备。查自己的作品请传 `context.Id`和已声明依赖之间的引用 |
| `long GetPrefabId(string modId, string localId)` | 按稳定身份取得保存 ID；计算 ID 本身不完成注册 |
| `bool IsTemplate(GameObject value)` | 判断对象是否为底座托管模板；作者扫描场景时排除它们 |

`localId` 是同一个 Mod 内稳定、唯一的类型标识：1–64 个小写字母、数字、点、横线或下划线，首位为字母或数字。Mod ID 与局部 ID 共同决定保存/联网身份，不能用显示名称、加载顺序或运行时随机值替代。跨 Mod 使用模板时声明必要依赖。返回对象是模板，不要把它当作世界实例移动、激活或销毁。

同一 owner、localId 和 source 重复调用返回现有模板，不再次执行 `configure`；相同 ID 换成另一个 source 会报冲突。登记后模板名称与 `CozyObjectIdentifier.objectId` 为 `<Mod ID>:<localId>`，保存和网络身份由底座设置。显示名称/资源名称字段 `objectName` 可在配置中按原生用途设置，不能把显示名与身份字段混用。

`source` 根部必须具备 `ES3Internal.ES3Prefab` 与 `CozyObjectIdentifier`；联网物品还需要一个根部 `Mirror.NetworkIdentity`，不能有子身份、多个身份或已生成实例的 netId。`ES3Prefab.localRefs` 需要已准备的非空引用表，引用对象非空，ID 为正数且唯一，内部引用须能映射到副本。配置后重新校验基础条件；并不机械要求根 GameObject/Transform 都显式出现。配置回调只改本 Mod 的副本，保留底座父对象；不要修改共享材质或源资产，也不要在其中生成世界物体。

保留原版模板的组件与引用，适合做已有模型/行为的变体。新建层级或添加组件时，作者须准备稳定的保存引用并实际冷启动读回。基础静态校验不是所有组件/字段可序列化或引用完整的证明。该游戏 Player 的 `GeneratePrefabReferences()` 不能被当作运行时自动生成完整引用的工具；调用后没有异常也不证明引用有效。详细结构见 [Prefab 指南](Game-and-Prefab-Guide.md)。

底座管理模板归属、稳定身份、ES3/Mirror 注册及上下文释放，不承担食物数值、家具双向引用、配方、角色外观或奖励规则。只有通过注册接口成功登记的定义进入底座保存与缺包协调；直接调用 ES3/Mirror 注册或只计算 ID 不会自动取得这些保证。

## 保存、联网与失败时的生命周期

保存目录来自注册表的成功归属记录，不从玩法 JSON 推断。正常实例仍走游戏现有保存链路，模板本身不作为世界物体保存。作者保持 Mod ID 和局部 ID 稳定，并测试更新后的组件/引用；版本号变化本身不证明兼容。

读档时先准备当前定义，再识别保存目录中确认属于 Mod、当前又缺失的物品。需要移除时，原始文件逐字节备份到岛屿存档目录 `Backups/BeforeModRemoval`，本次用临时副本清理已知世界物体、背包、工具和快捷键引用后继续读取。原存档不改写；后续保存产生清理后的版本。重新安装不能自动补回新版本中已删除的物品；需要使用清理前的存档或备份。未知原版 ID 不应被猜成 Mod 内容删除。

Windows 下若追加备份目录及文件名会使路径过长，备份改存 `Application.persistentDataPath/ModSaveBackups/<原文件 SHA256>.backup`。备份保留原始压缩/加密字节，相邻 `.source.json` 记录原路径；实际位置写入 Player 日志。恢复时将所需备份复制到对应岛屿目录，使用新的 `SaveFile<序号>.es3` 文件名，保留当前存档后再加载；不要直接编辑 `.backup` 内容。

联网托管模板按稳定身份注册。客机缺少房主声明的 Mod 物品时，协调层可跳过相应对象及标准对象引用相关消息，保留房主的数据；这不保证自定义网络组件布局、任意二进制消息或作者状态兼容。客机缺包不能触发房主存档清理。清单旧字段 `multiplayer` 已废弃，详细规则见[清单字段](Manifest.md#字段)。

世界运行中托管回调失败时，加载器停止该作品的回调和补丁，但保留当前实例仍需要的模板、归属和资源，直到退出世界返回菜单后处理清理。作者不要在错误路径自行销毁正在被世界引用的模板；`IsDisposed == false` 也不代表作品未被暂停。菜单中的加载或清理失败会隔离作品，并在不能可靠清理时要求重启。

任务等自定义状态使用 [RegisterSaveData 存档契约](Save-Data.md)，由作者设计 JSON 与迁移；地图结构和网络协议仍需作者设计。底座保证范围不等于所有玩法都已验收；[原生创作验收表](Native-Authoring.md)列出了需要实际执行的检查。

## Harmony

通过 `context.Harmony.Patch(...)` 或仅对自己的补丁类型/程序集 `PatchAll(...)` 应用补丁。owner 为 `cozy.mod.<Mod ID>`，正常停用后由上下文撤销。不要调用全局 `UnpatchAll()`，不要随作品携带另一套 Harmony 或 MonoMod。

[MinimalCodeMod](../Examples/MinimalCodeMod/README.md) 用自身方法证明真实补丁执行；[BuildingHints](../Examples/BuildingHints/README.md) 演示直接修补原生建筑提示 getter。前者验证加载基础，后者依赖特定游戏方法，需要随游戏更新重新验证签名、语义和其他 Mod 冲突。
