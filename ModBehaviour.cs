using BetterSaveSlot.Core;
using BetterSaveSlot.Patches;
using BetterSaveSlot.UI;
using JmcModLib.Core;
using JmcModLib.Utils;

namespace BetterSaveSlot
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private HarmonyHelper harmonyHelper = new($"{VersionInfo.Name}");
        void OnEnable()
        {
        }
        void OnDisable()
        {
            ModLogger.Info("Mod 即将禁用，配置已保存");
            harmonyHelper.OnDisable();
        }

        protected override void OnAfterSetup()
        {
            ModRegistry.Register(true, info, VersionInfo.Name, VersionInfo.Version)
                       .RegisterL10n()
                       .RegisterLogger(uIFlags: LogConfigUIFlags.All)
                       .Done();
            harmonyHelper.OnEnable();
            SaveSlotActionButton.ReapplyAll();
            // L10n.LanguageChanged += SaveSlotSelectionButtonPatch.OnLanguegeChanged;
        }

        protected override void OnBeforeDeactivate()
        {
            // L10n.LanguageChanged -= SaveSlotSelectionButtonPatch.OnLanguegeChanged;
            SaveSlotExpansionPatch.Cleanup();
            SaveSlotActionButton.Cleanup();
            ModLogger.Info("Mod 已禁用，配置已保存");
        }
    }
}
