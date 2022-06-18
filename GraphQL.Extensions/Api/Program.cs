using Api.GraphQL.AspNetCore;
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
 *  Id�ias para fazer.
 *  Criar um tipo de schema padr�o gen�rico que busca do service provider um builder de query e mutation.
 *  Ver se pode ser um builder s� ou se � melhor separar.
 *  Criar um componente que registra queries.
 *  Tentar usar um ObjectType gen�rico para evitar a necessidade de ficar criando tipos.
 *  Verificar os filtros para poderem ser tamb�m gen�ricos
 *  ---
 *  Estudar como paginar.
 *  ---
 *  Testar com InMemoryDbContext.
 */


builder.Services.AddGraphQL(b => b
    .AddHttpMiddleware<ISchema>()
    .AddUserContextBuilder(httpContext => new GraphQLUserContext { User = httpContext.User })
    .AddSystemTextJson()
    .AddErrorInfoProvider(opt => opt.ExposeExceptionStackTrace = true)
    .AddAutoSchema<object>()
    //.AddSchema<StarWarsSchema>()
    //.AddGraphTypes(typeof(StarWarsSchema).Assembly)
    );

var app = builder.Build();

app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.UseGraphQLGraphiQL();
    app.UseGraphQLAltair();
    
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();