using RARPEditor.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RARPEditor.Logic
{
    public static class CodeNoteLoader
    {
        public static List<CodeNote> LoadNotesForRichFile(string richFilePath)
        {
            var notes = new List<CodeNote>();
            string dir = Path.GetDirectoryName(richFilePath) ?? "";
            string fileName = Path.GetFileName(richFilePath);

            // Extract Game ID (assumes format like "34822-Rich.txt" or just "34822.json")
            // Regex matches numbers at the start of the string
            var match = Regex.Match(fileName, @"^(\d+)");
            if (!match.Success) return notes;

            string gameId = match.Groups[1].Value;

            // 1. Try Loading *-Notes.json (Server Data)
            string jsonPath = Path.Combine(dir, $"{gameId}-Notes.json");
            if (File.Exists(jsonPath))
            {
                try
                {
                    string json = File.ReadAllText(jsonPath);
                    var serverNotes = JsonSerializer.Deserialize<List<CodeNote>>(json);
                    if (serverNotes != null) notes.AddRange(serverNotes);
                }
                catch { /* Ignore parse errors */ }
            }

            // 2. Try Loading *-User.txt (Local Data)
            // Local data usually overrides or adds to server data.
            string userPath = Path.Combine(dir, $"{gameId}-User.txt");
            if (File.Exists(userPath))
            {
                var localNotes = ParseUserTxt(userPath);

                // Merge strategy: Update existing by address, add new
                foreach (var note in localNotes)
                {
                    var existing = notes.FirstOrDefault(n => n.Address == note.Address);
                    if (existing != null)
                    {
                        existing.Note = note.Note; // Update content
                    }
                    else
                    {
                        notes.Add(note);
                    }
                }
            }

            return notes;
        }

        private static List<CodeNote> ParseUserTxt(string path)
        {
            var list = new List<CodeNote>();
            try
            {
                var lines = File.ReadAllLines(path);
                foreach (var line in lines)
                {
                    // Format: N0:0x1234:"Note Content"
                    var match = Regex.Match(line, @"^N0:\s*(0x[0-9A-Fa-f]+)\s*:\s*""(.+)""\s*$");
                    if (match.Success)
                    {
                        list.Add(new CodeNote
                        {
                            Address = match.Groups[1].Value,
                            Note = match.Groups[2].Value.Replace("\\r\\n", "\r\n").Replace("\\\"", "\""),
                            User = "Local"
                        });
                    }
                }
            }
            catch { }
            return list;
        }
    }
}