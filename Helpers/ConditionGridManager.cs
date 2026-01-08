using RARPEditor.Controls;
using RARPEditor.Definitions;
using RARPEditor.Models;
using RARPEditor.Parsers;
using RARPEditor.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace RARPEditor.Helpers
{
    public class ConditionGridManager
    {
        private readonly DoubleBufferedDataGridView _grid;
        private bool _isUpdating = false;

        private readonly ContextMenuStrip _flagContextMenu = new();
        private readonly ContextMenuStrip _typeContextMenu = new();
        private readonly ContextMenuStrip _sizeContextMenu = new();
        private readonly ContextMenuStrip _cmpContextMenu = new();
        private readonly ContextMenuStrip _cmpArithmeticContextMenu = new();
        private readonly ContextMenuStrip _cmpStrictContextMenu = new();

        public event EventHandler? LogicChanged;
        public event EventHandler<string>? StatusUpdateRequested;
        public event EventHandler<string>? PasteRequested;

        public bool ShowDecimal { get; set; } = false;
        public bool ShowAliases { get; set; } = false;
        public bool CollapseChains { get; set; } = false;

        public Dictionary<string, List<NoteNode>> NotesLookup { get; set; } = new();
        public List<(long Start, long End, NoteNode Node)> RangeCache { get; set; } = new();

        public ConditionGridManager(DoubleBufferedDataGridView grid)
        {
            _grid = grid;
            InitializeContextMenus();
            WireUpGridEvents();
        }

        public void BeginUpdate() => _isUpdating = true;
        public void EndUpdate() => _isUpdating = false;

        private void WireUpGridEvents()
        {
            _grid.CellValueChanged += Grid_CellValueChanged;
            _grid.CellMouseDown += Grid_CellMouseDown;
            _grid.CellDoubleClick += Grid_CellDoubleClick;
            _grid.EditingControlShowing += Grid_EditingControlShowing;
            _grid.CellEndEdit += Grid_CellEndEdit;
            _grid.KeyDown += Grid_KeyDown;
            _grid.CellToolTipTextNeeded += Grid_CellToolTipTextNeeded;
        }

        private void InitializeContextMenus()
        {
            CreateMenu(_flagContextMenu, RaLogicSyntax.Flags);
            CreateMenu(_typeContextMenu, RaLogicSyntax.OperandTypes);
            CreateMenu(_sizeContextMenu, RaLogicSyntax.MemorySizes);
            CreateMenu(_cmpContextMenu, RaLogicSyntax.ComparisonOperators);
            CreateMenu(_cmpArithmeticContextMenu, RaLogicSyntax.ArithmeticOperators);
            CreateMenu(_cmpStrictContextMenu, RaLogicSyntax.ComparisonOperators.Where(op => op != RaLogicSyntax.NO_OPERATOR_TEXT).ToArray());
        }

        private void CreateMenu(ContextMenuStrip menu, string[] items)
        {
            foreach (var item in items)
            {
                var menuItem = new ToolStripMenuItem(item.Replace("&", "&&"), null, OnContextMenuItemClick) { Tag = item };
                menu.Items.Add(menuItem);
            }
        }

        private void Grid_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.A) { _grid.SelectAll(); e.Handled = true; return; }
            if (_grid.ReadOnly && e.KeyCode != Keys.C) return;

            if (e.Control && e.KeyCode == Keys.C && !e.Shift) { CopySelectedLogic(); e.Handled = true; }
            else if (e.Control && e.KeyCode == Keys.V) { if (Clipboard.ContainsText()) PasteRequested?.Invoke(this, Clipboard.GetText()); e.Handled = true; }
            else if (e.KeyCode == Keys.Delete) { DeleteSelected(); e.Handled = true; }
        }

        private void Grid_CellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                if (!_grid.Rows[e.RowIndex].Selected)
                {
                    _grid.ClearSelection();
                    _grid.Rows[e.RowIndex].Selected = true;
                }
                if (_grid.ReadOnly) return;

                var colName = _grid.Columns[e.ColumnIndex].Name;
                if (colName == "ColFlag") _flagContextMenu.Show(Cursor.Position);
                else if (colName == "ColLType" || colName == "ColRType") _typeContextMenu.Show(Cursor.Position);
                else if (colName == "ColLSize" || colName == "ColRSize") _sizeContextMenu.Show(Cursor.Position);
                else if (colName == "ColCmp")
                {
                    var flag = _grid.Rows[e.RowIndex].Cells["ColFlag"].Value?.ToString() ?? "";
                    if (RaLogicSyntax.ArithmeticFlags.Contains(flag)) _cmpArithmeticContextMenu.Show(Cursor.Position);
                    else if (RaLogicSyntax.StrictComparisonFlags.Contains(flag)) _cmpStrictContextMenu.Show(Cursor.Position);
                    else _cmpContextMenu.Show(Cursor.Position);
                }
            }
        }

        private void Grid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) Grid_CellMouseDown(sender, new DataGridViewCellMouseEventArgs(e.ColumnIndex, e.RowIndex, 0, 0, new MouseEventArgs(MouseButtons.Right, 1, 0, 0, 0)));
        }

        private void Grid_CellToolTipTextNeeded(object? sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                var colName = _grid.Columns[e.ColumnIndex].Name;
                if (colName == "ColLValue" || colName == "ColRValue")
                {
                    var row = _grid.Rows[e.RowIndex];
                    if (row.Tag is AchievementCondition cond)
                    {
                        Operand op = colName == "ColLValue" ? cond.LeftOperand : cond.RightOperand;
                        if (RaLogicSyntax.MemoryTypes.Contains(op.Type))
                        {
                            string addr = LogicFormatter.NormalizeAddress(op.Value);
                            if (NotesLookup.TryGetValue(addr, out var nodes) && nodes.Count > 0)
                            {
                                var root = nodes.FirstOrDefault(n => n.IndentLevel == -2) ?? nodes[0];
                                e.ToolTipText = Regex.Replace(root.Description, @"\[.*?\]", "").Trim();
                            }
                        }
                    }
                }
            }
        }

        private void Grid_EditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (e.Control is TextBox editingControl && _grid.CurrentCell != null)
            {
                editingControl.CharacterCasing = CharacterCasing.Lower;
            }
        }

        private void Grid_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (_isUpdating || e.RowIndex < 0) return;
            var row = _grid.Rows[e.RowIndex];
            if (row.Tag is not AchievementCondition cond) return;
            var cell = row.Cells[e.ColumnIndex];
            string value = cell.Value?.ToString() ?? "";

            if (cell.OwningColumn.Name == "ColLValue") cond.LeftOperand.Value = LogicFormatter.ParseAndFormatValue(value, cond.LeftOperand.Type, cond.LeftOperand.Size);
            else if (cell.OwningColumn.Name == "ColRValue") cond.RightOperand.Value = LogicFormatter.ParseAndFormatValue(value, cond.RightOperand.Type, LogicFormatter.GetRightOperandSizeReference(cond));
            else if (cell.OwningColumn.Name == "ColHits") { var match = Regex.Match(value, @"\d+"); cond.RequiredHits = match.Success ? uint.Parse(match.Value) : 0; }
        }

        private void Grid_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = _grid.Rows[e.RowIndex];
            if (row.Tag is AchievementCondition cond && _grid.Tag is List<AchievementCondition> list)
            {
                ApplyConditionLogicRules(cond);
                int modelIndex = list.IndexOf(cond);
                _isUpdating = true;
                try
                {
                    ConditionGridRenderer.SetRowValues(row, cond, _grid.ReadOnly, ShowDecimal, ShowAliases, NotesLookup, RangeCache, list, modelIndex);
                }
                finally { _isUpdating = false; }
                LogicChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnContextMenuItemClick(object? sender, EventArgs e)
        {
            if (_grid.ReadOnly || sender is not ToolStripMenuItem item) return;
            if (_grid.Tag is not List<AchievementCondition> list) return;

            string val = item.Tag as string ?? "";
            if (val == RaLogicSyntax.NO_FLAG_TEXT || val == RaLogicSyntax.NO_OPERATOR_TEXT) val = "";

            bool changed = false;
            _isUpdating = true;
            try
            {
                foreach (DataGridViewRow row in _grid.SelectedRows)
                {
                    if (row.Tag is AchievementCondition cond)
                    {
                        if (_grid.CurrentCell == null) continue;
                        string colName = _grid.Columns[_grid.CurrentCell.ColumnIndex].Name;

                        if (colName == "ColFlag") { string old = cond.Flag; cond.Flag = val; HandleFlagChange(cond, old); }
                        else if (colName == "ColLType") HandleTypeChange(cond.LeftOperand, val, cond.LeftOperand.Size);
                        else if (colName == "ColLSize") cond.LeftOperand.Size = val;
                        else if (colName == "ColCmp") { string old = cond.Operator; cond.Operator = val; HandleOperatorChange(cond, old); }
                        else if (colName == "ColRType") HandleTypeChange(cond.RightOperand, val, cond.LeftOperand.Size);
                        else if (colName == "ColRSize") cond.RightOperand.Size = val;

                        ApplyConditionLogicRules(cond);
                        int modelIndex = list.IndexOf(cond);
                        ConditionGridRenderer.SetRowValues(row, cond, _grid.ReadOnly, ShowDecimal, ShowAliases, NotesLookup, RangeCache, list, modelIndex);
                        changed = true;
                    }
                }
            }
            finally { _isUpdating = false; }

            if (changed) { LogicChanged?.Invoke(this, EventArgs.Empty); StatusUpdateRequested?.Invoke(this, "Updated condition."); }
        }

        private void HandleTypeChange(Operand op, string newType, string refSize)
        {
            if (op.Type == newType) return;
            if (newType == "Recall") { op.Value = ""; op.Type = newType; return; }
            if (string.IsNullOrEmpty(op.Value) || op.Type == "Recall")
            {
                if (newType == "Float") op.Value = "1.0";
                else op.Value = "0x" + 1.ToString("x" + LogicFormatter.GetPaddingForSize(refSize));
            }
            op.Type = newType;
        }

        private void HandleFlagChange(AchievementCondition cond, string oldFlag)
        {
            bool wasA = RaLogicSyntax.ArithmeticFlags.Contains(oldFlag);
            bool isA = RaLogicSyntax.ArithmeticFlags.Contains(cond.Flag);
            if (wasA && !isA) { cond.Operator = "="; cond.RightOperand = new Operand { Type = "Value", Value = "0x1" }; }
            else if (!wasA && isA) { cond.Operator = ""; cond.RightOperand = new Operand(); }
        }

        private void HandleOperatorChange(AchievementCondition cond, string oldOp)
        {
            bool wasE = string.IsNullOrEmpty(oldOp);
            bool isE = string.IsNullOrEmpty(cond.Operator);
            if (wasE && !isE) cond.RightOperand = new Operand { Type = "Value", Value = "0x1" };
            else if (!wasE && isE) cond.RightOperand = new Operand();
        }

        private void ApplyConditionLogicRules(AchievementCondition cond)
        {
            if (cond.LeftOperand.Type is "Value" or "Float" or "Recall") cond.LeftOperand.Size = "";
            else if (RaLogicSyntax.MemoryTypes.Contains(cond.LeftOperand.Type) && string.IsNullOrEmpty(cond.LeftOperand.Size)) cond.LeftOperand.Size = "32-bit";

            if (cond.RightOperand.Type is "Value" or "Float" or "Recall") cond.RightOperand.Size = "";
            else if (RaLogicSyntax.MemoryTypes.Contains(cond.RightOperand.Type) && string.IsNullOrEmpty(cond.RightOperand.Size)) cond.RightOperand.Size = "32-bit";

            if (RaLogicSyntax.StrictComparisonFlags.Contains(cond.Flag))
            {
                if (!RaLogicSyntax.ComparisonOperators.Contains(cond.Operator) || string.IsNullOrEmpty(cond.Operator)) { cond.Operator = "="; cond.RightOperand = new Operand { Type = "Value", Value = "0x1" }; }
            }
            else if (RaLogicSyntax.ArithmeticFlags.Contains(cond.Flag))
            {
                if (RaLogicSyntax.ComparisonOperators.Contains(cond.Operator)) { cond.Operator = ""; cond.RightOperand = new Operand(); }
            }
        }

        private List<AchievementCondition> GetEffectiveSelection()
        {
            if (_grid.Tag is not List<AchievementCondition> list) return new List<AchievementCondition>();
            var selected = _grid.SelectedRows.Cast<DataGridViewRow>().Select(r => r.Tag as AchievementCondition).Where(c => c != null).OrderBy(c => list.IndexOf(c!)).Cast<AchievementCondition>().ToList();
            if (!CollapseChains) return selected;

            var expanded = new List<AchievementCondition>();
            var processed = new HashSet<AchievementCondition>();
            foreach (var cond in selected)
            {
                int idx = list.IndexOf(cond);
                if (idx == -1) continue;
                int scan = idx - 1;
                var chain = new List<AchievementCondition>();
                while (scan >= 0)
                {
                    var prev = list[scan];
                    if (prev.Flag == "Add Address") { chain.Insert(0, prev); scan--; } else break;
                }
                foreach (var c in chain) if (processed.Add(c)) expanded.Add(c);
                if (processed.Add(cond)) expanded.Add(cond);
            }
            return expanded;
        }

        public void CopySelectedLogic()
        {
            var conditionsToCopy = GetEffectiveSelection();
            if (conditionsToCopy.Count == 0) return;
            var sb = new StringBuilder();
            foreach (var cond in conditionsToCopy)
            {
                if (sb.Length > 0) sb.Append('_');
                sb.Append(LogicFormatter.ConditionToString(cond));
            }
            Clipboard.SetText(sb.ToString());
            StatusUpdateRequested?.Invoke(this, "Logic string copied.");
        }

        public void PasteLogic()
        {
            if (!Clipboard.ContainsText() || _grid.ReadOnly || _grid.Tag is not List<AchievementCondition> list) return;
            string txt = Clipboard.GetText().Trim();
            if (string.IsNullOrWhiteSpace(txt)) return;

            int insertIdx = list.Count;
            if (_grid.SelectedRows.Count > 0)
            {
                var lastRow = _grid.SelectedRows.Cast<DataGridViewRow>().OrderByDescending(r => r.Index).First();
                if (lastRow.Tag is AchievementCondition c) insertIdx = list.IndexOf(c) + 1;
            }

            // FIXED: Removed the 3rd argument (false) to match RARPEditor's Parser signature
            var groups = Parsers.AchievementParser.ParseAchievementTrigger(txt, 'S');
            var conds = groups.SelectMany(g => g.Conditions).ToList();

            list.InsertRange(insertIdx, conds);
            _isUpdating = true;
            try
            {
                ConditionGridRenderer.PopulateGridRows(_grid, list, _grid.ReadOnly, ShowDecimal, ShowAliases, NotesLookup, RangeCache, CollapseChains);
            }
            finally { _isUpdating = false; }
            LogicChanged?.Invoke(this, EventArgs.Empty);
        }

        public void DeleteSelected()
        {
            if (_grid.ReadOnly || _grid.Tag is not List<AchievementCondition> list) return;
            var toDelete = GetEffectiveSelection();
            if (toDelete.Count == 0) return;

            foreach (var cond in toDelete) list.Remove(cond);

            _isUpdating = true;
            try { ConditionGridRenderer.PopulateGridRows(_grid, list, _grid.ReadOnly, ShowDecimal, ShowAliases, NotesLookup, RangeCache, CollapseChains); }
            finally { _isUpdating = false; }
            LogicChanged?.Invoke(this, EventArgs.Empty);
        }

        public void MoveSelected(int direction)
        {
            if (_grid.ReadOnly || _grid.Tag is not List<AchievementCondition> list || direction == 0) return;
            var selected = GetEffectiveSelection();
            if (selected.Count == 0) return;

            var units = new List<List<AchievementCondition>>();
            if (CollapseChains)
            {
                var cur = new List<AchievementCondition>();
                foreach (var cond in list)
                {
                    cur.Add(cond);
                    if (cond.Flag != "Add Address") { units.Add(new List<AchievementCondition>(cur)); cur.Clear(); }
                }
                if (cur.Count > 0) units.Add(cur);
            }
            else
            {
                foreach (var cond in list) units.Add(new List<AchievementCondition> { cond });
            }

            var selSet = new HashSet<AchievementCondition>(selected);
            var selIndices = new HashSet<int>();
            for (int i = 0; i < units.Count; i++) if (units[i].Any(c => selSet.Contains(c))) selIndices.Add(i);

            var sorted = selIndices.OrderBy(i => i).ToList();
            if (direction < 0 && sorted.Min() == 0) return;
            if (direction > 0 && sorted.Max() == units.Count - 1) return;

            var newUnits = new List<List<AchievementCondition>>(units);
            if (direction < 0)
            {
                foreach (var idx in sorted) { var u = newUnits[idx]; newUnits.RemoveAt(idx); newUnits.Insert(idx - 1, u); }
            }
            else
            {
                for (int i = sorted.Count - 1; i >= 0; i--) { var idx = sorted[i]; var u = newUnits[idx]; newUnits.RemoveAt(idx); newUnits.Insert(idx + 1, u); }
            }

            list.Clear();
            foreach (var u in newUnits) list.AddRange(u);

            _isUpdating = true;
            try { ConditionGridRenderer.PopulateGridRows(_grid, list, _grid.ReadOnly, ShowDecimal, ShowAliases, NotesLookup, RangeCache, CollapseChains); }
            finally { _isUpdating = false; }
            LogicChanged?.Invoke(this, EventArgs.Empty);
        }

        public void AddCondition()
        {
            if (_grid.ReadOnly || _grid.Tag is not List<AchievementCondition> list) return;
            var newCond = new AchievementCondition { LeftOperand = { Type = "Value", Value = "0" }, Operator = "=", RightOperand = { Type = "Value", Value = "1" } };
            int idx = list.Count;
            if (_grid.SelectedRows.Count > 0)
            {
                var r = _grid.SelectedRows.Cast<DataGridViewRow>().OrderByDescending(x => x.Index).First();
                if (r.Tag is AchievementCondition c) idx = list.IndexOf(c) + 1;
            }
            list.Insert(idx, newCond);
            _isUpdating = true;
            try { ConditionGridRenderer.PopulateGridRows(_grid, list, _grid.ReadOnly, ShowDecimal, ShowAliases, NotesLookup, RangeCache, CollapseChains); }
            finally { _isUpdating = false; }
            LogicChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}