namespace HostSimulator.Domain
{
    public enum StateChangeReason
    {
        None,
        UserRequest,
        ConnectionEstablished,
        SelectCompleted,
        SelectFailed,
        SelectTimeout,
        LinktestFailed,
        CommunicationError,
        RemoteSeparate,
        T5Timeout,
        T6Timeout,
        T7Timeout,
        T8Timeout,
        ReconnectAttempt,
        Disposed
    }
}
