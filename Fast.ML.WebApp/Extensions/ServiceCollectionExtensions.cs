using System;
using System.Net.Http;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fast.ML.WebApp.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDependencies(
        this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddCookies()
            .AddGoogleAuthentication(configuration)
            .AddHttpClients(configuration);
    }

    private static IServiceCollection AddCookies(this IServiceCollection services)
    {
        return services
            .Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = _ => true;
            })
            .AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(60);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });
    }

    private static IServiceCollection AddGoogleAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var googleAuthenticationOptions =
            configuration.GetSection("GoogleAuthenticationOptions");

        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
            })
            .AddCookie()
            .AddGoogle(googleOptions =>
            {
                googleOptions.ClientId = googleAuthenticationOptions
                    .GetValue<string>("ClientId");
                googleOptions.ClientSecret = googleAuthenticationOptions
                    .GetValue<string>("ClientSecret");
                googleOptions.SaveTokens = true;
            });

        return services;
    }

    private static IHttpClientBuilder ConfigureHttpHandler(
        this IHttpClientBuilder httpClientBuilder)
    {
        return httpClientBuilder.ConfigurePrimaryHttpMessageHandler(
            () => new HttpClientHandler
            {
                AllowAutoRedirect = false,
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback =
                    (httpRequestMessage, cert, cetChain, policyErrors) => true
            });
    }

    private static IServiceCollection AddHttpClients(this IServiceCollection services, IConfiguration configuration)
    {
        var apiHttpClientOptions = configuration.GetSection("ApiHttpClient");
        services
            .AddHttpClient(
                "ApiHttpClient",
                client => client.BaseAddress = new 
                    Uri(apiHttpClientOptions.GetValue<string>("Uri")))
            .ConfigureHttpHandler();
        return services;
    }
}