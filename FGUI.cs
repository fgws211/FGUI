using System.Collections.Generic;
using System.Text;
using FairyGUI;
using UnityEngine;

public static class FGUI
{
    private static Dictionary<string, GComponent> uiCache = new Dictionary<string, GComponent>();               // UI缓存：组件名 → 界面
    private static List<UIPackage> loadedPkgs = new List<UIPackage>();                                          // 已加载的包实例（仅枚举用）
    private static List<GComponent> FGUIStack = new List<GComponent>();                                         // 弹窗栈（主界面不入栈）
    private static string[] PreCreateList = new string[0];                                                      // 预创建名单

    private static bool isAwake = false;
    
    
    
    #region 初始化
    
    /// <summary>
    /// 初始化FGUI的UI包
    /// </summary>
    public static void Awake(string[] preCreate = null)  
    {
        TextAsset[] packages = Resources.LoadAll<TextAsset>("FGUI");
        StringBuilder packages_Debug = new StringBuilder();
        PreCreateList = preCreate ?? new string[0];
        
        loadedPkgs.Clear();
        uiCache.Clear();
        
        
        foreach (TextAsset pkg in packages)
        {
            if (!pkg.name.EndsWith("_fui")) continue;
            
            string publishName = pkg.name.Substring(0, pkg.name.Length - 4);
            UIPackage uiPkg = UIPackage.AddPackage("FGUI/" + publishName);
            if (uiPkg != null) loadedPkgs.Add(uiPkg);
            
            if (packages_Debug.Length > 0)
                packages_Debug.Append(" 和 ");
            packages_Debug.Append(publishName);

        }
        if (packages_Debug.Length > 0)
        {
            isAwake = true;
            PreCreateAll();
            Debug.Log($"已加载包: {packages_Debug}");
        }
        else
        {
            Debug.LogWarning("没有找到任何FGUI包");
        }
    }
    
    /// <summary>
    /// 预创建所有包内组件，按组件名存入uiCache（只创建不渲染）
    /// </summary>
    private static void PreCreateAll()
    {
        int count = 0;
        foreach (string resName in PreCreateList)
        {
            if (string.IsNullOrEmpty(resName)) continue;
            if (uiCache.ContainsKey(resName)) continue;     // 已存在跳过

            GComponent ui = CreateUI(resName);              // 复用 CreateUI：遍历包自动找
            if (ui != null) count++;
        }
        Debug.Log($"预创建完成，共 {count} 个组件");
    }
    
    #endregion
    
    
    
    #region UI管理
    
    /// <summary>
    /// 打开界面：传组件名，从uiCache检索后渲染
    /// </summary>
    /// <param name="resName">组件名，如 "main ui"、"Game"</param>
    /// <param name="isPopup">是否弹窗（true 时压入弹窗栈，CloseAllPopups 会关它）</param>
    public static GComponent OpenFGUI(string resName,bool isPopup)
    {
        if (!isAwake)
            Awake();  
        
        if (!uiCache.TryGetValue(resName, out GComponent ui))
        {
            ui = CreateUI(resName);                         // ② 未创建 → 自动创建
            if (ui == null) return null;
        }

        if (ui.parent != null)
        {
            Debug.LogWarning($"界面 '{resName}' 已在屏幕上，忽略重复打开");
            return ui;
        }

        GRoot.inst.AddChild(ui);
        ui.MakeFullScreen();
        if (isPopup) FGUIStack.Add(ui);     // 只有弹窗进栈
        Debug.Log($"打开UI: {resName}" + (isPopup ? "（弹窗）" : ""));
        return ui;
    }
    
    /// <summary>
    /// 隐藏主UI
    /// </summary>
    public static void CloseUI(GComponent FGUI)
    {
        if (FGUI == null || FGUI.parent == null) return;
        
        Debug.Log($"关闭弹窗: {FGUI.name}");
        FGUI.parent.RemoveChild(FGUI);
        FGUIStack.Remove(FGUI);
    }
    
    /// <summary>
    /// 隐藏所有弹窗
    /// </summary>
    private static void CloseAllPopups()
    {
        while (FGUIStack.Count > 0)
        {
            CloseUI(FGUIStack[FGUIStack.Count - 1]);
        }
    }
    
    /// <summary>
    /// 给界面里的按钮绑定点击事件：界面名 + 按钮相对路径
    /// </summary>
    /// <param name="uiName">界面组件名，如 "Game"</param>
    /// <param name="btnPath">按钮相对路径，如 "n1/n6"（从界面根节点开始）</param>
    public static void BindButton(string uiName, string btnPath, EventCallback0 callback)
    {
        if (!isAwake) Awake();
        
        if (!uiCache.TryGetValue(uiName, out GComponent ui))
        {
            ui = CreateUI(uiName);          // 缓存没有 → 动态创建
            if (ui == null) return;         // 所有包都找不到才退出
        }

        GObject btn = ui;
        string[] parts = btnPath.Split('/');
        foreach (string part in parts)
        {
            if (btn is GComponent comp) btn = comp.GetChild(part);
            if (btn == null)
            {
                Debug.LogError($"按钮路径 '{btnPath}' 中 '{part}' 不存在");
                return;
            }
        }

        btn.onClick.Add(callback);
        Debug.Log($"按钮 '{uiName}/{btnPath}' 绑定成功");
    }
    
    /// <summary>
    /// 动态创建界面：遍历所有包查找组件，创建成功写入缓存（只创建不渲染）
    /// </summary>
    private static GComponent CreateUI(string resName)
    {
        foreach (UIPackage pkg in loadedPkgs)
        {
            GObject obj = UIPackage.CreateObject(pkg.name, resName);
            if (obj == null) continue;              // 这个包里没有，试下一个包

            GComponent ui = obj.asCom;
            if (ui == null) { obj.Dispose(); continue; }

            uiCache[resName] = ui;
            Debug.Log($"动态创建并缓存: {resName}（包 {pkg.name}）");
            return ui;
        }

        Debug.LogError($"创建界面失败: 所有包中都找不到组件 '{resName}'");
        return null;
    }
    
    
    #endregion
    
    
    
    #region 通用UI创建和缓存
    
    /// <summary>
    /// 获取或创建UI组件
    /// </summary>
    /// <param name="pkgName"></param>
    /// <param name="resName"></param>
    /// <param name="isCache"></param>
    /// <param name="targetCache"></param>
    /// <returns></returns>
    private static GComponent GetOrCreateUI(string pkgName, string resName, bool isCache)
    {
        string cacheKey = $"{pkgName}_{resName}";
        
        // 尝试从缓存获取
        if (isCache && uiCache.ContainsKey(cacheKey))
        {
            return uiCache[cacheKey];
        }
        
        // 创建UI
        GObject obj = UIPackage.CreateObject(pkgName, resName);
        if (obj == null)
        {
            Debug.LogError($"创建UI失败: 包名 '{pkgName}' 或组件名 '{resName}' 不存在");
            return null;
        }
        
        GComponent ui = obj.asCom;
        if (ui == null)
        {
            Debug.LogError($"创建UI失败: 组件 '{resName}' 不是 GComponent 类型");
            return null;
        }
        
        // 缓存UI
        if (isCache)
            uiCache[cacheKey] = ui;
        
        return ui;
    }
    
    #endregion
    
    
    
    #region 清理和关闭
    
    /// <summary>
    /// 关闭所有UI（包括主UI和弹窗）
    /// </summary>
    /// <param name="clearCache">是否清空UI缓存</param>
    public static void CloseAllUI(bool clearCache)
    {
        CloseAllPopups();
        
        if (clearCache)
        {
            uiCache.Clear();
        }
        
        Debug.Log("关闭所有UI");
    }
    
    /// <summary>
    /// 清理资源（在场景切换时调用）
    /// </summary>
    public static void Cleanup()
    {
        CloseAllUI(true);
        loadedPkgs.Clear();
        UIPackage.RemoveAllPackages();
        isAwake = false;
        Debug.Log("FGUI资源已清理");
    }
    
    #endregion
}
