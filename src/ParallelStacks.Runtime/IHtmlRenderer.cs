
namespace ParallelStacks.Runtime
{
    /// <summary>
    /// The method of this interface are called to render each part of the parallel call stacks in HTML.
    /// </summary>
    /// <remarks>
    /// The new methods are called when a new line (separator, frame group or frame) need to be added to the HTML page.
    /// </remarks>
    public interface IHtmlRenderer : IRenderer
    {
        /// <summary>
        /// Called before any rendering of the given ParallelStack.
        /// </summary>
        /// <remarks>
        /// Could be used to show the number of threads and roots for example.
        /// </remarks>
        /// <param name="stacks"></param>
        void EnterRender(ParallelStack stacks);

        /// <summary>
        /// Called for each call stacks root (starting of common call stacks)
        /// </summary>
        void EnterStackRoot();

        /// <summary>
        /// Called before displaying:  ~~~~ (thread list)
        /// </summary>
        /// <param name="indentationLevel">indentation of the frame group</param>
        void EnterFrameGroupEnd(int indentationLevel);

        /// <summary>
        /// Called before displaying a frame:  count namespace.type.method(signature)
        /// </summary>
        /// <param name="indentationLevel">indentation of the frame</param>
        void EnterFrame(int indentationLevel);

        /// <summary>
        /// Called after displaying a frame
        /// </summary>
        void LeaveFrame();

        /// <summary>
        /// Called after the last frame of a frame group
        /// </summary>
        void LeaveFrameGroupEnd();

        /// <summary>
        /// Called after the last frame of each stacks root
        /// </summary>
        void LeaveStackRoot();

        /// <summary>
        /// Called at the end of the parsing; nothing is rendered after this call.
        /// This could be used to compute CSS classes based on what has been rendered
        /// </summary>
        /// <example>
        /// Compute indentation classes based on deepest frame. 
        /// </example>
        void EndRender();
    }
}
