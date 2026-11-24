# 游戏结构与原生 Prefab 指南

本文介绍《舒舒服服小岛时光》原生对象的结构。用自己安装的配套游戏程序集查证类型，再参考物品和配方目录查找资源。只做角色皮肤或服装外观可以直接用 GLB。本文的 Unity Prefab 和注册流程适用于原生物品与复杂对象。 [游戏引用](../Tools/README.md#直接编译) · [物品目录](../Reference/GameItems.json) · [配方目录](../Reference/GameRecipes.json) · [GLB 外观制作指南](../Examples/AppearanceFramework/README.md#做自己的外观包)

## 从菜单到世界

主菜单场景为 `MainMenu`，正式世界场景为 `CozyIsland 2.0`。加载器在菜单发现作品，按依赖与玩家顺序创建上下文。此时世界管理器、玩家或骨骼可能不存在。`OnSceneLoaded` 只表示场景加载通知，不保证远端玩家已生成；需要玩家的行为应等待实际对象就绪。

用于读档/网络生成的类型通过 `context.OnContentReady` 同步准备，在世界类型集合冻结前登记。普通观察、HUD 和行为更新可使用 `context.OnUpdate` 或自有组件，不能用首帧延迟注册替代保存与联网的准备关口。写世界前检查实际 `context.IsHost`。

## Prefab 是配合工作的组件与引用

```text
物品根对象（基础模板决定实际组件）
├─ CozyObjectIdentifier       原生资源名与对象身份
├─ ES3Internal.ES3Prefab      保存 Prefab 标识和 localRefs
├─ Mirror.NetworkIdentity    联网物品需要的身份
├─ Rigidbody / Collider      物理与碰撞
├─ CozyBackpackItem           可进入背包的物品
├─ CozyFood                  可食用物品
└─ 子对象 Renderer            模型、材质和视觉
```

这些组件不是每个物体都应该添加的清单。选择与目标行为接近的原版模板，再检查其实际组件与引用。可食用物品的 `CozyFood.powerContain`、背包属性、碰撞和交互相互配合；只画出一个模型不等于物品可以拾取、食用或保存。

`CozyModRegistry.RegisterPrefab` 从源对象建立自有非活动模板，管理身份、归属及 ES3/Mirror 接入。作者通过配置回调修改副本。源根对象需要 `ES3Prefab` 和 `CozyObjectIdentifier`，联网需要合适的根 `NetworkIdentity`。已有 spawned netId、多个身份或子对象 NetworkIdentity 不能被当作合法物品模板带入。

返回的是模板，不是世界实例；不要激活、移动或销毁它。用游戏既有生成/制作链路生成实例。底座排除托管模板的世界保存，作者自己的对象扫描也应使用 `CozyModRegistry.IsTemplate` 排除它们。

## 保存引用必须完整

`ES3Prefab.localRefs` 将模板中的对象/组件关联到局部保存 ID。源对象与配置后的副本都需要已准备的非空引用表，引用对象非空，局部 ID 为正数且唯一。原生模板的表不一定显式包含根 GameObject 和 Transform，不能机械添加这条要求。克隆后内部引用应指向对应副本，不能仍指回原模板内部对象。

复用原版模板时保留原有组件与引用，再核对修改后新增对象的引用。自行制作复杂层级或添加组件时，作者需要稳定的引用准备流程并实际保存、冷启动读回；注册时的静态校验不是所有引用完整或组件均可序列化的证明。稳定 Prefab ID 只能解决类型身份，不能补全组件引用或自定义字段序列化。

当前游戏 Player 的 `ES3Prefab.GeneratePrefabReferences()` 是空实现，不能当作运行时引用生成器。它返回且未抛异常，不表示 `localRefs` 已正确生成。作者可从 Player 程序集确认这一行为，必须检查实际引用并执行保存/冷启动读回测试。

## 食物、家具、蓝图和配方

新食物可以基于目录中确认过的原版食物模板，保留食用和背包行为，在作者代码中配置自有副本的名称、图标、颜色和 `CozyFood.powerContain`。注册不会自动把物品送进背包；作者通过原生配方或房主生成行为提供获取路径。

家具实体与蓝图是两个相关物品。蓝图的 `PCDBuildingPart.buildingPrefab` 指向放置的家具；家具的 `PCDBuilding.buildingPartPrefab` 指回拆除后归还的蓝图。制作家具变体时，需要作者同时准备并关联两者，不能只改颜色而沿用错误的原版蓝图关系。相关对象都要保持稳定局部 ID。

原生 `CozyRecipe` 是 ScriptableObject，包含 `productName`、`productDesc`、`productPrefab`、`workshopName`、`category`、`needUnlock` 和 `demands`。`CozyWorkshopRecipeMgr` 用 `productName` 做查找键，作者名称须避免冲突；添加/移除要对应当前场景管理器。材料 `ResourceDemand.resourceName` 要使用实际资源标识。具体步骤见[原生创作指南](Native-Authoring.md)。

## 自有资源与 AssetBundle

作者纹理可通过 `context.LoadTexture/LoadSprite` 读取；材质覆盖使用自有材质实例或属性块，并在清理时恢复。不要修改原版共享材质，也不要把需要世界继续引用的资源放到错误发生时立即销毁的路径。

运行包允许 `.bundle` / `.assetbundle` 文件。作者可用 `context.ReadPath` 得到路径，再调用 Unity `AssetBundle.LoadFromFile` 与 `LoadAsset<GameObject>`，自行管理依赖和卸载。加载器不扫描 bundle 自动登记物品，没有把任意原始 `.fbx`、`.obj`、`.blend` 或 Unity `.prefab` 文本作为运行时资源导入的承诺。

构建 bundle 需要作者自己的 Unity 工程，匹配实际目标 Player 的 Unity 版本、目标平台和渲染管线；测试 Shader、材质、骨骼、动画和依赖资源。AssetBundle 不含可执行脚本，自定义组件必须来自 Player 或作者 DLL，类型及程序集身份须匹配。自定义 NetworkBehaviour 需要相应 Mirror 代码生成，普通 C# build 不自动完成它。

纯视觉对象可以由作者加载、展示并清理。成为正式游戏物品还需身份、保存引用、所有端网络注册/生成，以及缺失定义处理；自定义保存和网络协议仍由作者负责。本文说明接入条件，不声称已经提供任意复杂 Prefab 或全新地图的完整作者工作流。
