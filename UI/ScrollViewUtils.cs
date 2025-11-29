using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BetterSaveSlot.UI
{
    public static class ScrollViewUtils
    {
        // 强制锁定子元素高度
        public static void EnforceChildHeight(Transform container, float targetHeight, System.Type itemComponentType)
        {
            if (targetHeight <= 0) return;

            // 这里传入 type 是为了只锁定特定类型的按钮（比如 SaveSlotSelectionButton）
            foreach (Transform child in container)
            {
                if (child.GetComponent(itemComponentType) != null)
                {
                    LayoutElement le = child.GetComponent<LayoutElement>();
                    if (le == null) le = child.gameObject.AddComponent<LayoutElement>();

                    if (Mathf.Abs(le.preferredHeight - targetHeight) > 0.1f)
                    {
                        le.preferredHeight = targetHeight;
                        le.minHeight = targetHeight;
                        le.flexibleHeight = -1;
                    }
                }
            }
        }

        // 创建滚动视图结构
        public static void CreateScrollView(Transform container, string scrollViewName)
        {
            GameObject scrollViewObj = new(scrollViewName);
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
        }

        // 回退到普通列表
        public static void RevertToNormal(GameObject scrollViewObj, Transform container, int currentCount, int originalCount, float itemHeight)
        {
            Transform originalParent = scrollViewObj.transform.parent;

            // 1. 还原杂项 UI
            var rescuedItems = originalParent.GetComponentsInChildren<RescuedUIBackup>(true);
            foreach (var marker in rescuedItems)
            {
                marker.transform.SetParent(container, false);
                marker.Restore(marker.GetComponent<RectTransform>());
                Object.DestroyImmediate(marker);
            }

            // 2. 还原层级
            container.SetParent(originalParent, true);
            container.SetSiblingIndex(scrollViewObj.transform.GetSiblingIndex());

            // 3. 销毁壳子
            Object.DestroyImmediate(scrollViewObj);

            // 4. 恢复布局
            var backup = container.GetComponent<LayoutBackup>();
            if (backup != null)
            {
                backup.Restore(container as RectTransform);
                RectTransform rt = container as RectTransform;

                // 核心分歧逻辑：根据数量决定是否需要手动撑大
                // 注意：这里需要传入 originalCount 和 itemHeight
                if (originalCount > 0 && currentCount > originalCount)
                {
                    float currentVisualWidth = rt.rect.width;
                    Vector3 originalWorldPos = rt.position;

                    float spacing = 0;
                    float padding = 0;
                    var vlg = container.GetComponent<VerticalLayoutGroup>();
                    if (vlg != null)
                    {
                        spacing = vlg.spacing;
                        padding = vlg.padding.top + vlg.padding.bottom;
                    }
                    float targetHeight = padding + (itemHeight * currentCount) + (spacing * (currentCount - 1));

                    rt.anchorMin = rt.pivot;
                    rt.anchorMax = rt.pivot;
                    rt.sizeDelta = new Vector2(currentVisualWidth, targetHeight);
                    rt.position = originalWorldPos;

                    var fitter = container.GetComponent<ContentSizeFitter>();
                    if (fitter != null) Object.DestroyImmediate(fitter);

                    if (vlg != null)
                    {
                        vlg.childControlHeight = true;
                        vlg.childForceExpandHeight = false;
                    }
                }
                else
                {
                    var fitter = container.GetComponent<ContentSizeFitter>();
                    if (fitter != null) Object.DestroyImmediate(fitter);
                }

                if (currentCount == 0) Object.DestroyImmediate(backup);
            }
            else
            {
                // 兜底
                var fitter = container.GetComponent<ContentSizeFitter>();
                if (fitter == null) container.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(container as RectTransform);
        }

        // 拯救非列表项的 UI
        public static void RescueFloatingUI(Transform container, System.Type itemComponentType)
        {
            List<Transform> toRescue = [];
            foreach (Transform child in container)
            {
                // 只要没挂载指定的 Item 脚本，就视为杂项
                if (child.GetComponent(itemComponentType) == null)
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

        // 调整高度
        public static void AdjustHeight(RectTransform svRect, Transform contentContainer, float itemHeight, int visibleItemCount)
        {
            if (itemHeight <= 0 || visibleItemCount <= 0) return;

            float spacing = 0;
            float padding = 0;
            var vlg = contentContainer.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                spacing = vlg.spacing;
                padding = vlg.padding.top + vlg.padding.bottom;
            }

            float targetHeight = padding + (itemHeight * visibleItemCount) + (spacing * (visibleItemCount - 1));

            float availableScreenHeight = Screen.height;
            var rootCanvas = svRect.GetComponentInParent<Canvas>();
            if (rootCanvas != null)
                availableScreenHeight = rootCanvas.GetComponent<RectTransform>().rect.height;

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
    }
}