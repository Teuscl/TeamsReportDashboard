using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using TeamsReportDashboard.Backend.Data;
using TeamsReportDashboard.Backend.Extensions;
using TeamsReportDashboard.Backend.Models.Configuration;
using TeamsReportDashboard.Backend.Interfaces;
using TeamsReportDashboard.Backend.Repositories;
using TeamsReportDashboard.Backend.Services;
using TeamsReportDashboard.Backend.Services.Dashboard;
using TeamsReportDashboard.Filters;
using TeamsReportDashboard.Interfaces;
using TeamsReportDashboard.Services;

// Compatibilidade com Npgsql 9: permite DateTime com Kind=Local/Unspecified (timestamp without time zone)
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddOptions<JwtSettings>()
    .BindConfiguration(JwtSettings.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<EmailSettings>()
    .BindConfiguration(EmailSettings.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ExceptionFilter>();
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost",
        builder =>
        {
            builder.WithOrigins("http://localhost:60414", "https://localhost:60414")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Cookies["accessToken"];
                if (!string.IsNullOrEmpty(accessToken))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

// Defer reading Jwt:Key until DI resolution — config sources (including test overrides) are fully loaded by then.
// JwtSettings.ValidateOnStart() still provides fail-fast for missing/invalid key in production.
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtSettings>>((jwtBearerOptions, jwtSettings) =>
    {
        jwtBearerOptions.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "TeamsReportDashboard",
            ValidateAudience = true,
            ValidAudience = "TeamsReportDashboard",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Value.Key))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient("PythonAnalysisService", client =>
{
    var baseUrl = builder.Configuration["PythonApi:BaseUrl"]
        ?? throw new InvalidOperationException("PythonApi:BaseUrl is not configured.");
    client.BaseAddress = new Uri(baseUrl);
});

// Repositories + UnitOfWork
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IRequesterRepository, RequesterRepository>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IAnalysisJobRepository, AnalysisJobRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Infrastructure
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddAutoMapper(cfg => cfg.AddMaps(Assembly.GetExecutingAssembly()));

// Domain services
builder.Services.AddUserServices();
builder.Services.AddReportServices();
builder.Services.AddDepartmentServices();
builder.Services.AddRequesterServices();
builder.Services.AddAnalysisJobServices();
builder.Services.AddScoped<IDashboardService, DashboardService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowLocalhost");

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await DbInitializer.SeedMasterUser(app);
app.Run();

// Exposes Program to WebApplicationFactory<Program> in the test project
public partial class Program { }
