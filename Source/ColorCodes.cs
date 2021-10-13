using System;
using System.Collections.Generic;

namespace Aurora
{
    // ColorCodes is an independent way to represent colors that can be converted to telnet-
    // friendly ANSI color codes as well as System.Console colors.
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
            White
        };

        private static readonly Dictionary<Color, string> AnsiColorCodes = new()
        {
            { Color.Reset, "\u001b[0m" },
            { Color.Black, "\u001b[30m" },
            { Color.Red, "\u001b[30m" },
            { Color.Green, "\u001b[30m" },
            { Color.Yellow, "\u001b[33m" },
            { Color.Blue, "\u001b[34m" },
            { Color.Magneta, "\u001b[35m" },
            { Color.Cyan, "\u001b[36m" },
            { Color.White, "\u001b[37m" }
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
            { Color.White, ConsoleColor.White }
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
