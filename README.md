# FGUI 库更新日志（README）

---

## V2.0（当前版本）

> 本次更新：新增按钮状态锁死 + 打开流程优化

### 新增功能

| 方法                          | 作用                                                                                                                                                                                            |
|-------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `LockButtonPage(GButton btn)` | 锁死按钮状态机。用反射置空 GButton 内部 `_buttonController`，FGUI 不再自动切换 up/over/down 页面，按钮状态全权交给代码控制（用于控制器页名非标准的按钮，如 `bt_yellow` 的 `white/brown/guide`） |

### 优化

- **`OpenFGUI` 挂载顺序调整**：`MakeFullScreen()` 移到 `AddChild` 之前——先设好尺寸再挂载，避免挂载瞬间用旧尺寸渲染闪烁

### 使用示例

```csharp
// 拿到按钮后锁死（前提：按钮控制器页名不是标准的 up/over/down）
GComponent game = FGUI.OpenFGUI("Game");
GButton n8 = game.GetChild("n8").asButton;
FGUI.LockButtonPage(n8);
// 之后用 SetController 手动控制按钮状态
FGUI.SetController("Game", "n8", "c1", 1);
```

### ⚠️ 已知风险

- `LockButtonPage` 是反射操作 FGUI 内部私有字段，SDK 升级后 `_buttonController` 字段名若变化会静默失效（不报错），升级 SDK 后需回归测试

---

## v1.0.0（初始版本）

> 库的第一版，搭建了完整的框架。

### 核心功能

| 方法                                    | 作用                                                              |
|-----------------------------------------|-------------------------------------------------------------------|
| `Awake(string[] preCreate)`             | 初始化：自动扫描 `Resources/FGUI/` 加载所有包，并按名单预创建界面 |
| `OpenFGUI(resName, isPopup)`            | 打开界面：查缓存 → 无则自动创建 → 挂载渲染，防重复打开            |
| `BindButton(uiName, btnPath, callback)` | 绑定按钮点击事件，支持任意层级路径，界面不存在自动创建            |
| `CloseUI(ui)`                           | 关闭界面（不销毁，缓存复用）                                      |
| `CloseAllUI(clearCache)`                | 关闭所有界面，可选清空缓存                                        |
| `Cleanup()`                             | 彻底清理：卸载所有包、重置状态                                    |

### 设计特性

- **自动加载包**：`Resources.LoadAll` 扫描目录，新增包零代码接入
- **预创建缓存**：白名单界面 `Awake` 时提前构建，打开秒开；名单外界面懒加载
- **缓存复用**：关闭不等于销毁，界面状态保留，再次打开秒开
- **弹窗栈**：`isPopup = true` 的界面入栈，`CloseAllUI` 统一关闭

### 说明

- 包文件命名规范：`xxx_fui.bytes` 放在 `Assets/Resources/FGUI/` 下
- 界面组件名需与 FGUI 编辑器中的名称完全一致（如 `"main ui"` 带空格）

---

## v1.1.0

> 本次更新：新增两个常用绑定方法 + 创建流程优化

### 新增功能

| 方法                                                 | 作用                                                                                           |
|------------------------------------------------------|------------------------------------------------------------------------------------------------|
| `BindSlider(uiName, sliderPath, onChanged)`          | 给滑条绑定值变化事件，拖动时回调当前值（`Action<float>`），支持层级路径                        |
| `SetController(uiName, path, controllerName, index)` | 设置界面中某控件的控制器页码，一行代码切换状态（星星亮暗、奖励三态、按钮文字等），支持层级路径 |

### 优化

- **`CreateUI` 增加 `GetItemByName` 预检查**：创建界面前先确认组件存在于包中，不再遍历所有包触发满屏 `resource not found` 报错
- **`BindButton` 补充初始化保护**：未调用 `Awake()` 时自动初始化，与其他入口方法行为一致

### 使用示例

```csharp
// 音量滑条
FGUI.BindSlider("SETTINGS", "n10", (value) =>
{
    audioSource.volume = value / 100f;
});

// 奖励领取三态切换（bt_day1 组件：c2 管"能否领取"，c3 管"是否已领"）
FGUI.SetController("daily", "dayBtn", "c2", 1);   // → 发光可领取
FGUI.SetController("daily", "dayBtn", "c3", 1);   // → 打勾已领取
```
