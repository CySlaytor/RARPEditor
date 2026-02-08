using RARPEditor.Models;
using RARPEditor.Parsers;
using System.IO;
using System.Text;
using RARPEditor.Controls;
using System.Collections.Specialized;
using RARPEditor.Properties;
using System.Drawing.Drawing2D;
using RARPEditor.Logic;
using RARPEditor.Forms;
using System.Text.RegularExpressions;
using Timer = System.Windows.Forms.Timer;

namespace RARPEditor.Forms
{
    public partial class MainForm : Form
    {
        private RichPresenceScript _currentScript;
        private string _currentFilePath = "";
        private bool _isDirty;

        // Cache for Code Notes to pass to new tabs/controls
        private List<CodeNote> _currentNotes = new List<CodeNote>();

        private readonly Timer _undoDebounceTimer;
        private const int DebounceIntervalMs = 500;

        private string _projectFilter = "";

        private const string AppTitle = "RARP Editor";
        private const string UnsavedChangesMarker = "*";

        private const string LookupsNodeKey = "lookupsNode";
        private const string FormattersNodeKey = "formattersNode";
        private const string DisplayLogicNodeKey = "displayLogicNode";

        private readonly LookupEditorControl _lookupEditor;
        private readonly FormatterEditorControl _formatterEditor;
        private readonly DisplayLogicEditorControl _displayLogicEditor;
        private readonly LivePreviewControl _livePreviewControl;
        private readonly HelpAndValidationControl _helpAndValidationControl;

        private readonly StateManager<RichPresenceScript> _stateManager;
        private readonly List<TreeNode> _selectedNodes = new List<TreeNode>();
        private TreeNode? _anchorNode;

        private const string ErrorIconKey = "errorIcon";
        private const string SuccessIconKey = "successIcon";
        private const string WarningIconKey = "warningIcon";
        private const string InfoIconKey = "infoIcon";

        public MainForm()
        {
            InitializeComponent();
            _currentScript = new RichPresenceScript();
            _stateManager = new StateManager<RichPresenceScript>(_currentScript.Clone());

            _undoDebounceTimer = new Timer { Interval = DebounceIntervalMs };
            _undoDebounceTimer.Tick += UndoDebounceTimer_Tick;

            InitializeTreeViewImageList();

            _lookupEditor = new LookupEditorControl { Dock = DockStyle.Fill, Visible = false };
            _formatterEditor = new FormatterEditorControl { Dock = DockStyle.Fill, Visible = false };
            _displayLogicEditor = new DisplayLogicEditorControl { Dock = DockStyle.Fill, Visible = false };
            _livePreviewControl = new LivePreviewControl { Dock = DockStyle.Fill };
            _helpAndValidationControl = new HelpAndValidationControl { Dock = DockStyle.Fill };

            _displayLogicEditor.NewMacroRequested += DisplayLogicEditor_NewMacroRequested;

            _lookupEditor.StatusUpdateRequested += OnStatusUpdateRequested;
            _displayLogicEditor.StatusUpdateRequested += OnStatusUpdateRequested;

            editorPanel.Controls.Add(_lookupEditor);
            editorPanel.Controls.Add(_formatterEditor);
            editorPanel.Controls.Add(_displayLogicEditor);
            previewContainer.Panel1.Controls.Add(_helpAndValidationControl);
            previewContainer.Panel2.Controls.Add(_livePreviewControl);

            CheckAndCreateDefaultDisplayString();

            UpdateFormTitle();
            PopulateProjectExplorer();
            UpdateUndoRedoMenu();
            BuildRecentFilesMenu();

            var defaultDisplayNode = FindNodeByTag(projectExplorerTreeView.Nodes, _currentScript.DisplayStrings.First(ds => ds.IsDefault));
            if (defaultDisplayNode != null)
            {
                projectExplorerTreeView.SelectedNode = defaultDisplayNode;
            }

            statusLabel.Text = "Ready";
        }

        private void OnStatusUpdateRequested(object? sender, string message)
        {
            UpdateStatus(message);
        }

        private void UpdateStatus(string message)
        {
            statusLabel.Text = message;
        }


        private void DisplayLogicEditor_NewMacroRequested(object? sender, NewMacroEventArgs e)
        {
            var newLookup = new RichPresenceLookup { Name = e.Name };

            if (e.Type == MacroType.Lookup)
            {
                newLookup.Format = "VALUE";
                newLookup.Default = "";
            }
            else // Formatter
            {
                newLookup.Format = "SCORE";
            }

            _currentScript.Lookups.Add(newLookup);

            // Reorder execution. PopulateProjectExplorer recreates nodes (defaulting to Error Icon).
            // OnDataChanged runs validation. We must populate FIRST, then validate.
            PopulateProjectExplorer();
            OnDataChanged();

            var newNode = FindNodeByTag(projectExplorerTreeView.Nodes, newLookup);
            if (newNode != null)
            {
                projectExplorerTreeView.SelectedNode = newNode;
            }
        }

        private void InitializeTreeViewImageList()
        {
            var imageList = new ImageList();

            // Error Icon (!)
            var errorBmp = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(errorBmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.FillEllipse(Brushes.Red, new Rectangle(0, 0, 15, 15));
                g.DrawString("!", new Font("Arial", 10, FontStyle.Bold), Brushes.White, 2, 0);
            }
            imageList.Images.Add(ErrorIconKey, errorBmp);

            // Success Icon (✓)
            var successBmp = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(successBmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.FillEllipse(Brushes.Green, new Rectangle(0, 0, 15, 15));
                g.DrawString("✓", new Font("Arial", 10, FontStyle.Bold), Brushes.White, 0, 0);
            }
            imageList.Images.Add(SuccessIconKey, successBmp);

            // Warning Icon (⚠)
            var warningBmp = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(warningBmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var points = new Point[] { new Point(8, 1), new Point(15, 14), new Point(1, 14) };
                g.FillPolygon(Brushes.Gold, points);
                g.DrawString("!", new Font("Arial", 10, FontStyle.Bold), Brushes.Black, 4, -1);
            }
            imageList.Images.Add(WarningIconKey, warningBmp);


            // Info Icon (i)
            var infoBmp = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(infoBmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.FillEllipse(Brushes.DodgerBlue, new Rectangle(0, 0, 15, 15));
                g.DrawString("i", new Font("Arial", 10, FontStyle.Italic | FontStyle.Bold), Brushes.White, 4, 0);
            }
            imageList.Images.Add(InfoIconKey, infoBmp);

            projectExplorerTreeView.ImageList = imageList;
        }


        private void UpdateFormTitle()
        {
            string fileName = string.IsNullOrEmpty(_currentFilePath) ? "Untitled" : Path.GetFileName(_currentFilePath);
            string dirtyMarker = _isDirty ? UnsavedChangesMarker : "";
            this.Text = $"{fileName}{dirtyMarker} - {AppTitle}";
        }

        private void OnDataChanged()
        {
            if (!_isDirty)
            {
                _isDirty = true;
                UpdateFormTitle();
            }

            _undoDebounceTimer.Stop();
            _undoDebounceTimer.Start();

            UpdateActiveNodeText();

            if (projectExplorerTreeView.SelectedNode?.Tag is RichPresenceDisplayString displayStringForPreview)
            {
                _livePreviewControl.LoadDisplayString(displayStringForPreview, _currentScript);
            }

            _helpAndValidationControl.UpdateView(projectExplorerTreeView.SelectedNode?.Tag ?? projectExplorerTreeView.SelectedNode, _currentScript);

            ValidateAllNodes();
        }

        private void UndoDebounceTimer_Tick(object? sender, EventArgs e)
        {
            _undoDebounceTimer.Stop();
            _stateManager.RecordState(_currentScript.Clone());
            UpdateUndoRedoMenu();
        }

        private void UpdateActiveNodeText()
        {
            if (projectExplorerTreeView.SelectedNode != null)
            {
                var node = projectExplorerTreeView.SelectedNode;
                switch (node.Tag)
                {
                    case RichPresenceLookup lookup:
                        node.Text = lookup.Name;
                        break;
                    case RichPresenceDisplayString displayString:
                        node.Text = displayString.ToFormattedString(_currentScript);
                        break;
                }
            }
        }

        private void RefactorMacroReferences(string oldName, string newName)
        {
            if (string.IsNullOrEmpty(oldName) || oldName == newName) return;

            bool changed = false;
            foreach (var displayString in _currentScript.DisplayStrings)
            {
                foreach (var part in displayString.Parts)
                {
                    if (part.IsMacro && part.Text == oldName)
                    {
                        part.Text = newName;
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                UpdateStatus($"Refactored references from '{oldName}' to '{newName}'.");
                PopulateProjectExplorer();
            }
        }

        #region Undo / Redo

        private void ApplyState(RichPresenceScript state)
        {
            _currentScript = state.Clone();
            var selectedIdentifiers = GetSelectedNodes()
                .Select(n => n.Tag)
                .Where(t => t != null)
                .ToList();

            PopulateProjectExplorer();
            ValidateAllNodes();

            var nodesToSelect = new List<TreeNode>();
            foreach (var identifier in selectedIdentifiers)
            {
                TreeNode? foundNode = FindNodeByTag(projectExplorerTreeView.Nodes, identifier!);
                if (foundNode != null)
                {
                    nodesToSelect.Add(foundNode);
                }
            }

            if (nodesToSelect.Any())
            {
                ClearAndSelectNode(nodesToSelect.First(), true);
                foreach (var node in nodesToSelect.Skip(1))
                {
                    ToggleNodeInSelection(node);
                }
                LoadEditorForSelectedNode();
            }
            else
            {
                ClearEditorPanel();
            }

            UpdateUndoRedoMenu();
        }


        private TreeNode? FindNodeByTag(TreeNodeCollection nodes, object tag)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Tag is RichPresenceLookup lookupTag && tag is RichPresenceLookup lookupToFind)
                {
                    if (lookupTag.Name.Equals(lookupToFind.Name, StringComparison.OrdinalIgnoreCase)) return node;
                }
                else if (node.Tag is RichPresenceDisplayString dsTag && tag is RichPresenceDisplayString dsToFind)
                {
                    if (dsTag.InternalId == dsToFind.InternalId) return node;
                }

                var foundNode = FindNodeByTag(node.Nodes, tag);
                if (foundNode != null) return foundNode;
            }
            return null;
        }

        private void editToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            this.ActiveControl = null;
        }

        private void UpdateUndoRedoMenu()
        {
            undoToolStripMenuItem.Enabled = _stateManager.CanUndo;
            redoToolStripMenuItem.Enabled = _stateManager.CanRedo;
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.ActiveControl = null;
            _undoDebounceTimer.Stop();

            var previousState = _stateManager.Undo();
            if (previousState != null)
            {
                ApplyState(previousState);
                UpdateStatus("Undo successful.");
            }
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.ActiveControl = null;
            _undoDebounceTimer.Stop();

            var futureState = _stateManager.Redo();
            if (futureState != null)
            {
                ApplyState(futureState);
                UpdateStatus("Redo successful.");
            }
        }

        #endregion

        #region File Menu Handlers
        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!PromptToSaveIfDirty()) return;

            _currentScript = new RichPresenceScript();
            CheckAndCreateDefaultDisplayString();
            _currentFilePath = "";
            _isDirty = false;

            // Clear notes and reset editor state
            _currentNotes.Clear();
            _displayLogicEditor.SetNotes(_currentNotes);

            _stateManager.Clear();
            _stateManager.RecordState(_currentScript.Clone());
            UpdateUndoRedoMenu();

            PopulateProjectExplorer();
            UpdateFormTitle();
            UpdateStatus("New script created.");

            var defaultDisplayNode = FindNodeByTag(projectExplorerTreeView.Nodes, _currentScript.DisplayStrings.First(ds => ds.IsDefault));
            if (defaultDisplayNode != null)
            {
                projectExplorerTreeView.SelectedNode = defaultDisplayNode;
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!PromptToSaveIfDirty()) return;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                LoadFile(openFileDialog.FileName);
            }
        }

        private void EnforceLookupDefaults(RichPresenceScript script)
        {
            foreach (var lookup in script.Lookups)
            {
                if (lookup.Entries.Any() && lookup.Default == null)
                {
                    lookup.Default = "";
                }
            }
        }

        private void CheckAndCreateDefaultDisplayString()
        {
            foreach (var ds in _currentScript.DisplayStrings) ds.IsDefault = false;

            var lastDisplayString = _currentScript.DisplayStrings.LastOrDefault();

            if (lastDisplayString != null && string.IsNullOrEmpty(lastDisplayString.Condition) && lastDisplayString.Parts.All(p => !p.IsMacro))
            {
                lastDisplayString.IsDefault = true;
            }
            else
            {
                var newDefault = new RichPresenceDisplayString
                {
                    IsDefault = true,
                    Parts = { new RichPresenceDisplayPart { Text = "Playing..." } }
                };
                _currentScript.DisplayStrings.Add(newDefault);
            }
        }


        private void LoadFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show($"File not found:\n{filePath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                RemoveRecentFile(filePath);
                return;
            }

            try
            {
                string scriptContent = File.ReadAllText(filePath);
                _currentScript = RichPresenceParser.Parse(scriptContent);
                EnforceLookupDefaults(_currentScript);
                CheckAndCreateDefaultDisplayString();
                _currentFilePath = filePath;

                // Load Notes!
                _currentNotes = CodeNoteLoader.LoadNotesForRichFile(filePath);

                // Pass notes to the editor (which will pass it to trigger editors)
                _displayLogicEditor.SetNotes(_currentNotes);

                _stateManager.Clear();
                _stateManager.RecordState(_currentScript.Clone());
                UpdateUndoRedoMenu();

                PopulateProjectExplorer();
                ValidateAllNodes();
                UpdateStatus($"Successfully loaded '{Path.GetFileName(_currentFilePath)}' with {_currentNotes.Count} code notes.");
                _isDirty = false;
                UpdateFormTitle();
                AddRecentFile(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open or parse file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveCurrentScript();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveAsCurrentScript();
        }

        private bool SaveCurrentScript()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                return SaveAsCurrentScript();
            }
            else
            {
                SaveScriptToFile(_currentFilePath);
                return true;
            }
        }

        private bool SaveAsCurrentScript()
        {
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                SaveScriptToFile(saveFileDialog.FileName);
                return true;
            }
            return false;
        }


        private void SaveScriptToFile(string path)
        {
            try
            {
                string scriptContent = RichPresenceParser.Serialize(_currentScript);
                File.WriteAllText(path, scriptContent);
                _currentFilePath = path;
                UpdateStatus($"Script saved to '{Path.GetFileName(_currentFilePath)}'.");
                _isDirty = false;
                UpdateFormTitle();
                AddRecentFile(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!PromptToSaveIfDirty())
            {
                e.Cancel = true;
            }
        }

        private bool PromptToSaveIfDirty()
        {
            if (!_isDirty) return true;

            var result = MessageBox.Show(
                "You have unsaved changes. Do you want to save them?",
                "Unsaved Changes",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning);

            switch (result)
            {
                case DialogResult.Yes:
                    return SaveCurrentScript();
                case DialogResult.No:
                    return true;
                case DialogResult.Cancel:
                default:
                    return false;
            }
        }
        #endregion

        #region Recent Files
        private void BuildRecentFilesMenu()
        {
            recentFilesToolStripMenuItem.DropDownItems.Clear();
            if (Settings.Default.RecentFiles == null || Settings.Default.RecentFiles.Count == 0)
            {
                recentFilesToolStripMenuItem.Enabled = false;
                return;
            }

            recentFilesToolStripMenuItem.Enabled = true;
            foreach (var file in Settings.Default.RecentFiles)
            {
                var item = new ToolStripMenuItem(file, null, RecentFile_Click) { Tag = file };
                recentFilesToolStripMenuItem.DropDownItems.Add(item);
            }
            recentFilesToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            recentFilesToolStripMenuItem.DropDownItems.Add("Clear", null, ClearRecentFiles_Click);
        }

        private void AddRecentFile(string path)
        {
            if (Settings.Default.RecentFiles == null)
            {
                Settings.Default.RecentFiles = new StringCollection();
            }

            Settings.Default.RecentFiles.Remove(path);
            Settings.Default.RecentFiles.Insert(0, path);

            while (Settings.Default.RecentFiles.Count > 10)
            {
                Settings.Default.RecentFiles.RemoveAt(10);
            }

            Settings.Default.Save();
            BuildRecentFilesMenu();
        }

        private void RemoveRecentFile(string path)
        {
            Settings.Default.RecentFiles?.Remove(path);
            Settings.Default.Save();
            BuildRecentFilesMenu();
        }

        private void RecentFile_Click(object? sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem { Tag: string filePath })
            {
                if (!PromptToSaveIfDirty()) return;
                LoadFile(filePath);
            }
        }

        private void ClearRecentFiles_Click(object? sender, EventArgs e)
        {
            Settings.Default.RecentFiles?.Clear();
            Settings.Default.Save();
            BuildRecentFilesMenu();
        }
        #endregion

        #region UI Population & Updates

        private string GetIconKeyForValidationResults(List<ValidationResult> results)
        {
            if (results.Any(r => r.Type == ValidationType.Error)) return ErrorIconKey;
            if (results.Any(r => r.Type == ValidationType.Warning)) return WarningIconKey;
            if (results.Any(r => r.Type == ValidationType.Info)) return InfoIconKey;
            return SuccessIconKey;
        }

        private void ValidateAllNodes()
        {
            foreach (TreeNode parentNode in projectExplorerTreeView.Nodes)
            {
                ValidationType overallSeverity = ValidationType.Success;

                if (parentNode.Nodes.Count == 0)
                {
                    parentNode.ImageKey = InfoIconKey;
                    parentNode.SelectedImageKey = InfoIconKey;
                    continue;
                }

                foreach (TreeNode node in parentNode.Nodes)
                {
                    List<ValidationResult> results = new List<ValidationResult>();
                    if (node.Tag is RichPresenceLookup lookup)
                    {
                        results = ScriptValidator.Validate(lookup, _currentScript);
                    }
                    else if (node.Tag is RichPresenceDisplayString displayString)
                    {
                        results = ScriptValidator.Validate(displayString, _currentScript);
                    }

                    string iconKey = GetIconKeyForValidationResults(results);
                    if (node.ImageKey != iconKey) node.ImageKey = iconKey;
                    if (node.SelectedImageKey != iconKey) node.SelectedImageKey = iconKey;

                    if (results.Any(r => r.Type == ValidationType.Error))
                    {
                        overallSeverity = ValidationType.Error;
                    }
                    else if (results.Any(r => r.Type == ValidationType.Warning) && overallSeverity != ValidationType.Error)
                    {
                        overallSeverity = ValidationType.Warning;
                    }
                    else if (results.Any(r => r.Type == ValidationType.Info) && overallSeverity is ValidationType.Success)
                    {
                        overallSeverity = ValidationType.Info;
                    }
                }

                string parentIconKey = overallSeverity switch
                {
                    ValidationType.Error => ErrorIconKey,
                    ValidationType.Warning => WarningIconKey,
                    ValidationType.Info => InfoIconKey,
                    _ => SuccessIconKey,
                };

                if (parentNode.ImageKey != parentIconKey) parentNode.ImageKey = parentIconKey;
                if (parentNode.SelectedImageKey != parentIconKey) parentNode.SelectedImageKey = parentIconKey;
            }
        }

        private void ClearProjectExplorer()
        {
            _selectedNodes.Clear();
            projectExplorerTreeView.Nodes[LookupsNodeKey]?.Nodes.Clear();
            projectExplorerTreeView.Nodes[FormattersNodeKey]?.Nodes.Clear();
            projectExplorerTreeView.Nodes[DisplayLogicNodeKey]?.Nodes.Clear();
        }

        private void PopulateProjectExplorer()
        {
            projectExplorerTreeView.BeginUpdate();
            ClearProjectExplorer();

            var lookupsNode = projectExplorerTreeView.Nodes[LookupsNodeKey];
            var formattersNode = projectExplorerTreeView.Nodes[FormattersNodeKey];
            var displayLogicNode = projectExplorerTreeView.Nodes[DisplayLogicNodeKey];

            if (lookupsNode == null || formattersNode == null || displayLogicNode == null)
            {
                projectExplorerTreeView.EndUpdate();
                return;
            }

            string filter = _projectFilter.Trim();
            bool isFiltering = !string.IsNullOrEmpty(filter);

            foreach (var lookup in _currentScript.Lookups)
            {
                if (isFiltering && !lookup.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)) continue;

                var node = new TreeNode(lookup.Name) { Tag = lookup };
                if (lookup.Format == "VALUE" && (lookup.Entries.Any() || lookup.Default != null))
                {
                    lookupsNode.Nodes.Add(node);
                }
                else
                {
                    formattersNode.Nodes.Add(node);
                }
            }

            foreach (var displayString in _currentScript.DisplayStrings)
            {
                string formattedText = displayString.ToFormattedString(_currentScript);
                if (isFiltering && !formattedText.Contains(filter, StringComparison.OrdinalIgnoreCase)) continue;

                var node = new TreeNode(formattedText) { Tag = displayString };
                displayLogicNode.Nodes.Add(node);
            }

            projectExplorerTreeView.ExpandAll();
            projectExplorerTreeView.EndUpdate();
        }

        private void searchTextBox_TextChanged(object sender, EventArgs e)
        {
            _projectFilter = searchTextBox.Text;
            PopulateProjectExplorer();

            ValidateAllNodes();

            if (projectExplorerTreeView.SelectedNode == null)
            {
                ClearEditorPanel();
            }
            else
            {
                projectExplorerTreeView.SelectedNode.EnsureVisible();
            }
        }
        #endregion

        #region Event Handlers
        private void projectExplorerTreeView_AfterSelect(object? sender, TreeViewEventArgs e)
        {
            if (e?.Node != null)
            {
                if (!_selectedNodes.Contains(e.Node) && Control.ModifierKeys == Keys.None)
                {
                    ClearSelectionStyles();
                    _selectedNodes.Clear();
                    _selectedNodes.Add(e.Node);
                    e.Node.BackColor = SystemColors.Highlight;
                    e.Node.ForeColor = SystemColors.HighlightText;
                }
            }
            else
            {
                ClearEditorPanel();
            }

            _helpAndValidationControl.UpdateView(e?.Node?.Tag ?? e?.Node, _currentScript);
        }

        private void LoadEditorForSelectedNode()
        {
            var primaryNode = projectExplorerTreeView.SelectedNode;
            if (primaryNode?.Tag == null)
            {
                ClearEditorPanel();
                return;
            }

            switch (primaryNode.Tag)
            {
                case RichPresenceLookup lookup:
                    _formatterEditor.Visible = false;
                    _displayLogicEditor.Visible = false;
                    welcomeLabel.Visible = false;
                    _livePreviewControl.ClearPreview();

                    if (lookup.Entries.Any() || lookup.Default != null)
                    {
                        _lookupEditor.LoadLookup(lookup, _currentScript, OnDataChanged);
                        _lookupEditor.Visible = true;
                    }
                    else
                    {
                        _lookupEditor.Visible = false;
                        _formatterEditor.LoadFormatter(lookup, _currentScript, OnDataChanged);
                        _formatterEditor.Visible = true;
                    }
                    break;
                case RichPresenceDisplayString displayString:
                    _lookupEditor.Visible = false;
                    _formatterEditor.Visible = false;
                    welcomeLabel.Visible = false;
                    // Pass existing notes to ensure tooltips/aliases work
                    _displayLogicEditor.SetNotes(_currentNotes);
                    _displayLogicEditor.LoadDisplayString(displayString, _currentScript, OnDataChanged);
                    _displayLogicEditor.Visible = true;
                    _livePreviewControl.LoadDisplayString(displayString, _currentScript);
                    break;
                default:
                    ClearEditorPanel();
                    break;
            }
        }

        private void ClearEditorPanel()
        {
            _lookupEditor.Visible = false;
            _formatterEditor.Visible = false;
            _displayLogicEditor.Visible = false;
            _livePreviewControl.ClearPreview();
            welcomeLabel.Visible = true;
        }

        private void projectExplorerTreeView_MouseDown(object sender, MouseEventArgs e)
        {
            var node = projectExplorerTreeView.GetNodeAt(e.X, e.Y);
            if (node == null) return;

            if (e.Button == MouseButtons.Right)
            {
                if (!_selectedNodes.Contains(node))
                {
                    ClearAndSelectNode(node, true);
                }
                return;
            }

            if (e.Button != MouseButtons.Left) return;

            if (Control.ModifierKeys != Keys.Shift)
            {
                _anchorNode = node;
            }

            if (Control.ModifierKeys == Keys.Control)
            {
                ToggleNodeInSelection(node);
            }
            else if (Control.ModifierKeys == Keys.Shift)
            {
                SelectNodeRange(_anchorNode, node);
            }
            else
            {
                ClearAndSelectNode(node, true);
            }
        }

        private void projectExplorerTreeView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.D)
            {
                duplicateToolStripMenuItem_Click(sender, e);
                e.Handled = true;
                return;
            }

            if (e.KeyCode == Keys.Delete)
            {
                deleteToolStripMenuItem_Click(sender, e);
                e.Handled = true;
                return;
            }

            if (e.KeyCode == Keys.Enter)
            {
                if (projectExplorerTreeView.SelectedNode != null)
                {
                    LoadEditorForSelectedNode();
                    e.Handled = true;
                    return;
                }
            }

            TreeNode? nextNode = null;
            if (e.KeyCode == Keys.Up) nextNode = projectExplorerTreeView.SelectedNode?.PrevVisibleNode;
            if (e.KeyCode == Keys.Down) nextNode = projectExplorerTreeView.SelectedNode?.NextVisibleNode;

            if (nextNode == null) return;

            if (e.Shift)
            {
                SelectNodeRange(_anchorNode, nextNode);
                projectExplorerTreeView.SelectedNode = nextNode;
                e.Handled = true;
            }
            else
            {
                _anchorNode = nextNode;
                ClearAndSelectNode(nextNode, true);
                e.Handled = true;
            }
        }

        private void projectExplorerTreeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node != null)
            {
                LoadEditorForSelectedNode();
            }
        }
        #endregion

        #region Project Explorer Context Menu
        private void projectExplorerContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var selectedNode = projectExplorerTreeView.SelectedNode;

            if (selectedNode == null)
            {
                e.Cancel = true;
                return;
            }

            var selectedItems = GetSelectedNodes();
            bool hasSelection = selectedItems.Any();
            bool containsParents = selectedItems.Any(n => n.Tag == null);
            bool containsDefault = selectedItems.Any(n => n.Tag is RichPresenceDisplayString ds && ds.IsDefault);

            bool allLookups = hasSelection && selectedItems.All(n => n.Tag is RichPresenceLookup lookup && (lookup.Entries.Any() || lookup.Default != null));
            bool allFormatters = hasSelection && selectedItems.All(n => n.Tag is RichPresenceLookup lookup && !lookup.Entries.Any() && lookup.Default == null);
            bool allDisplayLogic = hasSelection && selectedItems.All(n => n.Tag is RichPresenceDisplayString);

            addLookupToolStripMenuItem.Enabled = false;
            addFormatterToolStripMenuItem.Enabled = false;
            addDisplayToolStripMenuItem.Enabled = false;

            deleteToolStripMenuItem.Enabled = hasSelection && !containsDefault && !containsParents;
            duplicateToolStripMenuItem.Enabled = hasSelection && !containsDefault && !containsParents;
            moveUpToolStripMenuItem.Enabled = (allDisplayLogic || allLookups || allFormatters) && !containsDefault && !containsParents;
            moveDownToolStripMenuItem.Enabled = (allDisplayLogic || allLookups || allFormatters) && !containsDefault && !containsParents;

            if (allDisplayLogic && !containsDefault)
            {
                var defaultString = _currentScript.DisplayStrings.LastOrDefault(ds => ds.IsDefault);
                if (defaultString != null)
                {
                    int secondToLastIndex = _currentScript.DisplayStrings.Count - 2;
                    if (secondToLastIndex >= 0)
                    {
                        var secondToLastItem = _currentScript.DisplayStrings[secondToLastIndex];
                        if (selectedItems.Any(n => n.Tag == secondToLastItem))
                        {
                            moveDownToolStripMenuItem.Enabled = false;
                        }
                    }
                }
            }

            string nodeKey = selectedNode.Parent?.Name ?? selectedNode.Name ?? "";

            switch (nodeKey)
            {
                case LookupsNodeKey:
                    addLookupToolStripMenuItem.Enabled = true;
                    break;

                case FormattersNodeKey:
                    addFormatterToolStripMenuItem.Enabled = true;
                    break;

                case DisplayLogicNodeKey:
                    addDisplayToolStripMenuItem.Enabled = true;
                    break;
            }
        }

        private void moveUpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selectedNodes = GetSelectedNodes();
            if (!selectedNodes.Any() || selectedNodes.First().Index == 0) return;

            var firstItemTag = selectedNodes.First().Tag;

            if (firstItemTag is RichPresenceDisplayString)
            {
                var selectedItems = GetDisplayStringsFromNodes(selectedNodes);
                foreach (var item in selectedItems)
                {
                    int currentIndex = _currentScript.DisplayStrings.IndexOf(item);
                    if (currentIndex > 0)
                    {
                        _currentScript.DisplayStrings.RemoveAt(currentIndex);
                        _currentScript.DisplayStrings.Insert(currentIndex - 1, item);
                    }
                }
            }
            else if (firstItemTag is RichPresenceLookup)
            {
                var selectedItems = GetLookupsFromNodes(selectedNodes);
                foreach (var item in selectedItems)
                {
                    int currentIndex = _currentScript.Lookups.IndexOf(item);
                    if (currentIndex > 0)
                    {
                        _currentScript.Lookups.RemoveAt(currentIndex);
                        _currentScript.Lookups.Insert(currentIndex - 1, item);
                    }
                }
            }
            else { return; }

            OnDataChanged();
            UpdateStatus($"Moved {selectedNodes.Count} item(s) up.");

            projectExplorerTreeView.BeginUpdate();
            foreach (var node in selectedNodes)
            {
                var parent = node.Parent;
                int currentIndex = node.Index;
                if (parent != null && currentIndex > 0)
                {
                    parent.Nodes.RemoveAt(currentIndex);
                    parent.Nodes.Insert(currentIndex - 1, node);
                }
            }
            projectExplorerTreeView.SelectedNode = selectedNodes.First();
            projectExplorerTreeView.EndUpdate();
            selectedNodes.First().EnsureVisible();
        }

        private void moveDownToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selectedNodes = GetSelectedNodes();
            if (!selectedNodes.Any()) return;

            var lastNode = selectedNodes.Last();
            var parent = lastNode.Parent;
            if (parent == null || lastNode.Index >= parent.Nodes.Count - 1) return;

            var firstItemTag = selectedNodes.First().Tag;

            if (firstItemTag is RichPresenceDisplayString)
            {
                var selectedItems = GetDisplayStringsFromNodes(selectedNodes).AsEnumerable().Reverse().ToList();
                foreach (var item in selectedItems)
                {
                    int currentIndex = _currentScript.DisplayStrings.IndexOf(item);
                    if (currentIndex < _currentScript.DisplayStrings.Count - 1 && !_currentScript.DisplayStrings[currentIndex + 1].IsDefault)
                    {
                        _currentScript.DisplayStrings.RemoveAt(currentIndex);
                        _currentScript.DisplayStrings.Insert(currentIndex + 1, item);
                    }
                }
            }
            else if (firstItemTag is RichPresenceLookup)
            {
                var selectedItems = GetLookupsFromNodes(selectedNodes).AsEnumerable().Reverse().ToList();
                foreach (var item in selectedItems)
                {
                    int currentIndex = _currentScript.Lookups.IndexOf(item);
                    if (currentIndex < _currentScript.Lookups.Count - 1)
                    {
                        _currentScript.Lookups.RemoveAt(currentIndex);
                        _currentScript.Lookups.Insert(currentIndex + 1, item);
                    }
                }
            }
            else { return; }

            OnDataChanged();
            UpdateStatus($"Moved {selectedNodes.Count} item(s) down.");

            projectExplorerTreeView.BeginUpdate();
            foreach (var node in selectedNodes.AsEnumerable().Reverse())
            {
                int currentIndex = node.Index;
                if (currentIndex < parent.Nodes.Count - 1)
                {
                    parent.Nodes.RemoveAt(currentIndex);
                    parent.Nodes.Insert(currentIndex + 1, node);
                }
            }
            projectExplorerTreeView.SelectedNode = selectedNodes.First();
            projectExplorerTreeView.EndUpdate();
            selectedNodes.Last().EnsureVisible();
        }

        private void addDisplayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var newDisplayString = new RichPresenceDisplayString
            {
                Condition = "1=1",
                Parts = { new RichPresenceDisplayPart { Text = "New Display String" } }
            };

            int insertionIndex = _currentScript.DisplayStrings.Count > 0 ? _currentScript.DisplayStrings.Count - 1 : 0;
            _currentScript.DisplayStrings.Insert(insertionIndex, newDisplayString);

            OnDataChanged();
            UpdateStatus("Added new display string.");

            var displayLogicNode = projectExplorerTreeView.Nodes[DisplayLogicNodeKey];
            if (displayLogicNode != null)
            {
                var newNode = new TreeNode(newDisplayString.ToFormattedString(_currentScript)) { Tag = newDisplayString };
                displayLogicNode.Nodes.Insert(insertionIndex, newNode);
                ClearAndSelectNode(newNode, true);
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var nodesToDelete = GetSelectedNodes();
            if (!nodesToDelete.Any()) return;

            if (nodesToDelete.Any(n => n.Tag == null)) return;

            if (nodesToDelete.Any(n => n.Tag is RichPresenceDisplayString ds && ds.IsDefault))
            {
                MessageBox.Show("The default display string cannot be deleted.", "Action Prohibited", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirmResult = MessageBox.Show($"Are you sure you want to delete {nodesToDelete.Count} item(s)?", "Confirm Deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirmResult == DialogResult.No) return;

            TreeNode? parent = nodesToDelete.First().Parent;
            int topIndex = nodesToDelete.First().Index;

            foreach (var node in nodesToDelete)
            {
                switch (node.Tag)
                {
                    case RichPresenceDisplayString ds:
                        _currentScript.DisplayStrings.Remove(ds);
                        break;
                    case RichPresenceLookup lookup:
                        _currentScript.Lookups.Remove(lookup);
                        break;
                }
            }

            // Do NOT call OnDataChanged here (which runs validation on the old tree).
            // Instead, we remove the UI nodes first, then call OnDataChanged at the end.
            // OnDataChanged();
            UpdateStatus($"Deleted {nodesToDelete.Count} item(s).");

            projectExplorerTreeView.BeginUpdate();
            foreach (var node in nodesToDelete)
            {
                _selectedNodes.Remove(node);
                node.Remove();
            }
            projectExplorerTreeView.EndUpdate();

            // Now that the tree is updated (nodes removed), run validation/state saving.
            OnDataChanged();

            if (parent != null && parent.Nodes.Count > 0)
            {
                int newIndex = Math.Min(topIndex, parent.Nodes.Count - 1);
                ClearAndSelectNode(parent.Nodes[newIndex], true);
            }
            else if (parent != null)
            {
                ClearAndSelectNode(parent, true);
            }
        }

        private void duplicateToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            var nodesToDuplicate = GetSelectedNodes();
            if (!nodesToDuplicate.Any()) return;

            if (nodesToDuplicate.Any(n => n.Tag == null)) return;

            if (nodesToDuplicate.Any(n => n.Tag is RichPresenceDisplayString ds && ds.IsDefault)) return;

            var newTreeNodesToSelect = new List<TreeNode>();
            projectExplorerTreeView.BeginUpdate();

            foreach (var node in nodesToDuplicate)
            {
                TreeNode newNode;
                int insertionTreeIndex = node.Index + 1;
                var parentNode = node.Parent;
                if (parentNode == null) continue;

                switch (node.Tag)
                {
                    case RichPresenceDisplayString ds:
                        var newDs = ds.Clone();
                        _currentScript.DisplayStrings.Insert(insertionTreeIndex, newDs);
                        newNode = new TreeNode(newDs.ToFormattedString(_currentScript)) { Tag = newDs };
                        parentNode.Nodes.Insert(insertionTreeIndex, newNode);
                        newTreeNodesToSelect.Add(newNode);
                        break;
                    case RichPresenceLookup lookup:
                        var newLookup = lookup.Clone();
                        newLookup.Name = GetUniqueName(lookup.Name);
                        int dataIndex = _currentScript.Lookups.IndexOf(lookup);
                        _currentScript.Lookups.Insert(dataIndex + 1, newLookup);

                        newNode = new TreeNode(newLookup.Name) { Tag = newLookup };
                        parentNode.Nodes.Insert(insertionTreeIndex, newNode);
                        newTreeNodesToSelect.Add(newNode);
                        break;
                }
            }

            OnDataChanged();
            UpdateStatus($"Duplicated {nodesToDuplicate.Count} item(s).");

            projectExplorerTreeView.EndUpdate();

            if (newTreeNodesToSelect.Any())
            {
                ClearAndSelectNode(newTreeNodesToSelect.First(), true);
                foreach (var node in newTreeNodesToSelect.Skip(1))
                {
                    ToggleNodeInSelection(node, false);
                }
                newTreeNodesToSelect.First().EnsureVisible();
            }
        }

        private string GetUniqueName(string baseName)
        {
            var match = Regex.Match(baseName, @"^(.*?)(\d+)$");
            string rootName;
            int counter;

            if (match.Success)
            {
                rootName = match.Groups[1].Value;
                counter = int.Parse(match.Groups[2].Value) + 1;
            }
            else
            {
                rootName = baseName;
                counter = 1;
            }

            string newName;
            do
            {
                newName = $"{rootName}{counter++}";
            }
            while (_currentScript.Lookups.Any(l => l.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)));

            return newName;
        }

        private void addLookupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var newLookup = new RichPresenceLookup
            {
                Name = GetUniqueName("NewLookup"),
                Format = "VALUE",
                Default = ""
            };
            _currentScript.Lookups.Add(newLookup);
            OnDataChanged();
            UpdateStatus($"Added lookup '{newLookup.Name}'.");
            var lookupsNode = projectExplorerTreeView.Nodes[LookupsNodeKey];
            if (lookupsNode != null)
            {
                var newNode = new TreeNode(newLookup.Name) { Tag = newLookup };
                lookupsNode.Nodes.Add(newNode);
                ClearAndSelectNode(newNode, true);
            }
        }

        private void addFormatterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var newFormatter = new RichPresenceLookup { Name = GetUniqueName("NewFormatter"), Format = "SCORE" };
            _currentScript.Lookups.Add(newFormatter);
            OnDataChanged();
            UpdateStatus($"Added formatter '{newFormatter.Name}'.");
            var formattersNode = projectExplorerTreeView.Nodes[FormattersNodeKey];
            if (formattersNode != null)
            {
                var newNode = new TreeNode(newFormatter.Name) { Tag = newFormatter };
                formattersNode.Nodes.Add(newNode);
                ClearAndSelectNode(newNode, true);
            }
        }
        #endregion

        #region Drag and Drop
        private void projectExplorerTreeView_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if (e.Item is TreeNode node && node.Tag is RichPresenceDisplayString ds && !ds.IsDefault)
            {
                DoDragDrop(GetSelectedNodes(), DragDropEffects.Move);
            }
        }

        private void projectExplorerTreeView_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void projectExplorerTreeView_DragDrop(object sender, DragEventArgs e)
        {
            Point targetPoint = projectExplorerTreeView.PointToClient(new Point(e.X, e.Y));
            TreeNode? targetNode = projectExplorerTreeView.GetNodeAt(targetPoint);
            var draggedNodes = e.Data.GetData(typeof(List<TreeNode>)) as List<TreeNode>;

            if (draggedNodes == null || !draggedNodes.Any() || targetNode == null || targetNode.Parent == null ||
                targetNode.Parent.Name != DisplayLogicNodeKey)
            {
                return;
            }

            var draggedItems = GetDisplayStringsFromNodes(draggedNodes);
            if (!draggedItems.Any()) return;

            int targetIndex = targetNode.Index;

            var defaultString = _currentScript.DisplayStrings.LastOrDefault(d => d.IsDefault);
            if (defaultString != null)
            {
                int defaultNodeIndex = -1;
                for (int i = 0; i < targetNode.Parent.Nodes.Count; i++)
                {
                    if (targetNode.Parent.Nodes[i].Tag == defaultString)
                    {
                        defaultNodeIndex = i;
                        break;
                    }
                }

                if (defaultNodeIndex != -1 && targetIndex >= defaultNodeIndex)
                {
                    targetIndex = defaultNodeIndex;
                }
            }


            foreach (var item in draggedItems)
            {
                _currentScript.DisplayStrings.Remove(item);
            }

            int itemsBeforeTarget = draggedItems.Count(ds => _currentScript.DisplayStrings.IndexOf(ds) < targetIndex);
            targetIndex -= itemsBeforeTarget;


            if (targetIndex > _currentScript.DisplayStrings.Count)
            {
                targetIndex = _currentScript.DisplayStrings.Count;
            }

            _currentScript.DisplayStrings.InsertRange(targetIndex, draggedItems);

            OnDataChanged();
            PopulateAndRestoreSelection(draggedItems.Cast<object>().ToList());
        }
        #endregion

        #region Multi-Select Helpers
        private void ClearAndSelectNode(TreeNode node, bool setPrimary)
        {
            ClearSelectionStyles();
            _selectedNodes.Clear();
            _selectedNodes.Add(node);
            node.BackColor = SystemColors.Highlight;
            node.ForeColor = SystemColors.HighlightText;
            if (setPrimary) projectExplorerTreeView.SelectedNode = node;
        }

        private void ToggleNodeInSelection(TreeNode node, bool setPrimary = true)
        {
            if (!(node.Tag is RichPresenceDisplayString) && !(node.Tag is RichPresenceLookup)) return;

            if (_selectedNodes.Contains(node))
            {
                _selectedNodes.Remove(node);
                node.BackColor = projectExplorerTreeView.BackColor;
                node.ForeColor = projectExplorerTreeView.ForeColor;
            }
            else
            {
                _selectedNodes.Add(node);
                node.BackColor = SystemColors.Highlight;
                node.ForeColor = SystemColors.HighlightText;
            }
            if (setPrimary) projectExplorerTreeView.SelectedNode = node;
        }

        private void SelectNodeRange(TreeNode? startNode, TreeNode? endNode)
        {
            if (startNode == null || endNode == null || startNode.Parent != endNode.Parent) return;
            if (!(endNode.Tag is RichPresenceDisplayString) && !(endNode.Tag is RichPresenceLookup)) return;


            ClearSelectionStyles();
            _selectedNodes.Clear();

            var parent = endNode.Parent;
            if (parent == null) return;
            int start = Math.Min(startNode.Index, endNode.Index);
            int end = Math.Max(startNode.Index, endNode.Index);
            for (int i = start; i <= end; i++)
            {
                var nodeInRange = parent.Nodes[i];
                _selectedNodes.Add(nodeInRange);
                nodeInRange.BackColor = SystemColors.Highlight;
                nodeInRange.ForeColor = SystemColors.HighlightText;
            }
        }

        private void ClearSelectionStyles()
        {
            foreach (var n in _selectedNodes)
            {
                n.BackColor = projectExplorerTreeView.BackColor;
                n.ForeColor = projectExplorerTreeView.ForeColor;
            }
        }

        private List<TreeNode> GetSelectedNodes()
        {
            if (_selectedNodes.Any()) return _selectedNodes.OrderBy(n => n.Index).ToList();
            if (projectExplorerTreeView.SelectedNode != null)
                return new List<TreeNode> { projectExplorerTreeView.SelectedNode };
            return new List<TreeNode>();
        }

        private List<RichPresenceDisplayString> GetDisplayStringsFromNodes(List<TreeNode> nodes)
        {
            return nodes
                .Select(n => n.Tag)
                .OfType<RichPresenceDisplayString>()
                .OrderBy(ds => _currentScript.DisplayStrings.IndexOf(ds))
                .ToList();
        }

        private List<RichPresenceLookup> GetLookupsFromNodes(List<TreeNode> nodes)
        {
            return nodes
                .Select(n => n.Tag)
                .OfType<RichPresenceLookup>()
                .OrderBy(l => _currentScript.Lookups.IndexOf(l))
                .ToList();
        }

        private void PopulateAndRestoreSelection(List<object> itemsToSelect)
        {
            PopulateProjectExplorer();
            ValidateAllNodes();
            var nodesToSelect = new List<TreeNode>();
            foreach (var item in itemsToSelect)
            {
                TreeNode? foundNode = FindNodeByTag(projectExplorerTreeView.Nodes, item);
                if (foundNode != null)
                {
                    nodesToSelect.Add(foundNode);
                }
            }
            if (nodesToSelect.Any())
            {
                ClearAndSelectNode(nodesToSelect[0], true);
                for (int i = 1; i < nodesToSelect.Count; i++)
                {
                    ToggleNodeInSelection(nodesToSelect[i]);
                }
            }
        }
        #endregion
    }
}