namespace WhereIsMyAi.RestSharpBase.Core.Tms;

/// <summary>A bug from the local bug repository; Status is null when the file has no valid one.</summary>
public record Bug(string Id, BugStatus? Status, string Title)
{
    public bool IsOpen => Status == BugStatus.Open;
}
