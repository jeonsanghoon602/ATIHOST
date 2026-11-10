namespace HostSimulator.Domain
{
    public interface IMessageLogger
    {
        void Log(MessageLogEntry entry);
    }
}
