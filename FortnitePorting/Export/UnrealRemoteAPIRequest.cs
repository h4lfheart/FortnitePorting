namespace FortnitePorting.Export;

public class UnrealRemoteAPIParameters{
    public string response;
}

public class UnrealRemoteAPIRequest
{
    public string objectPath;
    public string functionName;
    public UnrealRemoteAPIParameters parameters;
}