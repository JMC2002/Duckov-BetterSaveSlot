using BetterSaveSlot.Core;
using BetterSaveSlot.UI;
using Duckov.UI.MainMenu;
using HarmonyLib;
using JmcModLib.Config;
using JmcModLib.Config.UI;
using JmcModLib.Reflection;
using JmcModLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BetterSaveSlot
{
    public static class ExtraSlotsConfig
    {
        [UIIntSlider(0, 102)]
        [Config("额外存档槽位数量")]
        public static int ExtraSlotsCount = 0;
        public static int MaxVisibleSlots = 8;
    }

    public class RescuedUIBackup : MonoBehaviour
    {
        public int originalSiblingIndex;
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public Vector3 localScale;
        public Quaternion localRotation;

        public void Capture(RectTransform rt)
        {
            originalSiblingIndex = rt.GetSiblingIndex();
            anchorMin = rt.anchorMin;
            anchorMax = rt.anchorMax;
            anchoredPosition = rt.anchoredPosition;
            sizeDelta = rt.sizeDelta;
            localScale = rt.localScale;
            localRotation = rt.localRotation;
        }

        public void Restore(RectTransform rt)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;
            rt.localScale = localScale;
            rt.localRotation = localRotation;
            rt.SetSiblingIndex(originalSiblingIndex);
        }
    }

    public class LayoutBackup : MonoBehaviour
    {
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public bool vlgExpandHeight;

        public void Capture(RectTransform rt)
        {
            anchorMin = rt.anchorMin;
            anchorMax = rt.anchorMax;
            pivot = rt.pivot;
            anchoredPosition = rt.anchoredPosition;
            sizeDelta = rt.sizeDelta;
            var vlg = rt.GetComponent<VerticalLayoutGroup>();
            if (vlg != null) vlgExpandHeight = vlg.childForceExpandHeight;
        }

        public void Restore(RectTransform rt)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;
            var vlg = rt.GetComponent<VerticalLayoutGroup>();
            vlg?.childForceExpandHeight = vlgExpandHeight;
        }
    }

    [HarmonyPatch(typeof(SaveSlotSelectionMenu))]
    public static class SaveSlotInputBlocker
    {
        // 拦截 OnCancel 方法
        [HarmonyPatch("OnCancel")]
        [HarmonyPrefix]
        public static bool OnCancelPrefix()
        {
            // 如果弹窗是激活状态
            if (SimpleConfirmUI.IsActive)
            {
                ModLogger.Debug("拦截了主菜单的 ESC，因为弹窗正在显示。");

                return false; // 返回 false 阻止原版方法执行
            }

            return true; // 没有弹窗时，允许原版逻辑执行 (退回主菜单)
        }

        [HarmonyPatch("OnDisable")]
        [HarmonyPostfix]
        public static void OnDisablePostfix()
        {
            // 当存档选择菜单关闭/隐藏时，强制清除复制状态
            if (CopyStateManager.SourceSlotIndex != null)
            {
                ModLogger.Debug("菜单关闭，自动重置复制状态。");
                CopyStateManager.ClearSource();
            }
        }
    }


    [HarmonyPatch(typeof(SaveSlotSelectionMenu))]
    public static class SaveSlotExpansionPatch
    {
        private const string ModdedSlotNamePrefix = "Modded_SaveSlot_";
        private const string ModScrollViewName = "Mod_SaveSlot_ScrollView";

        private static readonly MemberAccessor IndexAccessor = MemberAccessor.Get(typeof(SaveSlotSelectionButton), "index");
        private static readonly MethodAccessor RefreshAccessor = MethodAccessor.Get(typeof(SaveSlotSelectionButton), "Refresh");

        // 【新增】全局缓存，记录“第一次见到”时的纯净数据
        private static float _cachedOriginalItemHeight = -1f;
        private static int _cachedOriginalCount = -1;

        [HarmonyPatch("OnEnable")]
        [HarmonyPostfix]
        public static void OnEnablePostfix(SaveSlotSelectionMenu __instance)
        {
            try
            {
                // 在污染发生前，捕获纯净数据
                CaptureOriginalData(__instance);

                // 增删槽位
                UpdateSlotCount(__instance);

                // 应用列表/滚动逻辑
                ApplyScrollLogic(__instance);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"扩展槽位失败: {ex}");
            }
        }

        private static void CaptureOriginalData(SaveSlotSelectionMenu menu)
        {
            // 只有当从未缓存过，或者缓存无效时才捕获
            if (_cachedOriginalItemHeight > 0 && _cachedOriginalCount > 0) return;

            var allButtons = menu.GetComponentsInChildren<SaveSlotSelectionButton>(true);
            if (allButtons.Length == 0) return;

            // 必须确保还没生成 ScrollView，且还没生成 Mod 按钮
            Transform container = allButtons[0].transform.parent;
            if (container.parent != null && container.parent.name == ModScrollViewName) return;

            // 过滤出原版按钮
            int originalCount = 0;
            SaveSlotSelectionButton firstOriginal = null;
            foreach (var btn in allButtons)
            {
                if (!btn.name.StartsWith(ModdedSlotNamePrefix))
                {
                    originalCount++;
                    if (firstOriginal == null) firstOriginal = btn;
                }
            }

            if (firstOriginal != null && originalCount > 0)
            {
                _cachedOriginalCount = originalCount;
                _cachedOriginalItemHeight = firstOriginal.GetComponent<RectTransform>().rect.height;
                ModLogger.Info($"捕获原版数据: 单个高度={_cachedOriginalItemHeight}, 数量={_cachedOriginalCount}");
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
            bool isAlreadyScrolled = buttonsContainer.parent != null && buttonsContainer.parent.name == ModScrollViewName;

            // --- 场景 A: 数量少，需要回退 ---
            if (currentCount <= ExtraSlotsConfig.MaxVisibleSlots)
            {
                if (isAlreadyScrolled)
                {
                    RevertToNormalList(buttonsContainer.parent.gameObject, buttonsContainer, currentCount);
                }
                return;
            }

            // --- 场景 B: 数量多，需要滚动 ---
            if (!isAlreadyScrolled)
            {
                var backup = buttonsContainer.GetComponent<LayoutBackup>();
                if (backup == null)
                {
                    backup = buttonsContainer.gameObject.AddComponent<LayoutBackup>();
                    backup.Capture(buttonsContainer as RectTransform);
                }

                // 锁定按钮高度，防止放入 ScrollView 后膨胀
                EnforceButtonLayout(buttonsContainer);

                RescueFloatingUI(buttonsContainer);
                CreateScrollView(buttonsContainer);
                isAlreadyScrolled = true;
            }

            if (isAlreadyScrolled)
            {
                // 再次锁定，防止新加的按钮没被锁定
                EnforceButtonLayout(buttonsContainer);

                RectTransform svRect = buttonsContainer.parent.GetComponent<RectTransform>();
                AdjustScrollViewHeight(svRect, buttonsContainer);
                LayoutRebuilder.ForceRebuildLayoutImmediate(buttonsContainer as RectTransform);
            }
        }

        // 基于纯净高度锁定布局
        private static void EnforceButtonLayout(Transform container)
        {
            if (_cachedOriginalItemHeight <= 0) return; // 没抓到原版数据，不敢动

            var allSlots = container.GetComponentsInChildren<SaveSlotSelectionButton>(true);
            foreach (var slot in allSlots)
            {
                LayoutElement le = slot.GetComponent<LayoutElement>();
                if (le == null) le = slot.gameObject.AddComponent<LayoutElement>();

                // 强制使用“第一次见到”时的原版高度
                if (Mathf.Abs(le.preferredHeight - _cachedOriginalItemHeight) > 0.1f)
                {
                    le.preferredHeight = _cachedOriginalItemHeight;
                    le.minHeight = _cachedOriginalItemHeight;
                    le.flexibleHeight = -1;
                }
            }
        }

        private static void CreateScrollView(Transform container)
        {
            GameObject scrollViewObj = new(ModScrollViewName);
            RectTransform svRect = scrollViewObj.AddComponent<RectTransform>();

            scrollViewObj.transform.SetParent(container.parent, false);
            scrollViewObj.transform.SetSiblingIndex(container.GetSiblingIndex());

            RectTransform containerRect = container as RectTransform;
            CopyRectTransform(containerRect, svRect);

            Image bg = scrollViewObj.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.0f);
            bg.raycastTarget = true;
            scrollViewObj.AddComponent<RectMask2D>();
            ScrollRect scrollRect = scrollViewObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 30f;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;

            container.SetParent(scrollViewObj.transform, true);
            scrollRect.content = containerRect;

            containerRect.anchorMin = new Vector2(0, 1);
            containerRect.anchorMax = new Vector2(1, 1);
            containerRect.pivot = new Vector2(0.5f, 1);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.sizeDelta = Vector2.zero;

            ContentSizeFitter fitter = container.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = container.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            VerticalLayoutGroup vlg = container.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.childForceExpandHeight = false;
                vlg.childControlHeight = true;
            }

            ModLogger.Info("[BetterSaveSlot] 已转换为 ScrollView");
        }

        private static void RevertToNormalList(GameObject scrollViewObj, Transform container, int currentCount)
        {
            ModLogger.Info($"回退到正常列表 (当前数量: {currentCount})...");

            Transform originalParent = scrollViewObj.transform.parent;

            // 还原被救出的 UI
            var rescuedItems = originalParent.GetComponentsInChildren<RescuedUIBackup>(true);
            foreach (var marker in rescuedItems)
            {
                marker.transform.SetParent(container, false);
                marker.Restore(marker.GetComponent<RectTransform>());
                UnityEngine.Object.DestroyImmediate(marker);
            }

            // 还原容器层级
            container.SetParent(originalParent, true);
            container.SetSiblingIndex(scrollViewObj.transform.GetSiblingIndex());

            // 销毁 ScrollView
            UnityEngine.Object.DestroyImmediate(scrollViewObj);

            // 恢复布局
            var backup = container.GetComponent<LayoutBackup>();
            if (backup != null)
            {
                // A. 先完全还原到原版状态 (位置是对的，锚点可能是拉伸的)
                backup.Restore(container as RectTransform);
                RectTransform rt = container as RectTransform;

                // --- 分歧逻辑 ---

                // 情况 A: 数量 > 原版 -> 需要撑大容器
                if (_cachedOriginalCount > 0 && currentCount > _cachedOriginalCount)
                {
                    // 1. 记录此时此刻的“正确状态”
                    // 在拉伸状态下，rect.width 是屏幕算出来的正确宽度
                    float currentVisualWidth = rt.rect.width;
                    Vector3 originalWorldPos = rt.position; // 记录世界坐标，防止跑偏

                    // 2. 数学计算目标高度
                    float itemHeight = _cachedOriginalItemHeight > 0 ? _cachedOriginalItemHeight : 100f;
                    float spacing = 0;
                    float padding = 0;
                    var vlg = container.GetComponent<VerticalLayoutGroup>();
                    if (vlg != null)
                    {
                        spacing = vlg.spacing;
                        padding = vlg.padding.top + vlg.padding.bottom;
                    }
                    float targetHeight = padding + (itemHeight * currentCount) + (spacing * (currentCount - 1));

                    // 3. 解除锚点拉伸
                    rt.anchorMin = rt.pivot;
                    rt.anchorMax = rt.pivot;

                    // 4. 应用计算好的尺寸
                    // 宽 = 刚才记录的原版视觉宽度
                    // 高 = 算出来的目标高度
                    rt.sizeDelta = new Vector2(currentVisualWidth, targetHeight);

                    // 5. 还原世界坐标
                    // 因为改变 anchor 可能会导致 anchoredPosition 对应的位置发生变化
                    // 强制把物体移回它刚才所在的世界坐标
                    rt.position = originalWorldPos;

                    // 6. 确保不需要 Fitter
                    var fitter = container.GetComponent<ContentSizeFitter>();
                    if (fitter != null) UnityEngine.Object.DestroyImmediate(fitter);

                    // 7. 布局组件设置
                    if (vlg != null)
                    {
                        vlg.childControlHeight = true;
                        vlg.childForceExpandHeight = false; // 必须关掉强制拉伸
                    }

                    ModLogger.Debug($"[BetterSaveSlot] 回退模式：手动计算高度 {targetHeight} 并应用。");
                }
                // 情况 B: 数量正常 (<= 原版) -> 严格还原
                else
                {
                    var fitter = container.GetComponent<ContentSizeFitter>();
                    if (fitter != null) UnityEngine.Object.DestroyImmediate(fitter);
                    // backup.Restore 已经完成了剩下的工作
                }

                if (currentCount == 0)
                {
                    UnityEngine.Object.DestroyImmediate(backup);
                }
            }
            else
            {
                // 兜底
                var fitter = container.GetComponent<ContentSizeFitter>();
                if (fitter == null) container.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            // 5. 强制刷新
            LayoutRebuilder.ForceRebuildLayoutImmediate(container as RectTransform);
        }

        private static void RescueFloatingUI(Transform container)
        {
            List<Transform> toRescue = [];
            foreach (Transform child in container)
            {
                if (child.GetComponent<SaveSlotSelectionButton>() == null)
                {
                    toRescue.Add(child);
                }
            }

            if (toRescue.Count > 0)
            {
                foreach (var t in toRescue)
                {
                    var backup = t.GetComponent<RescuedUIBackup>();
                    if (backup == null) backup = t.gameObject.AddComponent<RescuedUIBackup>();
                    backup.Capture(t as RectTransform);

                    t.SetParent(container.parent, true);
                    t.SetAsLastSibling();
                }
            }
        }

        // 基于纯净数据计算列表高度
        private static void AdjustScrollViewHeight(RectTransform svRect, Transform contentContainer)
        {
            // 如果没捕获到原版数据，就没法精确还原，只能兜底
            if (_cachedOriginalItemHeight <= 0 || _cachedOriginalCount <= 0) return;

            float spacing = 0;
            float padding = 0;
            var vlg = contentContainer.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                spacing = vlg.spacing;
                padding = vlg.padding.top + vlg.padding.bottom;
            }

            // 目标高度 = 原版数量 * 原版高度 + 间距
            // 这确保了列表框和左边的原版框 一模一样大
            float targetHeight = padding + (_cachedOriginalItemHeight * _cachedOriginalCount) + (spacing * (_cachedOriginalCount - 1));

            // 获取 Canvas 高度
            float availableScreenHeight = Screen.height;
            var rootCanvas = svRect.GetComponentInParent<Canvas>();
            if (rootCanvas != null)
                availableScreenHeight = rootCanvas.GetComponent<RectTransform>().rect.height;

            // 95% 屏幕限制
            float maxAllowedHeight = availableScreenHeight * 0.95f;

            float finalHeight = Mathf.Min(targetHeight, maxAllowedHeight);

            svRect.anchorMin = new Vector2(svRect.anchorMin.x, 0.5f);
            svRect.anchorMax = new Vector2(svRect.anchorMax.x, 0.5f);
            svRect.pivot = new Vector2(0.5f, 0.5f);
            svRect.sizeDelta = new Vector2(svRect.sizeDelta.x, finalHeight);
            svRect.anchoredPosition = new Vector2(svRect.anchoredPosition.x, 0);
        }

        private static void CopyRectTransform(RectTransform source, RectTransform target)
        {
            target.anchorMin = source.anchorMin;
            target.anchorMax = source.anchorMax;
            target.anchoredPosition = source.anchoredPosition;
            target.sizeDelta = source.sizeDelta;
            target.pivot = source.pivot;
            target.rotation = source.rotation;
            target.localScale = source.localScale;
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
            if (container.parent != null && container.parent.name == ModScrollViewName)
            {
                RevertToNormalList(container.parent.gameObject, container, 0);
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