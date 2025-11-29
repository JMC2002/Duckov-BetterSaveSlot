using BetterSaveSlot.Core;
using Duckov.UI.MainMenu;
using JmcModLib.Reflection;
using JmcModLib.Utils;
using Saves;
using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BetterSaveSlot.UI
{
    // =========================================================
    // 按钮逻辑控制器 
    // =========================================================
    public class SaveSlotActionButton : MonoBehaviour
    {
        internal static readonly string ModBtnName = "BetterSaveSlot_CopyBtn";
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
            L10n.LanguageChanged += OnLanguegeChanged;
        }

        private void OnDestroy()
        {
            L10n.LanguageChanged -= OnLanguegeChanged;
            CopyStateManager.OnStateChanged -= UpdateDisplay;
        }

        internal void OnLanguegeChanged(SystemLanguage language)
        {
            ModLogger.Debug($"检测到语言变更为 {language}， 正在重建槽位按钮...");
            UpdateDisplay();
        }

        private void OnBtnClick()
        {
            // 设定为源
            if (CopyStateManager.SourceSlotIndex == null)
            {
                string relPath = SavesSystem.GetFilePath(_mySlotIndex);
                string fullPath = GetAbsolutePath(relPath);

                if (!File.Exists(fullPath))
                {
                    ModLogger.Warn($"Slot {_mySlotIndex} 无文件，无法复制。");
                    return;
                }
                CopyStateManager.SetSource(_mySlotIndex);
            }
            // 取消
            else if (CopyStateManager.SourceSlotIndex == _mySlotIndex)
            {
                CopyStateManager.ClearSource();
            }
            // 粘贴 (包含覆盖检查)
            else
            {
                int sourceSlot = CopyStateManager.SourceSlotIndex.Value;
                int targetSlot = _mySlotIndex;

                // 检查目标槽位是否已有文件
                string targetRelPath = SavesSystem.GetFilePath(targetSlot);
                string targetFullPath = GetAbsolutePath(targetRelPath);

                if (File.Exists(targetFullPath))    // 有个存档位就会有个占位文件，不过不想处理了，刚好当一个快速删档的功能吧
                {
                    // --- 弹出警告 ---
                    ModLogger.Info($"目标 Slot {targetSlot} 已存在，请求用户确认...");

                    SimpleConfirmUI.Show(
                        transform,
                        $"<color=yellow>{L10n.Get("警告")}: {L10n.Get("覆盖存档")}</color>\n\n" +
                        L10n.GetF("粘贴警告1", null, targetSlot) + "\n" +
                        $"{L10n.Get("粘贴警告2")}\n" +
                        L10n.Get("粘贴警告3"),
                        onConfirm: () =>
                        {
                            DoPaste(sourceSlot, targetSlot);
                        },
                        onCancel: () =>
                        {
                            ModLogger.Info("用户取消了覆盖操作。");
                            // 取消后是否要重置源选择
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
            ModLogger.Info($"执行粘贴: {sourceSlot} -> {targetSlot}");

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
                    ModLogger.Error($"刷新UI失败: {e}");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"粘贴过程出错: {ex}");
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
                catch (Exception ex) { ModLogger.Error($"复制失败 {fileDesc}: {ex.Message}"); }
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
                _text.text = L10n.Get("复制");
                _text.color = Color.white;
            }
            else if (CopyStateManager.SourceSlotIndex == _mySlotIndex)
            {
                _text.text = L10n.Get("取消");
                _text.color = new Color(1f, 0.8f, 0.2f);
            }
            else
            {
                _text.text = L10n.Get("粘贴");
                _text.color = Color.green;
            }
        }

        // =========================================================
        // 确保按钮存在
        // =========================================================
        public static void EnsureButton(SaveSlotSelectionButton slotScript)
        {
            ModLogger.Debug($"尝试为对象 {slotScript.name} 检查/创建按钮...");
            // 1. 防止重复添加
            if (slotScript.transform.Find(ModBtnName) != null) 
            {
                ModLogger.Debug($"对象 {slotScript.name} 已存在按钮，跳过。");
                return; 
            }

            // 2. 获取 index
            var indexAccessor = MemberAccessor.Get(typeof(SaveSlotSelectionButton), "index");
            int slotIndex = indexAccessor.GetValue<SaveSlotSelectionButton, int>(slotScript);

            ModLogger.Debug($"正在创建按钮 -> 父物体: {slotScript.name}, 槽位Index: {slotIndex}");

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
            if (actionScript == null)
            {
                ModLogger.Error($"严重错误：SaveSlotActionButton 组件挂载失败！");
                return;
            }
            actionScript.Init(slotScript, slotIndex);
        }

        // =========================================================
        // 热重载恢复逻辑
        // =========================================================
        public static void ReapplyAll()
        {
            ModLogger.Info("正在为现有槽位补发按钮...");
            var allSlots = UnityEngine.Object.FindObjectsOfType<SaveSlotSelectionButton>(true);
            foreach (var slot in allSlots)
            {
                EnsureButton(slot);
            }
        }

        // =========================================================
        // 清理逻辑
        // =========================================================
        public static void Cleanup()
        {
            ModLogger.Info("清理复制按钮...");
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