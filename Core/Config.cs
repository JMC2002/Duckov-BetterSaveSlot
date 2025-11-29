using JmcModLib.Config;
using JmcModLib.Config.UI;

namespace BetterSaveSlot.Core
{
    public static class ExtraSlotsConfig
    {
        [UIIntSlider(0, 102)]
        [Config("额外存档槽位数量")]
        public static int ExtraSlotsCount = 0;
        public static int MaxVisibleSlots = 8;
    }

    // 如果有常量定义也可以放在这里
    public static class ModConstants
    {
        public const string ModScrollViewName = "Mod_Generic_ScrollView";
    }
}
