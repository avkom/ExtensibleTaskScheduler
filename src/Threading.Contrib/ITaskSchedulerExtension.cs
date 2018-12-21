namespace Threading.Contrib
{
    public interface ITaskSchedulerExtension
    {
        object GetContextFromParentThread();
        object EnterChildThread(object contextFromParentTread);
        void ExitChildThread(object contextFromParentThread, object contextFromChildThread);
    }
}
