using OpenTracing;
using OpenTracing.Util;

namespace Threading.Contrib
{
    public class TracingTaskSchedulerExtension : ITaskSchedulerExtension
    {
        public object GetContextFromParentThread()
        {
            return GlobalTracer.Instance.ScopeManager.Active.Span;
        }

        public object EnterChildThread(object contextFromParentTread)
        {
            ISpan span = (ISpan) contextFromParentTread;
            GlobalTracer.Instance.ScopeManager.Activate(span, false);
            return null;
        }

        public void ExitChildThread(object contextFromParentThread, object contextFromChildThread)
        {
        }
    }
}
