using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace BuildHelper
{
    public static class PluginInfo
    {
        public const string Name = "BuildHelper";
        public const string Guid = "Xenofell." + Name;
        public const string Version = "0.2";
    }

    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim_server.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<float> WorkbenchRadius;
        public static ConfigEntry<float> WorkbenchAttachmentRadius;
        public static ConfigEntry<float> BuildDistance;
        public static ConfigEntry<float> AreaRepairRadius;
        public static ConfigEntry<bool> DropExcludedResources;

        public void Awake()
        {
            WorkbenchRadius = Config.Bind("BuildHelper", "WorkbenchRadius", 150.0f, "Changes the workbench/stonecutter/etc distance. (Valheim default is 20.0)");
            WorkbenchAttachmentRadius = Config.Bind("BuildHelper", "WorkbenchAttachmentRadius", 10.0f, "Changes the distance that station extensions can apply to a station. (Valheim default is 5.0)");
            BuildDistance = Config.Bind("BuildHelper", "BuildDistance", 10.0f, "Changes the distance you can build using the hammer. (Valheim default is 5.0)");
            AreaRepairRadius = Config.Bind("BuildHelper", "AreaRepairRadius", 7.5f, "The radius of pieces that will be repaired around the original piece.");
            DropExcludedResources = Config.Bind("BuildHelper", "DropExcludedResources", true, "Pieces will always drop full resources, even when a resource is flagged to never drop.");

            Harmony harmony = new Harmony(PluginInfo.Guid);
            harmony.PatchAll();
        }
    }

    [HarmonyPatch]
    public static class Patches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CraftingStation), "Start")]
        public static void CraftingStation_Start(ref float ___m_rangeBuild, ref GameObject ___m_areaMarker)
        {
            ___m_rangeBuild = Plugin.WorkbenchRadius.Value;

            if (___m_areaMarker != null)
            {
                CircleProjector proj = ___m_areaMarker.GetComponent<CircleProjector>();

                if (proj != null)
                {
                    proj.m_radius = ___m_rangeBuild;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Humanoid), "EquipItem")]
        public static void Humanoid_EquipItem(ItemDrop.ItemData item, bool triggerEquipEffects)
        {
            if (item.m_shared.m_name.Contains("item_hammer") ||
                item.m_shared.m_name.Contains("item_hoe") ||
                item.m_shared.m_name.Contains("item_cultivator"))
            {
                item.m_shared.m_attack.m_attackStamina = 0.0f;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StationExtension), "Awake")]
        public static void StationExtension_Awake(ref float ___m_maxStationDistance)
        {
            ___m_maxStationDistance = Plugin.WorkbenchAttachmentRadius.Value;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), "Awake")]
        public static void Player_Awake(ref float ___m_maxPlaceDistance)
        {
            ___m_maxPlaceDistance = Plugin.BuildDistance.Value;
        }

        [HarmonyPatch(typeof(Piece), "DropResources")]
        public static class Piece_DropResources
        {
            private static MethodInfo method_Piece_IsPlacedByPlayer = AccessTools.Method(typeof(Piece), "IsPlacedByPlayer");
            private static FieldInfo field_Requirement_m_recover = AccessTools.Field(typeof(Piece.Requirement), "m_recover");

            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> il = instructions.ToList();

                // Patch out the call to Piece::IsPlacedByPlayer()
                for (int i = 0; i < il.Count; ++i)
                {
                    if (il[i].Calls(method_Piece_IsPlacedByPlayer))
                    {
                        il[i] = new CodeInstruction(OpCodes.Ldc_I4_1, null); // replace with a true return value
                        il.RemoveAt(i - 1); // remove prev ldarg.0
                    }
                }

                if (Plugin.DropExcludedResources.Value)
                {
                    // Patch out the m_recover check
                    for (int i = 0; i < il.Count; ++i)
                    {
                        if (il[i].LoadsField(field_Requirement_m_recover))
                        {
                            il.RemoveRange(i - 1, 3); // ldloc.3, ldfld, brfalse
                        }
                    }
                }

                return il.AsEnumerable();
            }
        }

        [HarmonyPatch]
        public static class AreaRepair
        {
            [HarmonyPatch(typeof(Player), "UpdatePlacement")]
            public static class Player_UpdatePlacement
            {
                private static MethodInfo method_Player_Repair = AccessTools.Method(typeof(Player), "Repair");
                private static AccessTools.FieldRef<Player, Piece> field_Player_m_hoveringPiece = AccessTools.FieldRefAccess<Player, Piece>("m_hoveringPiece");
                private static MethodInfo method_RepairNearby = AccessTools.Method(typeof(Player_UpdatePlacement), "RepairNearby");

                public static int RepairCount;

                [HarmonyTranspiler]
                public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
                {
                    List<CodeInstruction> il = instructions.ToList();

                    // Replace call to Player::Repair with our own stub
                    for (int i = 0; i < il.Count; ++i)
                    {
                        if (il[i].Calls(method_Player_Repair))
                        {
                            il[i].operand = method_RepairNearby;
                        }
                    }

                    return il.AsEnumerable();
                }

                public static void RepairNearby(Player instance, ItemDrop.ItemData toolItem, Piece _1)
                {
                    Piece selected_piece = instance.GetHoveringPiece();
                    Vector3 position = selected_piece != null ? selected_piece.transform.position : instance.transform.position;

                    List<Piece> pieces = new List<Piece>();
                    Piece.GetAllPiecesInRadius(position, Plugin.AreaRepairRadius.Value, pieces);

                    RepairCount = 0;

                    ref Piece ___m_hoveringPiece = ref field_Player_m_hoveringPiece.Invoke(instance);
                    Piece original_piece = ___m_hoveringPiece;

                    foreach (Piece piece in pieces)
                    {
                        bool has_stamina = instance.HaveStamina(toolItem.m_shared.m_attack.m_attackStamina);
                        bool uses_durability = toolItem.m_shared.m_useDurability;
                        bool has_durability = toolItem.m_durability > 0.0f;

                        if (has_stamina && (!uses_durability || has_durability))
                        {
                            ___m_hoveringPiece = piece;
                            method_Player_Repair.Invoke(instance, new object[] { toolItem, _1 });
                            ___m_hoveringPiece = original_piece;
                        }
                    }

                    instance.Message(MessageHud.MessageType.TopLeft, string.Format("{0} pieces repaired", RepairCount));
                }
            }

            [HarmonyPatch(typeof(Player), "Repair")]
            public static class Player_Repair
            {
                private static MethodInfo method_Character_Message = AccessTools.Method(typeof(Character), "Message");
                private static MethodInfo method_MessageNoop = AccessTools.Method(typeof(Player_Repair), "MessageNoop");

                [HarmonyTranspiler]
                public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
                {
                    List<CodeInstruction> il = instructions.ToList();

                    // Replace calls to Character::Message with our own noop stub
                    // First call pushes 1, then 0 - the first call is the one where a repair was successful.
                    int count = 0;
                    for (int i = 0; i < il.Count; ++i)
                    {
                        if (il[i].Calls(method_Character_Message))
                        {
                            il[i].operand = method_MessageNoop;
                            il.Insert(i++, new CodeInstruction(count == 0 ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0, null));
                            ++count;
                        }
                    }

                    return il.AsEnumerable();
                }
                public static void MessageNoop(Character instance, MessageHud.MessageType _1, string _2, int _3, Sprite _4, int repaired)
                {
                    Player_UpdatePlacement.RepairCount += repaired;
                }
            }
        }
    }
}
