# 直接使用原生 API 制作 Mod

本文面向拿到《舒舒服服小岛时光》的配套完整 Mono Build 和本创作包的作者。作者不需要游戏源码工程。目标是在自己的 DLL 中实现玩法，通过加载底座管理生命周期与归属；不预设每一种游戏行为都需要额外 SDK 包装。

## 查证可用类型

先运行 `Tools/Import-GameReferences.ps1`，从自己的 Player 导入 `References/Managed`。在 IDE 对象浏览器、程序集反编译工具或反射中查看实际类型、程序集、可见性、方法参数、返回值与事件签名。游戏类型通常位于 `Assembly-CSharp.dll`，其他类型以导入的真实程序集为准。导入的 DLL 不得随作品或工具包分发。

[GameItems.json](../Reference/GameItems.json) 和 [GameRecipes.json](../Reference/GameRecipes.json) 是带 `gameVersion`、`apiVersion` 与 `generatedUtc` 的目录快照。它们是精选条目，不覆盖场景内全部登记对象。使用下面的只读示例在实际 Player 核对。物品用 `prefabId` 取得 ES3 登记模板，`prefabName` 是对象名，不能当作 `Resources.Load` 路径。`objectId` 允许为空，显示名和 Prefab 名可能重名。配方的 `name` 对应 `CozyRecipe.productName`，`product` 对应产品 Prefab 名。`ingredients[].item` 对应 `ResourceDemand.resourceName`，由原生材料组件提供，不能用物品的 `objectId` 代替。

类型存在只证明可以引用，不能证明调用时机、权限、保存或多人语义。记录所用 Build 版本及引用哈希清单；游戏更新后重新导入和验证。`CozyIsland.Modding` 的底座接口承担通用生命周期和归属，直接调用的原生游戏实现可能变化。

## 核对一个原生模板与配方

用 `New-CodeMod.ps1` 建一个本地测试工程，把下面的代码替换 `Source/ModEntry.cs`。将清单中的 `code.type` 改为 `InspectWorkbench`，保留原 `uuid` 和 `code.assembly`。编译、安装并启用后进入测试岛。此例只读取现有模板和配方，输出到 Player 日志。

```csharp
using CozyIsland.Modding;
using ES3Internal;
using UnityEngine;

public sealed class InspectWorkbench : ICozyMod
{
    public void OnLoad(CozyModContext context)
    {
        // GameItems.json 中的基础工作台蓝图。
        const long blueprintId = 3112758868L;
        context.OnContentReady(() =>
        {
            var references = ES3ReferenceMgrBase.Current;
            if (references == null) return;
            var prefab = references.GetPrefab(blueprintId, true);
            if (prefab != null)
                context.Log($"模板 {blueprintId} = {prefab.gameObject.name}");
        });

        // 配方管理器可能晚于模板准备才就绪。
        bool reported = false;
        context.OnUpdate(_ =>
        {
            if (reported) return;
            var manager = Object.FindObjectOfType<CozyWorkshopRecipeMgr>();
            if (manager == null) return;
            reported = true;
            var recipe = manager.GetRecipeByName("基础工作台");
            if (recipe == null)
            {
                context.Log("当前场景未找到基础工作台配方");
                return;
            }
            context.Log($"产品 = {recipe.productPrefab?.name}");
            foreach (var demand in recipe.demands)
                context.Log($"材料 {demand.resourceName} × {demand.amount}");
        });
    }

    public void OnUnload() { }
}
```

## 从已有例子开始

| 需求 | 可检查的原生类型和成员 | 随包源码 |
| --- | --- | --- |
| 在建筑交互文字后追加名称 | `PCDBuilding.interactDesc` getter、`buildingName`，`EasyLocalText.Localized` | [BuildingHints](../Examples/BuildingHints/Source/BuildingHints.cs) |
| 调整房主农田生长和耗水 | `CozyFallowLand.Update`、`isGrowing`、`growTimeCount`、`waterNum` | [GreenThumb](../Examples/GreenThumb/Source/GreenThumb.cs) |
| 暂时修改移动字段并恢复 | `CozyPlayer`、`PCDMovementMgr.walkSpeed`、`CharacterManager.sprintSpeed` | [BriskWalk](../Examples/BriskWalk/Source/BriskWalk.cs) |
| 新物品、原生配方与食用事件 | `CozyWorkshopRecipeMgr.AddRecipe/RemoveRecipe`、`CozyRecipe`、`EasyEvent` 的 `EatFood` | [TrailTreat](../Examples/TrailTreat/README.md) 与下一节制作路线 |
| 读玩家、监听服务端事件 | `CozyPlayer`、`PlayerJoinQuitMgr` 的公开成员 | 先在当前 Player 中确认类型和事件，容许对象尚未就绪 |

这些名字是查证入口，不是所有修改自动稳定的保证。BuildingHints 不调用加载器的玩法专用函数，而是直接 patch 原生 getter；GreenThumb 的数值规则写在作者 DLL 中；BriskWalk 记录原值并在停用时恢复。加载器不需要新增建筑提示、农田倍率或步行倍率字段。

先取得实际场景对象再执行逻辑，场景加载完成不保证联网玩家已经生成。写世界前检查 `context.IsHost`。少量对象可以周期性查找，大型 Mod 应按场景管理缓存和退订，扫描时用 `CozyModRegistry.IsTemplate` 排除托管模板。

## 新物品与原生配方的制作路线

新增物品涉及原生组件、稳定身份与资源所有权。先读 [Prefab 指南](Game-and-Prefab-Guide.md) 和[基础注册契约](API.md)，再在作者代码中执行：

1. 在 `OnLoad` 登记 `context.OnContentReady`。回调内通过实际 Player 的资源/保存引用管理器取得正确源模板；核对目录 ID 和组件，不把世界中随机找到的实例当成原版模板。
2. 使用稳定的局部 ID，例如 `berry-toast`。先 `CozyModRegistry.FindPrefab(context.Id, localId)`；没有时调用 `RegisterPrefab(context, localId, source, configure)`。`configure` 修改自有副本的名称、食物字段、图标或其他原生组件，不改源资产。
3. 为当前场景查找 `CozyWorkshopRecipeMgr`，构造自己的 `CozyRecipe`。设置唯一的 `productName`、真实的 `productPrefab`、`workshopName`、`category`、`needUnlock` 和 `demands`。材料使用实际资源标识；`ResourceDemand` 包含 `resourceName` 和 `amount`。不要凭显示名假设材料键。
4. 检查 `AddRecipe` 的返回值。重入世界时针对新管理器重新接入，幂等检查不能只依赖上一个场景的布尔标记。清理只 `RemoveRecipe` 自己成功添加的对象，再销毁自有 `ScriptableObject`，不要清空全局配方表或删除其他作者的同名对象。
5. 由原生制作/生成链路创建已注册模板的实例。底座不负责提供“发奖励”“找农田”或“设置食物数值”的便捷函数；按当前游戏原生路径实现并验证房主权限。

同一个 context、localId 和 source 重复登记会返回已有模板，不重复执行 `configure`。修改配置不应依赖重复登记偷偷改写正在使用的模板。世界运行中不能添加新类型；准备工作必须在同步关口完成。模板拥有独立身份不代表自定义组件字段已经获得序列化支持。

把模板准备和场景接入分开：`OnContentReady` 同步登记固定的类型集合；`OnUpdate` 可等待 `CozyWorkshopRecipeMgr` 实例出现，再向当前实例添加已经准备好产品的配方。管理器更换时清理旧引用并重新接入；`GetRecipeByName` 若返回另一作者对象，应报告名称冲突，不覆盖它。不能因配方管理器尚未出现而异步推迟模板登记，也不能在世界冻结后由配方回调补登记新类型。

[TrailTreat](../Examples/TrailTreat/README.md) 提供这条路线的完整作者源码。1.1.0 曾在较早的 Windows Player 中执行原生制作、拾取、食用、A/B 存档冷读、停用清理和 Steam 下载版回测；使用外部测试驱动与本机网络回环的范围、尚未执行的手工/远程验收见其 README。它保留原生物理/食物组件，视觉由新贴图和程序化 Mesh 实现，不代表任意 AssetBundle 或自定义网络组件均已验收。任务进度通过 [基础存档契约](Save-Data.md) 随主档提交与回滚；作者仍负责状态迁移和客机同步。遇到无法取得正确模板、准备引用或确认生命周期的阻塞，应记录具体证据，不能用猜测 API 或修改内核绕过。

## 视觉、模型与复杂对象

现有原生 Renderer、材质、角色组件和 Unity API 可以由作者代码使用。替换材质时保留原值，只修改自有材质实例或属性块，清理时恢复；纹理可用 `context.LoadTexture/LoadSprite` 管理。纯视觉对象放在 `context.Root` 下不会自动成为可保存、可联网的物品。

新模型、骨骼、动画或 Prefab 可由作者自己的 Unity 工程构建 AssetBundle，然后在代码中读取；这需要与 Player 的 Unity 版本、平台和渲染管线匹配。原始模型文件和 `.prefab` 文本不是运行时导入方案。允许上传 `.bundle` 不意味着已经注册内容，详见 [Prefab 指南](Game-and-Prefab-Guide.md)。

[ResearchCamp](../Examples/ResearchCamp/README.md) 提供可独立重建的 Unity 资源工程：原创视觉 Prefab/Shader → AssetBundle → 作者 DLL 加载 → 附加到已登记的原生行为模板 → 完整退出后读取世界。它已验证同版本的多节点视觉、保存和本机客机生成，没有验证骨骼、动画、自定义 NetworkBehaviour 或任意场景加载。示例使用固定遍历顺序分配局部引用 ID；改变模型层级时必须维护旧 ID 映射并回测旧档，不能以一次冷读成功推断跨版本兼容。

制作成功事件通过 Harmony 观察原生方法，食用使用原生 `EatFood` 事件，营地交互保留原生工作台行为；进度、奖励、HUD 与网络协议全部位于作者 DLL。普通消息的手工序列化与缺包边界见 [作者网络消息](Networking.md)。

## 验收与证据

| 范围 | 实际检查 | 不能替代它的结果 |
| --- | --- | --- |
| 加载与补丁 | 冷启动启用，确认预期日志/效果；同进程停用与重新启用无重复状态 | 仅有 DLL 或列表勾选 |
| 原生玩法 | 固定场景、输入和配置，对比启用/停用效果，确认原值恢复 | 类型存在或编译成功 |
| 生命周期 | 退出世界重入，切换场景，回菜单停用；回调、配方和资源没有残留或重复 | 一次会话运行正常 |
| 已登记新物品 | 获取/制作、背包、使用或放置，保存后完整退出并读回 | 只生成一个可见物体 |
| 兼容更新 | 保持 Mod/局部 ID，混存原版与 Mod 物品，升级后数量/位置/引用保留 | 只改版本号或只比较 ID |
| 缺失定义 | 停用或移除定义后读档，核对备份、清理范围及原版内容，再保存和冷启动读回 | 原文件未改写但没有检查后续读档 |
| 多人 | 房主、远程客机同版本、晚加入、缺包、重连；房主数据不因客机缺包改变 | 单进程 Host 或静态配置信息 |
| 发布 | 本次上传成功回执；关闭本地副本后订阅下载并重复功能测试 | 非零工坊 ID 或本地目录运行正常 |

只报告实际执行的项目。未执行的平台、远程客机、存档、上传和订阅回测要明确标为未验证。测试日志、截图和 Build/作品版本应能定位到同一次运行；不要把早期版本结果当成新版本证据。

自动化测试工具调用原生方法不等同于玩家完成界面操作。分别记录作品本身和测试工具依赖的原生/私有接口：为驱动测试而调用私有入口，不能直接推导为正常玩法需要新增 SDK。切换世界、开始联机等测试仍须完成原生入口的前置条件；测试工具未清理旧会话造成的失败，应与底座缺陷区分。

## 独立作者实验的边界

用于检验创作能力的独立作者只接收完整游戏 Build、本工具包与范例，不接收游戏 Unity 工程。固定 Build 与指南版本后，作者只编辑自己的作品，使用 Player 程序集和随包目录查证接口。

遇到阻塞时记录：目标行为、尝试过的原生路径、实际错误/缺失证据，以及最小能力缺口。不要为了完成示例自行修改加载器、增加玩法包装 API 或依赖未提供的私有文件。若确认是底座缺陷，由维护者单独修复并记录版本，再重新执行实验；是否需要额外 SDK，依据真实重复出现的缺口决定。
