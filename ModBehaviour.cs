using BetterSaveSlot.Core;
using BetterSaveSlot.Patches;
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
                       .RegisterLogger(uIFlags: LogConfigUIFlags.All)
                       .Done();
            harmonyHelper.OnEnable();
            SaveSlotSelectionButtonPatch.ReapplyAll();
        }

        protected override void OnBeforeDeactivate()
        {
            SaveSlotExpansionPatch.Cleanup();
            SaveSlotSelectionButtonPatch.Cleanup();
            ModLogger.Info("Mod 已禁用，配置已保存");
        }
    }
}
