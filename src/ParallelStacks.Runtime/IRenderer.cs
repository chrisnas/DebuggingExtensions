namespace ParallelStacks.Runtime
{
    /// <summary>
    /// The method of this interface are called to render each part of the parallel call stacks
    /// </summary>
    /// <remarks>
    /// Each method is responsible for adding color, tags or decoration on each element of the parallel stacks
    /// </remarks>
    public interface IRenderer
    {
        /// <summary>
        /// Render empty line
        /// </summary>
        /// <param name="text"></param>
        void Write(string text);

        /// <summary>
        /// Render count at the beginning of each line
        /// </summary>
        /// <param name="count"></param>
        void WriteCount(string count);

        /// <summary>
        /// Render namespace of each method type
        /// </summary>
        /// <param name="ns"></param>
        void WriteNamespace(string ns);

        /// <summary>
        /// Render each type in method signatures
        /// </summary>
        /// <param name="type"></param>
        void WriteType(string type);

        /// <summary>
        /// Render separators such as ( and .
        /// </summary>
        /// <param name="separator"></param>
        void WriteSeparator(string separator);

        /// <summary>
        /// Render dark signature element such as ByRef
        /// </summary>
        /// <param name="separator"></param>
        void WriteDark(string separator);

        /// <summary>
        /// Render method name
        /// </summary>
        /// <param name="method"></param>
        void WriteMethod(string method);

        /// <summary>
        /// Render method type (not including namespace)
        /// </summary>
        /// <param name="type"></param>
        void WriteMethodType(string type);

        /// <summary>
        /// Render separator between different stack frame blocks
        /// </summary>
        /// <param name="text"></param>
        void WriteFrameSeparator(string text);
    }
}
