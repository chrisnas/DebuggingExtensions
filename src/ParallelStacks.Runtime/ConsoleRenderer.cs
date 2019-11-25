using System;

namespace ParallelStacks.Runtime
{
    public class ConsoleRenderer : RendererBase
    {
        private readonly bool _useDml;

        public ConsoleRenderer(bool useDml, int limit = -1) : base(limit)
        {
            _useDml = useDml;
        }

        public override void Write(string text)
        {
            Console.Write(text);
        }

        public override void WriteCount(string count)
        {
            if (_useDml)
            {
                Console.Write($"<col fg=\"srcpair\" bg=\"wbg\">{count}</col>");
                return;
            }

            WriteWithColor(count, ConsoleColor.Cyan);
        }

        public override void WriteNamespace(string ns)
        {
            WriteWithColor(ns, ConsoleColor.DarkCyan);
        }

        public override void WriteType(string type)
        {
            WriteWithColor(type, ConsoleColor.Gray);
        }

        public override void WriteSeparator(string separator)
        {
            WriteWithColor(separator, ConsoleColor.White);
        }

        public override void WriteDark(string separator)
        {
            WriteWithColor(separator, ConsoleColor.DarkGray);
        }

        public override void WriteMethod(string method)
        {
            if (_useDml)
            {
                Console.Write($"<col fg=\"srckw\" bg=\"wbg\">{method}</col>");
                return;
            }

            WriteWithColor(method, ConsoleColor.Cyan);
        }

        public override void WriteMethodType(string type)
        {
            if (_useDml)
            {
                Console.Write($"<b><col fg=\"srckw\" bg=\"wbg\">{type}</col></b>");
                return;
            }

            WriteWithColor(type, ConsoleColor.DarkCyan);
        }

        public override void WriteFrameSeparator(string text)
        {
            if (_useDml)
            {
                Console.Write($"<b><col fg=\"srcpair\" bg=\"wbg\">{text}</col></b>");
                return;
            }

            WriteWithColor(text, ConsoleColor.Yellow);
        }

        public override string FormatTheadId(uint threadID)
        {
            var idInHex = threadID.ToString("x");
            if (_useDml)
            {
                // change current thread
                return $"<link cmd=\"~~[{idInHex}]s\">{idInHex}</link>";
            }

            return idInHex;
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
