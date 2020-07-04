using GameCore;
using HarmonyLib;
using Hints;
using Mirror;
using SixModLoader.Api.Configuration;
using SixModLoader.Api.Extensions;
using SixModLoader.Events;
using SixModLoader.Mods;
using UnityEngine;
using Logger = SixModLoader.Logger;

namespace WaitingAndChilling
{
    [Mod]
    public class WaitingAndChillingMod
    {
        public static WaitingAndChillingMod Instance { get; private set; }

        public WaitingAndChillingMod()
        {
            Instance = this;
        }

        [AutoConfiguration(ConfigurationType.Configuration)]
        public Configuration Configuration { get; set; }

        public Harmony Harmony { get; } = new Harmony("pl.js6pak.WaitingAndChilling");

        [EventHandler(typeof(ModEnableEvent))]
        public void OnEnable()
        {
            Harmony.PatchAll();
        }

        [EventHandler(typeof(ModDisableEvent))]
        public void OnDisable()
        {
            Harmony.UnpatchAll();
        }

        public static void Spawn(ReferenceHub player)
        {
            if (player.isDedicatedServer)
                return;

            Logger.Info($"Spawning {player.nicknameSync.MyNick}");

            player.characterClassManager.SetPlayersClass(Instance.Configuration.Role, player.gameObject);
            player.playerMovementSync.OverridePosition(Instance.Configuration.Position);
            
            if (Instance.Configuration.GiveItems)
            {
                player.inventory.AddNewItem(ItemType.GunUSP);
                player.ammoBox[(int) AmmoType._9] = 100;
                player.inventory.AddNewItem(ItemType.GunE11SR);
                player.ammoBox[(int) AmmoType._762] = 100;
                player.inventory.AddNewItem(ItemType.GunLogicer);
                player.ammoBox[(int) AmmoType._556] = 100;
            }
        }

        [HarmonyPatch(typeof(RoundStart), nameof(RoundStart.OnSerialize))]
        public static class SerializeTimerPatch
        {
            public static void Prefix(RoundStart __instance, NetworkWriter writer, bool forceAll, out short __state)
            {
                __state = __instance.Timer;
                __instance.Timer = -1;
            }

            public static void Postfix(RoundStart __instance, short __state)
            {
                __instance.Timer = __state;
            }
        }

        [HarmonyPatch(typeof(RoundStart), nameof(RoundStart.NetworkTimer), MethodType.Setter)]
        public static class TimerPatch
        {
            public static void Postfix(RoundStart __instance, short value)
            {
                if (value == -1) // Round started
                    return;

                string message;

                if (value == -2)
                {
                    message = "Waiting for players...";
                }
                else
                {
                    message = $"Game starts in {value.ToString().Tag("b")}";
                }

                message = message.Size(30).VerticalOffset(-10);

                foreach (var referenceHub in ReferenceHub.Hubs.Values)
                {
                    if (referenceHub.isDedicatedServer)
                        continue;

                    referenceHub.hints.Show(new TextHint(message,
                        new HintParameter[] {new StringHintParameter(string.Empty)},
                        HintEffectPresets.FadeInAndOut(0f),
                        1f));
                }
            }
        }

        [HarmonyPatch(typeof(CharacterClassManager), nameof(CharacterClassManager.FixedUpdate))]
        public static class PlayerJoinedPatch
        {
            public static void Prefix(CharacterClassManager __instance)
            {
                if (NetworkServer.active && __instance.CurClass <= RoleType.None && __instance.IsVerified &&
                    ReferenceHub.HostHub != null && !ReferenceHub.HostHub.characterClassManager.RoundStarted)
                {
                    Spawn(__instance._hub);
                }
            }
        }

        [HarmonyPatch(typeof(PlayerStats), nameof(PlayerStats.HurtPlayer))]
        public static class PlayerPreDeathPatch
        {
            public static bool Prefix(PlayerStats __instance, PlayerStats.HitInfo info, GameObject go)
            {
                var referenceHub = ReferenceHub.GetHub(go);

                if (referenceHub.playerStats.Health - info.Amount <= 0 &&
                    ReferenceHub.HostHub != null && !ReferenceHub.HostHub.characterClassManager.RoundStarted)
                {
                    Spawn(referenceHub);
                    return false;
                }

                return true;
            }
        }
    }
}