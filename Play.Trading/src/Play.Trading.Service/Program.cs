using System.Reflection;
using GreenPipes;
using MassTransit;
using Play.Common.Identity;
using Play.Common.MassTransit;
using Play.Common.Mongo;
using Play.Common.Mongo.Settings;
using Play.Common.Settings;
using Play.Identity.Contracts;
using Play.Inventory.Contracts;
using Play.Trading.Service.Entities;
using Play.Trading.Service.Exceptions;
using Play.Trading.Service.Settings;
using Play.Trading.Service.StateMachines;

var AllowedOriginSetting = "AllowedOrigin";
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var serviceSettings = builder.Configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();
var mongoDbSettings = builder.Configuration.GetSection(nameof(MongoSettings)).Get<MongoSettings>();
var queueSettings = builder.Configuration.GetSection(nameof(QueueSettings)).Get<QueueSettings>();

builder.Services
    .AddMongo(builder.Configuration, serviceSettings.ServiceName)
    .AddMongoRepository<CatalogItem>("catalogItems");

builder.Services.AddJwtBearerAuthentication();

builder.Services.AddMassTransit(configure =>
{
    configure.UsingPlayEconomyRabbitMq(builder.Configuration, serviceSettings.ServiceName,
        retryCfg =>
        {
            retryCfg.Interval(3, TimeSpan.FromSeconds(5));
            retryCfg.Ignore<UnknownItemException>();
        });
    configure.AddConsumers(Assembly.GetEntryAssembly());
    configure.AddSagaStateMachine<PurchaseStateMachine, PurchaseState>(sagaCfg =>
        {
            sagaCfg.UseInMemoryOutbox();
        })
        .MongoDbRepository(mongoRepository =>
        {
            mongoRepository.Connection = mongoDbSettings.ConnectionString;
            mongoRepository.DatabaseName = serviceSettings.ServiceName;
            mongoRepository.CollectionName = "purchases";
        });
});

EndpointConvention.Map<GrantItems>(new Uri(queueSettings.GrantItemsQueueAddress));
EndpointConvention.Map<DebitGil>(new Uri(queueSettings.DebitGilQueueAddress));
EndpointConvention.Map<SubstractItems>(new Uri(queueSettings.SubstractItemsQueueAddress));

builder.Services.AddMassTransitHostedService();
builder.Services.AddGenericRequestClient();

builder.Services.AddControllers(opts =>
{
    opts.SuppressAsyncSuffixInActionNames = false;
})
.AddJsonOptions(options => options.JsonSerializerOptions.IgnoreNullValues = true);

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
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();