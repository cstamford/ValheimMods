using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine.UI;

namespace ModernMP
{
    public static class PluginInfo
    {
        public const string Name = "ModernMP";
        public const string Guid = "Xenofell." + Name;
        public const string Version = "0.1";
    }

    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> SharePosition;

        public void Awake()
        {
            SharePosition = Config.Bind("ModernMP", "SharePosition", true, "Whether to share your position when connecting to a server.");

            Harmony harmony = new Harmony(PluginInfo.Guid);
            harmony.PatchAll();
        }
    }

    [HarmonyPatch]
    public static class Patches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Minimap), "Awake")]
        public static void Minimap_Setup()
        {
            ZNet.instance.SetPublicReferencePosition(Plugin.SharePosition.Value);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Minimap), "OnTogglePublicPosition")]
        public static void Minimap_OnTogglePublicPosition(Toggle ___m_publicPosition)
        {
            Plugin.SharePosition.Value = ___m_publicPosition.isOn;
        }

        [HarmonyPatch(typeof(Minimap), "SetMapMode")]
        public static class Minimap_SetMapMode
        {
            private static FieldInfo field_Minimap_m_largeRoot = AccessTools.Field(typeof(Minimap), "m_largeRoot");
            private static MethodInfo method_UpdateToggle = AccessTools.Method(typeof(Minimap_SetMapMode), "UpdateToggle");

            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> il = instructions.ToList();

                // patch m_largeRoot.SetActive(true) with a call afterwards
                for (int i = 0; i < il.Count - 2; ++i)
                {
                    if (il[i].LoadsField(field_Minimap_m_largeRoot) &&
                        il[i + 1].opcode == OpCodes.Ldc_I4_1 &&
                        il[i + 2].opcode == OpCodes.Callvirt)
                    {
                        il.Insert(i + 3, new CodeInstruction(OpCodes.Ldarg_0));
                        il.Insert(i + 4, new CodeInstruction(OpCodes.Call, method_UpdateToggle));
                        break;
                    }
                }

                return il.AsEnumerable();
            }

            private static void UpdateToggle(Minimap instance)
            {
                instance.m_publicPosition.isOn = ZNet.instance.IsReferencePositionPublic();
            }
        }
    }
}
