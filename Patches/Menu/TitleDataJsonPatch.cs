/*
 * ii's Stupid Menu  Patches/Menu/TitleDataJsonPatch.cs
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

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace iiMenu.Patches.Menu
{
    internal static class TitleDataJsonSanitizer
    {
        internal static string TrimToSingleRoot(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            int start = -1;
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (c == '{' || c == '[')
                {
                    start = i;
                    break;
                }
            }

            if (start < 0)
                return input;

            int depth = 0;
            bool inString = false;
            bool escaped = false;

            for (int i = start; i < input.Length; i++)
            {
                char c = input[i];

                if (inString)
                {
                    if (escaped)
                    {
                        escaped = false;
                        continue;
                    }

                    if (c == '\\')
                    {
                        escaped = true;
                        continue;
                    }

                    if (c == '"')
                        inString = false;

                    continue;
                }

                if (c == '"')
                {
                    inString = true;
                    continue;
                }

                if (c == '{' || c == '[')
                    depth++;
                else if (c == '}' || c == ']')
                {
                    depth--;
                    if (depth == 0)
                        return input.Substring(start, i - start + 1);
                }
            }

            return input;
        }

        internal static Exception SuppressJsonException(Exception exception)
        {
            if (exception == null)
                return null;

            if (exception is ArgumentException argumentException &&
                argumentException.Message.IndexOf("JSON parse error", StringComparison.OrdinalIgnoreCase) >= 0)
                return null;

            // Newtonsoft and similar JSON parser exceptions (by name to avoid hard type dependency).
            if (exception.GetType().Name.IndexOf("JsonReaderException", StringComparison.OrdinalIgnoreCase) >= 0)
                return null;

            return exception;
        }
    }

    [HarmonyPatch]
    internal static class KIDManagerGetPhasePatch
    {
        private static MethodBase TargetMethod() => AccessTools.Method("KIDManager:GetPhase", new[] { typeof(string) });

        private static void Prefix(ref string jsonTxt)
        {
            jsonTxt = TitleDataJsonSanitizer.TrimToSingleRoot(jsonTxt);
        }

        private static Exception Finalizer(Exception __exception) =>
            TitleDataJsonSanitizer.SuppressJsonException(__exception);
    }

    [HarmonyPatch]
    internal static class KIDManagerGetNewPlayerDateTimePatch
    {
        private static MethodBase TargetMethod() => AccessTools.Method("KIDManager:GetNewPlayerDateTime", new[] { typeof(string) });

        private static void Prefix(ref string jsonTxt)
        {
            jsonTxt = TitleDataJsonSanitizer.TrimToSingleRoot(jsonTxt);
        }

        private static Exception Finalizer(Exception __exception) =>
            TitleDataJsonSanitizer.SuppressJsonException(__exception);
    }

    [HarmonyPatch]
    internal static class KIDManagerGetIsEnabledPatch
    {
        private static MethodBase TargetMethod() => AccessTools.Method("KIDManager:GetIsEnabled", new[] { typeof(string) });

        private static void Prefix(ref string jsonTxt)
        {
            jsonTxt = TitleDataJsonSanitizer.TrimToSingleRoot(jsonTxt);
        }

        private static Exception Finalizer(Exception __exception) =>
            TitleDataJsonSanitizer.SuppressJsonException(__exception);
    }

    [HarmonyPatch]
    internal static class BundleListFromJsonPatch
    {
        private static MethodBase TargetMethod() => AccessTools.Method("BundleList:FromJson", new[] { typeof(string) });

        private static void Prefix(ref string jsonString)
        {
            jsonString = TitleDataJsonSanitizer.TrimToSingleRoot(jsonString);
        }

        private static Exception Finalizer(Exception __exception) =>
            TitleDataJsonSanitizer.SuppressJsonException(__exception);
    }

    [HarmonyPatch]
    internal static class SpeakerVoiceToLoudnessConfigPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            MethodBase typoMethod = AccessTools.Method("SpeakerVoiceToLoudnessConfig:OnTitleDataCacheRespsonse", new[] { typeof(string) });
            if (typoMethod != null)
                yield return typoMethod;

            MethodBase correctMethod = AccessTools.Method("SpeakerVoiceToLoudnessConfig:OnTitleDataCacheResponse", new[] { typeof(string) });
            if (correctMethod != null)
                yield return correctMethod;
        }

        private static void Prefix(ref string json)
        {
            json = TitleDataJsonSanitizer.TrimToSingleRoot(json);
        }

        private static Exception Finalizer(Exception __exception) =>
            TitleDataJsonSanitizer.SuppressJsonException(__exception);
    }

    [HarmonyPatch]
    internal static class TitleDataFeatureFlagsPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            Type type = AccessTools.TypeByName("GorillaNetworking.TitleDataFeatureFlags");
            if (type == null)
                yield break;

            foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string) && method.Name.Contains("FetchFeatureFlags", StringComparison.Ordinal))
                    yield return method;
            }
        }

        private static void Prefix(ref string json)
        {
            json = TitleDataJsonSanitizer.TrimToSingleRoot(json);
        }

        private static Exception Finalizer(Exception __exception) =>
            TitleDataJsonSanitizer.SuppressJsonException(__exception);
    }
}
