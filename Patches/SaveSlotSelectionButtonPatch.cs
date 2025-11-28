using Duckov.UI.MainMenu;
using HarmonyLib;
using JmcModLib.Reflection;
using JmcModLib.Utils;
using Saves;
using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BetterSaveSlot
{
    // =========================================================
    // 1. 简单的确认弹窗系统 (新增)
    // =========================================================
    public class SimpleConfirmUI : MonoBehaviour
    {
        public static void Show(string message, Action onConfirm, Action onCancel = null)
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                onConfirm?.Invoke();
                return;
            }

            // --- 1. 背景遮罩 ---
            GameObject overlayObj = new GameObject("Mod_Confirm_Overlay");
            overlayObj.transform.SetParent(canvas.transform, false);
            overlayObj.transform.SetAsLastSibling();

            RectTransform overlayRect = overlayObj.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            Image bg = overlayObj.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.9f); // 加深一点背景
            bg.raycastTarget = true;

            // --- 2. 内容面板 (居中，更宽一点以容纳按钮) ---
            GameObject panelObj = new GameObject("ContentPanel");
            panelObj.transform.SetParent(overlayObj.transform, false);

            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            // 屏幕中心，宽 50%，高 30%
            panelRect.anchorMin = new Vector2(0.25f, 0.35f);
            panelRect.anchorMax = new Vector2(0.75f, 0.65f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // --- 3. 资源获取 ---
            var refBtn = FindObjectOfType<SaveSlotSelectionButton>();
            TMP_FontAsset font = refBtn != null ? refBtn.GetComponentInChildren<TextMeshProUGUI>()?.font : null;

            GameObject btnTemplate = null;
            var tempBtns = Resources.FindObjectsOfTypeAll<ContinueButton>();
            if (tempBtns != null && tempBtns.Length > 0) btnTemplate = tempBtns[0].gameObject;

            // --- 4. 标题文字 (加大字号) ---
            GameObject textObj = new GameObject("Message");
            textObj.transform.SetParent(panelObj.transform, false);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = message;
            tmp.font = font;
            tmp.fontSize = 40; // 标题设大
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 20;
            tmp.fontSizeMax = 50;
            tmp.alignment = TextAlignmentOptions.Bottom; // 靠下一点，接近按钮
            tmp.color = Color.white;
            tmp.richText = true;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            // 占上半部分空间
            textRect.anchorMin = new Vector2(0, 0.4f);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.offsetMin = new Vector2(0, 0);
            textRect.offsetMax = new Vector2(0, -20); // 留点顶边距

            // --- 5. 按钮容器 ---
            GameObject btnContainer = new GameObject("Buttons");
            btnContainer.transform.SetParent(panelObj.transform, false);
            RectTransform btnConRect = btnContainer.AddComponent<RectTransform>();
            // 占下半部分
            btnConRect.anchorMin = new Vector2(0, 0);
            btnConRect.anchorMax = new Vector2(1, 0.4f);
            btnConRect.offsetMin = Vector2.zero;
            btnConRect.offsetMax = Vector2.zero;

            // --- 6. 创建按钮 (强制参数) ---
            // 确认按钮 (左)
            CreateButton(btnTemplate, btnContainer, "覆盖存档", Color.red, new Vector2(0.3f, 0.5f), () =>
            {
                Destroy(overlayObj);
                onConfirm?.Invoke();
            });

            // 取消按钮 (右)
            CreateButton(btnTemplate, btnContainer, "取消操作", Color.white, new Vector2(0.7f, 0.5f), () =>
            {
                Destroy(overlayObj);
                onCancel?.Invoke();
            });
        }

        private static void CreateButton(GameObject template, GameObject parent, string text, Color textColor, Vector2 centerAnchor, Action onClick)
        {
            GameObject btnObj;
            if (template != null)
            {
                btnObj = Instantiate(template, parent.transform);
                DestroyImmediate(btnObj.GetComponent<ContinueButton>());
                // 移除所有可能干扰的布局组件
                foreach (var layout in btnObj.GetComponentsInChildren<LayoutElement>(true)) DestroyImmediate(layout);
                foreach (var fitter in btnObj.GetComponentsInChildren<ContentSizeFitter>(true)) DestroyImmediate(fitter);
                foreach (var group in btnObj.GetComponentsInChildren<LayoutGroup>(true)) DestroyImmediate(group);
            }
            else
            {
                btnObj = new GameObject("Btn_" + text);
                btnObj.transform.SetParent(parent.transform);
                btnObj.AddComponent<Image>().color = Color.gray;
                btnObj.AddComponent<Button>();
            }

            // 1. 强制设定按钮大小和位置
            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = centerAnchor;
            rect.anchorMax = centerAnchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(220, 60);
            rect.anchoredPosition = Vector2.zero;

            // --- 2. 修复图标漂移 (新增逻辑) ---
            // 逻辑：寻找按钮内的所有Image，排除掉作为背景的那个Image，剩下的通常就是图标
            Button btnComp = btnObj.GetComponent<Button>();
            Image bgImage = btnComp.targetGraphic as Image; // 获取按钮自身的背景图
            Image[] allImages = btnObj.GetComponentsInChildren<Image>(true);

            foreach (var img in allImages)
            {
                // 如果这个图片不是背景图，那它就是那个漂移的小图标
                if (img != bgImage)
                {
                    // 1. 提级：挂到按钮根节点，防止嵌套层级带来的坐标问题
                    img.transform.SetParent(btnObj.transform, false);

                    // 2. 归位：设定到左侧垂直居中
                    RectTransform iconRect = img.GetComponent<RectTransform>();
                    iconRect.anchorMin = new Vector2(0, 0.5f); // 左侧锚点
                    iconRect.anchorMax = new Vector2(0, 0.5f);
                    iconRect.pivot = new Vector2(0.5f, 0.5f);  // 中心点在左中

                    // 3. 设定偏移和大小
                    iconRect.anchoredPosition = new Vector2(25, 0); // 距离左边距 25px
                    iconRect.sizeDelta = new Vector2(20, 20); // 强制限制图标大小，防止过大

                    // 确保图标可见
                    img.gameObject.SetActive(true);
                }
            }

            // --- 3. 处理文字 (保持之前的修复) ---
            TextMeshProUGUI tmp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp == null)
            {
                GameObject tObj = new GameObject("Text");
                tObj.transform.SetParent(btnObj.transform);
                tmp = tObj.AddComponent<TextMeshProUGUI>();
            }
            else
            {
                tmp.transform.SetParent(btnObj.transform, false);
            }

            RectTransform textRect = tmp.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = new Vector2(30, 0); // 左侧留出 30px 给图标，防止重叠
            textRect.offsetMax = Vector2.zero;

            tmp.margin = Vector4.zero;
            tmp.text = text;
            Color finalColor = textColor;
            finalColor.a = 1f;
            tmp.color = finalColor;
            tmp.enableAutoSizing = false;
            tmp.fontSize = 32;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.richText = true;

            btnComp.onClick.RemoveAllListeners();
            btnComp.onClick.AddListener(() => onClick());
        }
    }

    // =========================================================
    // 2. 状态管理器 (不变)
    // =========================================================
    public static class CopyStateManager
    {
        public static int? SourceSlotIndex { get; private set; } = null;
        public static event Action OnStateChanged;

        public static void SetSource(int index)
        {
            SourceSlotIndex = index;
            OnStateChanged?.Invoke();
        }

        public static void ClearSource()
        {
            SourceSlotIndex = null;
            OnStateChanged?.Invoke();
        }
    }

    // =========================================================
    // 3. 按钮逻辑控制器 (修改了 OnBtnClick)
    // =========================================================
    public class SaveSlotActionButton : MonoBehaviour
    {
        private SaveSlotSelectionButton _parentSlotScript;
        private int _mySlotIndex;
        private Button _btn;
        private TextMeshProUGUI _text;

        private static readonly MethodAccessor RefreshMethodAccessor =
            MethodAccessor.Get(typeof(SaveSlotSelectionButton), "Refresh");
        private static readonly MethodAccessor GetBackupPathAccessor =
            MethodAccessor.Get(typeof(SavesSystem), "GetBackupPathByIndex", new[] { typeof(int), typeof(int) });
        private static readonly Func<int, int, string> GetBackupPathDelegate =
            (Func<int, int, string>)GetBackupPathAccessor.TypedDelegate!;

        private static int _backupListCount = -1;

        public void Init(SaveSlotSelectionButton parentScript, int slotIndex)
        {
            _parentSlotScript = parentScript;
            _mySlotIndex = slotIndex;
            _btn = GetComponent<Button>();
            _text = GetComponentInChildren<TextMeshProUGUI>();

            _btn.onClick.RemoveAllListeners();
            _btn.onClick.AddListener(OnBtnClick);

            CopyStateManager.OnStateChanged += UpdateDisplay;
            UpdateDisplay();

            if (_backupListCount == -1)
            {
                try
                {
                    var field = MemberAccessor.Get(typeof(SavesSystem), "BackupListCount");
                    _backupListCount = (int)field.GetValue(null);
                }
                catch
                {
                    _backupListCount = 5;
                }
            }
        }

        private void OnDestroy()
        {
            CopyStateManager.OnStateChanged -= UpdateDisplay;
        }

        // --- 核心修改点 ---
        private void OnBtnClick()
        {
            // 1. 设定为源
            if (CopyStateManager.SourceSlotIndex == null)
            {
                string relPath = SavesSystem.GetFilePath(_mySlotIndex);
                string fullPath = GetAbsolutePath(relPath);

                if (!File.Exists(fullPath))
                {
                    ModLogger.Warn($"[BetterSaveSlot] Slot {_mySlotIndex} 无文件，无法复制。");
                    return;
                }
                CopyStateManager.SetSource(_mySlotIndex);
            }
            // 2. 取消
            else if (CopyStateManager.SourceSlotIndex == _mySlotIndex)
            {
                CopyStateManager.ClearSource();
            }
            // 3. 粘贴 (包含覆盖检查)
            else
            {
                int sourceSlot = CopyStateManager.SourceSlotIndex.Value;
                int targetSlot = _mySlotIndex;

                // 检查目标槽位是否已有文件
                string targetRelPath = SavesSystem.GetFilePath(targetSlot);
                string targetFullPath = GetAbsolutePath(targetRelPath);

                if (File.Exists(targetFullPath))
                {
                    // --- 弹出警告 ---
                    ModLogger.Info($"[BetterSaveSlot] 目标 Slot {targetSlot} 已存在，请求用户确认...");

                    SimpleConfirmUI.Show(
                        $"<color=yellow>警告：覆盖存档</color>\n\n目标槽位 {targetSlot} 已有存档。\n粘贴操作将<color=red>永久覆盖</color>该存档。\n此操作不可撤销！",
                        onConfirm: () =>
                        {
                            DoPaste(sourceSlot, targetSlot);
                        },
                        onCancel: () =>
                        {
                            ModLogger.Info("[BetterSaveSlot] 用户取消了覆盖操作。");
                            // 可选：取消后是否要重置源选择？通常保持源选择状态体验更好
                            // CopyStateManager.ClearSource(); 
                        }
                    );
                }
                else
                {
                    // 目标为空，直接粘贴
                    DoPaste(sourceSlot, targetSlot);
                }
            }
        }

        private void DoPaste(int sourceSlot, int targetSlot)
        {
            ModLogger.Info($"[BetterSaveSlot] 执行粘贴: {sourceSlot} -> {targetSlot}");

            try
            {
                string srcRelPath = SavesSystem.GetFilePath(sourceSlot);
                string targetRelPath = SavesSystem.GetFilePath(targetSlot);

                CopyFile(srcRelPath, targetRelPath, "主存档");

                for (int i = 0; i < _backupListCount; i++)
                {
                    string srcBackupRelPath = GetBackupPathDelegate(sourceSlot, i);
                    string targetBackupRelPath = GetBackupPathDelegate(targetSlot, i);
                    CopyFile(srcBackupRelPath, targetBackupRelPath, $"备份-{i}");
                }

                CopyStateManager.ClearSource();

                try
                {
                    RefreshMethodAccessor.InvokeVoid<SaveSlotSelectionButton>(_parentSlotScript);
                }
                catch (Exception e)
                {
                    ModLogger.Error($"[BetterSaveSlot] 刷新UI失败: {e}");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[BetterSaveSlot] 粘贴过程出错: {ex}");
            }
        }

        private void CopyFile(string srcRelPath, string targetRelPath, string fileDesc)
        {
            if (string.IsNullOrEmpty(srcRelPath) || string.IsNullOrEmpty(targetRelPath)) return;

            string srcFull = GetAbsolutePath(srcRelPath);
            string targetFull = GetAbsolutePath(targetRelPath);

            if (File.Exists(srcFull))
            {
                try
                {
                    string targetDir = Path.GetDirectoryName(targetFull);
                    if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
                    File.Copy(srcFull, targetFull, true);
                }
                catch (Exception ex) { ModLogger.Error($"[BetterSaveSlot] 复制失败 {fileDesc}: {ex.Message}"); }
            }
            else
            {
                if (File.Exists(targetFull))
                {
                    try { File.Delete(targetFull); } catch { }
                }
            }
        }

        private string GetAbsolutePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            if (Path.IsPathRooted(path)) return path;
            return Path.Combine(Application.persistentDataPath, path);
        }

        private void UpdateDisplay()
        {
            if (_text == null) return;

            if (CopyStateManager.SourceSlotIndex == null)
            {
                _text.text = "复制";
                _text.color = Color.white;
            }
            else if (CopyStateManager.SourceSlotIndex == _mySlotIndex)
            {
                _text.text = "取消";
                _text.color = new Color(1f, 0.8f, 0.2f);
            }
            else
            {
                _text.text = "粘贴";
                _text.color = Color.green;
            }
        }
    }

    // =========================================================
    // 4. Harmony 补丁 (不变)
    // =========================================================
    [HarmonyPatch(typeof(SaveSlotSelectionButton))]
    public static class SaveSlotSelectionButtonPatch
    {
        private static readonly string ModBtnName = "BetterSaveSlot_CopyBtn";

        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        public static void Postfix(SaveSlotSelectionButton __instance)
        {
            if (__instance.transform.Find(ModBtnName) != null) return;

            var indexAccessor = MemberAccessor.Get(typeof(SaveSlotSelectionButton), "index");
            int slotIndex = indexAccessor.GetValue<SaveSlotSelectionButton, int>(__instance);

            var templates = Resources.FindObjectsOfTypeAll<ContinueButton>();
            if (templates == null || templates.Length == 0)
                templates = UnityEngine.Object.FindObjectsOfType<ContinueButton>(true);

            if (templates == null || templates.Length == 0) return;

            GameObject newBtnObj = UnityEngine.Object.Instantiate(templates[0].gameObject, __instance.transform);
            newBtnObj.name = ModBtnName;
            UnityEngine.Object.DestroyImmediate(newBtnObj.GetComponent<ContinueButton>());

            RectTransform rect = newBtnObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 0.5f);
            rect.anchorMax = new Vector2(1, 0.5f);
            rect.pivot = new Vector2(1, 0.5f);
            rect.anchoredPosition = new Vector2(-20, 0);
            rect.sizeDelta = new Vector2(100, 40);

            var actionScript = newBtnObj.AddComponent<SaveSlotActionButton>();
            actionScript.Init(__instance, slotIndex);
        }

        // =========================================================
        // 核心逻辑提取：确保按钮存在
        // =========================================================
        public static void EnsureButton(SaveSlotSelectionButton slotScript)
        {
            // 1. 防止重复添加
            if (slotScript.transform.Find(ModBtnName) != null) return;

            // 2. 获取 index (你需要确保 MemberAccessor 可用)
            var indexAccessor = MemberAccessor.Get(typeof(SaveSlotSelectionButton), "index");
            int slotIndex = indexAccessor.GetValue<SaveSlotSelectionButton, int>(slotScript);

            // 3. 寻找模板
            var templates = Resources.FindObjectsOfTypeAll<ContinueButton>();
            if (templates == null || templates.Length == 0)
                templates = UnityEngine.Object.FindObjectsOfType<ContinueButton>(true);

            if (templates == null || templates.Length == 0) return;

            // 4. 实例化
            GameObject newBtnObj = UnityEngine.Object.Instantiate(templates[0].gameObject, slotScript.transform);
            newBtnObj.name = ModBtnName;
            UnityEngine.Object.DestroyImmediate(newBtnObj.GetComponent<ContinueButton>());

            // 5. 布局
            RectTransform rect = newBtnObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 0.5f);
            rect.anchorMax = new Vector2(1, 0.5f);
            rect.pivot = new Vector2(1, 0.5f);
            rect.anchoredPosition = new Vector2(-20, 0);
            rect.sizeDelta = new Vector2(100, 40);

            // 6. 挂载逻辑
            var actionScript = newBtnObj.AddComponent<SaveSlotActionButton>();
            actionScript.Init(slotScript, slotIndex);
        }

        // =========================================================
        // 新增：热重载恢复逻辑 (Mod启动时调用)
        // =========================================================
        public static void ReapplyAll()
        {
            ModLogger.Info("[BetterSaveSlot] 正在为现有槽位补发按钮...");
            var allSlots = UnityEngine.Object.FindObjectsOfType<SaveSlotSelectionButton>(true);
            foreach (var slot in allSlots)
            {
                EnsureButton(slot);
            }
        }

        // =========================================================
        // 清理逻辑 (Mod卸载时调用)
        // =========================================================
        public static void Cleanup()
        {
            ModLogger.Info("[BetterSaveSlot] 清理复制按钮...");
            var allSlots = UnityEngine.Object.FindObjectsOfType<SaveSlotSelectionButton>(true);
            foreach (var slot in allSlots)
            {
                Transform modBtn = slot.transform.Find(ModBtnName);
                if (modBtn != null)
                {
                    UnityEngine.Object.DestroyImmediate(modBtn.gameObject);
                }
            }
        }
    }
}