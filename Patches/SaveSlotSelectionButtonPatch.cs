using BetterSaveSlot.UI;
using Duckov.UI.MainMenu;
using HarmonyLib;
using JmcModLib.Reflection;
using UnityEngine;

namespace BetterSaveSlot
{

    // =========================================================
    // 4. Harmony 补丁 (不变)
    // =========================================================
    [HarmonyPatch(typeof(SaveSlotSelectionButton))]
    public static class SaveSlotSelectionButtonPatch
    {
        private static readonly string ModBtnName = SaveSlotActionButton.ModBtnName;

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


    }
}