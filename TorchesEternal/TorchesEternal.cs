using BepInEx;
using HarmonyLib;

namespace TorchesEternal
{
    public static class PluginInfo
    {
        public const string Name = "TorchesEternal";
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
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Fireplace), "UpdateFireplace")]
        public static void Fireplace_UpdateFireplace(Fireplace __instance, ref ZNetView ___m_nview)
        {
            ___m_nview.GetZDO().Set("fuel", __instance.m_maxFuel);
        }
    }
}
