

[DataService.DataModel("Events")]
public class Event
{
    [DataService.Column("event_id", true)]
    public string? EventId { get; set; } = null;
    [DataService.Column("title")]
    public string? Title { get; set; } = null;
    [DataService.Column("start_time")]
    public long? StartTime { get; set; } = null;
    [DataService.Column("end_time")]
    public long? EndTime { get; set; } = null;
    [DataService.Column("description")]
    public string? Description { get; set; } = null;
    [DataService.Column("app_key")]
    public string? AppId { get; set; } = null;

    public Event()
    {
        
    }

    public Event(string? eventId = null, string? title = null, long? startTime = null, long? endTime = null, string? description = null, string? appId = null)
    {
        this.EventId = eventId;
        this.Title = title;
        this.StartTime = startTime;
        this.EndTime = endTime;
        this.Description = description;
        this.AppId = appId;
    }
}