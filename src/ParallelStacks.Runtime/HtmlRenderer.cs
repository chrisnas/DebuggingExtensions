using System;
using System.Net;
using System.Text;

namespace ParallelStacks.Runtime
{
    public class HtmlRenderer : HtmlRendererBase
    {
        private static readonly string IndentClassNameTemplate = "indent{0}";
        private static readonly string IndentClassTemplate = "." + IndentClassNameTemplate + " {{ position:relative;left:{1}em; }}\r\n";

        private static readonly string TitleClassName = "title";
        private static readonly string TitleClass = "." + TitleClassName + " { font-size:x-large;font-weight:bold;color:darkblue;margin-right:0.5em }";
        private static readonly string CountClassName = "count";
        private static readonly string CountClass = "." + CountClassName + " { color:red;margin-right:0.5em }";
        private static readonly string NamespaceClassName = "namespace";
        private static readonly string NamespaceClass = "." + NamespaceClassName + " { color:darkslategray }";
        private static readonly string TypeClassName = "type";
        private static readonly string TypeClass = "." + TypeClassName + " { color:dodgerblue }";
        private static readonly string SeparatorClassName = "separator";
        private static readonly string SeparatorClass = "." + SeparatorClassName + " { color:lightgray }";
        private static readonly string DarkClassName = "dark";
        private static readonly string DarkClass = "." + DarkClassName + " { color:darkgray }";
        private static readonly string MethodClassName = "method";
        private static readonly string MethodClass = "." + MethodClassName + " { color:darkblue;font-weight:bold }";
        private static readonly string MethodTypeClassName = "methodType";
        private static readonly string MethodTypeClass = "." + MethodTypeClassName + " { color:darkslategray;font-weight:bold }";
        private static readonly string FrameSeparatorClassName = "frameseparator";
        private static readonly string FrameSeparatorClass = "." + FrameSeparatorClassName + " { color:red;margin-bottom: 0px }";

        private readonly StringBuilder _bufferHtml;
        private readonly StringBuilder _bufferStyle;

        public int MaxIndentationLevel { get; set; }

        public HtmlRenderer(StringBuilder bufferHtml, StringBuilder bufferStyle, int limit = 4) : base(limit)
        {
            _bufferHtml = bufferHtml;
            _bufferStyle = bufferStyle;
        }

        public override void Write(string text)
        {
        }

        public override void WriteCount(string count)
        {
            _bufferHtml.Append($"<span class=\"{CountClassName}\">{count}</span>");
        }

        public override void WriteNamespace(string ns)
        {
            _bufferHtml.Append($"<span class=\"{NamespaceClassName}\">{WebUtility.HtmlEncode(ns)}</span>");
        }

        public override void WriteType(string type)
        {
            _bufferHtml.Append($"<span class=\"{TypeClassName}\">{WebUtility.HtmlEncode(type)}</span>");
        }

        public override void WriteSeparator(string separator)
        {
            _bufferHtml.Append($"<span class=\"{DarkClassName}\">{separator}</span>");
        }

        public override void WriteDark(string separator)
        {
            _bufferHtml.Append($"<span class=\"{DarkClassName}\">{separator}</span>");
        }

        public override void WriteMethod(string method)
        {
            _bufferHtml.Append($"<span class=\"{MethodClassName}\">{WebUtility.HtmlEncode(method)}</span>");
        }

        public override void WriteMethodType(string type)
        {
            _bufferHtml.Append($"<span class=\"{MethodTypeClassName}\">{WebUtility.HtmlEncode(type)}</span>");
        }

        public override void WriteFrameSeparator(string text)
        {
            _bufferHtml.Append($"<span class=\"{FrameSeparatorClassName}\">{text}</span>");
        }

        public override string FormatTheadId(uint threadID)
        {
            return threadID.ToString("x");
        }

        public override void EnterRender(ParallelStack stacks)
        {
            _bufferHtml.Append($"<p><span class=\"{TitleClassName}\">{stacks.ThreadIds.Count} threads for {stacks.Stacks.Count} roots</span></p>");
        }

        public override void EnterStackRoot()
        {
            _bufferHtml.Append("<p>________________________________________</p><p>");
        }

        public override void EnterFrameGroupEnd(int indentationLevel)
        {
            MaxIndentationLevel = Math.Max(indentationLevel, MaxIndentationLevel);
            var template = $"<div class=\"{IndentClassNameTemplate}\">";
            _bufferHtml.AppendFormat(template, indentationLevel);
        }

        public override void EnterFrame(int indentationLevel)
        {
            MaxIndentationLevel = Math.Max(indentationLevel, MaxIndentationLevel);

            var template = $"<div class=\"{IndentClassNameTemplate}\">";
            _bufferHtml.AppendFormat(template, indentationLevel);
        }

        public override void LeaveFrame()
        {
            _bufferHtml.AppendFormat("</div>");
        }

        public override void LeaveFrameGroupEnd()
        {
            _bufferHtml.AppendFormat("</div>");
        }

        public override void LeaveStackRoot()
        {
            _bufferHtml.Append("</p>");
        }

        public override void EndRender()
        {
            // static classes for stack frames
            _bufferStyle.AppendLine(TitleClass);
            _bufferStyle.AppendLine(CountClass);
            _bufferStyle.AppendLine(NamespaceClass);
            _bufferStyle.AppendLine(TypeClass);
            _bufferStyle.AppendLine(SeparatorClass);
            _bufferStyle.AppendLine(DarkClass);
            _bufferStyle.AppendLine(MethodClass);
            _bufferStyle.AppendLine(MethodTypeClass);
            _bufferStyle.AppendLine(FrameSeparatorClass);

            // compute the styles based on max indentation level
            for (int indentation = 0; indentation <= MaxIndentationLevel; indentation++)
            {
                _bufferStyle.AppendFormat(IndentClassTemplate, indentation, 3 * (indentation + 1));
            }
        }
    }

}
