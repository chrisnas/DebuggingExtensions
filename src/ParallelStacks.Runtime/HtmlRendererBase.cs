
namespace ParallelStacks.Runtime
{
    public abstract class HtmlRendererBase : RendererBase, IHtmlRenderer
    {
        protected HtmlRendererBase(int limit) : base(limit)
        {
        }

        public abstract void EnterRender(ParallelStack stacks);
        public abstract void EnterStackRoot();
        public abstract void EnterFrameGroupEnd(int indentationLevel);
        public abstract void EnterFrame(int indentationLevel);
        public abstract void LeaveFrame();
        public abstract void LeaveFrameGroupEnd();
        public abstract void LeaveStackRoot();
        public abstract void EndRender();
    }
}
