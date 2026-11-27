using System;
using System.IO;
using UnityEditor;

namespace SimpleCore.Tools.EditorTools
{
    /// <summary>
    /// Tells the AI chats that the human is using the Editor (người quyết 2026-11-27), so the server grants no Editor
    /// ticket and the guard refuses Editor calls of the chats meanwhile:
    /// - Play pressed while no chat holds the Editor lock: the human plays; Stop gives the Editor back.
    /// - Menu "SimpleCore/Tôi đang dùng Editor": for editing scenes or prefabs by hand; ends by itself after 30 minutes.
    /// Both write ~/.coresimple-guard/human-editor.json, read by .claude/hooks/guard.py and Management/tools/setup_server.py.
    /// </summary>
    [InitializeOnLoad]
    internal static class HumanEditorUse
    {
        private const string MenuPath = "SimpleCore/Tôi đang dùng Editor";
        private const int MenuMinutes = 30;
        private const string ReasonPlay = "play";
        private const string ReasonMenu = "menu";

        private static string StoreDir =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".coresimple-guard");

        private static string MarkFile => Path.Combine(StoreDir, "human-editor.json");
        private static string LockFile => Path.Combine(StoreDir, "editor-lock.json");

        static HumanEditorUse()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode && !ChatHoldsEditor() && Reason() == null)
            {
                Write(ReasonPlay, null);
            }
            else if (change == PlayModeStateChange.EnteredEditMode && Reason() == ReasonPlay)
            {
                Clear();
            }
        }

        [MenuItem(MenuPath, false, 1)]
        private static void ToggleMenu()
        {
            if (Reason() == ReasonMenu)
            {
                Clear();
            }
            else
            {
                Write(ReasonMenu, Now + MenuMinutes * 60);
            }
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleMenuValidate()
        {
            Menu.SetChecked(MenuPath, Reason() == ReasonMenu);
            return true;
        }

        /// <summary>A chat holds the guard's Editor lock now: Play was its own (tests, a play-test), not the human's.</summary>
        private static bool ChatHoldsEditor()
        {
            try
            {
                var text = File.ReadAllText(LockFile);
                var until = NumberAfter(text, "\"until\"");
                return until.HasValue && until.Value > Now;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        /// <summary>The reason of the human's mark when it is still valid (play or menu), else null.</summary>
        private static string Reason()
        {
            try
            {
                var text = File.ReadAllText(MarkFile);
                var until = NumberAfter(text, "\"until\"");
                if (until.HasValue && until.Value < Now)
                {
                    return null;
                }
                return text.Contains("\"reason\": \"" + ReasonMenu + "\"") ? ReasonMenu
                    : text.Contains("\"reason\": \"" + ReasonPlay + "\"") ? ReasonPlay : null;
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }

        private static void Write(string reason, long? until)
        {
            try
            {
                Directory.CreateDirectory(StoreDir);
                var json = "{\"since\": " + Now + ", \"until\": " + (until.HasValue ? until.Value.ToString() : "null")
                           + ", \"reason\": \"" + reason + "\"}";
                var tmp = MarkFile + ".tmp";
                File.WriteAllText(tmp, json);
                File.Copy(tmp, MarkFile, true);
                File.Delete(tmp);
            }
            catch (IOException)
            {
                // the mark only keeps the chats out of the way; the Editor works without it
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static void Clear()
        {
            try
            {
                File.Delete(MarkFile);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static double? NumberAfter(string text, string key)
        {
            var i = text.IndexOf(key, StringComparison.Ordinal);
            if (i < 0)
            {
                return null;
            }
            i = text.IndexOf(':', i) + 1;
            var j = i;
            while (j < text.Length && (char.IsDigit(text[j]) || text[j] == '.' || text[j] == ' ' || text[j] == '-'))
            {
                j++;
            }
            return double.TryParse(text.Substring(i, j - i).Trim(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : (double?)null;
        }
    }
}
