# Mod 数据与世界存档

本文适用于本仓库配套的 Mono 游戏版本。编译时使用配套游戏提供的程序集。作品的 `schemaVersion` 为 1。下文的数据版本是作者为自己的存档数据维护的整数。 [编译方法](../Tools/README.md#直接编译)

保存归属使用清单的 `uuid`，`context.Id` 返回这个永久身份。物品与作者数据都依赖它，更新作品时保留原值。

## 三种数据分别放在哪里

| 数据 | 位置、写入者与更新规则 | 是否随世界回滚或联网 |
| --- | --- | --- |
| 模型、纹理、默认配置等作品资源 | `context.Directory` 内，由作者打包，运行时按只读处理；Steam 更新可以替换文件 | 不随世界回滚，也不自动同步运行状态 |
| 用户跨存档设置，例如 HUD 大小 | 作者在 `Application.persistentDataPath` 下按自己的 Mod UUID 隔离文件；不要写入 Steam 安装目录，也不要修改加载器的 `mods-<SteamID>.json` | 不随世界回滚；作者决定是否有联网需求 |
| 任务进度、已领取奖励等世界状态 | 房主通过 `RegisterSaveData` 提交 JSON，游戏写进该版本的 ES3 主存档 `CozyModState` 区域 | 与选中的世界版本一起读取和回滚；不自动发给客机 |

玩家的加载器启停、顺序和发布关联与上述作者数据不同，见 [模组账号设置](Workshop.md#账号设置)。不要把任务进度写成每次事件立即覆盖的岛屿侧文件：它不能与世界原子提交，加载较旧世界时也不会回滚。

## 原生世界存档位置与测试隔离

原生世界存档由 `SaveFileMgrV2` 写入 `Application.dataPath/SaveFiles/<岛屿目录>/SaveFile<序号>.es3`。Windows 通常位于 `<游戏目录>/CozyIsland_Data/SaveFiles`，macOS 通常位于 `CozyIsland.app/Contents/SaveFiles`。启动不同 Build 时以实际目录为准。相同岛名可能生成带数字后缀的新目录；以实际生成目录和所选存档为准。这个路径属于原生实现，游戏更新后应重新查证，作者不应把路径写死到玩法代码中；世界任务数据仍通过 `RegisterSaveData` 交给游戏保存。

测试前分别核对两处数据：

- **世界数据：** 使用独立测试岛，备份已有岛屿目录；保存 A/B、冷读、卸载回测都记录具体 `.es3` 路径。运行完整 Build 会在其数据目录内新增存档，不能把“输入程序集不修改”误写成“游戏目录不会产生任何文件”。
- **用户数据：** 本地 Mod、加载器选择和作者偏好位于该 Player 的 `Application.persistentDataPath`。仅复制游戏到另一个文件夹，通常仍使用同一产品的用户数据目录，不会自动隔离启停状态或用户偏好。先备份相关配置，测试结束后恢复自己的改动；不要删除其他作品或覆盖玩家原配置。

对冻结 Build 做完整性检查时，分别记录原有程序/资源文件的哈希与运行中新建的存档，不把两者混为一项。测试工具若另行设置存档目录，应记录这是工具的测试条件，不能据此声称默认游戏路径或界面流程已验证。

## 登记与回调

在 `ICozyMod.OnLoad` 中调用一次：

```csharp
context.RegisterSaveData(
    dataVersion: 2,
    capture: CaptureJson,
    restore: RestoreJson);
```

每个 Mod UUID 只能有一个参与者。需要多个任务时，在自己的 JSON 内组织对象；不要为每个任务登记新的伪 Mod。登记必须在进入世界之前完成，不能在保存回调中追加登记。

- `restore(string json, int savedVersion)` 在房主准备新世界或读选中版本时同步调用。新世界、旧存档没有这个 Mod 的数据时为 `null, 0`；有数据时返回保存时的版本。回调负责解析、校验与迁移，成功后再替换内存状态。它可能在网络启动之前执行，不要因为 `context.IsHost` 此时为 false 而跳过恢复。也不要在回调内生成物品、发奖励或要求玩家已经生成。
- `capture()` 在房主正常保存路径同步调用，返回当前状态的有效 JSON。它是快照函数，不应发奖励、改世界、再调用保存或写入其他文件。作者捕获、序列化、完整文件写入和发布成功后，整个版本才会成为正式档。原生保存回调按组件隔离错误。某个原生回调报错会记录后继续，仍可能成功生成正式档，因此要根据日志和冷读结果核对相关对象。
- Mod 因世界运行故障被暂停时，底座不会调用其 `capture`，而是沿用本世界上次成功读入或正式提交的记录。此前没有记录时，本次可以没有该 Mod 的状态。
- `dataVersion` 是本 Mod 当前输出格式的正整数。读取旧版本后迁移为当前内存结构；下一次正常保存才写入新版本。不要把未知将来版本静默重置为空，以免降级丢数据。
- 单 Mod JSON 最多 4 MiB，整个状态区最多 32 MiB、最多 1024 个 owner。这是状态快照，不适合塞模型、贴图或巨量地图资源。所有回调在主线程同步执行，不返回未完成的异步任务。
- 上下文清理会撤销登记；作者仍须清理自己订阅的原生事件、静态状态及外部工作。返回菜单后，再进入另一个世界会重新恢复那个世界的数据。

一个最小迁移例子：

```csharp
[Serializable] class ProgressV1 { public int eaten; public bool rewarded; }
[Serializable] class ProgressV2 { public int biscuitsEaten; public bool rewardClaimed; }
ProgressV2 current = new ProgressV2();

string CaptureJson() => JsonUtility.ToJson(current);

void RestoreJson(string json, int version) {
    ProgressV2 next;
    if (version == 0 && json == null) next = new ProgressV2();
    else if (version == 1) {
        var old = JsonUtility.FromJson<ProgressV1>(json);
        if (old == null) throw new InvalidDataException("Missing progress");
        next = new ProgressV2 { biscuitsEaten = old.eaten, rewardClaimed = old.rewarded };
    } else if (version == 2) next = JsonUtility.FromJson<ProgressV2>(json);
    else throw new InvalidDataException("Unsupported progress version: " + version);
    if (next == null || next.biscuitsEaten < 0) throw new InvalidDataException("Invalid progress");
    current = next;
}
```

导入 `System`、`System.IO`、`UnityEngine`，在自己的入口类中使用这些成员。完整物品、配方、原生事件和迁移源码见 [TrailTreat](../Examples/TrailTreat/README.md)。此例没有自动导入旧侧文件：旧侧文件没有主存档版本，不能可靠判断它属于哪一次保存。

## 缺失内容与失败是不同情况

**主动停用或卸载：** 已登记物品沿用 [API 保存兼容规则](API.md#保存联网与失败时的生命周期)：清理缺失定义的已知对象和背包引用，保留原始文件，下一次正常保存生成新版本。该 Mod 的自定义 JSON 则不透明保留；游戏不加载缺失 DLL、不解析作者业务对象。重新启用同一个 Mod UUID 后，可以读到这些状态。已被新存档删除的物品不会因此重新出现。

**仍启用但未就绪：** 已启用作品的更新、清单、初始化或依赖问题会挡住进入世界。Steam 加入可以静默等待[仍在进行的准备](Networking.md#进入世界前的模组准备)。这些问题不会被当成卸载。客机从未安装房主某个作品时，仍可按缺包容错规则进入。

**作者状态恢复、版本迁移、Mod 物件恢复或整档反序列化失败：** 游戏判定读档失败，阻止后续保存。个别原版物件生成或原版组件恢复出错时，游戏可能只记录错误并继续进岛，之后仍可保存。请检查相关对象的实际状态和 Player 日志。**作者捕获、序列化或写盘失败：** 不发布新版本，已存在存档保持原样。查看 Player 日志定位具体 Mod/组件。任意代码自行写文件或绕过这些入口，不受此事务保护。

房主退出或返回菜单时，若保存失败，可选择「留在岛上」重试，或明确放弃本次进度并退出。若日志明确报告完整写入后仅最后的正式文件发布失败，日志会指出同目录的 `~*.tmp`。退出游戏并备份岛屿目录后，将日志所指的临时文件复制为该岛目录下未被使用的 `SaveFile<序号>.es3`，再从保存列表读回核对。不要覆盖已有档，也不要尝试恢复没有这条日志的中间文件。

## 联网责任

状态属于房主世界，不是每位玩家独立文件，也不自动同步给客机。需要每玩家进度时，由作者选择稳定玩家身份作为 JSON 键；需要客机 HUD 时，由作者使用兼容的网络消息或已有原生状态同步。所有世界修改和奖励仍检查实际 `context.IsHost`；客机不能代替房主保存进度。

## 作者验收

用测试岛验证：新建初值 → 推进并保存 A → 再推进并保存 B → 完全退出 → 分别读取 A/B → 检查世界物品与进度一致。另测数据版本迁移、未知版本拒绝、捕获/恢复故障、停用后读取另存再重装，以及同包/缺包远程客机。仅编译或校验成功不代表这些路径已通过。
