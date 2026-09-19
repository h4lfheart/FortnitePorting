using System.Net;
using System.Threading.Tasks;
using FortnitePorting.Models.API.Base;
using FortnitePorting.Models.API.Responses;
using RestSharp;

namespace FortnitePorting.Models.API;

public class FortniteGGApi(RestClient client) : APIBase(client)
{
    protected override string BaseURL => "https://fortnite.gg/api";

    public async Task<FortniteGGPreviewResponse?> ResolvePreview(string cosmeticId)
    {
        var response = await ExecuteAsync("item", verbose: false, parameters:
        [
            new QueryParameter("id", cosmeticId)
        ]);

        if (response.StatusCode != HttpStatusCode.OK || !int.TryParse(response.Content, out var itemId))
            return null;

        return new FortniteGGPreviewResponse(itemId);
    }
}
