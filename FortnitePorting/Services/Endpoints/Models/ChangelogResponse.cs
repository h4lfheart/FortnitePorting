using System;
using System.Drawing;

namespace FortnitePorting.Services.Endpoints.Models;

public class ChangelogResponse
{
    public string Title;
    public DateTime PublishTime;
    public string Text;
    public string[] Tags;
    public string ImageURL;
}