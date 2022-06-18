var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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