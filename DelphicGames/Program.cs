using DelphicGames.Data;
using DelphicGames.Data.Models;
using DelphicGames.Models;
using DelphicGames.Services;
using DelphicGames.Services.Streaming;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft.AspNetCore.Hosting", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Mvc", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Routing", LogEventLevel.Warning)
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting web application");

    var builder = WebApplication.CreateBuilder(args);

    builder.Configuration.AddEnvironmentVariables();

    builder.Services.AddSerilog();
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c => { });

    builder.Services.AddAuthorization();
    builder.Services.AddRazorPages();

    builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
    var emailSettings = builder.Configuration.GetSection("EmailSettings").Get<EmailSettings>();
    if (emailSettings.EnableSendConfirmationEmail)
    {
        builder.Services.AddTransient<IEmailSender, EmailSender>();
    }
    else
    {
        builder.Services.AddTransient<IEmailSender, NoOpEmailSender>();
    }

    builder.Services.AddDbContext<ApplicationContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
            .UseSnakeCaseNamingConvention());

    builder.Services.AddIdentity<User, IdentityRole>(options =>
        {
            options.SignIn.RequireConfirmedAccount = true;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddEntityFrameworkStores<ApplicationContext>()
        .AddDefaultTokenProviders();

    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });


    builder.Services.AddSingleton<IStreamProcessor, TestStreamProcessor>();
    builder.Services.AddSingleton<StreamManager>();
    builder.Services.AddScoped<CameraService>();
    builder.Services.AddScoped<StreamService>();
    builder.Services.AddScoped<PlatformService>();
    builder.Services.AddScoped<NominationService>();

    var rootUserConfig = builder.Configuration.GetSection("RootUser").Get<RootUserConfig>();


    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapRazorPages();

    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        Log.Information("Applying migrations...");
        dbContext.Database.Migrate();
        dbContext.EnsurePlatforms();
        Log.Information("Migrations applied");
    }


    // Создаем роли
    using (var scope = app.Services.CreateScope())
    {
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roleManager.RoleExistsAsync(nameof(UserRoles.Root)))
        {
            var rootRole = new IdentityRole(nameof(UserRoles.Root));
            await roleManager.CreateAsync(rootRole);
        }

        if (!await roleManager.RoleExistsAsync(nameof(UserRoles.Admin)))
        {
            var adminRole = new IdentityRole(nameof(UserRoles.Admin));
            await roleManager.CreateAsync(adminRole);
        }

        if (!await roleManager.RoleExistsAsync(nameof(UserRoles.Specialist)))
        {
            var specRole = new IdentityRole(nameof(UserRoles.Specialist));
            await roleManager.CreateAsync(specRole);
        }
    }

    // Создаем роль cуперадминистратора и пользователя-cуперадминистратора, если их нет
    using (var scope = app.Services.CreateScope())
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Создаем роль cуперадминистратора, если ее нет
        if (!await roleManager.RoleExistsAsync(nameof(UserRoles.Root)))
        {
            var rootRole = new IdentityRole(nameof(UserRoles.Root));
            await roleManager.CreateAsync(rootRole);
        }

        var rootUser = await userManager.FindByNameAsync(rootUserConfig.Email);
        // Проверяем, есть ли уже пользователь-cуперадминистратора
        if (rootUser == null)
        {
            rootUser = new User
            {
                UserName = rootUserConfig.Email,
                Email = rootUserConfig.Email,
                EmailConfirmed = true
            };

            // Создаем пользователя с паролем
            var result = await userManager.CreateAsync(rootUser, rootUserConfig.Password);
            if (result.Succeeded)
            {
                // Назначаем роль cуперадминистратора
                await userManager.AddToRoleAsync(rootUser, nameof(UserRoles.Root));
            }
        }
    }

    // Добавляем регион и город
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
        var region = new Region()
        {
            Name = "Республика Коми",
            Cities = new List<City>()
        };
        var city = new City()
        {
            Name = "Сыктывкар",
            Region = region
        };

        region.Cities.Add(city);

        if (!await dbContext.Regions.AnyAsync(x => x.Name == region.Name))
        {
            await dbContext.Regions.AddAsync(region);

            await dbContext.SaveChangesAsync();
        }
    }


    // Остановка трансляций при завершении работы приложения
    app.Lifetime.ApplicationStopping.Register(() =>
    {
        using var scope = app.Services.CreateScope();
        var streamService = scope.ServiceProvider.GetRequiredService<StreamService>();
        streamService.StopAllStreams();
        Log.Information("Все трансляции остановлены");
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}