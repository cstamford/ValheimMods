using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace ImNoMasochist
{
    public static class PluginInfo
    {
        public const string Name = "ImNoMasochist";
        public const string Guid = "Xenofell." + Name;
        public const string Version = "0.1";
    }

    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim_server.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<float> DeathPenaltyModifier;
        public static ConfigEntry<int> Smelter_MaxFuel;
        public static ConfigEntry<int> Smelter_MaxOre;
        public static ConfigEntry<int> Kiln_MaxFuel;
        public static ConfigEntry<float> SecondsPerComfortLevel;
        public static ConfigEntry<float> PieceComfortDistance;

        public void Awake()
        {
            DeathPenaltyModifier = Config.Bind("ImNoMasochist", "DeathPenaltyModifier", 0.0f, "Changes the death penalty modifier - 0 disables death penalties. (Valheim default is 0.25f)");
            Smelter_MaxFuel = Config.Bind("ImNoMasochist", "Smelter_MaxFuel", 200, "Changes the max wood the smelter/blast furnace can hold. (Valheim default is 20)");
            Smelter_MaxOre = Config.Bind("ImNoMasochist", "Smelter_MaxOre", 100, "Changes the max ore the smelter/blast furnace can hold. (Valheim default is 10)");
            Kiln_MaxFuel = Config.Bind("ImNoMasochist", "Kiln_MaxFuel", 200, "Changes the max wood the kiln can hold. (Valheim default is 25)");
            SecondsPerComfortLevel = Config.Bind("ImNoMasochist", "SecondsPerComfortLevel", 180.0f, "Changes the rested length per comfort level. (Valheim default is 60.0)");
            PieceComfortDistance = Config.Bind("ImNoMasochist", "PieceComfortDistance", 20.0f, "Changes how close you msut be to a piece to gain its comfort. (Valheim default is 10.0)");

            Harmony harmony = new Harmony(PluginInfo.Guid);
            harmony.PatchAll();
        }
    }

    [HarmonyPatch]
    public static class Patches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Skills), "OnDeath")]
        public static bool Skills_OnDeath(Skills __instance)
        {
            if (Plugin.DeathPenaltyModifier.Value > 0.0f)
            {
                __instance.m_DeathLowerFactor = Plugin.DeathPenaltyModifier.Value;
            }

            return Plugin.DeathPenaltyModifier.Value > 0.0f;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Smelter), "UpdateSmelter")]
        public static void Smelter_UpdateSmelter(ref string ___m_name, ref int ___m_maxFuel, ref int ___m_maxOre)
        {
            if (___m_name.Contains("smelter") || ___m_name.Contains("furnace"))
            {
                ___m_maxFuel = Plugin.Smelter_MaxFuel.Value;
                ___m_maxOre = Plugin.Smelter_MaxOre.Value;
            }
            else if (___m_name.Contains("kiln"))
            {
                ___m_maxOre = Plugin.Kiln_MaxFuel.Value;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SE_Rested), "UpdateTTL")]
        public static void SE_Rested_UpdateTTL(ref float ___m_TTLPerComfortLevel)
        {
            ___m_TTLPerComfortLevel = Plugin.SecondsPerComfortLevel.Value;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SE_Rested), "GetNearbyPieces")]
        public static bool SE_Rested_GetNearbyPieces(Vector3 point, ref List<Piece> __result)
        {
            __result = new List<Piece>();
            Piece.GetAllPiecesInRadius(point, Plugin.PieceComfortDistance.Value, __result);
            return false;
        }
    }
}
