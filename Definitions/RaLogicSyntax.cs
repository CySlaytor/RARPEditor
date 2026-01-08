using System.Collections.Generic;

namespace RARPEditor.Definitions
{
    // Centralizes all static definitions for RA Logic syntax
    public static class RaLogicSyntax
    {
        public const string NO_FLAG_TEXT = "(No Flag)";
        public const string NO_OPERATOR_TEXT = "(None)";

        public static readonly string[] Flags = { NO_FLAG_TEXT, "Reset If", "Pause If", "Add Source", "Sub Source", "Add Hits", "Sub Hits", "Add Address", "AndNext", "OrNext", "Measured", "MeasuredIf", "Measured%", "Trigger", "ResetNextIf", "Remember" };
        public static readonly string[] OperandTypes = { "Mem", "Value", "Delta", "Prior", "BCD", "Inverted", "Float", "Recall" };
        public static readonly string[] MemorySizes = { "", "8-bit", "16-bit", "24-bit", "32-bit", "Bit0", "Bit1", "Bit2", "Bit3", "Bit4", "Bit5", "Bit6", "Bit7", "Lower4", "Upper4", "BitCount", "16-bit BE", "24-bit BE", "32-bit BE", "Float", "Float BE", "Double32", "Double32 BE", "MBF32", "MBF32 LE" };
        public static readonly string[] ComparisonOperators = { NO_OPERATOR_TEXT, "=", "!=", "<", "<=", ">", ">=" };
        public static readonly string[] ArithmeticOperators = { NO_OPERATOR_TEXT, "&", "^", "+", "-", "*", "/", "%" };

        public static readonly HashSet<string> ArithmeticFlags = new() { "Add Source", "Sub Source", "Add Address", "Remember" };
        public static readonly HashSet<string> StrictComparisonFlags = new() { "Reset If", "Pause If", "Add Hits", "Sub Hits", "AndNext", "OrNext", "MeasuredIf", "Trigger", "ResetNextIf" };
        public static readonly HashSet<string> FlagsWithoutHits = new() { "Add Source", "Sub Source", "Add Address" };
        public static readonly HashSet<string> MemoryTypes = new() { "Mem", "Delta", "Prior", "BCD", "Inverted" };

        public static readonly Dictionary<string, char> ReverseFlagMap = new() { { "Reset If", 'R' }, { "Pause If", 'P' }, { "Add Source", 'A' }, { "Sub Source", 'B' }, { "Add Hits", 'C' }, { "Sub Hits", 'D' }, { "Add Address", 'I' }, { "AndNext", 'N' }, { "OrNext", 'O' }, { "Measured", 'M' }, { "MeasuredIf", 'Q' }, { "Measured%", 'G' }, { "Trigger", 'T' }, { "ResetNextIf", 'Z' }, { "Remember", 'K' } };
        public static readonly Dictionary<string, char> ReverseSizeMap = new() { { "", ' ' }, { "16-bit", ' ' }, { "8-bit", 'H' }, { "32-bit", 'X' }, { "24-bit", 'W' }, { "Bit0", 'M' }, { "Bit1", 'N' }, { "Bit2", 'O' }, { "Bit3", 'P' }, { "Bit4", 'Q' }, { "Bit5", 'R' }, { "Bit6", 'S' }, { "Bit7", 'T' }, { "Lower4", 'L' }, { "Upper4", 'U' }, { "BitCount", 'K' }, { "32-bit BE", 'G' }, { "16-bit BE", 'I' }, { "24-bit BE", 'J' } };
        public static readonly Dictionary<string, char> ReverseFloatSizeMap = new() { { "Float", 'F' }, { "Float BE", 'B' }, { "Double32", 'H' }, { "Double32 BE", 'I' }, { "MBF32", 'M' }, { "MBF32 LE", 'L' } };
        public static readonly Dictionary<char, string> FloatSizeMap = new() { { 'F', "Float" }, { 'B', "Float BE" }, { 'H', "Double32" }, { 'I', "Double32 BE" }, { 'M', "MBF32" }, { 'L', "MBF32 LE" } };
    }
}