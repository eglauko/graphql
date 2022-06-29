using Api.GraphQL.AspNetCore;
using Api.Infra.Persistence;
using GraphQL;
using GraphQL.MicrosoftDI;
using GraphQL.Server;
using GraphQL.SystemTextJson;
using GraphQL.Types;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

/*
 *  Idéias para fazer.
 *  Criar um tipo de schema padrão genérico que busca do service provider um builder de query e mutation.
 *  Ver se pode ser um builder só ou se é melhor separar.
 *  Criar um componente que registra queries.
 *  Tentar usar um ObjectType genérico para evitar a necessidade de ficar criando tipos.
 *  Verificar os filtros para poderem ser também genéricos
 *  ---
 *  Estudar como paginar.
 *  ---
 *  Testar com InMemoryDbContext.
 */

builder.Services.AddGraphQLExtensions();

builder.Services.AddGraphQL(b => b
    .AddHttpMiddleware<ISchema>()
    .AddSchema<ApiSchema>()
    .AddUserContextBuilder(httpContext => new GraphQLUserContext { User = httpContext.User })
    .AddSystemTextJson()
    .AddErrorInfoProvider(opt => opt.ExposeExceptionStackTrace = true)
);

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<AppDbContext>();


var app = builder.Build();

app.UseAuthorization();

app.UseGraphQL<ISchema>();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.UseGraphQLGraphiQL();
    app.UseGraphQLAltair();
    
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();