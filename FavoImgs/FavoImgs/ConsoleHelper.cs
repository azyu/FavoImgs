using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FavoImgs
{
    public static class ConsoleHelper
    {
        public static void WriteException(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(" - {0}", ex.Message);
            Console.ResetColor();
        }
    }
}
