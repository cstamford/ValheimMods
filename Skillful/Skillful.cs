using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Skillful
{
    public static class PluginInfo
    {
        public const string Name = "Skillful";
        public const string Guid = "Xenofell." + Name;
        public const string Version = "0.3";
    }

    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<float> BaseCarryWeight;
        public static ConfigEntry<float> MaxBonusCarryWeight;

        public static ConfigEntry<float> BaseSneakSpeed;
        public static ConfigEntry<float> MaxBonusSneakSpeed;

        public void Awake()
        {
            BaseCarryWeight = Config.Bind("Skillful", "BaseCarryWeight", 300.0f, "Changes the base carry weight. (Valheim default is 300.0)");
            MaxBonusCarryWeight = Config.Bind("Skillful", "MaxBonusCarryWeight", 300.0f, "The bonus to carry weight when at 100 run skill.");
            BaseSneakSpeed = Config.Bind("Skillful", "BaseSneakSpeed", 3.0f, "Changes the base sneak speed. (Valheim default is 2.0)");
            MaxBonusSneakSpeed = Config.Bind("Skillful", "MaxBonusSneakSpeed", 3.0f, "The bonus to sneak speed when at 100 sneak skill.");

            Harmony harmony = new Harmony(PluginInfo.Guid);
            harmony.PatchAll();
        }
    }

    [HarmonyPatch]
    public static class Patches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), "GetMaxCarryWeight")]
        public static void Player_GetMaxCarryWeight(ref float ___m_maxCarryWeight, ref Skills ___m_skills)
        {
            float base_weight = Plugin.BaseCarryWeight.Value;
            float bonus_carry = Plugin.MaxBonusCarryWeight.Value * ___m_skills.GetSkillFactor(Skills.SkillType.Run);
            ___m_maxCarryWeight = base_weight + bonus_carry;
        }

        [HarmonyPatch(typeof(Character), "UpdateWalking")]
        public static class Character_UpdateWalking
        {
            private static FieldInfo field_Character_m_crouchSpeed = AccessTools.Field(typeof(Character), "m_crouchSpeed");
            private static MethodInfo method_GetMoveSpeed = AccessTools.Method(typeof(Character_UpdateWalking), "GetMoveSpeed");

            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> il = instructions.ToList();

                // change the loas from m_crouchSpeed to a function call instead
                for (int i = 0; i < il.Count; ++i)
                {
                    if (il[i].LoadsField(field_Character_m_crouchSpeed))
                    {
                        il[i].opcode = OpCodes.Call;
                        il[i].operand = method_GetMoveSpeed;
                    }
                }

                return il.AsEnumerable();
            }

            public static float GetMoveSpeed(Character __instance)
            {
                if (!__instance.IsEncumbered() && __instance.m_name.Contains("Human"))
                {
                    float base_sneak_speed = Plugin.BaseSneakSpeed.Value;
                    float bonus_sneak_speed = Plugin.MaxBonusSneakSpeed.Value * __instance.GetSkillFactor(Skills.SkillType.Sneak);
                    return base_sneak_speed + bonus_sneak_speed;
                }

                return __instance.m_crouchSpeed;
            }
        }
    }
}
