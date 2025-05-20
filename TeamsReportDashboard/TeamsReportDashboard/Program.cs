using System.Text;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using TeamsReportDashboard.Data;
using TeamsReportDashboard.Filters;
using TeamsReportDashboard.Interfaces;
using TeamsReportDashboard.Models.Dto;
using TeamsReportDashboard.Repositories;
using TeamsReportDashboard.Services;
using TeamsReportDashboard.Services.User.ChangePassword;
using TeamsReportDashboard.Services.User.Create;
using TeamsReportDashboard.Services.User.Delete;
using TeamsReportDashboard.Services.User.Read;
using TeamsReportDashboard.Services.User.Update;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ExceptionFilter>();
});
// Configurar política de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost",
        builder =>
        {
            builder.WithOrigins("http://localhost:60414")
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
                // Pega o token do cookie "jwt-token"
                var accessToken = context.Request.Cookies["acessToken"];
                if (!string.IsNullOrEmpty(accessToken))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

builder.Services.AddAuthorization();
    
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Injeção de dependências
builder.Services.AddScoped<ITokenService, TokenService>();  // Adiciona o TokenService
builder.Services.AddScoped<IAuthService, AuthService>();  // Adiciona o AuthService
builder.Services.AddScoped<IValidator<CreateUserDto>, CreateUserValidator>();  // Validador do CreateUserDto
builder.Services.AddScoped<IValidator<UpdateUserDto>, UpdateUserValidator>();  // Validador do UpdateUserDto
builder.Services.AddScoped<IValidator<ChangePasswordDto>, ChangePasswordValidator>();  // Validador do UpdateUserDto
builder.Services.AddScoped<IUnitOfWork,  UnitOfWork>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICreateUserService, CreateUserService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IDeleteUserService, DeleteUserService>();
builder.Services.AddScoped<IGetUsersService, GetUsersService>();
builder.Services.AddScoped<IUpdateUserService, UpdateUserService>();
builder.Services.AddScoped<IChangePasswordService, ChangePasswordService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Usar CORS
app.UseCors("AllowLocalhost");
// Configuração de autorização
app.UseHttpsRedirection();
app.UseAuthentication(); //Add configuration to use JWT
app.UseAuthorization(); //Add configuration to use JWT

// Mapear os controllers
app.MapControllers();


app.Run();

