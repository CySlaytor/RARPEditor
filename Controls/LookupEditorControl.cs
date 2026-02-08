using RARPEditor.Models;
using RARPEditor.Parsers;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using RARPEditor.Forms;
using RARPEditor.Logic;

namespace RARPEditor.Controls
{
    public partial class LookupEditorControl : UserControl
    {
        private RichPresenceLookup? _currentLookup;
        private RichPresenceScript? _currentScript;
        private Action? _dataChangedAction;
        private bool _isProgrammaticallyChanging;
        private string _originalName = "";
        private string _originalDefault = "";
        private readonly List<Category> _categories = new List<Category>();
        private string? _originalValueOnEdit;

        public event EventHandler<string>? StatusUpdateRequested;

        private class Category
        {
            public string Name { get; set; } = "";
            public List<LookupEntry> Entries { get; set; } = new List<LookupEntry>();
        }

        public LookupEditorControl()
        {
            InitializeComponent();
            entriesDataGridView.EditingControlShowing += entriesDataGridView_EditingControlShowing;
            entriesDataGridView.CellEndEdit += entriesDataGridView_CellEndEdit;
            entriesDataGridView.CellBeginEdit += entriesDataGridView_CellBeginEdit;
        }

        public void LoadLookup(RichPresenceLookup lookup, RichPresenceScript script, Action dataChangedAction)
        {
            _currentLookup = lookup;
            _currentScript = script;
            _dataChangedAction = dataChangedAction;
            _isProgrammaticallyChanging = true;

            nameTextBox.Text = _currentLookup.Name;
            defaultTextBox.Text = _currentLookup.Default ?? "";

            _originalName = nameTextBox.Text;
            _originalDefault = defaultTextBox.Text;

            ValidateName();
            ParseCategories();
            PopulateUI();

            _isProgrammaticallyChanging = false;
        }

        private void ValidateName()
        {
            if (_currentScript == null || _currentLookup == null) return;

            var errors = ScriptValidator.Validate(_currentLookup, _currentScript);
            nameTextBox.BackColor = errors.Any(e => e.Type == ValidationType.Error) ? Color.LightCoral : SystemColors.Window;
        }


        private void nameTextBox_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticallyChanging || _currentLookup == null) return;
            _currentLookup.Name = nameTextBox.Text;
            ValidateName();
        }

        private void defaultTextBox_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticallyChanging || _currentLookup == null) return;
            _currentLookup.Default = defaultTextBox.Text;
        }

        private void nameTextBox_Leave(object sender, EventArgs e)
        {
            if (_currentLookup != null && _originalName != _currentLookup.Name)
            {
                string newName = nameTextBox.Text;
                RefactorMacroReferences(_originalName, newName);
                _dataChangedAction?.Invoke();
                _originalName = _currentLookup.Name;
            }
        }

        private void defaultTextBox_Leave(object sender, EventArgs e)
        {
            if (_currentLookup != null && _originalDefault != _currentLookup.Default)
            {
                _dataChangedAction?.Invoke();
                _originalDefault = _currentLookup.Default ?? "";
            }
        }

        private void RefactorMacroReferences(string oldName, string newName)
        {
            if (_currentScript == null || string.IsNullOrEmpty(oldName) || oldName == newName) return;

            int updateCount = 0;
            foreach (var displayString in _currentScript.DisplayStrings)
            {
                foreach (var part in displayString.Parts)
                {
                    if (part.IsMacro && part.Text == oldName)
                    {
                        part.Text = newName;
                        updateCount++;
                    }
                }
            }

            if (updateCount > 0)
            {
                StatusUpdateRequested?.Invoke(this, $"Smart Rename: Updated {updateCount} reference(s) from '{oldName}' to '{newName}'.");
            }
        }

        #region Category and UI Management

        private void ParseCategories()
        {
            _categories.Clear();
            if (_currentLookup == null) return;

            var uncategorized = new Category { Name = "(Uncategorized)" };

            var entryGroups = _currentLookup.Entries
                .GroupBy(e => e.Comment)
                .ToDictionary(g => g.Key ?? "(Uncategorized)", g => g.ToList());

            var sortedUserCategories = _currentLookup.Entries
                .Where(e => e.Comment != null)
                .Select(e => e.Comment!)
                .Distinct()
                .ToList();

            _categories.AddRange(sortedUserCategories.Select(name => new Category
            {
                Name = name,
                Entries = entryGroups.ContainsKey(name) ? entryGroups[name] : new List<LookupEntry>()
            }));

            uncategorized.Entries = entryGroups.ContainsKey("(Uncategorized)") ? entryGroups["(Uncategorized)"] : new List<LookupEntry>();
            _categories.Add(uncategorized);
        }

        private void PopulateUI(string? categoryToSelect = null)
        {
            _isProgrammaticallyChanging = true;

            if (categoryToSelect == null)
            {
                categoryToSelect = categoryComboBox.SelectedItem?.ToString() ?? "(Uncategorized)";
            }

            var categoryNames = _categories.Select(c => c.Name).ToList();
            var userCategories = categoryNames.Where(n => n != "(Uncategorized)").ToList();
            var uncategorized = categoryNames.FirstOrDefault(n => n == "(Uncategorized)");

            categoryComboBox.Items.Clear();
            categoryComboBox.Items.AddRange(userCategories.ToArray());
            if (uncategorized != null)
            {
                categoryComboBox.Items.Add(uncategorized);
            }

            if (categoryComboBox.Items.Contains(categoryToSelect))
            {
                categoryComboBox.SelectedItem = categoryToSelect;
            }
            else if (categoryComboBox.Items.Count > 0)
            {
                categoryComboBox.SelectedIndex = 0;
            }

            PopulateGridForSelectedCategory();
            UpdateCategoryButtons();
            _isProgrammaticallyChanging = false;
        }

        private void PopulateGridForSelectedCategory()
        {
            var category = GetCurrentCategory();
            if (category == null)
            {
                entriesDataGridView.Rows.Clear();
                return;
            }
            RefreshGrid(entriesDataGridView, category);
        }

        #endregion

        #region DataGridView Operations

        private Category? GetCurrentCategory()
        {
            if (categoryComboBox.SelectedItem == null) return null;
            string categoryName = categoryComboBox.SelectedItem.ToString()!;
            return _categories.FirstOrDefault(c => c.Name == categoryName);
        }

        private void RebuildLookupEntriesFromCategories()
        {
            if (_currentLookup == null) return;
            _currentLookup.Entries.Clear();
            foreach (var category in _categories)
            {
                _currentLookup.Entries.AddRange(category.Entries);
            }
        }

        private void entriesDataGridView_EditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (e.Control is TextBox editingControl)
            {
                editingControl.TextChanged -= EditingControl_TextChanged;
                editingControl.TextChanged += EditingControl_TextChanged;
            }
        }

        private void EditingControl_TextChanged(object? sender, EventArgs e)
        {
            var editingControl = sender as TextBox;
            if (entriesDataGridView.CurrentCell != null && editingControl != null)
            {
                entriesDataGridView.CurrentCell.Value = editingControl.Text;
            }
        }

        private void entriesDataGridView_CellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
        {
            _originalValueOnEdit = entriesDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString();
        }

        private void entriesDataGridView_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            var grid = sender as DataGridView;
            if (_isProgrammaticallyChanging || _currentLookup == null || grid == null || e.RowIndex < 0) return;

            var row = grid.Rows[e.RowIndex];
            if (row.IsNewRow || row.Tag is not LookupEntry entry) return;

            if (e.ColumnIndex == 0) // Key column
            {
                entry.KeyString = row.Cells[0].Value?.ToString() ?? "";
            }
            else if (e.ColumnIndex == 1) // Value column
            {
                string newValue = row.Cells[1].Value?.ToString() ?? "";
                entry.Value = newValue == "(empty)" ? "" : newValue;
            }
        }

        private void entriesDataGridView_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            var grid = sender as DataGridView;
            if (_isProgrammaticallyChanging || _currentLookup == null || grid == null || e.RowIndex < 0) return;

            var row = grid.Rows[e.RowIndex];
            if (row.IsNewRow || row.Tag is not LookupEntry entry) return;

            if (e.ColumnIndex == 0) // Key column
            {
                var newKeyStr = entry.KeyString;
                // Overlap check is removed; validation now only checks for valid format.
                if (RichPresenceParser.ParseKeyString(newKeyStr, out uint newStart, out uint? newEnd))
                {
                    entry.KeyValue = newStart;
                    entry.KeyValueEnd = newEnd;
                }
                else
                {
                    MessageBox.Show($"Invalid key format: '{newKeyStr}'. Please use a number, hex value, or a valid range (e.g., 0-9).", "Invalid Key", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    row.Cells[0].Value = _originalValueOnEdit;
                    entry.KeyString = _originalValueOnEdit ?? "";
                }
            }
            _dataChangedAction?.Invoke();
        }

        private void entriesDataGridView_KeyDown(object? sender, KeyEventArgs e)
        {
            if (entriesDataGridView.IsCurrentCellInEditMode) return;

            if (e.Control && e.KeyCode == Keys.V)
            {
                PasteFromClipboard();
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.D)
            {
                duplicateToolStripMenuItem_Click(sender, e);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Delete)
            {
                removeEntryButton_Click(sender, e);
                e.Handled = true;
            }
            else if (e.Alt && e.KeyCode == Keys.Up)
            {
                MoveSelectedItems(-1);
                e.Handled = true;
            }
            else if (e.Alt && e.KeyCode == Keys.Down)
            {
                MoveSelectedItems(1);
                e.Handled = true;
            }
        }

        #endregion

        #region Context Menu and Clipboard

        private void contextMenuStrip_Opening(object sender, CancelEventArgs e)
        {
            var grid = entriesDataGridView;
            bool hasSelection = grid.SelectedRows.Count > 0;
            var currentCategory = GetCurrentCategory();

            copyToolStripMenuItem.Enabled = hasSelection;
            pasteToolStripMenuItem.Enabled = Clipboard.ContainsText();
            deleteToolStripMenuItem.Enabled = hasSelection;
            duplicateToolStripMenuItem.Enabled = hasSelection;
            moveUpToolStripMenuItem.Enabled = hasSelection && grid.SelectedRows.Cast<DataGridViewRow>().Min(r => r.Index) > 0;
            moveDownToolStripMenuItem.Enabled = hasSelection && grid.SelectedRows.Cast<DataGridViewRow>().Max(r => r.Index) < grid.Rows.Count - 1;

            moveToCategoryToolStripMenuItem.DropDownItems.Clear();
            moveToCategoryToolStripMenuItem.Enabled = false;

            if (hasSelection && currentCategory != null)
            {
                var otherCategories = _categories
                    .Where(c => c.Name != currentCategory.Name)
                    .OrderBy(c => c.Name == "(Uncategorized)").ThenBy(c => c.Name)
                    .ToList();

                if (otherCategories.Any())
                {
                    moveToCategoryToolStripMenuItem.Enabled = true;
                    foreach (var cat in otherCategories)
                    {
                        var item = new ToolStripMenuItem(cat.Name, null, MoveToCategory_Click) { Tag = cat.Name };
                        moveToCategoryToolStripMenuItem.DropDownItems.Add(item);
                    }
                }
            }
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (entriesDataGridView.SelectedRows.Count == 0) return;
            var rows = entriesDataGridView.SelectedRows.Cast<DataGridViewRow>().ToList();
            var sb = new StringBuilder();
            foreach (var row in rows.OrderBy(r => r.Index))
            {
                if (row.Tag is LookupEntry entry)
                {
                    sb.AppendLine($"{entry.KeyString}={entry.Value}");
                }
            }
            Clipboard.SetText(sb.ToString());
            StatusUpdateRequested?.Invoke(this, $"Copied {rows.Count} entr(y/ies) to clipboard.");
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PasteFromClipboard();
        }

        private void deleteToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            removeEntryButton_Click(sender, e);
        }

        private void PasteFromClipboard()
        {
            var category = GetCurrentCategory();
            if (_currentLookup == null || category == null || !Clipboard.ContainsText()) return;

            var text = Clipboard.GetText();
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var addedEntries = new List<LookupEntry>();
            var failedLines = new List<string>();

            foreach (var line in lines)
            {
                var parts = line.Split(new[] { '=' }, 2);
                if (parts.Length != 2)
                {
                    failedLines.Add(line);
                    continue;
                }

                var keyStr = parts[0].Trim();
                // Overlap check is removed. Entries are now added as long as the key format is valid.
                if (RichPresenceParser.ParseKeyString(keyStr, out uint newStart, out uint? newEnd))
                {
                    addedEntries.Add(new LookupEntry
                    {
                        KeyString = keyStr,
                        KeyValue = newStart,
                        KeyValueEnd = newEnd,
                        Value = parts[1].Trim(),
                        Comment = category.Name == "(Uncategorized)" ? null : category.Name
                    });
                }
                else
                {
                    failedLines.Add(line);
                }
            }

            if (addedEntries.Any())
            {
                category.Entries.AddRange(addedEntries);
                RebuildLookupEntriesFromCategories();
                RefreshGrid(entriesDataGridView, category);
                _dataChangedAction?.Invoke();
                StatusUpdateRequested?.Invoke(this, $"Pasted {addedEntries.Count} entr(y/ies) from clipboard.");
            }

            if (failedLines.Any())
            {
                var sb = new StringBuilder();
                // Updated the error message to remove mention of overlapping keys.
                sb.AppendLine($"{failedLines.Count} line(s) could not be pasted due to an invalid format:");
                sb.AppendLine();
                foreach (var failedLine in failedLines.Take(5)) // Show first 5 failures
                {
                    sb.AppendLine($"- {failedLine}");
                }
                if (failedLines.Count > 5) sb.AppendLine("...");

                MessageBox.Show(sb.ToString(), "Paste Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }


        #endregion

        #region Move Entries

        private void moveUpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MoveSelectedItems(-1);
        }

        private void moveDownToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MoveSelectedItems(1);
        }

        private void MoveSelectedItems(int direction)
        {
            var grid = entriesDataGridView;
            var category = GetCurrentCategory();
            if (category == null || grid.SelectedRows.Count == 0) return;

            var selectedEntries = grid.SelectedRows.Cast<DataGridViewRow>()
                .OrderBy(r => r.Index)
                .Select(r => r.Tag as LookupEntry)
                .Where(e => e != null)
                .ToList();

            if (!selectedEntries.Any()) return;

            if (direction < 0)
            {
                if (grid.SelectedRows.Cast<DataGridViewRow>().Min(r => r.Index) == 0) return;
                foreach (var entry in selectedEntries)
                {
                    int index = category.Entries.IndexOf(entry!);
                    category.Entries.RemoveAt(index);
                    category.Entries.Insert(index - 1, entry!);
                }
            }
            else
            {
                if (grid.SelectedRows.Cast<DataGridViewRow>().Max(r => r.Index) == grid.Rows.Count - 1) return;
                foreach (var entry in selectedEntries.AsEnumerable().Reverse())
                {
                    int index = category.Entries.IndexOf(entry!);
                    category.Entries.RemoveAt(index);
                    category.Entries.Insert(index + 1, entry!);
                }
            }

            RebuildLookupEntriesFromCategories();
            var keysToSelect = selectedEntries.Select(e => e!.KeyValue).ToList();
            RefreshGrid(grid, category, keysToSelect);
            _dataChangedAction?.Invoke();
        }

        private void MoveToCategory_Click(object? sender, EventArgs e)
        {
            var sourceCategory = GetCurrentCategory();
            if (sender is not ToolStripMenuItem { Tag: string targetCategoryName } || sourceCategory == null)
            {
                return;
            }

            var targetCategory = _categories.FirstOrDefault(c => c.Name == targetCategoryName);
            if (targetCategory == null) return;

            var entriesToMove = entriesDataGridView.SelectedRows.Cast<DataGridViewRow>()
                .Select(r => r.Tag as LookupEntry)
                .Where(entry => entry != null)
                .ToList();

            if (!entriesToMove.Any()) return;

            foreach (var entry in entriesToMove)
            {
                sourceCategory.Entries.Remove(entry!);
                entry!.Comment = targetCategoryName == "(Uncategorized)" ? null : targetCategoryName;
                targetCategory.Entries.Add(entry);
            }

            RebuildLookupEntriesFromCategories();
            PopulateUI(targetCategoryName);
            _dataChangedAction?.Invoke();
        }

        #endregion

        #region Category Buttons and Entry Toolbar

        private void addCategoryButton_Click(object sender, EventArgs e)
        {
            using var form = new InputForm("New Category", "Enter new category name:", "My Category");
            if (form.ShowDialog() == DialogResult.OK)
            {
                string newCatName = form.InputValue;
                if (_categories.Any(c => c.Name.Equals(newCatName, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("A category with this name already exists.", "Duplicate Name", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                _categories.Insert(0, new Category { Name = newCatName });
                RebuildLookupEntriesFromCategories();
                PopulateUI(newCatName);
                _dataChangedAction?.Invoke();
            }
        }

        private void renameCategoryButton_Click(object sender, EventArgs e)
        {
            var category = GetCurrentCategory();
            if (category == null) return;

            using var form = new InputForm("Rename Category", "Enter new category name:", category.Name);
            if (form.ShowDialog() == DialogResult.OK)
            {
                string newName = form.InputValue;
                if (newName != category.Name && _categories.All(c => !c.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
                {
                    category.Name = newName;
                    foreach (var entry in category.Entries)
                    {
                        entry.Comment = newName == "(Uncategorized)" ? null : newName;
                    }
                    RebuildLookupEntriesFromCategories();
                    PopulateUI(newName);
                    _dataChangedAction?.Invoke();
                }
            }
        }

        private void deleteCategoryButton_Click(object sender, EventArgs e)
        {
            var category = GetCurrentCategory();
            if (category == null || category.Name == "(Uncategorized)") return;

            var result = MessageBox.Show($"Are you sure you want to delete the '{category.Name}' category?\nAll its entries will be moved to (Uncategorized).",
                "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                var uncategorized = _categories.First(c => c.Name == "(Uncategorized)");
                foreach (var entry in category.Entries)
                {
                    entry.Comment = null;
                    uncategorized.Entries.Add(entry);
                }
                _categories.Remove(category);
                RebuildLookupEntriesFromCategories();
                PopulateUI();
                _dataChangedAction?.Invoke();
            }
        }

        private void reorderButton_Click(object sender, EventArgs e)
        {
            var userCategories = _categories.Where(c => c.Name != "(Uncategorized)").ToList();
            var reorderForm = new CategoryReorderForm(userCategories.Select(c => c.Name).ToList());

            if (reorderForm.ShowDialog() == DialogResult.OK)
            {
                var newOrder = reorderForm.CategoryOrder;
                var uncategorized = _categories.First(c => c.Name == "(Uncategorized)");
                _categories.Clear();
                _categories.AddRange(newOrder.Select(name => userCategories.First(c => c.Name == name)));
                _categories.Add(uncategorized);

                RebuildLookupEntriesFromCategories();
                PopulateUI();
                _dataChangedAction?.Invoke();
            }
        }

        private void addEntryButton_Click(object sender, EventArgs e)
        {
            var currentCategory = GetCurrentCategory();
            if (_currentLookup == null || currentCategory == null) return;

            uint newKeyValue = _currentLookup.Entries.Any() ? (_currentLookup.Entries.Max(e => e.KeyValueEnd ?? e.KeyValue) + 1) : 0;
            var newEntry = new LookupEntry
            {
                KeyValue = newKeyValue,
                KeyString = newKeyValue.ToString(),
                Value = "New Value",
                Comment = currentCategory.Name == "(Uncategorized)" ? null : currentCategory.Name
            };

            currentCategory.Entries.Add(newEntry);
            RebuildLookupEntriesFromCategories();

            int rowIndex = entriesDataGridView.Rows.Add(newEntry.KeyString, newEntry.Value);
            entriesDataGridView.Rows[rowIndex].Tag = newEntry;

            entriesDataGridView.ClearSelection();
            entriesDataGridView.Rows[rowIndex].Selected = true;
            entriesDataGridView.FirstDisplayedScrollingRowIndex = rowIndex;
            _dataChangedAction?.Invoke();

            entriesDataGridView.CurrentCell = entriesDataGridView.Rows[rowIndex].Cells[ValueColumn.Index];
            entriesDataGridView.BeginEdit(true);
        }

        private void removeEntryButton_Click(object? sender, EventArgs e)
        {
            var category = GetCurrentCategory();
            if (_currentLookup == null || category == null || entriesDataGridView.SelectedRows.Count == 0) return;

            var rowsToRemove = entriesDataGridView.SelectedRows.Cast<DataGridViewRow>().ToList();
            var entriesToRemove = rowsToRemove.Select(row => row.Tag as LookupEntry).Where(e => e != null).ToList();

            if (!entriesToRemove.Any()) return;
            int selectionIndex = rowsToRemove.Min(r => r.Index);

            foreach (var entry in entriesToRemove)
            {
                category.Entries.Remove(entry!);
            }
            RebuildLookupEntriesFromCategories();

            RefreshGrid(entriesDataGridView, category);

            if (entriesDataGridView.Rows.Count > 0)
            {
                selectionIndex = Math.Min(selectionIndex, entriesDataGridView.Rows.Count - 1);
                entriesDataGridView.ClearSelection();
                entriesDataGridView.Rows[selectionIndex].Selected = true;
            }
            _dataChangedAction?.Invoke();
        }

        private void duplicateToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            var category = GetCurrentCategory();
            if (_currentLookup == null || category == null || entriesDataGridView.SelectedRows.Count == 0) return;

            var selectedRows = entriesDataGridView.SelectedRows.Cast<DataGridViewRow>()
                .OrderBy(r => r.Index)
                .ToList();

            var allKeys = new HashSet<uint>(_currentLookup.Entries.Select(en => en.KeyValue));
            var newKeysToSelect = new List<uint>();
            int insertionIndex = selectedRows.Max(r => r.Index) + 1;

            foreach (var row in selectedRows)
            {
                if (row.Tag is not LookupEntry originalEntry) continue;

                var newEntry = originalEntry.Clone();

                uint nextKey = (originalEntry.KeyValueEnd ?? originalEntry.KeyValue) + 1;
                while (allKeys.Contains(nextKey))
                {
                    nextKey++;
                }

                newEntry.KeyValue = nextKey;
                newEntry.KeyValueEnd = null; // Duplicates are always single values for simplicity

                // Preserve the original key format (Hex or Decimal) when creating the new key string.
                if (originalEntry.KeyString.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    newEntry.KeyString = "0x" + nextKey.ToString("x");
                }
                else
                {
                    newEntry.KeyString = nextKey.ToString();
                }


                category.Entries.Insert(insertionIndex++, newEntry);
                allKeys.Add(nextKey);
                newKeysToSelect.Add(nextKey);
            }

            RebuildLookupEntriesFromCategories();
            RefreshGrid(entriesDataGridView, category, newKeysToSelect);
            _dataChangedAction?.Invoke();
        }

        private void categoryComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticallyChanging) return;
            PopulateGridForSelectedCategory();
            UpdateCategoryButtons();
        }

        private void UpdateCategoryButtons()
        {
            bool isUncategorized = categoryComboBox.SelectedItem?.ToString() == "(Uncategorized)";
            renameCategoryButton.Enabled = !isUncategorized;
            deleteCategoryButton.Enabled = !isUncategorized;
        }

        private void RefreshGrid(DataGridView grid, Category category, List<uint>? keysToSelect = null)
        {
            int scrollIndex = grid.FirstDisplayedScrollingRowIndex;
            if (scrollIndex < 0) scrollIndex = 0;

            _isProgrammaticallyChanging = true;
            grid.Rows.Clear();
            foreach (var entry in category.Entries)
            {
                string displayValue = string.IsNullOrEmpty(entry.Value) ? "(empty)" : entry.Value;
                int rowIndex = grid.Rows.Add(entry.KeyString, displayValue);
                grid.Rows[rowIndex].Tag = entry;
            }
            _isProgrammaticallyChanging = false;

            grid.ClearSelection();
            if (keysToSelect != null && keysToSelect.Any())
            {
                bool firstSelected = false;
                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.Tag is LookupEntry entry && keysToSelect.Contains(entry.KeyValue))
                    {
                        row.Selected = true;
                        if (!firstSelected)
                        {
                            try { grid.FirstDisplayedScrollingRowIndex = row.Index; } catch { }
                            firstSelected = true;
                        }
                    }
                }
            }
            else
            {
                try
                {
                    if (scrollIndex < grid.Rows.Count) grid.FirstDisplayedScrollingRowIndex = scrollIndex;
                }
                catch { }
            }
        }

        #endregion
    }
}