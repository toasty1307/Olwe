using System.Reflection;
using Olwe.Remora;
using Olwe.Remora.Attributes;
using Remora.Commands.Extensions;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Gateway.Commands;
using Remora.Discord.Caching.Extensions;
using Remora.Discord.Caching.Services;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
using Remora.Discord.Gateway.Responders;

namespace Olwe.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRemoraServices(this IServiceCollection services)
        => services
            .AddDiscordGateway(x =>
                x.GetRequiredService<IConfiguration>()["Olwe:Discord:Token"] ??
                throw new InvalidOperationException("Olwe:Discord:Token is not configured"))
            .Configure<DiscordGatewayClientOptions>(options => options.Intents |=
                GatewayIntents.GuildMembers |
                GatewayIntents.DirectMessages |
                GatewayIntents.DirectMessageReactions |
                GatewayIntents.MessageContents
            )
            .AddDiscordCaching()
            .Configure<CacheSettings>(settings =>
                {
                    settings.SetDefaultAbsoluteExpiration(TimeSpan.FromHours(2));
                    settings.SetDefaultSlidingExpiration(TimeSpan.FromMinutes(30));
                }
            )
            .AddResponders(typeof(Setup).Assembly)
            .AddCommands(typeof(Setup).Assembly)
            .AddSlashCommands(typeof(Setup).Assembly);
    
    public static IServiceCollection AddResponders(this IServiceCollection services, Assembly assembly)
    {
        var types = assembly
            .GetExportedTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsResponder());

        foreach (var type in types)
        {
            var responderGroup = type.GetCustomAttribute<ResponderGroupAttribute>()?.Group ?? ResponderGroup.Normal;
           
            services.AddResponder(type, responderGroup);
        }

        return services;
    }
    
    public static IServiceCollection AddCommands(this IServiceCollection services, Assembly assembly)
    {
        var types = assembly
            .GetExportedTypes()
            .Where(t => t.IsClass && !t.IsNested && !t.IsAbstract && t.IsAssignableTo(typeof(CommandGroup)));

        var tree = services.AddCommandTree();
        
        foreach (var type in types)
            tree.WithCommandGroup(type);
        
        return tree.Finish();
    }
    
    public static IServiceCollection AddSlashCommands(this IServiceCollection services, Assembly assembly)
    {
        var types = assembly
            .GetExportedTypes()
            .Where(t => t.IsClass && !t.IsNested && !t.IsAbstract && t.IsAssignableTo(typeof(CommandGroup)));

        var tree = services.AddCommandTree("olwe_slash_tree");
        
        foreach (var type in types.Where(t => t.GetCustomAttribute<ExcludeFromSlashCommandsAttribute>() is null))
            tree.WithCommandGroup(type);
        
        return tree.Finish();
    }
}