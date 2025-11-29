using BetterSaveSlot.Core;
using BetterSaveSlot.UI;
using Duckov.UI.Animations;
using Duckov.UI.SavesRestore;
using HarmonyLib;
using JmcModLib.Reflection;
using JmcModLib.Utils;
using Saves;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BetterSaveSlot.Patches
{
    // =========================================================
    // 新增：辅助组件，专门用于监听 OnDisable 事件
    // =========================================================
    public class BackupMenuStateResetter : MonoBehaviour
    {
        public FadeGroup TargetFadeGroup;

        private void OnDisable()
        {
            // 当物体被禁用/隐藏时，强制重置 FadeGroup
            TargetFadeGroup?.Hide();
        }
    }

    [HarmonyPatch(typeof(SavesBackupRestoreInvoker))]
    public static class BackupInvokerExpansionPatch
    {
        private const string ModdedBtnNamePrefix = "Modded_RestoreSelectBtn_";
        private const string ModScrollViewName = "Mod_RestoreDropdown_ScrollView";

        private static readonly MemberAccessor ButtonsAccessor = MemberAccessor.Get(typeof(SavesBackupRestoreInvoker), "buttons");
        private static readonly MemberAccessor MainButtonAccessor = MemberAccessor.Get(typeof(SavesBackupRestoreInvoker), "mainButton");
        private static readonly MemberAccessor FadeGroupAccessor = MemberAccessor.Get(typeof(SavesBackupRestoreInvoker), "menuFadeGroup");
        private static readonly MemberAccessor RestorePanelAccessor = MemberAccessor.Get(typeof(SavesBackupRestoreInvoker), "restorePanel");

        private static float _cachedItemHeight = -1f;
        private static int _cachedOriginalCount = -1;

        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        public static void AwakePostfix(SavesBackupRestoreInvoker __instance)
        {
            try
            {
                var buttonsList = ButtonsAccessor.GetValue<SavesBackupRestoreInvoker, List<Button>>(__instance);
                if (buttonsList == null || buttonsList.Count == 0) return;

                CaptureOriginalData(buttonsList);

                // 扩展按钮
                ExpandButtons(__instance, buttonsList);

                // 应用滚动逻辑
                ApplyDropdownScrollLogic(__instance, buttonsList);

                // 挂载状态重置器
                // 因为 SavesBackupRestoreInvoker 没有 OnDisable 方法可供 Patch
                // 所以我们手动挂一个组件来帮它监听
                var fadeGroup = FadeGroupAccessor.GetValue<SavesBackupRestoreInvoker, FadeGroup>(__instance);
                if (fadeGroup != null)
                {
                    var resetter = __instance.gameObject.GetComponent<BackupMenuStateResetter>();
                    if (resetter == null) resetter = __instance.gameObject.AddComponent<BackupMenuStateResetter>();
                    resetter.TargetFadeGroup = fadeGroup;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"备份菜单扩展失败: {ex}");
            }
        }

        private static void CaptureOriginalData(List<Button> buttons)
        {
            if (_cachedItemHeight > 0) return;
            var originalBtns = buttons.Where(b => !b.name.StartsWith(ModdedBtnNamePrefix)).ToList();
            if (originalBtns.Count > 0)
            {
                _cachedItemHeight = originalBtns[0].GetComponent<RectTransform>().rect.height;
                _cachedOriginalCount = originalBtns.Count;
            }
        }

        private static void ExpandButtons(SavesBackupRestoreInvoker invoker, List<Button> buttonsList)
        {
            int targetCount = _cachedOriginalCount + ExtraSlotsConfig.ExtraSlotsCount;
            if (buttonsList.Count >= targetCount) return;

            Button templateBtn = buttonsList.Last();
            Transform container = templateBtn.transform.parent;
            int countNeeded = targetCount - buttonsList.Count;

            var fadeGroup = FadeGroupAccessor.GetValue<SavesBackupRestoreInvoker, FadeGroup>(invoker);
            var restorePanel = RestorePanelAccessor.GetValue<SavesBackupRestoreInvoker, SavesBackupRestorePanel>(invoker);

            for (int i = 1; i <= countNeeded; i++)
            {
                int newSlotIndex = buttonsList.Count + 1;
                GameObject newObj = UnityEngine.Object.Instantiate(templateBtn.gameObject, container);
                newObj.name = $"{ModdedBtnNamePrefix}{newSlotIndex}";

                UpdateSlotNumberText(newObj, newSlotIndex);

                Button newBtn = newObj.GetComponent<Button>();
                newBtn.onClick.RemoveAllListeners();
                newBtn.onClick.AddListener(() =>
                {
                    if (fadeGroup != null) fadeGroup.Hide();
                    SavesSystem.SetFile(newSlotIndex);
                    restorePanel?.Open(newSlotIndex);
                });

                buttonsList.Add(newBtn);
                newObj.SetActive(true);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(container as RectTransform);
        }

        private static void UpdateSlotNumberText(GameObject btnObj, int newIndex)
        {
            var tmps = btnObj.GetComponentsInChildren<TextMeshProUGUI>(true);
            bool found = false;
            foreach (var t in tmps)
            {
                t.text = newIndex.ToString();
                found = true;
            }

            if (!found)
            {
                var texts = btnObj.GetComponentsInChildren<Text>(true);
                foreach (var t in texts)
                {
                    t.text = newIndex.ToString();
                }
            }
        }

        private static void ApplyDropdownScrollLogic(SavesBackupRestoreInvoker invoker, List<Button> buttonsList)
        {
            Button mainBtn = MainButtonAccessor.GetValue<SavesBackupRestoreInvoker, Button>(invoker);
            Transform originalParent = buttonsList[0].transform.parent;

            if (originalParent.Find(ModScrollViewName) != null) return;

            GameObject scrollViewObj = new GameObject(ModScrollViewName);
            RectTransform svRect = scrollViewObj.AddComponent<RectTransform>();

            scrollViewObj.transform.SetParent(originalParent, false);

            if (mainBtn != null && mainBtn.transform.parent == originalParent)
            {
                scrollViewObj.transform.SetSiblingIndex(mainBtn.transform.GetSiblingIndex() + 1);
            }
            else
            {
                scrollViewObj.transform.SetSiblingIndex(0);
            }

            float itemHeight = _cachedItemHeight > 0 ? _cachedItemHeight : 50f;
            int visibleCount = _cachedOriginalCount > 0 ? _cachedOriginalCount : 6;

            float spacing = 0;
            var parentVLG = originalParent.GetComponent<VerticalLayoutGroup>();
            if (parentVLG != null) spacing = parentVLG.spacing;

            float preferredHeight = (itemHeight * visibleCount) + (spacing * (visibleCount - 1));

            var le = scrollViewObj.AddComponent<LayoutElement>();
            le.preferredHeight = preferredHeight;
            le.minHeight = preferredHeight;
            le.flexibleHeight = 0;
            le.flexibleWidth = 1;

            Image bg = scrollViewObj.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0f);
            bg.raycastTarget = true;

            scrollViewObj.AddComponent<RectMask2D>();
            ScrollRect sr = scrollViewObj.AddComponent<ScrollRect>();
            sr.vertical = true;
            sr.horizontal = false;
            sr.scrollSensitivity = 30f;
            sr.movementType = ScrollRect.MovementType.Elastic;

            GameObject contentObj = new GameObject("Content");
            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentObj.transform.SetParent(scrollViewObj.transform, false);

            sr.content = contentRect;

            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = Vector2.zero;

            var contentVLG = contentObj.AddComponent<VerticalLayoutGroup>();
            if (parentVLG != null)
            {
                contentVLG.spacing = parentVLG.spacing;
                contentVLG.childAlignment = parentVLG.childAlignment;
                contentVLG.childControlHeight = true;
                contentVLG.childControlWidth = true;
                contentVLG.childForceExpandHeight = false;
                contentVLG.childForceExpandWidth = true;
            }

            var contentCSF = contentObj.AddComponent<ContentSizeFitter>();
            contentCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            foreach (var btn in buttonsList)
            {
                btn.transform.SetParent(contentRect, true);

                var btnLe = btn.GetComponent<LayoutElement>();
                if (btnLe == null) btnLe = btn.gameObject.AddComponent<LayoutElement>();
                btnLe.minHeight = itemHeight;
                btnLe.preferredHeight = itemHeight;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(originalParent as RectTransform);
        }

        public static void Cleanup()
        {
            ModLogger.Info("清理备份选择菜单...");
            var invoker = UnityEngine.Object.FindObjectOfType<SavesBackupRestoreInvoker>();
            if (invoker == null) return;
            var buttonsList = ButtonsAccessor.GetValue<SavesBackupRestoreInvoker, List<Button>>(invoker);
            if (buttonsList == null || buttonsList.Count == 0) return;

            // 1. 拆包
            Transform content = buttonsList[0].transform.parent;
            if (content.parent != null && content.parent.name == ModScrollViewName)
            {
                Transform scrollView = content.parent;
                Transform originalParent = scrollView.parent;

                int targetIndex = scrollView.GetSiblingIndex();

                foreach (var btn in buttonsList)
                {
                    btn.transform.SetParent(originalParent, true);
                    btn.transform.SetSiblingIndex(targetIndex++);
                }

                UnityEngine.Object.DestroyImmediate(scrollView.gameObject);
                LayoutRebuilder.ForceRebuildLayoutImmediate(originalParent as RectTransform);
            }

            // 2. 删除 Mod 按钮
            for (int i = buttonsList.Count - 1; i >= 0; i--)
            {
                var btn = buttonsList[i];
                if (btn.name.StartsWith(ModdedBtnNamePrefix))
                {
                    buttonsList.RemoveAt(i);
                    UnityEngine.Object.DestroyImmediate(btn.gameObject);
                }
            }

            //删除辅助组件
            var resetter = invoker.GetComponent<BackupMenuStateResetter>();
            if (resetter != null) UnityEngine.Object.DestroyImmediate(resetter);

            _cachedItemHeight = -1;
            _cachedOriginalCount = -1;
        }
    }
}