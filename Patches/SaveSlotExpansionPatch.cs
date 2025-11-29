using BetterSaveSlot.Core;
using BetterSaveSlot.UI;
using Duckov.UI.MainMenu;
using HarmonyLib;
using JmcModLib.Reflection;
using JmcModLib.Utils;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BetterSaveSlot.Patches
{
    [HarmonyPatch(typeof(SaveSlotSelectionMenu))]
    public static class SaveSlotExpansionPatch
    {
        private const string ModdedSlotNamePrefix = "Modded_SaveSlot_";

        // 缓存区
        private static float _cachedOriginalItemHeight = -1f;
        private static int _cachedOriginalCount = -1;

        // 反射访问器
        private static readonly MemberAccessor IndexAccessor = MemberAccessor.Get(typeof(SaveSlotSelectionButton), "index");
        private static readonly MethodAccessor RefreshAccessor = MethodAccessor.Get(typeof(SaveSlotSelectionButton), "Refresh");

        [HarmonyPatch("OnEnable")]
        [HarmonyPostfix]
        public static void OnEnablePostfix(SaveSlotSelectionMenu __instance)
        {
            try
            {
                // 1. 捕获数据
                CaptureOriginalData(__instance);

                // 2. 增删槽位 (这是特定于存档菜单的逻辑，保留在这里)
                UpdateSlotCount(__instance);

                // 3. 应用滚动逻辑 (调用通用工具)
                ApplyScrollLogic(__instance);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"扩展槽位失败: {ex}");
            }
        }

        private static void CaptureOriginalData(SaveSlotSelectionMenu menu)
        {
            if (_cachedOriginalItemHeight > 0 && _cachedOriginalCount > 0) return;
            var allButtons = menu.GetComponentsInChildren<SaveSlotSelectionButton>(true);
            if (allButtons.Length == 0) return;

            // 确保不是在 ScrollView 内部捕获
            Transform container = allButtons[0].transform.parent;
            if (container.parent != null && container.parent.name == ModConstants.ModScrollViewName) return;

            int count = 0;
            SaveSlotSelectionButton first = null;
            foreach (var btn in allButtons)
            {
                if (!btn.name.StartsWith(ModdedSlotNamePrefix))
                {
                    count++;
                    if (first == null) first = btn;
                }
            }

            if (first != null && count > 0)
            {
                _cachedOriginalCount = count;
                _cachedOriginalItemHeight = first.GetComponent<RectTransform>().rect.height;
            }
        }

        private static void UpdateSlotCount(SaveSlotSelectionMenu menu)
        {
            var allButtons = menu.GetComponentsInChildren<SaveSlotSelectionButton>(true).ToList();
            if (allButtons.Count == 0) return;

            allButtons.Sort((a, b) => IndexAccessor.GetValue<SaveSlotSelectionButton, int>(a).CompareTo(IndexAccessor.GetValue<SaveSlotSelectionButton, int>(b)));
            var lastButton = allButtons.Last();
            int maxCurrentIndex = IndexAccessor.GetValue<SaveSlotSelectionButton, int>(lastButton);

            var originalButtons = allButtons.Where(b => !b.name.StartsWith(ModdedSlotNamePrefix)).ToList();
            if (originalButtons.Count == 0) return;
            int baseMaxIndex = originalButtons.Max(IndexAccessor.GetValue<SaveSlotSelectionButton, int>);

            int targetMaxIndex = baseMaxIndex + ExtraSlotsConfig.ExtraSlotsCount;

            if (maxCurrentIndex < targetMaxIndex)
            {
                int countNeeded = targetMaxIndex - maxCurrentIndex;
                GameObject templateObj = lastButton.gameObject;
                Transform container = templateObj.transform.parent;

                for (int i = 1; i <= countNeeded; i++)
                {
                    int newIndex = maxCurrentIndex + i;
                    GameObject newSlotObj = UnityEngine.Object.Instantiate(templateObj, container);
                    newSlotObj.name = $"{ModdedSlotNamePrefix}{newIndex}";

                    var newSlotScript = newSlotObj.GetComponent<SaveSlotSelectionButton>();
                    IndexAccessor.SetValue<SaveSlotSelectionButton, int>(newSlotScript, newIndex);
                    RefreshAccessor.InvokeVoid<SaveSlotSelectionButton>(newSlotScript);

                    var actionBtn = newSlotObj.GetComponentInChildren<SaveSlotActionButton>(true);
                    actionBtn?.Init(newSlotScript, newIndex);

                    newSlotObj.SetActive(true);
                }
            }
            else if (maxCurrentIndex > targetMaxIndex)
            {
                for (int i = allButtons.Count - 1; i >= 0; i--)
                {
                    var btn = allButtons[i];
                    int idx = IndexAccessor.GetValue<SaveSlotSelectionButton, int>(btn);
                    if (idx > targetMaxIndex && btn.name.StartsWith(ModdedSlotNamePrefix))
                    {
                        UnityEngine.Object.DestroyImmediate(btn.gameObject);
                    }
                }
            }
        }

        private static void ApplyScrollLogic(SaveSlotSelectionMenu menu)
        {
            var allButtons = menu.GetComponentsInChildren<SaveSlotSelectionButton>(true);
            if (allButtons.Length == 0) return;

            Transform buttonsContainer = allButtons[0].transform.parent;
            int currentCount = allButtons.Length;
            bool isAlreadyScrolled = buttonsContainer.parent != null && buttonsContainer.parent.name == ModConstants.ModScrollViewName;

            // 场景 A: 需要回退
            if (currentCount <= ExtraSlotsConfig.MaxVisibleSlots)
            {
                if (isAlreadyScrolled)
                {
                    // 【调用工具类】
                    ScrollViewUtils.RevertToNormal(
                        buttonsContainer.parent.gameObject,
                        buttonsContainer,
                        currentCount,
                        _cachedOriginalCount,
                        _cachedOriginalItemHeight
                    );
                }
                return;
            }

            // 场景 B: 需要滚动
            if (!isAlreadyScrolled)
            {
                var backup = buttonsContainer.GetComponent<LayoutBackup>();
                if (backup == null)
                {
                    backup = buttonsContainer.gameObject.AddComponent<LayoutBackup>();
                    backup.Capture(buttonsContainer as RectTransform);
                }

                // 【调用工具类】
                ScrollViewUtils.EnforceChildHeight(buttonsContainer, _cachedOriginalItemHeight, typeof(SaveSlotSelectionButton));
                ScrollViewUtils.RescueFloatingUI(buttonsContainer, typeof(SaveSlotSelectionButton));
                ScrollViewUtils.CreateScrollView(buttonsContainer, ModConstants.ModScrollViewName);
                isAlreadyScrolled = true;
            }

            if (isAlreadyScrolled)
            {
                // 【调用工具类】
                ScrollViewUtils.EnforceChildHeight(buttonsContainer, _cachedOriginalItemHeight, typeof(SaveSlotSelectionButton));

                RectTransform svRect = buttonsContainer.parent.GetComponent<RectTransform>();
                ScrollViewUtils.AdjustHeight(svRect, buttonsContainer, _cachedOriginalItemHeight, _cachedOriginalCount);

                LayoutRebuilder.ForceRebuildLayoutImmediate(buttonsContainer as RectTransform);
            }
        }

        public static void Cleanup()
        {
            ModLogger.Info("开始执行 Cleanup...");

            var menu = UnityEngine.Object.FindObjectOfType<SaveSlotSelectionMenu>();
            if (menu == null) return;

            // 获取任意一个按钮来定位容器
            var anyBtn = menu.GetComponentInChildren<SaveSlotSelectionButton>(true);
            if (anyBtn == null) return;

            Transform container = anyBtn.transform.parent;

            // 检查是否在 ScrollView 模式，如果是，先拆包
            if (container.parent != null && container.parent.name == ModConstants.ModScrollViewName)
            {
                // 传入 0 进行完全清理
                ScrollViewUtils.RevertToNormal(container.parent.gameObject, container, 0, _cachedOriginalCount, _cachedOriginalItemHeight);
            }

            // 删除所有 Mod 生成的槽位
            var currentButtons = menu.GetComponentsInChildren<SaveSlotSelectionButton>(true);
            for (int i = currentButtons.Length - 1; i >= 0; i--)
            {
                var btn = currentButtons[i];
                if (btn.name.StartsWith(ModdedSlotNamePrefix))
                {
                    UnityEngine.Object.DestroyImmediate(btn.gameObject);
                }
            }


            // 重置缓存数据，确保下次启用 Mod 能重新捕获
            _cachedOriginalItemHeight = -1f;
            _cachedOriginalCount = -1;

            // 强制刷新布局
            LayoutRebuilder.ForceRebuildLayoutImmediate(container as RectTransform);

            ModLogger.Info("清理完成。");
        }

    }
}