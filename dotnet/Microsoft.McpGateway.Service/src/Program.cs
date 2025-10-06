// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Security.Claims;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Identity.Web;
using Microsoft.McpGateway.Management.Deployment;
using Microsoft.McpGateway.Management.Service;
using Microsoft.McpGateway.Management.Store;
using Microsoft.McpGateway.Service.Routing;
using Microsoft.McpGateway.Service.Session;
using Microsoft.McpGateway.Management.Configuration;
using ModelContextProtocol.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);
var credential = new DefaultAzureCredential();

builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddLogging();

// Configure KubernetesSettings
builder.Services.Configure<KubernetesSettings>(builder.Configuration.GetSection("KubernetesSettings"));

builder.Services.AddSingleton<IKubernetesClientFactory, LocalKubernetesClientFactory>();
builder.Services.AddSingleton<IAdapterSessionStore, DistributedMemorySessionStore>();
builder.Services.AddSingleton<IServiceNodeInfoProvider, AdapterKubernetesNodeInfoProvider>();
builder.Services.AddSingleton<ISessionRoutingHandler, AdapterSessionRoutingHandler>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IAdapterResourceStore, InMemoryAdapterResourceStore>();
    builder.Services.AddDistributedMemoryCache();
}
else
{
    var azureAdConfig = builder.Configuration.GetSection("AzureAd");
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddScheme<McpAuthenticationOptions, McpSubPathAwareAuthenticationHandler>(
        McpAuthenticationDefaults.AuthenticationScheme,
        McpAuthenticationDefaults.DisplayName,
    options =>
    {
        options.ResourceMetadata = new()
        {
            Resource = new Uri(builder.Configuration.GetValue<string>("PublicOrigin")!),
            AuthorizationServers = { new Uri($"https://login.microsoftonline.com/{azureAdConfig["TenantId"]}/v2.0") },
            ScopesSupported = [$"api://{azureAdConfig["ClientId"]}/.default"]
        };
    })
    .AddMicrosoftIdentityWebApi(azureAdConfig);

    builder.Services.AddSingleton<IAdapterResourceStore>(c =>
    {
        var cosmosSettings = builder.Configuration.GetSection("CosmosSettings").Get<CosmosSettings>() ?? new CosmosSettings();
        var connectionString = cosmosSettings.ConnectionString;
        var client = string.IsNullOrEmpty(connectionString) ? new CosmosClient(cosmosSettings.AccountEndpoint, credential) : new CosmosClient(connectionString);
        return new CosmosAdapterResourceStore(client, cosmosSettings.DatabaseName, cosmosSettings.AdapterContainerName, c.GetRequiredService<ILogger<CosmosAdapterResourceStore>>());
    });
    builder.Services.AddCosmosCache(options =>
    {
        var cosmosSettings = builder.Configuration.GetSection("CosmosSettings").Get<CosmosSettings>() ?? new CosmosSettings();

        options.ContainerName = cosmosSettings.CacheContainerName;
        options.DatabaseName = cosmosSettings.DatabaseName;
        options.CreateIfNotExists = true;

        options.ClientBuilder = string.IsNullOrEmpty(cosmosSettings.ConnectionString) ? new CosmosClientBuilder(cosmosSettings.AccountEndpoint, credential) : new CosmosClientBuilder(cosmosSettings.ConnectionString);
    });
}

builder.Services.AddSingleton<IKubeClientWrapper>(c =>
{
    var kubeClientFactory = c.GetRequiredService<IKubernetesClientFactory>();
    var kubernetesSettings = builder.Configuration.GetSection("KubernetesSettings").Get<KubernetesSettings>() ?? new KubernetesSettings();
    return new KubeClient(kubeClientFactory, kubernetesSettings.Namespace);
});
builder.Services.AddSingleton<IAdapterDeploymentManager>(c =>
{
    var containerRegistrySettings = builder.Configuration.GetSection("ContainerRegistrySettings").Get<ContainerRegistrySettings>() ?? new ContainerRegistrySettings();
    var kubernetesSettings = builder.Configuration.GetSection("KubernetesSettings").Get<KubernetesSettings>() ?? new KubernetesSettings();
    var serviceSettings = builder.Configuration.GetSection("ServiceSettings").Get<ServiceSettings>() ?? new ServiceSettings();
    
    return new KubernetesAdapterDeploymentManager(
        containerRegistrySettings, 
        kubernetesSettings, 
        serviceSettings, 
        c.GetRequiredService<IKubeClientWrapper>(), 
        c.GetRequiredService<ILogger<KubernetesAdapterDeploymentManager>>());
});
builder.Services.AddSingleton<IAdapterManagementService, AdapterManagementService>();
builder.Services.AddSingleton<IAdapterRichResultProvider, AdapterRichResultProvider>();

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddHttpClient();

builder.WebHost.ConfigureKestrel(options =>
{
    var serviceSettings = builder.Configuration.GetSection("ServiceSettings").Get<ServiceSettings>() ?? new ServiceSettings();
    options.ListenAnyIP(serviceSettings.Port);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        var devIdentity = new ClaimsIdentity("Development");
        devIdentity.AddClaim(new Claim(ClaimTypes.Name, "dev"));
        context.User = new ClaimsPrincipal(devIdentity);
        await next();
    });
}

// Configure the HTTP request pipeline.
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
