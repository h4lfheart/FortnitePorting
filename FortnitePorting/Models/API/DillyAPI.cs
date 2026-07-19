using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CUE4Parse.Utils;
using FortnitePorting.Models.API.Base;
using FortnitePorting.Models.API.Requests;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Models.Map;
using Mapster;
using RestSharp;

namespace FortnitePorting.Models.API;

public class DillyAPI(RestClient client) : APIBase(client)
{
    protected override string BaseURL => "https://export-service-new.dillyapis.com/v1";

    public async Task<ManifestRequest[]> Manifests() => await ExecuteAsync<ManifestRequest[]>("manifests") ?? [];

}