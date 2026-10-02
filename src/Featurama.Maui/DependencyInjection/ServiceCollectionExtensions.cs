using Microsoft.Extensions.DependencyInjection;

namespace Featurama.Maui.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFeaturama(
        this IServiceCollection services,
        Action<FeaturamaOptionsBuilder> configure)
    {
        var builder = new FeaturamaOptionsBuilder();
        configure(builder);
        var options = builder.Build();

        services.AddSingleton(options);

        services.AddHttpClient<FeaturamaClient>(client => client.Timeout = System.Threading.Timeout.InfiniteTimeSpan)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });

        return services;
    }
}
