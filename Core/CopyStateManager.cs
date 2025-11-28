using System;

namespace BetterSaveSlot.Core
{
    // =========================================================
    // 状态管理器
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
}