
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WlcSistemaPedidos.Data;
using WlcSistemaPedidos.Models;
using WlcSistemaPedidos.Services;
using WlcSistemaPedidos.Validators;

var builder = WebApplication.CreateBuilder(args);

// BANCO DE DADOS
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "A connection string 'DefaultConnection' não foi configurada.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// IDENTITY
builder.Services
    .AddIdentity<Usuario, IdentityRole<int>>(options =>
    {
        options.Password.RequiredLength = 6;
        options.Password.RequiredUniqueChars = 1;
        options.Password.RequireDigit = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;

        options.Lockout.AllowedForNewUsers = false;

        options.User.RequireUniqueEmail = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<IPasswordValidator<Usuario>, SenhaNumericaValidator>();

// SERVIÇOS
builder.Services.AddScoped<AcessoClienteService>();
builder.Services.AddScoped<ProcessadorLembretesService>();

// WHATSAPP
builder.Services.AddSingleton<IWhatsAppSender, WhatsAppSenderPendente>();

// LEMBRETES AUTOMÁTICOS
builder.Services.AddHostedService<LembretesBackgroundService>();

// COOKIE
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Conta/Login";
    options.AccessDeniedPath = "/Conta/AcessoNegado";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// SESSÃO
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// INICIALIZAÇÃO DO BANCO
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var context = services.GetRequiredService<AppDbContext>();
    var userManager = services.GetRequiredService<UserManager<Usuario>>();

    await DbInitializer.InicializarAsync(
        context,
        userManager,
        app.Configuration,
        app.Environment);
}

// PIPELINE
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
