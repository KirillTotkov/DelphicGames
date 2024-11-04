using DelphicGames.Data;
using DelphicGames.Data.Models;
using DelphicGames.Models;
using DelphicGames.Services;
using DelphicGames.Services.Streaming;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
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
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "YourAPI", Version = "v1" });

        // Add security definition for Cookie
        c.AddSecurityDefinition("cookieAuth", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Cookie,
            Name = "Cookie",
            Description = "Cookie-based authentication."
        });

        // Add security requirement
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "cookieAuth"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

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


    builder.Services.AddSingleton<StreamProcessor>();
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

    // Остановка трансляций при завершении работы приложения
    var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
    lifetime.ApplicationStopping.Register(() =>
    {
        app.Services.GetRequiredService<StreamManager>().StopAllStreams();
        Console.WriteLine("Application is stopping");
    });

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