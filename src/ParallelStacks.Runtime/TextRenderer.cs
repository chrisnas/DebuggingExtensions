using System;
using System.Text;

namespace ParallelStacks.Runtime
{
    public class TextRenderer : IRenderer
    {
        private readonly StringBuilder _buffer;

        public TextRenderer(StringBuilder buffer)
        {
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
        }

        public void Write(string text)
        {
            _buffer.Append(text);
        }

        public void WriteCount(string count)
        {
            _buffer.Append(count);
        }

        public void WriteNamespace(string ns)
        {
            _buffer.Append(ns);
        }

        public void WriteType(string type)
        {
            _buffer.Append(type);
        }

        public void WriteSeparator(string separator)
        {
            _buffer.Append(separator);
        }

        public void WriteDark(string separator)
        {
            _buffer.Append(separator);
        }

        public void WriteMethod(string method)
        {
            _buffer.Append(method);
        }

        public void WriteMethodType(string type)
        {
            _buffer.Append(type);
        }

        public void WriteFrameSeparator(string text)
        {
            _buffer.Append(text);
        }
    }
}
