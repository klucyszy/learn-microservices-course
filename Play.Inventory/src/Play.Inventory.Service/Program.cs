using GreenPipes;
using Play.Common.Identity;
using Play.Common.MassTransit;
using Play.Common.Mongo;
using Play.Common.Settings;
using Play.Inventory.Service.Clients;
using Play.Inventory.Service.Entities;
using Play.Inventory.Service.Exceptions;

var AllowedOriginSetting = "AllowedOrigin";
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var serviceSettings = builder.Configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();

builder.Services
    .AddMongo(builder.Configuration, serviceSettings.ServiceName)
    .AddMongoRepository<InventoryItem>("inventoryItems")
    .AddMongoRepository<CatalogItem>("catalogItems");

builder.Services.AddJwtBearerAuthentication();

builder.Services.AddMassTransitWithRabbitMq(builder.Configuration, serviceSettings.ServiceName,retryCfg =>
{
    retryCfg.Interval(3, TimeSpan.FromSeconds(5));
    retryCfg.Ignore(typeof(UnknownItemException));
});

builder.Services.AddHttpClient<CatalogClient>(client =>
{
    client.BaseAddress = new Uri("https://localhost:7000");
});

builder.Services.AddControllers(opts =>
{
    opts.SuppressAsyncSuffixInActionNames = false;
});

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
