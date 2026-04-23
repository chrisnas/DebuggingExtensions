using System.Diagnostics;
using System.Text;
using Microsoft.Diagnostics.Runtime;

namespace ParallelStacks;

public class StackFrame
{
    public string TypeName { get; private set; }
    public string MethodName { get; private set; }
    public List<string> Signature { get; }

    public string Text { get; }

    public StackFrame(ClrStackFrame frame)
    {
        var signature = frame.Method?.Signature;
        Text = string.IsNullOrEmpty(signature) ? "?" : string.Intern(signature);
        TypeName = string.Empty;
        MethodName = string.Empty;
        Signature = new List<string>();
        ComputeNames(frame);
    }

    private void ComputeNames(ClrStackFrame frame)
    {
        if (frame.Method == null)
            return;

        var typeName = frame.Method.Type?.Name;
        TypeName = string.IsNullOrEmpty(typeName) ? string.Empty : typeName;

        var fullName = frame.Method.Signature ?? string.Empty;
        MethodName = frame.Method.Name ?? string.Empty;
        if (MethodName.EndsWith("]]"))
        {
            MethodName = GetGenericMethodName(fullName);
        }

        Signature.AddRange(BuildSignature(fullName));
    }

    public static string GetShortTypeName(string typeName, int start, int end)
    {
        return GetNextTypeName(typeName, ref start, ref end);
    }

    public static string GetNextTypeName(string typeName, ref int start, ref int end)
    {
        if (string.IsNullOrEmpty(typeName))
            return string.Empty;

        var sb = new StringBuilder();

        var pos = typeName.IndexOf('`', start, end - start);
        var next = typeName.IndexOf(',', start, end - start);

        if ((pos == -1) && (next == -1))
        {
            AppendTypeNameWithoutNamespace(sb, typeName, start, end);
            start = end;
            return sb.ToString();
        }

        if (next == -1)
        {
            return GetGenericTypeName(typeName, ref start, ref end);
        }

        if (pos == -1)
        {
            AppendTypeNameWithoutNamespace(sb, typeName, start, next - 1);
            start = next + 1;
            return sb.ToString();
        }

        if (pos < next)
        {
            return GetGenericTypeName(typeName, ref start, ref end);
        }

        AppendTypeNameWithoutNamespace(sb, typeName, start, next - 1);
        start = next + 1;
        return sb.ToString();
    }

    public static string GetGenericTypeName(string typeName, ref int start, ref int end)
    {
        var sb = new StringBuilder();

        var pos = typeName.IndexOf('`', start, end - start);
        AppendTypeNameWithoutNamespace(sb, typeName, start, pos - 1);
        sb.Append('<');

        start = typeName.IndexOf('<', pos, end - pos) + 1;

        while (start < end)
        {
            var genericParameter = GetNextTypeName(typeName, ref start, ref end);
            sb.Append(genericParameter);
            if (start < end)
            {
                sb.Append(',');
            }
        }

        return sb.ToString();
    }

    public static void AppendTypeNameWithoutNamespace(StringBuilder sb, string typeName, int start, int end)
    {
        var pos = typeName.LastIndexOf('.', end, end - start);
        if (pos == -1)
        {
            sb.Append(typeName, start, end - start + 1);
        }
        else
        {
            sb.Append(typeName, pos + 1, end - pos);
        }
    }

    public static IEnumerable<string> BuildSignature(string fullName)
    {
        var parameters = new List<string>();
        var pos = fullName.LastIndexOf('(');
        if (pos == -1)
        {
            return parameters;
        }

        int next = pos;
        while (next != (fullName.Length - 1))
        {
            next = fullName.IndexOf(", ", pos);
            if (next == -1)
            {
                next = fullName.IndexOf(')');
                Debug.Assert(next == fullName.Length - 1);
            }

            var parameter = GetParameter(fullName, pos + 1, next - 1);
            if (parameter != null) parameters.Add(parameter);

            pos = next + 1;
        }

        return parameters;
    }

    public static string? GetParameter(string fullName, int start, int end)
    {
        const string BYREF = " ByRef";
        if (start >= end)
            return null;

        var sb = new StringBuilder();

        var isByRef = false;
        if (fullName.LastIndexOf(BYREF, end) == end - BYREF.Length)
        {
            isByRef = true;
            end -= BYREF.Length;
        }

        var typeName = GetShortTypeName(fullName, start, end);
        sb.Append(typeName);

        if (isByRef)
            sb.Append(BYREF);

        return sb.ToString();
    }

    public static string GetGenericMethodName(string fullName)
    {
        var pos = fullName.IndexOf("[[");
        if (pos == -1)
        {
            return fullName;
        }

        var start = fullName.LastIndexOf('.', pos);
        return fullName.Substring(start + 1, pos - start - 1);
    }
}
