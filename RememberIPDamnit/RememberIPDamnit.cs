using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine.UI;

namespace RememberIPDamnit
{
    public static class PluginInfo
    {
        public const string Name = "RememberIPDamnit";
        public const string Guid = "Xenofell." + Name;
        public const string Version = "0.2";
    }

    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<string> Address;
        public static ConfigEntry<string> Password;

        public void Awake()
        {
            Address = Config.Bind<string>("RememberIPDamnit", "Address", null);
            Password = Config.Bind<string>("RememberIPDamnit", "Password", null);

            Harmony harmony = new Harmony(PluginInfo.Guid);
            harmony.PatchAll();
        }
    }

    [HarmonyPatch]
    public static class Patches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ZSteamMatchmaking), "QueueServerJoin")]
        public static void ZSteamMatchmaking_QueueServerJoin(string addr)
        {
            Plugin.Address.Value = addr;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FejdStartup), "OnJoinIPOpen")]
        public static void FejdStartup_OnJoinIPOpen(ref InputField ___m_joinIPAddress)
        {
            if (!string.IsNullOrWhiteSpace(Plugin.Address.Value))
            {
                ___m_joinIPAddress.text = Plugin.Address.Value;
            }
        }

        [HarmonyPatch(typeof(ZNet), "RPC_ClientHandshake")]
        public static class ZNet_RPC_ClientHandshake
        {
            private static MethodInfo method_PasswordTrampoline = AccessTools.Method(typeof(ZNet_RPC_ClientHandshake), "PasswordTrampoline");

            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> il = instructions.ToList();

                // Patch out the first load of empty string to be instead a call with a ret value.
                for (int i = 0; i < il.Count; ++i)
                {
                    if (il[i].opcode == OpCodes.Ldstr && (string)il[i].operand == "")
                    {
                        il[i].opcode = OpCodes.Call;
                        il[i].operand = method_PasswordTrampoline;
                        break;
                    }
                }

                return il.AsEnumerable();
            }

            private static string PasswordTrampoline()
            {
                return Plugin.Password.Value;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ZNet), "OnPasswordEnter")]
        public static void ZNet_OnPasswordEnter(string pwd)
        {
            Plugin.Password.Value = pwd;
        }
    }
}
