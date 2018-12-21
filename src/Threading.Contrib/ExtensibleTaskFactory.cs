using System.Threading.Tasks;

namespace Threading.Contrib
{
    public class ExtensibleTaskFactory : TaskFactory
    {
        public ExtensibleTaskFactory(ExtensibleTaskScheduler taskScheduler)
            : base(taskScheduler)
        {
        }
    }
}
