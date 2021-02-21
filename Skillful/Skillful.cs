using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Character), "UpdateWalking")]
        public static void Character_UpdateWalking(Character __instance, float dt)
        {
            if (__instance.m_name.Contains("Human"))
            {
                float base_sneak_speed = Plugin.BaseSneakSpeed.Value;
                float bonus_sneak_speed = Plugin.MaxBonusSneakSpeed.Value * __instance.GetSkillFactor(Skills.SkillType.Sneak);
                __instance.m_crouchSpeed = base_sneak_speed + bonus_sneak_speed;
            }
        }
    }
}
