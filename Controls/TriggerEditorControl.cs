using RARPEditor.Definitions;
using RARPEditor.Helpers;
using RARPEditor.Models;
using RARPEditor.Parsers;
using RARPEditor.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace RARPEditor.Controls
{
    public partial class TriggerEditorControl : UserControl
    {
        private readonly ConditionGridManager _gridManager;
        private string _triggerText = "";
        private List<AchievementCondition> _conditions = new(); // The model
        private bool _collapseChains = false;

        public event EventHandler? TriggerTextChanged;
        public event EventHandler<string>? StatusUpdateRequested;

        public char GroupSeparator { get; set; } = 'S';

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string TriggerText { get => _triggerText; set { _triggerText = value; UpdateUIFromText(); } }
        public string GroupBoxText { get => mainGroupBox.Text; set => mainGroupBox.Text = value; }

        public TriggerEditorControl()
        {
            InitializeComponent();
            _gridManager = new ConditionGridManager(triggerGrid);
            InitializeEvents();
            SetupGridColumns();
        }

        public void SetNotes(List<CodeNote> notes)
        {
            var parser = new PointerTreeParser();
            _gridManager.NotesLookup.Clear();
            _gridManager.RangeCache.Clear();

            if (notes == null) return;

            foreach (var note in notes)
            {
                string key = LogicFormatter.NormalizeAddress(note.Address);
                var nodes = parser.ParseNoteText(note.Note);
                _gridManager.NotesLookup[key] = nodes;

                if (nodes.Count > 0 && nodes[0].Size > 1)
                {
                    long start = long.Parse(key.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);
                    _gridManager.RangeCache.Add((start, start + nodes[0].Size, nodes[0]));
                }
            }
            RefreshGrid();
        }

        private void InitializeEvents()
        {
            _gridManager.LogicChanged += (s, e) => BuildTriggerAndNotify();
            _gridManager.StatusUpdateRequested += (s, msg) => StatusUpdateRequested?.Invoke(this, msg);
            _gridManager.PasteRequested += (s, txt) => { Clipboard.SetText(txt); _gridManager.PasteLogic(); };

            showDecimalCheckBox.CheckedChanged += (s, e) => { _gridManager.ShowDecimal = showDecimalCheckBox.Checked; RefreshGrid(); };
            chkAliasMode.CheckedChanged += (s, e) => { _gridManager.ShowAliases = chkAliasMode.Checked; RefreshGrid(); };
            collapseChainsCheckBox.CheckedChanged += (s, e) => { _collapseChains = collapseChainsCheckBox.Checked; _gridManager.CollapseChains = _collapseChains; RefreshGrid(); };

            contextMenuStrip.Opening += ContextMenu_Opening;
            copyLogicToolStripMenuItem.Click += (s, e) => _gridManager.CopySelectedLogic();
            pasteToolStripMenuItem.Click += (s, e) => _gridManager.PasteLogic();
            moveUpToolStripMenuItem.Click += (s, e) => _gridManager.MoveSelected(-1);
            moveDownToolStripMenuItem.Click += (s, e) => _gridManager.MoveSelected(1);
            deleteToolStripMenuItem.Click += (s, e) => _gridManager.DeleteSelected();
            duplicateToolStripMenuItem.Click += (s, e) => { _gridManager.CopySelectedLogic(); _gridManager.PasteLogic(); };
            addConditionToolStripMenuItem.Click += (s, e) => _gridManager.AddCondition();

            clearButton.Click += (s, e) => {
                if (MessageBox.Show("Clear logic?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    TriggerText = "";
            };
            copyButton.Click += (s, e) => _gridManager.CopySelectedLogic();
        }

        private void SetupGridColumns()
        {
            AddCol("ColID", "ID", 35, true);
            AddCol("ColFlag", "Flag", 80, false);
            AddCol("ColLType", "Type", 50, false);
            AddCol("ColLSize", "Size", 60, false);
            AddCol("ColLValue", "Memory", 100, false);
            AddCol("ColCmp", "Cmp", 40, false);
            AddCol("ColRType", "Type", 50, false);
            AddCol("ColRSize", "Size", 60, false);
            AddCol("ColRValue", "Mem/Val", 100, false);
            AddCol("ColHits", "Hits", 50, false);
        }

        private void AddCol(string name, string header, int width, bool ro)
        {
            var col = new DataGridViewTextBoxColumn { Name = name, HeaderText = header, Width = width, ReadOnly = ro, SortMode = DataGridViewColumnSortMode.NotSortable };
            if (name == "ColFlag" || name == "ColLValue" || name == "ColRValue") col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            triggerGrid.Columns.Add(col);
        }

        private void UpdateUIFromText()
        {
            // Use existing parser to get groups, but flatten them since this control usually edits one group at a time
            var groups = AchievementParser.ParseAchievementTrigger(_triggerText, GroupSeparator);
            _conditions = groups.SelectMany(g => g.Conditions).ToList();

            // Bind to manager
            triggerGrid.Tag = _conditions;
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            _gridManager.BeginUpdate();
            ConditionGridRenderer.PopulateGridRows(triggerGrid, _conditions, false,
                showDecimalCheckBox.Checked, chkAliasMode.Checked,
                _gridManager.NotesLookup, _gridManager.RangeCache, _collapseChains);
            _gridManager.EndUpdate();
        }

        private void BuildTriggerAndNotify()
        {
            var sb = new StringBuilder();
            foreach (var cond in _conditions)
            {
                if (sb.Length > 0) sb.Append('_');
                sb.Append(LogicFormatter.ConditionToString(cond));
            }
            string newText = sb.ToString();
            if (_triggerText != newText)
            {
                _triggerText = newText;
                TriggerTextChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ContextMenu_Opening(object? sender, CancelEventArgs e)
        {
            bool any = triggerGrid.SelectedRows.Count > 0;
            moveUpToolStripMenuItem.Enabled = any;
            moveDownToolStripMenuItem.Enabled = any;
            deleteToolStripMenuItem.Enabled = any;
            duplicateToolStripMenuItem.Enabled = any;
            copyLogicToolStripMenuItem.Enabled = any;
            pasteToolStripMenuItem.Enabled = Clipboard.ContainsText();
        }
    }
}