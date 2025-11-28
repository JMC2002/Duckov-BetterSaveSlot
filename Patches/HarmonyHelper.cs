using BetterSaveSlot.Core;
using HarmonyLib;
using JmcModLib.Utils;
using System.Reflection;

namespace BetterSaveSlot.Patches
{
    public class HarmonyHelper(string patchId)
    {
        private string PatchTag => $"{VersionInfo.Name}.{patchId}";
        private Harmony? _harmony;

        public void OnEnable()
        {
            _harmony = new Harmony(PatchTag);
            _harmony.PatchAll(Assembly.GetExecutingAssembly());

            // 订阅配置变化事件
            // ModConfig.OnConfigChanged += OnConfigChanged;

            ModLogger.Info($"Harmony 补丁{patchId}已加载");
        }

        public void OnDisable()
        {
            _harmony?.UnpatchAll(PatchTag);

            // ModConfig.OnConfigChanged -= OnConfigChanged;

            ModLogger.Info($"Harmony 补丁{patchId}已卸载");
        }

        private void OnConfigChanged()
        {
            ModLogger.Info("检测到配置更新，正在刷新 Harmony 补丁{PatchId}...");

            // 完全重新加载：先禁用再启用
            OnDisable();
            OnEnable();

            ModLogger.Info($"Harmony 补丁{patchId} 已根据配置重新加载完成。");
        }
    }
}
