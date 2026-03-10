using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MLN131.Api.Common;

namespace MLN131.Api.Data;

public static class SeedData
{
    public static async Task EnsureSeededAsync(IServiceProvider sp, CancellationToken ct = default)
    {
        using var scope = sp.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("SeedData");

        // In containers, SQL Server might not be ready yet. Retry migrations for a short window.
        const int maxRetries = 10;
        var delay = TimeSpan.FromSeconds(2);

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                logger.LogInformation("Applying database migrations (attempt {attempt}/{maxRetries})", attempt, maxRetries);
                await db.Database.MigrateAsync(ct);
                break;
            }
            catch when (attempt < maxRetries)
            {
                await Task.Delay(delay, ct);
            }
        }

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var adminEmail = (config["SeedAdmin:Email"] ?? "").Trim();
        var adminPassword = config["SeedAdmin:Password"] ?? "";

        if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var admin = await userManager.FindByEmailAsync(adminEmail);
            if (admin is null)
            {
                admin = new AppUser
                {
                    Id = Guid.NewGuid(),
                    Email = adminEmail,
                    UserName = adminEmail,
                    EmailConfirmed = true,
                    FullName = "Administrator",
                };

                var create = await userManager.CreateAsync(admin, adminPassword);
                if (!create.Succeeded)
                {
                    throw new InvalidOperationException("Seed admin failed: " + string.Join("; ", create.Errors.Select(e => e.Description)));
                }
            }

            var roles = await userManager.GetRolesAsync(admin);
            if (!roles.Contains(Roles.Admin))
            {
                await userManager.AddToRoleAsync(admin, Roles.Admin);
            }
        }

        if (!await db.ContentPages.AnyAsync(p => p.Slug == "chuong-5", ct))
        {
            var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
            var faqsPath = Path.Combine(env.ContentRootPath, "Resources", "faqs_vi.txt");
            var body = File.Exists(faqsPath) ? await File.ReadAllTextAsync(faqsPath, ct) : "";

            db.ContentPages.Add(new ContentPage
            {
                Slug = "chuong-5",
                Title = "Chương 5 - Cơ cấu xã hội - giai cấp và liên minh giai cấp, tầng lớp",
                BodyMarkdown = body,
                CreatedAt = DateTimeOffset.UtcNow,
            });

            await db.SaveChangesAsync(ct);
        }
    }
}

