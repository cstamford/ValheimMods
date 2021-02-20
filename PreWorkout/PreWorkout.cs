using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace PreWorkout
{
    public static class PluginInfo
    {
        public const string Name = "PreWorkout";
        public const string Guid = "Xenofell." + Name;
        public const string Version = "0.1";
    }

    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim_server.exe")]
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
        private static FieldInfo field_Food_m_health = AccessTools.Field(typeof(Player.Food), "m_health");
        private static FieldInfo field_Food_m_stamina = AccessTools.Field(typeof(Player.Food), "m_stamina");

        private static FieldInfo field_Food_m_item = AccessTools.Field(typeof(Player.Food), "m_item");
        private static FieldInfo field_ItemData_m_shared = AccessTools.Field(typeof(ItemDrop.ItemData), "m_shared");
        private static FieldInfo field_SharedData_m_food = AccessTools.Field(typeof(ItemDrop.ItemData.SharedData), "m_food");
        private static FieldInfo field_SharedData_m_foodStamina = AccessTools.Field(typeof(ItemDrop.ItemData.SharedData), "m_foodStamina");

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Player), "GetTotalFoodValue")]
        public static IEnumerable<CodeInstruction> Player_GetTotalFoodValue(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> il = instructions.ToList();

            for (int i = 0; i < il.Count; ++i)
            {
                if (il[i].LoadsField(field_Food_m_health))
                {
                    il[i].operand = field_Food_m_item;
                    il.Insert(++i, new CodeInstruction(OpCodes.Ldfld, field_ItemData_m_shared));
                    il.Insert(++i, new CodeInstruction(OpCodes.Ldfld, field_SharedData_m_food));
                }
                else if (il[i].LoadsField(field_Food_m_stamina))
                {
                    il[i].operand = field_Food_m_item;
                    il.Insert(++i, new CodeInstruction(OpCodes.Ldfld, field_ItemData_m_shared));
                    il.Insert(++i, new CodeInstruction(OpCodes.Ldfld, field_SharedData_m_foodStamina));
                }
            }

            return il.AsEnumerable();
        }
    }
}
