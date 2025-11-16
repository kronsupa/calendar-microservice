using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api")]
public class ApiController : Controller
{
    private DataService _dataService;

    public ApiController(DataService dataService)
    {
        _dataService = dataService;
    }

    public class Period
    {
        public string? StartTime { get; set; } = null;
        public string? EndTime { get; set; } = null;
    }

    public class EventPayload
    {
        public string? EventId { get; set; } = null;
        public string? Title { get; set; } = null;
        public string? StartTime { get; set; } = null;
        public string? EndTime { get; set; } = null;
        public string? Description { get; set; } = null;
    }

    [HttpGet("genkey")]
    public string RequestAppKey()
    {
        string key = Guid.NewGuid().ToString();

        _dataService.Insert<AppId>(new AppId(key));

        return key;
    }

    private bool ValidateAppKey(string? appKey)
    {
        if (appKey == null || appKey == String.Empty)
            return false;

        List<AppId>? appIds = _dataService.Select<AppId>(new AppId(appKey));
        if (appIds == null || appIds.Count <= 0)
            return false;

        return true;
    }

    [HttpGet("events")]
    public ActionResult GetEvents([FromQuery] Period period)
    {
        string? app_id = HttpContext.Request.Headers["Authorization"].ToString();
        if (!ValidateAppKey(app_id))
            return StatusCode(403);

        long? startTime = null;
        long? endTime = null;

        if (period != null)
        {
            if (period.StartTime != null && period.StartTime != String.Empty)
            {
                try
                {
                    startTime = DateTimeOffset.Parse(period.StartTime).ToUnixTimeMilliseconds();
                }
                catch (Exception)
                {
                    startTime = null;
                }
            }

            if (period.EndTime != null && period.EndTime != String.Empty)
            {
                try
                {
                    endTime = DateTimeOffset.Parse(period.EndTime).ToUnixTimeMilliseconds();
                }
                catch (Exception)
                {
                    endTime = null;
                }
            }
        }

        List<Event>? events = _dataService.Select<Event>(new Event(appId: app_id));

        if (events == null)
            return StatusCode(500);

        if (startTime != null)
            events = events.Where(e => e.StartTime > startTime).ToList();

        if (endTime != null)
            events = events.Where(e => e.EndTime < endTime).ToList();


        List<EventPayload> payloads = new List<EventPayload>();

        foreach (Event v in events)
        {
            payloads.Add(new EventPayload()
            {
                EventId = v.EventId,
                Title = v.Title,
                StartTime = (v.StartTime == null) ? null : DateTimeOffset.FromUnixTimeMilliseconds((long)v.StartTime).UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                EndTime = (v.EndTime == null) ? null : DateTimeOffset.FromUnixTimeMilliseconds((long)v.EndTime).UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                Description = v.Description
            });
        }

        return Ok(payloads);
    }

    [HttpPost("events")]
    public ActionResult CreateEvent([FromBody] EventPayload payload)
    {
        string? app_id = HttpContext.Request.Headers["Authorization"].ToString();
        if (!ValidateAppKey(app_id))
            return StatusCode(403);

        if (payload == null)
            return BadRequest();

        if (payload.StartTime == null || payload.EndTime == null)
            return BadRequest();

        try
        {
            payload.EventId = $"event-{Guid.NewGuid().ToString()}";

            Event e = new Event()
            {
                EventId = payload.EventId,
                Title = payload.Title,
                StartTime = DateTimeOffset.Parse(payload.StartTime).ToUnixTimeMilliseconds(),
                EndTime = DateTimeOffset.Parse(payload.EndTime).ToUnixTimeMilliseconds(),
                Description = payload.Description,
                AppId = app_id
            };

            _dataService.Insert(e);
        }
        catch (Exception)
        {
            return BadRequest();
        }

        return Ok();
    }

    [HttpGet("events/{id}")]
    public ActionResult GetEvent(string id)
    {
        string? app_id = HttpContext.Request.Headers["Authorization"].ToString();
        if (!ValidateAppKey(app_id))
            return StatusCode(403);

        Event? v = _dataService.Select<Event>(new Event(appId: app_id))?.FirstOrDefault(defaultValue: null);

        if (v == null)
            return NotFound();

        EventPayload payload = new EventPayload()
        {
            EventId = v.EventId,
            Title = v.Title,
            StartTime = (v.StartTime == null) ? null : DateTimeOffset.FromUnixTimeMilliseconds((long)v.StartTime).UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
            EndTime = (v.EndTime == null) ? null : DateTimeOffset.FromUnixTimeMilliseconds((long)v.EndTime).UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
            Description = v.Description
        };

        return Ok(payload);

    }

    [HttpPut("events/{id}")]
    public ActionResult UpdateEvent(string id, [FromBody] EventPayload payload)
    {
        string? app_id = HttpContext.Request.Headers["Authorization"].ToString();
        if (!ValidateAppKey(app_id))
            return StatusCode(403);


        Event? v = _dataService.Select<Event>(new Event(appId: app_id))?.FirstOrDefault(defaultValue: null);

        try
        {
            if (v == null)
                return NotFound();

            if (payload.Title != null)
                v.Title = payload.Title;

            if (payload.StartTime != null)
                v.StartTime = DateTimeOffset.Parse(payload.StartTime).ToUnixTimeMilliseconds();

            if (payload.EndTime != null)
                v.EndTime = DateTimeOffset.Parse(payload.EndTime).ToUnixTimeMilliseconds();

            if (payload.Description != null)
                v.Description = payload.Description;
        }
        catch (Exception)
        {
            return BadRequest();
        }

        _dataService.Update(v);

        return Ok();
    }

    [HttpDelete("events/{id}")]
    public ActionResult DeleteEvent(string id)
    {
        string? app_id = HttpContext.Request.Headers["Authorization"].ToString();
        if (!ValidateAppKey(app_id))
            return StatusCode(403);

        Event? v = _dataService.Select<Event>(new Event(appId: app_id))?.FirstOrDefault(defaultValue: null);

        if (v == null)
            return NotFound();
        else
        {
            _dataService.Delete(v);

            return Ok();
        }
    }
}