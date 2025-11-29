using UnityEngine;
using UnityEngine.UI;

namespace BetterSaveSlot.UI
{
    // 用于标记和恢复被移出的杂项UI
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

    // 用于备份容器原本的布局设置
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
}