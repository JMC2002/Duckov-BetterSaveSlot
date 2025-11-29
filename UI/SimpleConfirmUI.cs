using Duckov.UI.MainMenu;
using JmcModLib.Utils;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BetterSaveSlot.UI
{
    // =========================================================
    // 简单的确认弹窗系统 
    // =========================================================
    public class SimpleConfirmUI : MonoBehaviour
    {
        // 全局静态标记：当前是否有弹窗在显示
        public static bool IsActive { get; private set; } = false;

        // 一个引用，方便在外部强制关闭它
        private static SimpleConfirmUI? _instance;
        private Action _onCancelAction;

        public static void Show(Transform contextObject, string message, Action onConfirm, Action onCancel = null)
        {

            // 防止重复打开
            if (IsActive) return;

            // 直接从当前按钮找到它所属的 Canvas
            Canvas canvas = contextObject.GetComponentInParent<Canvas>();

            // 如果实在找不到（极少见），再兜底
            if (canvas == null) canvas = FindObjectOfType<Canvas>();


            if (canvas == null)
            {
                ModLogger.Error("严重错误：找不到任何 Canvas，无法显示弹窗。");
                onCancel?.Invoke();
                return;
            }

            // --- 1. 背景遮罩 ---
            GameObject overlayObj = new("Mod_Confirm_Overlay");
            overlayObj.transform.SetParent(canvas.transform, false);
            overlayObj.transform.SetAsLastSibling();

            // 挂载脚本
            var ui = overlayObj.AddComponent<SimpleConfirmUI>();
            _instance = ui;
            ui._onCancelAction = onCancel;

            // --- 设置状态为 True ---
            IsActive = true;

            RectTransform overlayRect = overlayObj.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            // --- 修复点 2：强制覆盖层级 (Override Sorting) ---
            // 为了防止弹窗被其他 UI 遮挡（比如游戏原本的特效层），
            // 我们给弹窗单独加一个 Canvas 组件，并把层级设为极高。
            Canvas overlayCanvas = overlayObj.AddComponent<Canvas>();
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = 30000; // 设置一个很大的数字

            overlayObj.AddComponent<GraphicRaycaster>();

            Image bg = overlayObj.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.9f); // 加深一点背景
            bg.raycastTarget = true;

            // --- 2. 内容面板 (居中，更宽一点以容纳按钮) ---
            GameObject panelObj = new("ContentPanel");
            panelObj.transform.SetParent(overlayObj.transform, false);

            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            // 屏幕中心，宽 50%，高 30%
            panelRect.anchorMin = new Vector2(0.25f, 0.35f);
            panelRect.anchorMax = new Vector2(0.75f, 0.65f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // --- 3. 资源获取 ---
            var refBtn = FindObjectOfType<SaveSlotSelectionButton>();
            TMP_FontAsset font = refBtn?.GetComponentInChildren<TextMeshProUGUI>()?.font;

            GameObject btnTemplate = null;
            var tempBtns = Resources.FindObjectsOfTypeAll<ContinueButton>();
            if (tempBtns != null && tempBtns.Length > 0) btnTemplate = tempBtns[0].gameObject;

            // --- 4. 标题文字 (加大字号) ---
            GameObject textObj = new("Message");
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
            GameObject btnContainer = new("Buttons");
            btnContainer.transform.SetParent(panelObj.transform, false);
            RectTransform btnConRect = btnContainer.AddComponent<RectTransform>();
            // 占下半部分
            btnConRect.anchorMin = new Vector2(0, 0);
            btnConRect.anchorMax = new Vector2(1, 0.4f);
            btnConRect.offsetMin = Vector2.zero;
            btnConRect.offsetMax = Vector2.zero;

            // --- 6. 创建按钮 (强制参数) ---
            // 确认按钮 (左)
            CreateButton(btnTemplate, btnContainer, L10n.Get("覆盖存档"), Color.red, new Vector2(0.3f, 0.5f), () =>
            {
                Destroy(overlayObj);
                onConfirm?.Invoke();
            });

            // 取消按钮 (右)
            CreateButton(btnTemplate, btnContainer, L10n.Get("取消操作"), Color.white, new Vector2(0.7f, 0.5f), () =>
            {
                Destroy(overlayObj);
                onCancel?.Invoke();
            });
        }

        private void Update()
        {
            // 2. 监听 ESC 键
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // 手动触发取消逻辑
                _onCancelAction?.Invoke();
                Close();
            }
        }

        public void Close()
        {
            IsActive = false;
            _instance = null;
            Destroy(gameObject); // 销毁自己 (Overlay)
        }

        private void OnDestroy()
        {
            IsActive = false;
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

            // 强制设定按钮大小和位置
            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = centerAnchor;
            rect.anchorMax = centerAnchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(220, 60);
            rect.anchoredPosition = Vector2.zero;

            // --- 修复图标漂移---
            // 逻辑：寻找按钮内的所有Image，排除掉作为背景的那个Image，剩下的通常就是图标
            Button btnComp = btnObj.GetComponent<Button>();
            Image bgImage = btnComp.targetGraphic as Image; // 获取按钮自身的背景图
            Image[] allImages = btnObj.GetComponentsInChildren<Image>(true);

            foreach (var img in allImages)
            {
                // 如果这个图片不是背景图，那它就是那个漂移的小图标
                if (img != bgImage)
                {
                    // 提级：挂到按钮根节点，防止嵌套层级带来的坐标问题
                    img.transform.SetParent(btnObj.transform, false);

                    // 归位：设定到左侧垂直居中
                    RectTransform iconRect = img.GetComponent<RectTransform>();
                    iconRect.anchorMin = new Vector2(0, 0.5f); // 左侧锚点
                    iconRect.anchorMax = new Vector2(0, 0.5f);
                    iconRect.pivot = new Vector2(0.5f, 0.5f);  // 中心点在左中

                    // 设定偏移和大小
                    iconRect.anchoredPosition = new Vector2(25, 0); // 距离左边距 25px
                    iconRect.sizeDelta = new Vector2(20, 20); // 强制限制图标大小，防止过大

                    // 确保图标可见
                    img.gameObject.SetActive(true);
                }
            }

            // 处理文字
            TextMeshProUGUI tmp = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp == null)
            {
                GameObject tObj = new("Text");
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
}