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
        await db.Database.MigrateAsync(ct);

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

