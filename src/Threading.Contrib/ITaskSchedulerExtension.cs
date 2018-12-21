namespace Threading.Contrib
{
    public interface ITaskSchedulerExtension
    {
        object GetContextFromParentThread();
        object EnterChildThread(object contextFromParentTread);
        object ExitChildThread(object contextFromParentThread, object contextFromChildThread);
    }
}
