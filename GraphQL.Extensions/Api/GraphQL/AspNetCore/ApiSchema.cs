using Api.Core.Application.Commons;
using Api.Core.Application.Lojas;
using Api.Core.Domain.Lojas;
using Api.Infra.Persistence;
using GraphQL;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;
using System.Numerics;
using System.Reflection;

namespace Api.GraphQL.AspNetCore;

public class ApiSchema : Schema
{
    public ApiSchema()
    {
        var rootQueryType = new ObjectGraphType
        {
            Name = "Lojas"
        };

        rootQueryType.Field<ObjectGraphType<Empresa>>("empresas", "Empresas",
            new QueryArguments(
                new QueryArgument<ObjectGraphType<EmpresaFilter>>() { Name = "id" },
                new QueryArgument<ObjectGraphType<IdFilter>>() { Name = "filter" }
                ),
            Resolver);

        Query = rootQueryType;
        Mutation = new ObjectGraphType();
    }

    private object? Resolver(IResolveFieldContext<object?> arg)
    {
        var empresaFilter = arg.GetArgument<EmpresaFilter>("filter");
        var idFilter = arg.GetArgument<IdFilter>("id");

        var db = arg.RequestServices?.GetService<AppDbContext>()
            ?? throw new ArgumentException("Dependency injection is required");


        var empresas = db.Set<Empresa>().AsNoTracking();

        if (idFilter is not null)
            empresas = empresas.Where(e => e.Id == idFilter.Id);
        if (empresaFilter is not null)
            empresas = empresas.Where(e => e.Nome == empresaFilter.Nome);

        return empresas.ToList();
    }
}

public class GraphTypeProvider
{
    private readonly IGraphTypeConfigurers configurers;
    private readonly Dictionary<Type, object> types = new()
    {
        {typeof(string), new StringGraphType() },
        {typeof(int), new IntGraphType() },
        {typeof(long), new LongGraphType() },
        {typeof(float), new FloatGraphType() },
        {typeof(double), new FloatGraphType() },
        {typeof(decimal), new DecimalGraphType() },
        {typeof(DateTime), new DateTimeGraphType() },
        {typeof(DateOnly), new DateOnlyGraphType() },
        {typeof(DateTimeOffset), new DateTimeOffsetGraphType() },
        {typeof(TimeSpan), new TimeSpanMillisecondsGraphType() },
        {typeof(TimeOnly), new TimeOnlyGraphType() },
        {typeof(bool), new BooleanGraphType() },
        {typeof(BigInteger), new BigIntGraphType() },
        {typeof(byte), new ByteGraphType() },
        {typeof(short), new ShortGraphType() },
        {typeof(sbyte), new SByteGraphType() },
        {typeof(ushort), new UShortGraphType() },
        {typeof(uint), new UIntGraphType() },
        {typeof(ulong), new ULongGraphType() },
        {typeof(Guid), new GuidGraphType() },
        {typeof(Uri), new UriGraphType() },
    };

    public GraphTypeProvider(IGraphTypeConfigurers configurers)
    {
        this.configurers = configurers;
    }

    public ObjectGraphType<T> Find<T>(Action<ObjectGraphType<T>>? configure = null)
    {
        if (types.TryGetValue(typeof(T), out var obj))
            return (ObjectGraphType<T>)obj;

        var gtype = new ObjectGraphType<T>();

        configurers.GetConfigurer<T>()?.Configure(gtype);
        configure?.Invoke(gtype);

        if (string.IsNullOrEmpty(gtype.Name))
            gtype.Name = typeof(T).Name.ToLower();

        foreach(var property in typeof(T).GetTypeInfo().DeclaredProperties)
        {
            var name = property.Name.ToLower();
            if (gtype.HasField(name))
                continue;

            var propertGType = GetFieldType(property.PropertyType);
            gtype.Field(name, propertGType);
        }

        types[typeof(T)] = gtype;
        return gtype;
    }

    public IGraphType GetFieldType(Type propertyType)
    {
        // busca do dicionário
        // se não encontrar
        // verificar se é enum, tratar como enum.
        // verficar se é lista, e tratar como lista.
        // verificar se é lista de enum.

        // executa o find

        // adiciona o procesado do dicionario


        throw new NotImplementedException();
    }
}

public interface IGraphTypeConfigurers
{
    IGraphTypeConfigurer<T>? GetConfigurer<T>();
}

public interface IGraphTypeConfigurer<T>
{
    void Configure(ObjectGraphType<T> graphType);
}