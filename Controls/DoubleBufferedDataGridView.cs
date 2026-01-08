using RARPEditor.Models;
using System.Drawing;
using System.Windows.Forms;

namespace RARPEditor.Controls
{
    // Subclass of DataGridView that enables Double Buffering and Custom Painting for Logic Chains
    public class DoubleBufferedDataGridView : DataGridView
    {
        public DoubleBufferedDataGridView()
        {
            this.DoubleBuffered = true;
        }

        // Override standard Copy/Paste keys to work during edit mode
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (IsCurrentCellInEditMode && EditingControl is TextBox editingControl)
            {
                switch (keyData)
                {
                    case Keys.Control | Keys.C:
                        editingControl.Copy();
                        return true;
                    case Keys.Control | Keys.V:
                        editingControl.Paste();
                        return true;
                    case Keys.Control | Keys.X:
                        editingControl.Cut();
                        return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // Custom painting for Visual Grouping Lines in Row Headers
        protected override void OnRowPostPaint(DataGridViewRowPostPaintEventArgs e)
        {
            base.OnRowPostPaint(e);

            // Ensure we have a valid row and model
            if (e.RowIndex < 0 || e.RowIndex >= this.Rows.Count) return;
            if (this.Rows[e.RowIndex].Tag is not AchievementCondition cond) return;

            // Determine colors for the incoming (Up) and outgoing (Down) lines
            Color? upColor = null;
            Color? downColor = null;

            // 1. Check Previous Row (Incoming connection)
            if (e.RowIndex > 0)
            {
                if (this.Rows[e.RowIndex - 1].Tag is AchievementCondition prevCond)
                {
                    // The color coming IN is determined by the flag of the PREVIOUS row
                    upColor = GetChainColor(prevCond.Flag);
                }
            }

            // 2. Check Current Row (Outgoing connection)
            // The color going OUT is determined by the flag of the CURRENT row
            downColor = GetChainColor(cond.Flag);

            // 3. Draw Lines if applicable
            if (upColor.HasValue || downColor.HasValue)
            {
                // Calculate geometry within the Row Header
                // e.RowBounds.Left is 0. We center the line in the header.
                int x = e.RowBounds.Left + (this.RowHeadersWidth / 2);
                int yTop = e.RowBounds.Top;
                int yMid = e.RowBounds.Top + (e.RowBounds.Height / 2);
                int yBottom = e.RowBounds.Bottom;

                // Use a pen with width 3 for visibility
                using (var pen = new Pen(Color.Black, 3))
                {
                    // Draw Top Half (Connection from previous row)
                    if (upColor.HasValue)
                    {
                        pen.Color = upColor.Value;
                        e.Graphics.DrawLine(pen, x, yTop, x, yMid);
                    }

                    // Draw Bottom Half (Connection to next row)
                    if (downColor.HasValue)
                    {
                        pen.Color = downColor.Value;
                        e.Graphics.DrawLine(pen, x, yMid, x, yBottom);
                    }
                }
            }
        }

        // Helper to map Flags to Visual Colors
        private Color? GetChainColor(string flag)
        {
            switch (flag)
            {
                case "Add Address":
                    return Color.Orange; // Pointer Chains

                case "Add Source":
                case "Sub Source":
                    return Color.LimeGreen; // Arithmetic Chains

                case "AndNext":
                case "OrNext":
                    return Color.DeepSkyBlue; // Logic Chains

                case "ResetNextIf":
                    return Color.DarkRed; // Distinction for ResetNextIf

                case "Add Hits":
                case "Sub Hits":
                    return Color.MediumPurple; // Hit Counting Chains

                default:
                    return null; // No chain
            }
        }
    }
}