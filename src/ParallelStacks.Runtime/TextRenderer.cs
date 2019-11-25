using System;
using System.Text;

namespace ParallelStacks.Runtime
{
    public class TextRenderer : RendererBase
    {
        private readonly StringBuilder _buffer;

        public TextRenderer(StringBuilder buffer, int limit = 4) : base(limit)
        {
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
        }

        public override void Write(string text)
        {
            _buffer.Append(text);
        }

        public override void WriteCount(string count)
        {
            _buffer.Append(count);
        }

        public override void WriteNamespace(string ns)
        {
            _buffer.Append(ns);
        }

        public override void WriteType(string type)
        {
            _buffer.Append(type);
        }

        public override void WriteSeparator(string separator)
        {
            _buffer.Append(separator);
        }

        public override void WriteDark(string separator)
        {
            _buffer.Append(separator);
        }

        public override void WriteMethod(string method)
        {
            _buffer.Append(method);
        }

        public override void WriteMethodType(string type)
        {
            _buffer.Append(type);
        }

        public override void WriteFrameSeparator(string text)
        {
            _buffer.Append(text);
        }

        public override string FormatTheadId(uint threadID)
        {
            return threadID.ToString();
        }
    }
}
