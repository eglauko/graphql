using System.Security.Claims;

namespace Api.GraphQL.AspNetCore;

public class GraphQLUserContext : Dictionary<string, object>
{
    public ClaimsPrincipal? User { get; set; }
}
