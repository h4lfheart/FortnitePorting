using System;

namespace FortnitePorting.Models.API.Responses;

public class NewsResponse
{
    public string Title { get; set; }
    public string SubTitle { get; set; }
    public string Description { get; set; }
    public DateTime Date { get; set; }
    public string Image { get; set; }
}