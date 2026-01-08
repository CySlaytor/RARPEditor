using RARPEditor.Controls;
using RARPEditor.Definitions;
using RARPEditor.Models;
using RARPEditor.Utilities;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace RARPEditor.Helpers
{
    public static class ConditionGridRenderer
    {
        public static void PopulateGridRows(
            DoubleBufferedDataGridView grid,
            List<AchievementCondition> conditions,
            bool readOnlyMode,
            bool showDecimal,
            bool showAliases,
            Dictionary<string, List<NoteNode>>? notesLookup,
            List<(long Start, long End, NoteNode Node)>? rangeCache,
            bool collapseChains = false)
        {
            grid.Rows.Clear();
            if (conditions == null) return;

            grid.ReadOnly = readOnlyMode;
            grid.DefaultCellStyle.BackColor = readOnlyMode ? SystemColors.Control : SystemColors.Window;
            grid.DefaultCellStyle.ForeColor = readOnlyMode ? SystemColors.GrayText : SystemColors.ControlText;

            for (int i = 0; i < conditions.Count; i++)
            {
                var cond = conditions[i];

                if (collapseChains && cond.Flag == "Add Address") continue;

                // Important: Keeps ID in sync with Model Index
                cond.ID = i + 1;

                var row = new DataGridViewRow();
                row.CreateCells(grid);
                row.Tag = cond;

                SetRowValues(row, cond, readOnlyMode, showDecimal, showAliases, notesLookup, rangeCache, conditions, i);
                grid.Rows.Add(row);
            }
        }

        public static void SetRowValues(
            DataGridViewRow row,
            AchievementCondition cond,
            bool readOnlyMode,
            bool showDecimal,
            bool showAliases,
            Dictionary<string, List<NoteNode>>? notesLookup,
            List<(long Start, long End, NoteNode Node)>? rangeCache,
            List<AchievementCondition>? conditions = null,
            int rowIndex = -1)
        {
            row.Cells[0].Value = cond.ID;
            row.Cells[1].Value = cond.Flag;
            row.Cells[2].Value = cond.LeftOperand.Type;
            row.Cells[3].Value = cond.LeftOperand.Size;

            row.Cells[4].Value = FormatOperandDisplay(cond.LeftOperand, showDecimal, showAliases, notesLookup, rangeCache, conditions, rowIndex);

            row.Cells[5].Value = cond.Operator;
            row.Cells[6].Value = cond.RightOperand.Type;
            row.Cells[7].Value = cond.RightOperand.Size;

            string rightSizeRef = LogicFormatter.GetRightOperandSizeReference(cond);
            row.Cells[8].Value = FormatOperandDisplay(cond.RightOperand, showDecimal, showAliases, notesLookup, rangeCache, conditions, rowIndex, rightSizeRef);

            ApplyCellFormatting(row, cond, readOnlyMode, showAliases);
        }

        private static string FormatOperandDisplay(
            Operand op,
            bool showDecimal,
            bool showAliases,
            Dictionary<string, List<NoteNode>>? notesLookup,
            List<(long Start, long End, NoteNode Node)>? rangeCache,
            List<AchievementCondition>? conditions,
            int rowIndex,
            string sizeRef = "")
        {
            if (showAliases && RaLogicSyntax.MemoryTypes.Contains(op.Type) && notesLookup != null)
            {
                string addr = LogicFormatter.NormalizeAddress(op.Value);
                string alias = ResolveAlias(addr, conditions, rowIndex, notesLookup, rangeCache);
                if (!string.IsNullOrEmpty(alias)) return alias.Trim();
            }
            return LogicFormatter.FormatDisplayValue(op, showDecimal, sizeRef);
        }

        private static string ResolveAlias(
            string address,
            List<AchievementCondition>? conditions,
            int rowIndex,
            Dictionary<string, List<NoteNode>> notesLookup,
            List<(long Start, long End, NoteNode Node)>? rangeCache)
        {
            long ParseHex(string s)
            {
                string clean = Regex.Replace(s.Replace("0x", "").Trim(), "[^0-9A-Fa-f]", "");
                return long.TryParse(clean, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long v) ? v : -1;
            }

            long targetVal = ParseHex(address);

            // 1. Pointer Chain Logic
            if (conditions != null && rowIndex > 0 && rowIndex < conditions.Count)
            {
                var chainStack = new Stack<string>();
                int scan = rowIndex - 1;
                while (scan >= 0)
                {
                    if (scan >= conditions.Count) { scan--; continue; }
                    var prevCond = conditions[scan];
                    if (prevCond.Flag == "Add Address")
                    {
                        chainStack.Push(prevCond.LeftOperand.Value);
                        scan--;
                    }
                    else break;
                }

                if (chainStack.Count > 0)
                {
                    string baseAddrStr = chainStack.Pop();
                    string normalizedBase = LogicFormatter.NormalizeAddress(baseAddrStr);

                    if (notesLookup.TryGetValue(normalizedBase, out var nodes))
                    {
                        var currentLevelNodes = nodes.Where(n => n.IndentLevel != -2 && (n.Parent == null || n.Parent.IndentLevel == -1)).ToList();
                        bool chainValid = true;

                        while (chainStack.Count > 0)
                        {
                            string offsetStr = chainStack.Pop();
                            long offset = ParseHex(offsetStr);
                            if (offset == -1) { chainValid = false; break; }

                            NoteNode? match = null;
                            foreach (var node in currentLevelNodes)
                            {
                                long nodeOff = ParseHex(node.Offset);
                                if (node.Offset.Contains("-")) nodeOff = -nodeOff;
                                if (offset >= nodeOff && offset < nodeOff + node.Size)
                                {
                                    match = node;
                                    break;
                                }
                            }
                            if (match != null) currentLevelNodes = match.Children;
                            else { chainValid = false; break; }
                        }

                        if (chainValid && targetVal != -1)
                        {
                            foreach (var node in currentLevelNodes)
                            {
                                long nodeOff = ParseHex(node.Offset);
                                if (node.Offset.Contains("-")) nodeOff = -nodeOff;
                                if (targetVal >= nodeOff && targetVal < nodeOff + node.Size)
                                {
                                    string desc = Regex.Replace(node.Description, @"\[.*?\]", "").Trim();
                                    long delta = targetVal - nodeOff;
                                    if (delta > 0)
                                    {
                                        string suffix = $" +0x{delta:X}";
                                        if (!desc.EndsWith(suffix, System.StringComparison.OrdinalIgnoreCase)) desc += suffix;
                                    }
                                    return desc;
                                }
                            }
                        }
                    }
                }
            }

            // 2. Direct Lookup
            if (notesLookup.TryGetValue(address, out var directNodes))
            {
                var rootNode = directNodes.FirstOrDefault(n => n.IndentLevel == -2) ?? directNodes.FirstOrDefault();
                if (rootNode != null)
                {
                    string desc = rootNode.Description;
                    if (string.IsNullOrEmpty(desc) || desc.Equals("Full Note", System.StringComparison.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrEmpty(rootNode.Content))
                            desc = rootNode.Content.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
                    }
                    desc = Regex.Replace(desc, @"\[.*?\]", "").Trim();
                    return desc;
                }
            }

            // 3. Range
            if (rangeCache != null && targetVal != -1)
            {
                var match = rangeCache.FirstOrDefault(r => targetVal >= r.Start && targetVal < r.End);
                if (match.Node != null)
                {
                    string desc = match.Node.Description;
                    if (string.IsNullOrEmpty(desc) || desc.Equals("Full Note")) desc = match.Node.Content.Split('\n')[0];
                    desc = Regex.Replace(desc, @"\[.*?\]", "").Trim();
                    long delta = targetVal - match.Start;
                    if (delta > 0) desc += $" +0x{delta:X}";
                    return desc;
                }
            }
            return "";
        }

        private static void ApplyCellFormatting(DataGridViewRow row, AchievementCondition cond, bool readOnlyMode, bool showAliases)
        {
            Color editableColor = readOnlyMode ? SystemColors.Control : SystemColors.Window;
            Color staticColor = SystemColors.ControlLight;

            row.Cells[0].Style.BackColor = staticColor; // ID

            // Left
            var lSizeCell = row.Cells[3];
            var lValueCell = row.Cells[4];
            if (cond.LeftOperand.Type is "Float" or "Value" or "Recall")
            {
                lSizeCell.Style.BackColor = staticColor;
                lValueCell.ReadOnly = cond.LeftOperand.Type == "Recall" || readOnlyMode;
                lValueCell.Style.BackColor = cond.LeftOperand.Type == "Recall" ? staticColor : editableColor;
                if (cond.LeftOperand.Type == "Recall") row.Cells[4].Value = "";
            }
            else
            {
                if (string.IsNullOrEmpty(cond.LeftOperand.Size)) lSizeCell.Style.BackColor = Color.LemonChiffon;
                else lSizeCell.Style.BackColor = editableColor;

                if (showAliases && RaLogicSyntax.MemoryTypes.Contains(cond.LeftOperand.Type))
                {
                    lValueCell.ReadOnly = true;
                    lValueCell.Style.BackColor = staticColor;
                }
                else
                {
                    lValueCell.ReadOnly = readOnlyMode;
                    lValueCell.Style.BackColor = editableColor;
                }
            }

            // Right
            var rTypeCell = row.Cells[6];
            var rSizeCell = row.Cells[7];
            var rValueCell = row.Cells[8];

            if (string.IsNullOrEmpty(cond.Operator))
            {
                rTypeCell.Style.BackColor = staticColor;
                rSizeCell.Style.BackColor = staticColor;
                rValueCell.ReadOnly = true;
                rValueCell.Style.BackColor = staticColor;
                row.Cells[6].Value = ""; row.Cells[7].Value = ""; row.Cells[8].Value = "";
            }
            else
            {
                rTypeCell.Style.BackColor = editableColor;
                if (cond.RightOperand.Type == "Recall")
                {
                    rSizeCell.Style.BackColor = staticColor;
                    rValueCell.ReadOnly = true;
                    rValueCell.Style.BackColor = staticColor;
                    row.Cells[8].Value = "";
                }
                else if (cond.RightOperand.Type is "Float" or "Value")
                {
                    rValueCell.ReadOnly = readOnlyMode;
                    rValueCell.Style.BackColor = editableColor;
                    rSizeCell.Style.BackColor = staticColor;
                }
                else
                {
                    if (string.IsNullOrEmpty(cond.RightOperand.Size)) rSizeCell.Style.BackColor = Color.LemonChiffon;
                    else rSizeCell.Style.BackColor = editableColor;

                    if (showAliases && RaLogicSyntax.MemoryTypes.Contains(cond.RightOperand.Type))
                    {
                        rValueCell.ReadOnly = true;
                        rValueCell.Style.BackColor = staticColor;
                    }
                    else
                    {
                        rValueCell.ReadOnly = readOnlyMode;
                        rValueCell.Style.BackColor = editableColor;
                    }
                }
            }

            // Hits
            if (RaLogicSyntax.FlagsWithoutHits.Contains(cond.Flag))
            {
                row.Cells[9].Value = "";
                row.Cells[9].ReadOnly = true;
                row.Cells[9].Style.BackColor = staticColor;
            }
            else
            {
                row.Cells[9].Value = cond.RequiredHits > 0 ? cond.RequiredHits.ToString() : "";
                row.Cells[9].ReadOnly = readOnlyMode;
                row.Cells[9].Style.BackColor = editableColor;
            }
        }
    }
}