using System.Reflection;
using Amber.Application.Users.Queries.GetUserByUsername;
using Amber.Domain.Users.Entities;
using Amber.Infrastructure.Users.Repositories;

namespace Amber.WebApi.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddAllImplementationsForInterface(
        this IServiceCollection serviceCollection,
        Type interfaceType
    )
    {
        // Adding all assemblies.
        var types = Assembly.GetAssembly(typeof(User))!.GetTypes().ToHashSet();
        types.UnionWith(Assembly.GetAssembly(typeof(UserRepository))!.GetTypes());
        types.UnionWith(Assembly.GetAssembly(typeof(GetUserByUsernameQuery))!.GetTypes());

        foreach (var type in types)
        {
            if (type.IsInterface && type != interfaceType && type.IsAssignableTo(interfaceType))
            {
                // Adding interfaces inheriting the interface.
                serviceCollection.AddAllImplementationsForInterface(type);
                continue;
            }

            if (!type.IsClass || type.IsAbstract)
                continue;

            var shouldAdd = interfaceType.IsAssignableFrom(type);
            var typeToUseForInterface = interfaceType;

            if (interfaceType.IsGenericType)
            {
                typeToUseForInterface = type.GetInterfaces()
                    .FirstOrDefault(i =>
                        i.IsGenericType && i.GetGenericTypeDefinition() == interfaceType
                    );
                shouldAdd = typeToUseForInterface is not null;
            }

            if (shouldAdd)
            {
                serviceCollection.AddScoped(typeToUseForInterface!, type);
            }
        }
    }
}
