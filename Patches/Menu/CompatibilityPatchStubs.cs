using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using iiMenu.Managers;
using iiMenu.Patches;
using PlayFab;
using PlayFab.Internal;
using UnityEngine;

namespace iiMenu.Patches.Menu
{
    public static class BanPatches
    {
        public static bool enabled;

        public static class AntiBanCrash1
        {
            // Keep this enabled by default so ban-related PlayFab errors don't crash the game flow.
            public static bool enabled = true;
        }

        public static class CheckAutoBanListForName
        {
            public static bool CheckBanList(string nameToCheck) => true;
        }

        private static bool IsBanRelatedError(PlayFabError error)
        {
            string message = error?.ErrorMessage;
            if (string.IsNullOrEmpty(message))
                return false;

            message = message.ToLowerInvariant();
            return message.Contains("ban")
                || message.Contains("banned")
                || message.Contains("suspended")
                || message.Contains("suspension");
        }

        private static void NotifyBanError(PlayFabError error)
        {
            try
            {
                string message = error?.ErrorMessage?.ToLowerInvariant() ?? string.Empty;
                string text = message.Contains("this ip has been banned")
                    ? "<color=grey>[</color><color=red>ANTI-BAN</color><color=grey>]</color> Your IP address is currently banned."
                    : "<color=grey>[</color><color=red>ANTI-BAN</color><color=grey>]</color> Your account is currently banned.";
                NotificationManager.SendNotification(text);
            }
            catch
            {
                // Ignore UI notification failures to keep callback safe.
            }
        }

        private static bool WrapErrorCallback(object reqContainerObj)
        {
            CallRequestContainer callRequestContainer = reqContainerObj as CallRequestContainer;
            if (!AntiBanCrash1.enabled || callRequestContainer == null)
                return true;

            Action<PlayFabError> originalErrorCallback = callRequestContainer.ErrorCallback;
            if (originalErrorCallback == null)
                return true;

            callRequestContainer.ErrorCallback = error =>
            {
                try
                {
                    if (!IsBanRelatedError(error))
                    {
                        originalErrorCallback(error);
                        return;
                    }

                    NotifyBanError(error);
                    PlayFabError safeError = new PlayFabError
                    {
                        Error = PlayFabErrorCode.UnknownError,
                        ErrorMessage = "An unknown error occurred.",
                        ErrorDetails = new Dictionary<string, List<string>>()
                    };

                    originalErrorCallback(safeError);
                }
                catch
                {
                    try
                    {
                        originalErrorCallback(error);
                    }
                    catch
                    {
                        // Ignore callback failures to prevent hard crashes in error paths.
                    }
                }
            };

            return true;
        }

        [PatchHandler.PatchOnAwake]
        [HarmonyPatch(typeof(PlayFabUnityHttp), nameof(PlayFabUnityHttp.MakeApiCall))]
        internal static class AntiBanCrashUnityHttpPatch
        {
            private static bool Prefix(object reqContainerObj) =>
                WrapErrorCallback(reqContainerObj);
        }

        [PatchHandler.PatchOnAwake]
        [HarmonyPatch(typeof(PlayFabWebRequest), nameof(PlayFabWebRequest.MakeApiCall))]
        internal static class AntiBanCrashWebRequestPatch
        {
            private static bool Prefix(object reqContainerObj) =>
                WrapErrorCallback(reqContainerObj);
        }
    }

    public static class ClimbablePatch
    {
        public static bool enabled;
    }

    public static class CooldownPatch
    {
        public static bool enabled;
    }

    public static class DreidelPatch
    {
        public static bool enabled;
    }

    public static class EnablePatch
    {
        public static bool enabled;
    }

    public static class EntityGrabPatch
    {
        public static bool enabled;
    }

    public static class FirePatch
    {
        public static bool enabled;
    }

    public static class JoinedRoomPatch
    {
        public static bool enabled;
    }

    public static class MultiplyKnockbackPatch
    {
        public static bool enabled;
    }

    public static class MultiplyKnockback
    {
        public static bool enabled;
    }

    public static class MultiplySelfKnockbackPatch
    {
        public static bool enabled;
    }

    public static class PopulatePatch
    {
        public static bool enabled;
    }

    public static class PurchasePatch
    {
        public static bool enabled;
    }

    public static class QuitBoxPatch
    {
        public static bool enabled = true;
        public static bool teleportToStump;
    }

    public static class RankedPatch
    {
        public static bool enabled;
        public static string targetTier;
        public static string targetPlatform;
    }

    public static class ReleasePatch
    {
        public static bool enabled;
    }

    public static class RequestPatch
    {
        public static bool enabled;
        public static bool bypassCosmeticCheck;
        public static Coroutine currentCoroutine;

        public static IEnumerator LoadCosmetics()
        {
            yield break;
        }
    }

    public static class SetColorPatch
    {
    }

    public static class SetRankedPatch
    {
        public static bool enabled;
    }

    public static class TorsoPatch
    {
        public static event Action VRRigLateUpdate;
        public static bool enabled;
        public static int mode;

        public static void InvokeLateUpdate() => VRRigLateUpdate?.Invoke();
    }

    public static class VelocityPatches
    {
        public static bool enabled;
        public static float multipleFactor = 1f;
    }
}
