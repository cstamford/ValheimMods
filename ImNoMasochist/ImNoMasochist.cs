using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ImNoMasochist
{
    public static class PluginInfo
    {
        public const string Name = "ImNoMasochist";
        public const string Guid = "Xenofell." + Name;
        public const string Version = "0.2";
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
        public static ConfigEntry<float> WagonWeightModifier;

        public void Awake()
        {
            DeathPenaltyModifier = Config.Bind("ImNoMasochist", "DeathPenaltyModifier", 0.0f, "Changes the death penalty modifier - 0 disables death penalties. (Valheim default is 0.25f)");
            Smelter_MaxFuel = Config.Bind("ImNoMasochist", "Smelter_MaxFuel", 200, "Changes the max wood the smelter/blast furnace can hold. (Valheim default is 20)");
            Smelter_MaxOre = Config.Bind("ImNoMasochist", "Smelter_MaxOre", 100, "Changes the max ore the smelter/blast furnace can hold. (Valheim default is 10)");
            Kiln_MaxFuel = Config.Bind("ImNoMasochist", "Kiln_MaxFuel", 200, "Changes the max wood the kiln can hold. (Valheim default is 25)");
            SecondsPerComfortLevel = Config.Bind("ImNoMasochist", "SecondsPerComfortLevel", 180.0f, "Changes the rested length per comfort level. (Valheim default is 60.0)");
            PieceComfortDistance = Config.Bind("ImNoMasochist", "PieceComfortDistance", 20.0f, "Changes how close you msut be to a piece to gain its comfort. (Valheim default is 10.0)");
            WagonWeightModifier = Config.Bind("ImNoMasochist", "WagonWeightModifier", 0.2f, "Changes how heavy the wagon believes it is, leading to changed maneuverability. (Valheim default is 1.0)");

            Harmony harmony = new Harmony(PluginInfo.Guid);
            harmony.PatchAll();
        }
    }

    [HarmonyPatch]
    public static class Patches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Skills), "LowerAllSkills")]
        public static bool Skills_LowerAllSkills(ref float factor)
        {
            factor = Plugin.DeathPenaltyModifier.Value;
            return factor > 0.0f; // skip original call if penalty disabled
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

        [HarmonyPatch(typeof(SE_Rested), "CalculateComfortLevel")]
        public static class SE_Rested_CalculateComfortLevel
        {
            private static MethodInfo method_SE_Rested_GetNearbyPieces = AccessTools.Method(typeof(SE_Rested), "GetNearbyPieces");
            private static MethodInfo method_GetNearbyPieces = AccessTools.Method(typeof(SE_Rested_CalculateComfortLevel), "GetNearbyPieces");

            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> il = instructions.ToList();

                // replace call to original with our own
                for (int i = 0; i < il.Count; ++i)
                {
                    if (il[i].Calls(method_SE_Rested_GetNearbyPieces))
                    {
                        il[i].operand = method_GetNearbyPieces;
                        break;
                    }
                }

                return il.AsEnumerable();
            }

            public static List<Piece> GetNearbyPieces(Vector3 point)
            {
                List<Piece> pieces = new List<Piece>();
                Piece.GetAllPiecesInRadius(point, Plugin.PieceComfortDistance.Value, pieces);
                return pieces;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Vagon), "SetMass")]
        public static void Vagon_SetMass(ref float mass)
        {
            mass *= Plugin.WagonWeightModifier.Value;
        }
    }
}
