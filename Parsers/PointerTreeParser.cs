using RARPEditor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RARPEditor.Parsers
{
    public class PointerTreeParser
    {
        public List<NoteNode> ParseNoteText(string noteText)
        {
            string[] lines = noteText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            bool isPointerNote = noteText.Contains("Pointer", StringComparison.OrdinalIgnoreCase);

            var flatList = new List<NoteNode>();
            var root = new NoteNode { Offset = "(Default)", Description = "Full Note", IndentLevel = -2, Content = noteText };
            if (lines.Length > 0) root.Size = ParseSizeFromDescription(lines[0]);
            flatList.Add(root);

            if (isPointerNote)
            {
                var stack = new Stack<NoteNode>();
                var logicalRoot = new NoteNode { IndentLevel = -1 };
                stack.Push(logicalRoot);
                NoteNode? lastAddedNode = null;

                var offsetRegex = new Regex(@"^([.\+\s]*)([-+]?)(0x[0-9A-Fa-f]+)\s*([|=:])?\s*(.*)$");
                var prefixStripRegex = new Regex(@"^([.\+\s]+)");

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Replace("\r", "").Replace("\n", "");
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var match = offsetRegex.Match(line.Trim());
                    bool isNode = false;

                    if (match.Success)
                    {
                        string indentStr = match.Groups[1].Value;
                        string sign = match.Groups[2].Value;
                        string separator = match.Groups[4].Value;
                        bool hasSign = !string.IsNullOrEmpty(sign);
                        bool hasIndent = indentStr.Contains(".") || indentStr.Contains("+");

                        if (hasSign) isNode = true;
                        else if (indentStr.Contains("+")) isNode = true;
                        else if (hasIndent && separator == "|") isNode = true;
                    }

                    if (isNode)
                    {
                        string indentStr = match.Groups[1].Value;
                        int indent = 0;
                        if (indentStr.Contains("+"))
                        {
                            if (indentStr.Contains(".")) indent = indentStr.Count(c => c == '.');
                            else indent = Math.Max(0, indentStr.Count(c => c == '+') - 1);
                        }
                        else
                        {
                            indent = indentStr.Count(c => c == '.');
                        }

                        string sign = match.Groups[2].Value;
                        string hex = match.Groups[3].Value;
                        string desc = match.Groups[5].Value;

                        while (desc.Length > 0 && (desc[0] == '-' || desc[0] == ':' || desc[0] == '=' || char.IsWhiteSpace(desc[0])))
                            desc = desc.Substring(1);
                        desc = desc.Trim();

                        string offset = sign + hex;
                        if (!offset.StartsWith("+") && !offset.StartsWith("-")) offset = "+" + offset;

                        long size = ParseSizeFromDescription(desc);
                        var node = new NoteNode { Offset = offset, Description = desc, RawLineIndex = i, IndentLevel = indent, Size = size };

                        while (stack.Count > 0 && stack.Peek().IndentLevel >= indent) stack.Pop();
                        if (stack.Count == 0) stack.Push(logicalRoot);

                        var parent = stack.Peek();
                        node.Parent = parent;
                        parent.Children.Add(node);
                        stack.Push(node);
                        lastAddedNode = node;
                    }
                    else
                    {
                        if (lastAddedNode != null)
                        {
                            if (lastAddedNode.Content.Length > 0) lastAddedNode.Content += "\r\n";
                            string cleanContentLine = prefixStripRegex.Replace(line.Trim(), "");
                            lastAddedNode.Content += cleanContentLine;
                        }
                    }
                }
                CollectAllNodes(logicalRoot, flatList);
            }
            return flatList;
        }

        private long ParseSizeFromDescription(string desc)
        {
            if (string.IsNullOrEmpty(desc)) return 1;
            var bytesMatch = Regex.Match(desc, @"\[.*?(\d+)\s*[-]?\s*bytes?.*?\]", RegexOptions.IgnoreCase);
            if (bytesMatch.Success && long.TryParse(bytesMatch.Groups[1].Value, out long bSize)) return bSize;
            if (desc.Contains("[32-bit") || desc.Contains("[Float")) return 4;
            if (desc.Contains("[24-bit")) return 3;
            if (desc.Contains("[16-bit")) return 2;
            return 1;
        }

        private void CollectAllNodes(NoteNode node, List<NoteNode> flatList)
        {
            if (node.IndentLevel != -1) flatList.Add(node);
            foreach (var child in node.Children) CollectAllNodes(child, flatList);
        }
    }
}