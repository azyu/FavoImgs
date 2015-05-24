using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FavoImgs
{
    public static class ConsoleHelper
    {
        public static void WriteColoredLine(ConsoleColor color, String format, params object[] arg)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(format, arg);
            Console.ResetColor();
        }

        public static void WriteException(Exception ex)
        {
            WriteColoredLine(ConsoleColor.Yellow, " - {0}", ex.Message);
        }
    }
}
