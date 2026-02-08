using RARPEditor.Models;
using RARPEditor.Logic;

namespace RARPEditor.Controls
{
    public partial class FormatterEditorControl : UserControl
    {
        private RichPresenceLookup? _currentFormatter;
        private RichPresenceScript? _currentScript;
        private Action? _dataChangedAction;
        private bool _isProgrammaticallyChanging;
        // Store the original name to prevent creating undo states when no changes are made.
        private string _originalName = "";

        public event EventHandler<string>? StatusUpdateRequested;
        public FormatterEditorControl()
        {
            InitializeComponent();
            // Use the list of raw Format Types (VALUE, SECS, etc.)
            formatTypeComboBox.Items.AddRange(RichPresenceLookup.FormatTypes);
        }

        public void LoadFormatter(RichPresenceLookup formatter, RichPresenceScript script, Action dataChangedAction)
        {
            _currentFormatter = formatter;
            _currentScript = script;
            _dataChangedAction = dataChangedAction;
            _isProgrammaticallyChanging = true;

            nameTextBox.Text = _currentFormatter.Name;
            // Store the name when the control is loaded.
            _originalName = _currentFormatter.Name;

            int formatIndex = Array.IndexOf(RichPresenceLookup.FormatTypes, _currentFormatter.Format.ToUpper());
            formatTypeComboBox.SelectedIndex = formatIndex >= 0 ? formatIndex : 0;

            ValidateName();
            _isProgrammaticallyChanging = false;
        }

        private void ValidateName()
        {
            if (_currentScript == null || _currentFormatter == null) return;

            var errors = ScriptValidator.Validate(_currentFormatter, _currentScript);
            nameTextBox.BackColor = errors.Any() ? Color.LightCoral : SystemColors.Window;
        }

        private void nameTextBox_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticallyChanging || _currentFormatter == null) return;
            _currentFormatter.Name = nameTextBox.Text;
            ValidateName();
            // The expensive data changed action is no longer called on every keystroke.
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

        private void nameTextBox_Leave(object sender, EventArgs e)
        {
            if (_currentFormatter != null && _originalName != _currentFormatter.Name)
            {
                string newName = nameTextBox.Text;
                RefactorMacroReferences(_originalName, newName);
                _dataChangedAction?.Invoke();
                _originalName = _currentFormatter.Name;
            }
        }

        private void formatTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticallyChanging || _currentFormatter == null) return;
            _currentFormatter.Format = formatTypeComboBox.SelectedItem?.ToString() ?? "VALUE";
            _dataChangedAction?.Invoke();
        }
    }
}