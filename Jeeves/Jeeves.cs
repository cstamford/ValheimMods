using BepInEx;
using HarmonyLib;
using System.Reflection;

namespace Jeeves
{
    public static class PluginInfo
    {
        public const string Name = "Jeeves";
        public const string Guid = "Xenofell." + Name;
        public const string Version = "0.1";
    }

    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public void Awake()
        {
            Harmony harmony = new Harmony(PluginInfo.Guid);
            harmony.PatchAll();
        }
    }

    [HarmonyPatch]
    public static class Patches
    {
        private static MethodInfo method_InventoryGui_HaveRepairableItems = AccessTools.Method(typeof(InventoryGui), "HaveRepairableItems");
        private static MethodInfo method_InventoryGui_RepairOneItem = AccessTools.Method(typeof(InventoryGui), "RepairOneItem");

        [HarmonyPrefix]
        [HarmonyPatch(typeof(InventoryGui), "UpdateRepair")]
        public static void InventoryGui_UpdateRepair(InventoryGui __instance)
        {
            while ((bool)method_InventoryGui_HaveRepairableItems.Invoke(__instance, null))
            {
                method_InventoryGui_RepairOneItem.Invoke(__instance, null);
            }
        }
    }
}
