/*
 * ii's Stupid Menu  Managers/LogManager.cs
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

using BepInEx.Logging;
using System;

namespace iiMenu.Managers
{
    public class LogManager
    {
        private static bool initialized;
        public static string LogFilePath { get; private set; } = string.Empty;

        // Intentionally disabled: no runtime .txt file logging.
        public static void Initialize()
        {
            initialized = true;
            LogFilePath = string.Empty;
        }

        public static void Shutdown() =>
            initialized = false;

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="log">The message or object to log.</param>
        public static void Log(object log)
        {
            string message = SafeToString(log);
            TryLogToBepInEx(logger => logger.LogInfo(message));
        }

        /// <summary>
        /// Logs a formatted informational message.
        /// </summary>
        /// <param name="log">The message format string.</param>
        /// <param name="args">Arguments to format the message.</param>
        public static void Log(object log, object[] args) =>
            Log(SafeFormat(log, args));

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="log">The warning message or object to log.</param>
        public static void LogWarning(object log)
        {
            string message = SafeToString(log);
            TryLogToBepInEx(logger => logger.LogWarning(message));
        }

        /// <summary>
        /// Logs a formatted warning message.
        /// </summary>
        /// <param name="log">The warning message format string.</param>
        /// <param name="args">Arguments to format the warning message.</param>
        public static void LogWarning(object log, object[] args) =>
            LogWarning(SafeFormat(log, args));

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="log">The debug message or object to log.</param>
        public static void LogDebug(object log)
        {
            string message = SafeToString(log);
            TryLogToBepInEx(logger => logger.LogDebug(message));
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="log">The error message or object to log.</param>
        public static void LogError(object log)
        {
            string message = SafeToString(log);
            TryLogToBepInEx(logger => logger.LogError(message));
        }

        /// <summary>
        /// Logs a formatted error message.
        /// </summary>
        /// <param name="log">The error message format string.</param>
        /// <param name="args">Arguments to format the error message.</param>
        public static void LogError(object log, object[] args) =>
            LogError(SafeFormat(log, args));

        private static void TryLogToBepInEx(Action<ManualLogSource> loggerAction)
        {
            try
            {
                if (initialized && Plugin.instance != null)
                    loggerAction(Plugin.PluginLogger);
            }
            catch
            {
                // avoid recursion/crash from logging failures
            }
        }

        private static string SafeFormat(object format, object[] args)
        {
            string formatText = SafeToString(format);
            try
            {
                return string.Format(formatText, args ?? Array.Empty<object>());
            }
            catch
            {
                return formatText;
            }
        }

        private static string SafeToString(object value) =>
            value?.ToString() ?? "null";
    }
}
