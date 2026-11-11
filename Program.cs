using Analise.Data;
using Analise.Helper;
using Analise.Models;
using Analise.Repositories;
using Analise.Repositorio;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using QuestPDF.Infrastructure; // Necessário para a licença do QuestPDF


var builder = WebApplication.CreateBuilder(args);

// REGISTAR LICENÇA DO QUESTPDF (Obrigatório para .NET 8)
QuestPDF.Settings.UseSystemFonts = true;
QuestPDF.Settings.License = LicenseType.Community;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

//// CONFIGURAR PORTA PARA RAILWAY
//if (!builder.Environment.IsDevelopment())
//{
//    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
//    builder.WebHost.UseUrls($"http://*:{port}");
//}

// CONFIGURAR PORTA PARA RAILWAY E LOCAL
var portVar = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(portVar))
{
    builder.WebHost.UseUrls($"http://*:{portVar}");
}
else if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.UseUrls("http://0.0.0.0:8080");
}

// SMTP
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SMTP"));

// Serviços
builder.Services.AddTransient<EmailService>();
//builder.Services.AddTransient<TicketRepositorio>();
builder.Services.AddControllersWithViews();

// POSTGRESQL (IMPORTANTE)
builder.Services.AddDbContext<BancoContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<ISessao, Sessao>();
builder.Services.AddScoped<IEmail, Email>();
builder.Services.AddScoped<IAgenciaRepositorio, AgenciaRepositorio>();
builder.Services.AddScoped<ICargoRepositorio, CargoRepositorio>();
builder.Services.AddScoped<IEstadoRepositorio, EstadoRepositorio>();
builder.Services.AddScoped<IEntidadeRepositorio, EntidadeRepositorio>();
builder.Services.AddScoped<ISimuladorRepositorio, SimuladorRepositorio>();
builder.Services.AddScoped<IPedidoRepositorio, PedidoRepositorio>();
builder.Services.AddScoped<IConfiguracaoRepositorio, ConfiguracaoRepositorio>();
builder.Services.AddScoped<IContaRepositorio, ContaRepositorio>();
builder.Services.AddScoped<IMovimentoInternoRepositorio, MovimentoInternoRepositorio>();
builder.Services.AddScoped<IDespesaRepositorio, DespesaRepositorio>();
builder.Services.AddScoped<IFechoDiarioRepositorio, FechoDiarioRepositorio>();

builder.Services.AddSession(o =>
{
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

app.Run();