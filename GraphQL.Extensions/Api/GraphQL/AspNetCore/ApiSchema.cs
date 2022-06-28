using System.Collections;
using Api.Core.Application.Commons;
using Api.Core.Application.Lojas;
using Api.Core.Domain.Lojas;
using Api.Infra.Persistence;
using GraphQL;
using GraphQL.Types;
using Microsoft.EntityFrameworkCore;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using GraphQL.Resolvers;

namespace Api.GraphQL.AspNetCore;

public class ApiSchema : Schema
{
    public ApiSchema(GraphTypeProvider graphTypeProvider)
    {
        var rootQueryType = new ObjectGraphType
        {
            Name = "Lojas"
        };
        
        RegisterType(graphTypeProvider.GetQueryType<Empresa>());
        //RegisterType(graphTypeProvider.GetInputType<EmpresaFilter>());
        //RegisterType(graphTypeProvider.GetInputType<IdFilter>());
        
        var field = new FieldType
        {
            Name = "empresas",
            Description = "Empresas",
            Type = graphTypeProvider.GetQueryType<Empresa>().GetType(),
            Arguments = new QueryArguments(
                new QueryArgument(graphTypeProvider.GetInputType<IdFilter>()) {Name = "id"},
                new QueryArgument(graphTypeProvider.GetInputType<EmpresaFilter>()) {Name = "filter"}
            ),
            Resolver = new FuncFieldResolver<Empresa, object>(Resolver)
        };
        rootQueryType.AddField(field);

        Query = rootQueryType;
        //Mutation = new ObjectGraphType();
    }

    private static object Resolver(IResolveFieldContext<object?> arg)
    {
        var empresaFilter = arg.GetArgument<EmpresaFilter?>("filter");
        var idFilter = arg.GetArgument<IdFilter?>("id");

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
        {typeof(string), new StringGraphType()},
        {typeof(int), new IntGraphType()},
        {typeof(long), new LongGraphType()},
        {typeof(float), new FloatGraphType()},
        {typeof(double), new FloatGraphType()},
        {typeof(decimal), new DecimalGraphType()},
        {typeof(DateTime), new DateTimeGraphType()},
        {typeof(DateOnly), new DateOnlyGraphType()},
        {typeof(DateTimeOffset), new DateTimeOffsetGraphType()},
        {typeof(TimeSpan), new TimeSpanMillisecondsGraphType()},
        {typeof(TimeOnly), new TimeOnlyGraphType()},
        {typeof(bool), new BooleanGraphType()},
        {typeof(BigInteger), new BigIntGraphType()},
        {typeof(byte), new ByteGraphType()},
        {typeof(short), new ShortGraphType()},
        {typeof(sbyte), new SByteGraphType()},
        {typeof(ushort), new UShortGraphType()},
        {typeof(uint), new UIntGraphType()},
        {typeof(ulong), new ULongGraphType()},
        {typeof(Guid), new GuidGraphType()},
        {typeof(Uri), new UriGraphType()},
    };

    public GraphTypeProvider(IGraphTypeConfigurers configurers)
    {
        this.configurers = configurers;
    }

    public ObjectGraphType<T> GetQueryType<T>()
    {
        if (types.TryGetValue(typeof(T), out var obj))
            return (ObjectGraphType<T>) obj;

        var gtype = (ObjectGraphType<T>) TypeEmiter.EmitQueryType(typeof(T))
            .GetConstructors()
            .First(c => c.GetParameters().Length == 0)
            .Invoke(null);

        configurers.GetConfigurer<T>()?.Configure(gtype);
        ConfigureGraphType(gtype, typeof(T));

        types[typeof(T)] = gtype;
        return gtype;
    }
    
    public InputObjectGraphType<T> GetInputType<T>()
    {
        if (types.TryGetValue(typeof(T), out var obj))
            return (InputObjectGraphType<T>) obj;

        var gtype = (InputObjectGraphType<T>) TypeEmiter.EmitInputType(typeof(T))
            .GetConstructors()
            .First(c => c.GetParameters().Length == 0)
            .Invoke(null);

        configurers.GetConfigurer<T>()?.Configure(gtype);
        ConfigureGraphType(gtype, typeof(T));

        types[typeof(T)] = gtype;
        return gtype;
    }

    public IObjectGraphType Find(Type type)
    {
        if (types.TryGetValue(type, out var obj))
            return (IObjectGraphType) obj;
        
        var gtype = (IObjectGraphType) TypeEmiter.EmitQueryType(type)
            .GetConstructors()
            .First(c => c.GetParameters().Length == 0)
            .Invoke(null);
        
        ConfigureGraphType(gtype, type);
        
        types[type] = gtype;
        return gtype;
    }

    private void ConfigureGraphType(IComplexGraphType gtype, Type type)
    {
        if (string.IsNullOrEmpty(gtype.Name))
            gtype.Name = type.Name.ToLower();

        foreach (var property in type.GetTypeInfo().DeclaredProperties)
        {
            var name = property.Name.ToLower();
            if (gtype.HasField(name))
                continue;

            var propertGType = GetFieldType(property.PropertyType);
            
            var field = new FieldType
            {
                Name = name,
                ResolvedType = propertGType,
                Description = null,
                Arguments = null,
                Resolver = null
            };
            gtype.AddField(field);
        }
    }

    public IGraphType GetFieldType(Type propertyType)
    {
        if (propertyType.GetTypeInfo().IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            propertyType = propertyType.GetGenericArguments()[0];

        // busca do dicionário
        if (types.TryGetValue(propertyType, out var obj))
            return (IGraphType) obj;

        IGraphType graphType;
        
        // verificar se é enum, tratar como enum.
        if (propertyType.GetTypeInfo().IsEnum)
        {
            graphType = (IGraphType) typeof(EnumerationGraphType<>)
                .MakeGenericType(propertyType)
                .GetConstructors()
                .First(c => c.GetParameters().Length == 0)
                .Invoke(Array.Empty<object>());
            
            types[propertyType] = graphType;
        }
        // verficar se é lista, e tratar como lista.
        else if (propertyType.GetTypeInfo().IsGenericType &&
                 typeof(IEnumerable).IsAssignableFrom(propertyType.GetGenericTypeDefinition()))
        {
            var type = propertyType.GenericTypeArguments[0];
            var gtype = GetFieldType(type).GetType();
        
            graphType = (IGraphType) typeof(ListGraphType<>)
                .MakeGenericType(gtype)
                .GetConstructors()
                .First(c => c.GetParameters().Length == 0)
                .Invoke(Array.Empty<object>());
            
            types[propertyType] = graphType;
        }
        else
        {
            graphType = Find(propertyType);
        }
        
        return graphType;
    }
}

public interface IGraphTypeConfigurers
{
    IGraphTypeConfigurer<T>? GetConfigurer<T>();
}

public class GraphTypeOptions : IGraphTypeConfigurers
{
    private readonly Dictionary<Type, object> configurers = new();
    
    public GraphTypeOptions ConfigureGraphType<T>(Action<ComplexGraphType<T>> configure)
    {
        GraphTypeConfigurer<T> configurer;
        if (configurers.TryGetValue(typeof(T), out var obj))
        {
            configurer = (GraphTypeConfigurer<T>) obj;
        }
        else
        {
            configurer = new GraphTypeConfigurer<T>();
            configurers[typeof(T)] = configurer;
        }
        
        configurer.AddTypeConfiguration(configure);
        
        return this;
    }
    
    public GraphTypeOptions ConfigureQueryType<T>(Action<ObjectGraphType<T>> configure)
    {
        GraphTypeConfigurer<T> configurer;
        if (configurers.TryGetValue(typeof(T), out var obj))
        {
            configurer = (GraphTypeConfigurer<T>) obj;
        }
        else
        {
            configurer = new GraphTypeConfigurer<T>();
            configurers[typeof(T)] = configurer;
        }
        
        configurer.AddQueryConfiguration(configure);
        
        return this;
    }
    
    public GraphTypeOptions ConfigureInputType<T>(Action<InputObjectGraphType<T>> configure)
    {
        GraphTypeConfigurer<T> configurer;
        if (configurers.TryGetValue(typeof(T), out var obj))
        {
            configurer = (GraphTypeConfigurer<T>) obj;
        }
        else
        {
            configurer = new GraphTypeConfigurer<T>();
            configurers[typeof(T)] = configurer;
        }
        
        configurer.AddInputConfiguration(configure);
        
        return this;
    }
    
    public IGraphTypeConfigurer<T>? GetConfigurer<T>()
    {
        return configurers.TryGetValue(typeof(T), out var obj) 
            ? (IGraphTypeConfigurer<T>) obj 
            : null;
    }
}

public interface IGraphTypeConfigurer<T>
{
    void Configure(ObjectGraphType<T> graphType);
    
    void Configure(InputObjectGraphType<T> graphType);
}

internal class GraphTypeConfigurer<T> : IGraphTypeConfigurer<T>
{
    private Action<ObjectGraphType<T>>? configureQueryObjects;
    private Action<InputObjectGraphType<T>>? configureInputObjects;
    private Action<ComplexGraphType<T>>? configureObjects;

    internal void AddQueryConfiguration(Action<ObjectGraphType<T>> configure)
    {
        if (configureQueryObjects is null)
            configureQueryObjects = configure;
        else
            configureQueryObjects += configure;
    }
    
    internal void AddInputConfiguration(Action<InputObjectGraphType<T>> configure)
    {
        if (configureInputObjects is null)
            configureInputObjects = configure;
        else
            configureInputObjects += configure;
    }
    
    internal void AddTypeConfiguration(Action<ComplexGraphType<T>> configure)
    {
        if (configureObjects is null)
            configureObjects = configure;
        else
            configureObjects += configure;
    }

    public void Configure(ObjectGraphType<T> graphType)
    {
        configureQueryObjects?.Invoke(graphType);
        configureObjects?.Invoke(graphType);
    }

    public void Configure(InputObjectGraphType<T> graphType)
    {
        configureInputObjects?.Invoke(graphType);
        configureObjects?.Invoke(graphType);
    }
}

internal static class TypeEmiter
{
    private static readonly ModuleBuilder moduleBuilder;
    
    static TypeEmiter()
    {
        var an = new AssemblyName("GraphQL.Extensions.Runtime.Generated");
        AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
        moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
    }
    
    internal static Type EmitQueryType(Type type)
    {
        TypeBuilder typeBuilder = GetTypeBuilder(
            type.Name + "GraphType",
            typeof(ObjectGraphType<>).MakeGenericType(type));

        return typeBuilder.CreateType()!;
    }

    internal static Type EmitInputType(Type type)
    {
        TypeBuilder typeBuilder = GetTypeBuilder(
            type.Name + "InputGraphType",
            typeof(InputObjectGraphType<>).MakeGenericType(type));

        return typeBuilder.CreateType()!;
    }

    private static TypeBuilder GetTypeBuilder(string typeName, Type baseType)
    {
        TypeBuilder typeBuilder = moduleBuilder.DefineType(
            typeName, 
            TypeAttributes.Public | TypeAttributes.Class,
            baseType);
        
        typeBuilder.DefineDefaultConstructor(MethodAttributes.Public |
                                             MethodAttributes.SpecialName |
                                             MethodAttributes.RTSpecialName);
        return typeBuilder;
    }
}