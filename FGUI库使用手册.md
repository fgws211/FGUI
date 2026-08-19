# FGUI 库使用手册

> **当前版本：V2.0** ｜ 更新日志见 `README.md`
>
> 本手册面向团队开发者，帮助快速上手 FGUI 封装库（`FGUI.cs` + `FGUIManager.cs`）。

---

## 一、这个库是干什么的

FGUI 原生的使用流程是：手动 `AddPackage` 加载包 → `CreateObject` 创建界面 → `GRoot.AddChild` 挂载 → `GetChild` 找控件 → 绑事件。每一步都要写，重复且容易错。

这个库把这些流程封装成几个简单方法，你只需要关心两件事：

1. **要打开哪个界面**（传组件名）
2. **要绑哪个按钮**（传界面名 + 按钮路径）

其余（加载包、预创建、缓存、防重复打开）库内部自动处理。

---

## 二、快速开始（三步）

### 第 1 步：把 `FGUI.cs` 放进项目

放到 `Assets/Scripts/` 下任意位置，不用挂到任何 GameObject 上（它是静态类）。

### 第 2 步：初始化（在入口场景的脚本里）

```csharp
public class FGUIManager : MonoBehaviour
{
    private void Awake()
    {
        // 传入预创建名单：开场就要用的界面（组件名，要和 FGUI 编辑器里的名字完全一致） PS:这个名单也可以单独使用一个string[]存储
        FGUI.Awake(new string[]
        {
            "main ui",      // 主界面
            "Game",         // 游戏界面（Start 里要绑按钮）
            "SETTINGS",     // 常用弹窗
        });
    }

    private void Start()
    {
        // 打开主界面
        FGUI.OpenFGUI("main ui");

        // 给 Game 界面的按钮绑定点击事件
        FGUI.BindButton("Game", "n1/n6", OnPlayClick);
    }

    private void OnPlayClick()
    {
        Debug.Log("点击了按钮");
    }
}
```

### 第 3 步：跑起来

`Awake()` 会自动扫描 `Assets/Resources/FGUI/` 目录下的所有包（`.bytes` 文件），加载并预创建名单中的界面。控制台会打印：

```
已加载包: Common 和 Main
预创建完成，共 3 个组件
打开UI: main ui
按钮 'Game/n1/n6' 绑定成功
```

看到这些日志就说明一切正常。

---

## 三、API 速查

### `FGUI.Awake(string[] preCreate = null)`

**初始化库。** 加载 `Resources/FGUI/` 下所有包，并按名单预创建界面。

| 参数        | 说明                                                                |
|-------------|---------------------------------------------------------------------|
| `preCreate` | 预创建名单（组件名数组）。传 `null` 或空数组 = 不预创建，全部懒加载 |

```csharp
FGUI.Awake(new string[] { "main ui", "Game" });   // 预创建这两个
FGUI.Awake();                                     // 不预创建，用到再建
```

> **什么时候用：** 游戏入口场景的 `Awake()` 里，且只调一次。

---

### `GComponent FGUI.OpenFGUI(string resName, bool isPopup = false)`

**打开界面。** 从缓存取界面并挂到屏幕上显示。界面不在缓存会自动创建。

| 参数      | 说明                                               |
|-----------|----------------------------------------------------|
| `resName` | 组件名（如 `"main ui"`、`"Game"`）                 |
| `isPopup` | 是否弹窗。`true` 会压入弹窗栈，`CloseAllUI` 会关它 |

```csharp
FGUI.OpenFGUI("main ui");              // 打开主界面（非弹窗）
FGUI.OpenFGUI("SETTINGS", true);       // 打开设置弹窗
FGUI.OpenFGUI("RewardDialog0", true);  // 打开奖励弹窗
```

> **注意：** 同一界面已在屏幕上时，重复调用会被忽略并打 Warning，不会重复压栈。

---

### `void FGUI.BindButton(string uiName, string btnPath, EventCallback0 callback)`

**给界面里的按钮绑定点击事件。** 界面不在缓存会自动创建。

| 参数       | 说明                                        |
|------------|---------------------------------------------|
| `uiName`   | 界面组件名                                  |
| `btnPath`  | 按钮相对路径，从界面根节点开始，用 `/` 分隔 |
| `callback` | 点击回调（无参数方法）                      |

```csharp
FGUI.BindButton("Game", "n1/n6", OnPlayClick);        // 两级
FGUI.BindButton("Game", "n1/n5/n2", OnCashClick);     // 三级，任意层级都行
```

> **什么时候用：** 界面初始化时就确定要绑的事件，集中写在 `Start()` 里。

---

### `void FGUI.BindSlider(string uiName, string sliderPath, Action<float> onChanged)`

**给界面里的滑条绑定值变化事件。** 拖动滑条时回调，参数是当前值。界面不在缓存会自动创建。

| 参数         | 说明                                                    |
|--------------|---------------------------------------------------------|
| `uiName`     | 界面组件名                                              |
| `sliderPath` | 滑条相对路径，从界面根节点开始，用 `/` 分隔             |
| `onChanged`  | 拖动回调，参数是当前值（`float`，范围取决于滑条的 max） |

```csharp
FGUI.BindSlider("SETTINGS", "n10", (value) =>
{
    audioSource.volume = value / 100f;   // 音量 0~100 → 0~1
});
```

> **什么时候用：** 音量条、亮度条等需要响应拖动数值变化的控件。

---

### `void FGUI.SetController(string uiName, string path, string controllerName, int index)`

**设置界面中某控件的控制器页码。** 用于切换控件状态——星星亮暗、按钮文字、选中态等（原理见《FairyGUI API 速查手册》第十八章 Controller）。界面不在缓存会自动创建。

| 参数             | 说明                                                                          |
|------------------|-------------------------------------------------------------------------------|
| `uiName`         | 界面组件名                                                                    |
| `path`           | 控件相对路径，如 `"n3/n0"`（n3 里的第 1 颗星）。传空字符串 = 直接操作界面本身 |
| `controllerName` | 控制器名，如 `"c1"`（星星）或 `"button"`（按钮状态）                          |
| `index`          | 页码（从 0 起）                                                               |

```csharp
// 设置 reward_pop 里 n3 的第 1 颗星为"亮"（控制器 c1 第 1 页）
FGUI.SetController("reward_pop", "n3/n0", "c1", 1);

// 设置某按钮为"不可领取"状态
FGUI.SetController("daily", "dayBtn", "c2", 0);
```

> **什么时候用：** 奖励领取三态（不可领/可领/已领）、星级评价、Tab 切换等靠 Controller 实现的状态切换。

---

### `void FGUI.LockButtonPage(GButton btn)`

**锁死按钮的状态机。** 用反射把按钮内部的 `_buttonController` 置空，FGUI 不再自动切换按钮的 up/over/down 页面，按钮"死"在初始状态，只能由 `SetController` 手动控制。

| 参数  | 说明                           |
|-------|--------------------------------|
| `btn` | 要锁死的按钮（`GButton` 实例） |

```csharp
// 拿到按钮后锁死（前提：按钮控制器页名不是标准的 up/over/down）
GComponent daily = FGUI.OpenFGUI("daily");
GButton dayBtn = daily.GetChild("dayBtn").asButton;
FGUI.LockButtonPage(dayBtn);
```

> **什么时候用：** 按钮的控制器页名不是标准 `up/over/down` 时（如你们的 `bt_yellow` 用的是 `white/brown/guide`），FGUI 自动切页会切到不存在的页名导致状态错乱——锁死后由你代码全权控制。
>
> **⚠️ 注意：** 这是反射操作 FGUI 内部私有字段，SDK 升级后 `_buttonController` 字段名如果变了此方法会静默失效（不报错），升级 SDK 后注意回归测试。

---

### `void FGUI.CloseUI(GComponent ui)`

**关闭界面。** 从屏幕上移除，**不销毁、不清缓存**，下次 `OpenFGUI` 直接复用。

```csharp
GComponent game = FGUI.OpenFGUI("Game");
FGUI.CloseUI(game);
```

> **注意：** 复用同一个实例意味着界面状态会保留（列表滚动位置、输入框内容等）。需要"重开即重置"的界面，要在打开逻辑里手动重置。

---

### `void FGUI.CloseAllUI(bool clearCache)`

**关闭所有界面**（含弹窗栈里的）。

| 参数         | 说明                                         |
|--------------|----------------------------------------------|
| `clearCache` | 是否同时清空缓存（清空后下次打开会重新创建） |

```csharp
FGUI.CloseAllUI(false);   // 全关，但保留缓存
FGUI.CloseAllUI(true);    // 全关 + 清缓存
```

---

### `void FGUI.Cleanup()`

**彻底清理。** 关所有界面、清缓存、卸载所有包。场景切换时调用，回到登录界面后再 `Awake` 重新初始化即可。

```csharp
FGUI.Cleanup();
```

---

## 四、预创建机制（为什么打开快）

库的加载分三层：

```
Awake()
  ├── 1. 扫描 Resources/FGUI/ → 加载所有 .bytes 包
  ├── 2. 按名单预创建界面（只创建到内存，不显示）
  └── 3. 名单外的界面 → 懒加载（OpenFGUI/BindButton 用时才建）
```

- **预创建**：`Awake` 时把名单里的界面构建好放缓存。打开时直接显示，零等待。
- **懒加载**：名单外的界面第一次打开时才构建（会有一点卡顿，之后走缓存）。
- **缓存**：所有创建过的界面都留在 `uiCache`，关闭只是隐藏，再次打开秒开。

**怎么选名单：** 开场就要显示 / 要提前绑按钮的界面放名单里；低频弹窗不放，等用到再建。

---

## 五、常见使用模式

### 模式 1：打开主界面

```csharp
FGUI.OpenFGUI("main ui");
```

### 模式 2：打开弹窗 + 点关闭

```csharp
// 打开
GComponent dlg = FGUI.OpenFGUI("SETTINGS", true);

// 弹窗里关闭按钮的绑定（假设按钮路径是 closeBtn）
FGUI.BindButton("SETTINGS", "closeBtn", () =>
{
    FGUI.CloseUI(dlg);
});
```

### 模式 3：多个界面切换

```csharp
FGUI.CloseAllUI(false);          // 全部关闭（保留缓存）
FGUI.OpenFGUI("Game");           // 开下一个
```

### 模式 4：绑定后再打开（事件不丢）

`BindButton` 提前创建并绑事件，之后 `OpenFGUI` 打开的是**同一个实例**，事件不会丢：

```csharp
// Start 里
FGUI.BindButton("Game", "n1/n6", OnPlayClick);   // 此时 Game 被创建并缓存（不显示）

// 玩家点开始按钮后
FGUI.OpenFGUI("Game");                           // 显示的就是绑好事件的同一个 Game
```

### 模式 5：音量滑条

```csharp
FGUI.BindSlider("SETTINGS", "n10", (value) =>
{
    AudioManager.SetVolume(value / 100f);
});
```

### 模式 6：奖励领取三态（Controller 切换）

```csharp
// 未领取 → 可领取
FGUI.SetController("daily", "dayBtn", "c2", 1);   // 发光效果
FGUI.SetController("daily", "dayBtn", "c3", 0);

// 领取完成 → 显示遮罩+打勾
FGUI.SetController("daily", "dayBtn", "c2", 0);
FGUI.SetController("daily", "dayBtn", "c3", 1);
```

### 模式 7：非标准页名按钮锁死

```csharp
// bt_yellow 这种按钮控制器页名是 white/brown/guide，FGUI 自动切页会错乱：
GButton btn = FGUI.OpenFGUI("Game").GetChild("n8").asButton;
FGUI.LockButtonPage(btn);                       // 锁死，禁止自动切页
// 之后用 SetController 手动控制
```

---

## 六、注意事项（坑）

### 1. 组件名必须和 FGUI 编辑器完全一致

`"main ui"` 带空格、`"SETTINGS"` 全大写——这些是美术在编辑器里设置的组件**名称**，不是文件名、不是标题。写错一个字符就找不到。

### 2. 界面名不能重复

缓存 key 是纯组件名。如果两个包里有同名组件（比如都有 `Dialog`），后创建的那个会覆盖先创建的。团队约定：**界面组件名全局唯一**。

### 3. 关闭 ≠ 销毁

`CloseUI` 只隐藏不销毁。界面状态会保留。想要"重开重置"的界面需要自己写重置逻辑。

### 4. 包文件命名规范

包文件必须是 `xxx_fui.bytes` 放在 `Resources/FGUI/` 下。库通过 `_fui` 后缀识别包文件，其他文件会被跳过。

### 5. 图集命名

图集文件（`xxx_atlas0.png` 等）必须和包放同一目录，且 FGUI 发布时包名、发布名要规范（参考 FGUI 项目规范），否则图集加载不出来，界面会白屏。

---

## 七、代码结构

```
FGUI.cs（静态类）
├── 字段
│   ├── uiCache        UI缓存：组件名 → 界面
│   ├── loadedPkgs     已加载的包实例
│   ├── FGUIStack      弹窗栈（只有 isPopup=true 的界面入栈）
│   ├── PreCreateList  预创建名单
│   └── isAwake        初始化标记
├── Awake()            初始化：加载包 + 预创建
├── PreCreateAll()     按名单预创建
├── OpenFGUI()         打开界面（查缓存 → 无则创建 → 挂载显示）
├── BindButton()       绑定按钮（查缓存 → 无则创建 → 递归找按钮）
├── BindSlider()       绑定滑条值变化（回调当前值）
├── SetController()    设置控件控制器页码（状态切换）
├── LockButtonPage()   锁死按钮状态机（反射，非标准页名按钮用）
├── CreateUI()         动态创建界面（遍历所有包查找）
├── CloseUI()          关闭界面
├── CloseAllPopups()   关闭所有弹窗
├── CloseAllUI()       关闭所有界面
└── Cleanup()          彻底清理
```

---

## 八、排查指南

| 现象                                       | 原因                                   | 解决                                                                          |
|--------------------------------------------|----------------------------------------|-------------------------------------------------------------------------------|
| `没有找到任何FGUI包`                       | `Resources/FGUI/` 下没有 `.bytes` 文件 | 检查包是否导出到正确目录                                                      |
| `创建界面失败: 所有包中都找不到组件 'xxx'` | 组件名写错，或该组件不在任何包里       | 去 FGUI 编辑器确认组件名称（库已先按 `GetItemByName` 预检查，不会再满屏报错） |
| `界面 'xxx' 已在屏幕上，忽略重复打开`      | 同一个界面调了两次 `OpenFGUI`          | 业务逻辑检查，或先 `CloseUI` 再开                                             |
| 界面打开后白屏/图片丢失                    | 图集加载失败（命名或目录问题）         | 检查图集文件与包同目录、命名规范                                              |
| 按钮点了没反应                             | 按钮路径写错，或回调没绑上             | 看控制台有没有 `按钮路径 '...' 中 '...' 不存在`                               |
| `'xxx/yyy' 不是 GSlider 类型`              | `BindSlider` 的路径指向的不是滑条      | 确认路径指向 `GSlider` 控件                                                   |
| `'xxx/yyy' 没有控制器 'c1'`                | `SetController` 的控制器名写错         | 去 FGUI 编辑器确认控件的控制器名称                                            |
