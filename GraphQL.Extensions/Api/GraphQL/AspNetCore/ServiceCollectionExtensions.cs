using Microsoft.Extensions.Options;

namespace Api.GraphQL.AspNetCore;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGraphQLExtensions(this IServiceCollection services)
    {
        services.AddSingleton<IGraphTypeConfigurers>(
            sp => sp.GetRequiredService<IOptions<GraphTypeOptions>>().Value);

        services.AddSingleton<GraphTypeProvider>();
        
        return services;
    }
    
}