

[DataService.DataModel("AppIds")]
public class AppId
{
    [DataService.Column("app_key", true)]
    public string? AppKey { get; set; } = null;

    public AppId()
    {
        
    }

    public AppId(string? appKey = null)
    {
        this.AppKey = appKey;
    }
}