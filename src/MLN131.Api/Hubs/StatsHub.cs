using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MLN131.Api.Common;

namespace MLN131.Api.Hubs;

[Authorize(Roles = Roles.Admin)]
public sealed class StatsHub : Hub
{
}

