using MassTransit;
using Play.Common.Identity;
using Play.Common.MassTransit;
using Play.Common.Mongo;
using Play.Common.Mongo.Settings;
using Play.Common.Settings;
using Play.Trading.Service.StateMachines;

var AllowedOriginSetting = "AllowedOrigin";
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var serviceSettings = builder.Configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();
var mongoDbSettings = builder.Configuration.GetSection(nameof(MongoSettings)).Get<MongoSettings>();

builder.Services
    .AddMongo(builder.Configuration, serviceSettings.ServiceName);

builder.Services.AddJwtBearerAuthentication();

builder.Services.AddMassTransit(configure =>
{
    configure.UsingPlayEconomyRabbitMq(builder.Configuration, serviceSettings.ServiceName);
    configure.AddSagaStateMachine<PurchaseStateMachine, PurchaseState>()
        .MongoDbRepository(mongoRepository =>
        {
            mongoRepository.Connection = mongoDbSettings.ConnectionString;
            mongoRepository.DatabaseName = serviceSettings.ServiceName;
            mongoRepository.CollectionName = "purchases";
        });
});

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