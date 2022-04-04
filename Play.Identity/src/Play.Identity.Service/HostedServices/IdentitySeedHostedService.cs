using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Play.Identity.Service.Entities;
using Play.Identity.Service.Settings;

namespace Play.Identity.Service.HostedServices;

public class IdentitySeedHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IdentitySettings _settings;

    public IdentitySeedHostedService(IServiceScopeFactory scopeFactory,
        IOptions<IdentitySettings> settings)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await CreateRoleIfNotExists(Roles.Player, roleManager);
        await CreateRoleIfNotExists(Roles.Admin, roleManager);

        var adminUser = await userManager.FindByEmailAsync(_settings.AdminUserEmail);
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = _settings.AdminUserEmail,
                Email = _settings.AdminUserEmail
            };

            await userManager.CreateAsync(adminUser, _settings.AdminUserPassword);
            await userManager.AddToRoleAsync(adminUser, Roles.Admin);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static async Task CreateRoleIfNotExists(string role, RoleManager<ApplicationRole> roleManager)
    {
        var exists = await roleManager.RoleExistsAsync(role);
        if (!exists)
        {
            await roleManager.CreateAsync(new ApplicationRole
            {
                Name = role
            });
        }
    }
}