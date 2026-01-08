using System.Collections.Generic;

namespace RARPEditor.Models
{
    // Minimal CodeNote model for compatibility
    public class CodeNote
    {
        public string Address { get; set; } = "";
        public string Note { get; set; } = "";
        public string User { get; set; } = "";
    }

    // Represents a node in the pointer tree structure (e.g., +0x4)
    public class NoteNode
    {
        public string Offset { get; set; } = "";
        public string Description { get; set; } = "";

        // Size in bytes covered by this note (default 1)
        public long Size { get; set; } = 1;

        public int RawLineIndex { get; set; }
        public int IndentLevel { get; set; }
        public string Content { get; set; } = "";
        public NoteNode? Parent { get; set; }
        public List<NoteNode> Children { get; set; } = new();

        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(Description)) return Offset;
            return $"{Offset} | {Description}";
        }
    }
}