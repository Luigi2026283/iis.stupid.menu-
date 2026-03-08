/*
 * ii's Stupid Menu  Patches/Menu/AntiCrashPatches.cs
 * A mod menu for Gorilla Tag with over 1000+ mods
 *
 * Copyright (C) 2026  Goldentrophy Software
 * https://github.com/iiDk-the-actual/iis.Stupid.Menu
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using ExitGames.Client.Photon;
using GorillaExtensions;
using GorillaTagScripts;
using HarmonyLib;
using iiMenu.Managers;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace iiMenu.Patches.Menu
{
    public class AntiCrashPatches
    {
        public static bool enabled;

        [HarmonyPatch(typeof(VRRig), nameof(VRRig.DroppedByPlayer))]
        public class DroppedByPlayer
        {
            public static bool enabled;

            public static bool Prefix(VRRig __instance, VRRig grabbedByRig, Vector3 throwVelocity)
            {
                return !enabled || !__instance.isLocal || throwVelocity.IsValid();
            }
        }

        [HarmonyPatch(typeof(VRRig), nameof(VRRig.RequestCosmetics))]
        public class RequestCosmetics
        {
            private static readonly List<float> callTimestamps = new List<float>();
            public static bool Prefix(VRRig __instance)
            {
                if (enabled && __instance.isLocal)
                {
                    callTimestamps.Add(Time.time);
                    callTimestamps.RemoveAll(t => Time.time - t > 1);

                    return callTimestamps.Count < 15;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(VRRig), nameof(VRRig.RequestMaterialColor))]
        public class RequestMaterialColor
        {
            private static readonly List<float> callTimestamps = new List<float>();
            public static bool Prefix(VRRig __instance)
            {
                if (enabled && __instance.isLocal)
                {
                    callTimestamps.Add(Time.time);
                    callTimestamps.RemoveAll(t => Time.time - t > 1);

                    return callTimestamps.Count < 15;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(DeployedChild), nameof(DeployedChild.Deploy))]
        public class Deploy
        {
            public static void Postfix(DeployedChild __instance, DeployableObject parent, Vector3 launchPos, Quaternion launchRot, Vector3 releaseVel, bool isRemote = false)
            {
                if (enabled)
                    __instance._rigidbody.linearVelocity = __instance._rigidbody.linearVelocity.ClampMagnitudeSafe(100f);
            }
        }

        [HarmonyPatch(typeof(LuauVm), nameof(LuauVm.OnEvent))]
        public class OnEvent
        {
            public static bool Prefix(EventData eventData)
            {
                if (enabled)
                {
                    if (eventData.Code != 180) return false;

                    Player sender = PhotonNetwork.NetworkingClient.CurrentRoom.GetPlayer(eventData.Sender);

                    object[] args = eventData.CustomData == null ? new object[] { } : (object[])eventData.CustomData;
                    string command = args.Length > 0 ? (string)args[0] : "";

                    if (sender != PhotonNetwork.LocalPlayer && args[1] is double v && v == PhotonNetwork.LocalPlayer.ActorNumber && command == "leaveGame")
                        return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(RoomSystem), nameof(RoomSystem.SearchForShuttle))]
        public class SearchForShuttle
        {
            public static bool Prefix(object[] shuffleData, PhotonMessageInfoWrapped info) =>
                !enabled;
        }

        [HarmonyPatch(typeof(RoomInfo), nameof(RoomInfo.InternalCacheProperties))]
        public class InternalCacheProperties
        {
            public static bool Prefix(RoomInfo __instance, Hashtable propertiesToCache)
            {
                return __instance.masterClientId != PhotonNetwork.LocalPlayer.ActorNumber || propertiesToCache.Count != 1 || !propertiesToCache.ContainsKey(248) || !enabled;
            }
        }

        [HarmonyPatch(typeof(GameEntityManager), nameof(GameEntityManager.JoinWithItemsRPC))]
        public class JoinWithItemsRPC
        {
            public static bool Prefix(GameEntityManager __instance, byte[] stateData, int[] netIds, int joiningActorNum, PhotonMessageInfo info)
            {
                return stateData.Length <= 255;
            }
        }

        [HarmonyPatch(typeof(GorillaWrappedSerializer), nameof(GorillaWrappedSerializer.FailedToSpawn))]
        public class FailedToSpawn
        {
            public static bool Prefix(GorillaWrappedSerializer __instance)
            {
                if (enabled)
                {
                    __instance.gameObject.SetActive(false);
                    return false;
                }
                return true;
            }
        }

        private static int builderSuppressedCount;
        private static float builderNextSummaryTime;
        private static bool builderPoolBlockLogged;
        private static bool disableSimpleBackgroundWorkers;
        private static bool workerBlockLogged;

        private static Exception SuppressBuilderColliderNullReference(Exception exception, string source)
        {
            if (exception is NullReferenceException nullReferenceException)
            {
                string stackTrace = nullReferenceException.StackTrace ?? string.Empty;
                if (stackTrace.IndexOf("BuilderPiece.SetCollidersEnabled", StringComparison.Ordinal) >= 0 ||
                    source.IndexOf("BuilderPiece", StringComparison.Ordinal) >= 0 ||
                    source.IndexOf("BuilderPool", StringComparison.Ordinal) >= 0)
                {
                    disableSimpleBackgroundWorkers = true;
                    builderSuppressedCount++;
                    if (Time.realtimeSinceStartup >= builderNextSummaryTime)
                    {
                        builderNextSummaryTime = Time.realtimeSinceStartup + 2f;
                        LogManager.LogWarning($"Suppressed builder crash loop ({builderSuppressedCount} suppressed). Source: {source}");
                    }

                    return null;
                }
            }

            return exception;
        }

        [PatchHandler.PatchOnAwake]
        [HarmonyPatch(typeof(GorillaSimpleBackgroundWorkerManager), nameof(GorillaSimpleBackgroundWorkerManager.DoWork))]
        internal static class SimpleBackgroundWorkerDoWorkPatch
        {
            private static bool Prefix()
            {
                if (!disableSimpleBackgroundWorkers)
                    return true;

                if (!workerBlockLogged)
                {
                    workerBlockLogged = true;
                    LogManager.LogWarning("Blocked GorillaSimpleBackgroundWorkerManager.DoWork after builder crash detection.");
                }

                return false;
            }

            private static Exception Finalizer(Exception __exception) =>
                SuppressBuilderColliderNullReference(__exception, "GorillaSimpleBackgroundWorkerManager.DoWork");
        }

        [PatchHandler.PatchOnAwake]
        [HarmonyPatch(typeof(GorillaSimpleBackgroundWorkerManager), "_DoWork")]
        internal static class SimpleBackgroundWorkerInternalPatch
        {
            private static bool Prefix()
            {
                if (!disableSimpleBackgroundWorkers)
                    return true;

                return false;
            }

            private static Exception Finalizer(Exception __exception) =>
                SuppressBuilderColliderNullReference(__exception, "GorillaSimpleBackgroundWorkerManager._DoWork");
        }

        private static void TrySetEnabled(object component, bool enabled)
        {
            if (component == null)
                return;

            try
            {
                if (component is Behaviour behaviour)
                {
                    behaviour.enabled = enabled;
                    return;
                }

                PropertyInfo enabledProperty = component.GetType().GetProperty("enabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (enabledProperty != null && enabledProperty.PropertyType == typeof(bool) && enabledProperty.CanWrite)
                    enabledProperty.SetValue(component, enabled, null);
            }
            catch
            {
                // do nothing
            }
        }

        [PatchHandler.PatchOnAwake]
        [HarmonyPatch(typeof(BuilderPiece), "SetCollidersEnabled")]
        internal static class BuilderPieceSetCollidersEnabledPatch
        {
            private static bool Prefix(object components, bool enabled)
            {
                if (components is not IList list)
                    return true;

                try
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        TrySetEnabled(list[i], enabled);
                    }
                }
                catch
                {
                    // do nothing
                }

                // We safely handled this operation and avoid the crashing vanilla generic path.
                return false;
            }

            private static Exception Finalizer(Exception __exception) =>
                SuppressBuilderColliderNullReference(__exception, "BuilderPiece.SetCollidersEnabled");
        }

        [PatchHandler.PatchOnAwake]
        [HarmonyPatch(typeof(BuilderPiece), nameof(BuilderPiece.Awake))]
        internal static class BuilderPieceAwakePatch
        {
            private static Exception Finalizer(Exception __exception) =>
                SuppressBuilderColliderNullReference(__exception, "BuilderPiece.Awake");
        }

        [PatchHandler.PatchOnAwake]
        [HarmonyPatch(typeof(BuilderPool), nameof(BuilderPool.AddToPool))]
        internal static class BuilderPoolAddToPoolPatch
        {
            private static Exception Finalizer(Exception __exception) =>
                SuppressBuilderColliderNullReference(__exception, "BuilderPool.AddToPool");
        }

        [PatchHandler.PatchOnAwake]
        [HarmonyPatch(typeof(BuilderPool), nameof(BuilderPool.SimpleWork))]
        internal static class BuilderPoolSimpleWorkPatch
        {
            private static bool Prefix()
            {
                if (!builderPoolBlockLogged)
                {
                    builderPoolBlockLogged = true;
                    LogManager.LogWarning("Blocked BuilderPool.SimpleWork to stop BuilderPiece null-reference crash loop.");
                }

                return false;
            }
        }
    }
}
