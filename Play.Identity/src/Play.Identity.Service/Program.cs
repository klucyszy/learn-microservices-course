using GreenPipes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Play.Common.MassTransit;
using Play.Common.Mongo.Settings;
using Play.Common.Settings;
using Play.Identity.Service.Entities;
using Play.Identity.Service.Exceptions;
using Play.Identity.Service.HostedServices;
using Play.Identity.Service.Settings;

var AllowedOriginSetting = "AllowedOrigin";
var builder = WebApplication.CreateBuilder(args);

var serviceSetting = builder.Configuration
    .GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();
var mongoDbSettings = builder.Configuration
    .GetSection(nameof(MongoSettings)).Get<MongoSettings>();
var identityServerSettings = builder.Configuration
    .GetSection(nameof(IdentityServerSettings)).Get<IdentityServerSettings>();

BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));

builder.Services.Configure<IdentitySettings>(builder.Configuration.GetSection(nameof(IdentitySettings)));

builder.Services.AddDefaultIdentity<ApplicationUser>()
    .AddRoles<ApplicationRole>()
    .AddMongoDbStores<ApplicationUser, ApplicationRole, Guid>(
        mongoDbSettings.ConnectionString, serviceSetting.ServiceName);

builder.Services.AddMassTransitWithRabbitMq(builder.Configuration, serviceSetting.ServiceName, retryCfg =>
{
    retryCfg.Interval(3, TimeSpan.FromSeconds(5));
    retryCfg.Ignore(typeof(UserUnknownException));
    retryCfg.Ignore(typeof(NotEnoughGilException));
});

builder.Services.AddIdentityServer()
    .AddAspNetIdentity<ApplicationUser>()
    .AddInMemoryApiScopes(identityServerSettings.ApiScopes)
    .AddInMemoryApiResources(identityServerSettings.ApiResources)
    .AddInMemoryClients(identityServerSettings.Clients)
    .AddInMemoryIdentityResources(identityServerSettings.Resources)
    .AddDeveloperSigningCredential();

builder.Services.AddLocalApiAuthentication();
builder.Services.AddControllers();
builder.Services.AddHostedService<IdentitySeedHostedService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors(builder =>
    {
        builder.WithOrigins(app.Configuration[AllowedOriginSetting])
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseIdentityServer();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

app.Run();
