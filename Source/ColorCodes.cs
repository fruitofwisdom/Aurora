using System;
using System.Collections.Generic;

namespace Aurora
{
    // ColorCodes is an independent way to represent colors that can be converted to telnet-
    // friendly ANSI color codes as well as System.Console colors. The 8 standard colors are
    // supported as well as their bright terminal counterparts. TODO: Support the 256-color
    // extended set in the future?
    internal class ColorCodes
    {
        public enum Color
        {
            Reset,
            Black,
            Red,
            Green,
            Yellow,
            Blue,
            Magneta,
            Cyan,
            White,
            BrightBlack,
            BrightRed,
            BrightGreen,
            BrightYellow,
            BrightBlue,
            BrightMagenta,
            BrightCyan,
            BrightWhite
        };

        private static readonly Dictionary<Color, string> AnsiColorCodes = new()
        {
            { Color.Reset, "\u001b[0m" },
            { Color.Black, "\u001b[30m" },
            { Color.Red, "\u001b[31m" },
            { Color.Green, "\u001b[32m" },
            { Color.Yellow, "\u001b[33m" },
            { Color.Blue, "\u001b[34m" },
            { Color.Magneta, "\u001b[35m" },
            { Color.Cyan, "\u001b[36m" },
            { Color.White, "\u001b[37m" },
            { Color.BrightBlack, "\u001b[30;1m" },
            { Color.BrightRed, "\u001b[31;1m" },
            { Color.BrightGreen, "\u001b[32;1m" },
            { Color.BrightYellow, "\u001b[33;1m" },
            { Color.BrightBlue, "\u001b[34;1m" },
            { Color.BrightMagenta, "\u001b[35;1m" },
            { Color.BrightCyan, "\u001b[36;1m" },
            { Color.BrightWhite, "\u001b[37;1m" }
		};

        private static readonly Dictionary<Color, ConsoleColor> ConsoleColors = new()
        {
            { Color.Reset, ConsoleColor.Gray },
            { Color.Black, ConsoleColor.Black },
            { Color.Red, ConsoleColor.Red },
            { Color.Green, ConsoleColor.Green },
            { Color.Yellow, ConsoleColor.Yellow },
            { Color.Blue, ConsoleColor.Blue },
            { Color.Magneta, ConsoleColor.Magenta },
            { Color.Cyan, ConsoleColor.Cyan },
            { Color.White, ConsoleColor.White },
			{ Color.BrightBlack, ConsoleColor.Black },
			{ Color.BrightRed, ConsoleColor.Red },
			{ Color.BrightGreen, ConsoleColor.Green },
			{ Color.BrightYellow, ConsoleColor.Yellow },
			{ Color.BrightBlue, ConsoleColor.Blue },
			{ Color.BrightMagenta, ConsoleColor.Magenta },
			{ Color.BrightCyan, ConsoleColor.Cyan },
			{ Color.BrightWhite, ConsoleColor.White }
		};

        public static string GetAnsiColorCode(Color color)
        {
            string ansiColorCode;
            ansiColorCode = AnsiColorCodes[color];
            return ansiColorCode;
        }

        public static ConsoleColor GetConsoleColor(Color color)
        {
            ConsoleColor consoleColor;
            consoleColor = ConsoleColors[color];
            return consoleColor;
        }
    }
}
