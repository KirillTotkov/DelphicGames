using MediatR;

public class StreamStatusChangedEvent : INotification
{
    public int StreamEntityId { get; }
    public StreamStatus Status { get; }
    public string ErrorMessage { get; }

    public StreamStatusChangedEvent(int streamEntityId, StreamStatus status, string errorMessage = null)
    {
        StreamEntityId = streamEntityId;
        Status = status;
        ErrorMessage = errorMessage;
    }
}

public enum StreamStatus
{
    Running,
    Completed,
    Error
}