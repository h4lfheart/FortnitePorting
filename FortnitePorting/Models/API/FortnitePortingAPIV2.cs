using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FortnitePorting.Application;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Models.Help;
using FortnitePorting.Models.Leaderboard;
using FortnitePorting.Models.Voting;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Models.API;
using FortnitePorting.Shared.Models.API.Responses;
using Newtonsoft.Json;
using RestSharp;
using Poll = FortnitePorting.Models.Voting.Poll;

namespace FortnitePorting.Models.API;

public class FortnitePortingAPIV2(RestClient client) : APIBase(client)
{
    protected override string BaseURL => "https://api.fortniteporting.halfheart.dev";

    public async Task<UserInfoResponse?> UserInfo(string id) => await ExecuteAsync<UserInfoResponse>("v1/user", parameters: [
        new QueryParameter(nameof(id), id)
    ]);
}