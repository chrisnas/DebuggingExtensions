
using System;

namespace ParallelStacks.Runtime
{
    public class ConsoleRenderer : IRenderer
    {
        private readonly bool _useDml;

        public ConsoleRenderer(bool useDml)
        {
            _useDml = useDml;
        }

        public void Write(string text)
        {
            Console.Write(text);
        }

        public void WriteCount(string count)
        {
            if (_useDml)
            {
                Console.Write($"<col fg=\"srcpair\" bg=\"wbg\">{count}</col>");
                return;
            }

            WriteWithColor(count, ConsoleColor.Cyan);
        }

        public void WriteNamespace(string ns)
        {
            WriteWithColor(ns, ConsoleColor.DarkCyan);
        }

        public void WriteType(string type)
        {
            WriteWithColor(type, ConsoleColor.Gray);
        }

        public void WriteSeparator(string separator)
        {
            WriteWithColor(separator, ConsoleColor.White);
        }

        public void WriteDark(string separator)
        {
            WriteWithColor(separator, ConsoleColor.DarkGray);
        }

        public void WriteMethod(string method)
        {
            if (_useDml)
            {
                Console.Write($"<col fg=\"srckw\" bg=\"wbg\">{method}</col>");
                return;
            }

            WriteWithColor(method, ConsoleColor.Cyan);
        }

        public void WriteMethodType(string type)
        {
            if (_useDml)
            {
                Console.Write($"<b><col fg=\"srckw\" bg=\"wbg\">{type}</col></b>");
                return;
            }

            WriteWithColor(type, ConsoleColor.DarkCyan);
        }

        public void WriteFrameSeparator(string text)
        {
            if (_useDml)
            {
                Console.Write($"<b><col fg=\"srcpair\" bg=\"wbg\">{text}</col></b>");
                return;
            }

            WriteWithColor(text, ConsoleColor.Yellow);
        }


        private void WriteWithColor(string text, ConsoleColor color)
        {
            var current = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = current;
        }
    }
}
