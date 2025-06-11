using System.Reflection;
using System.Text;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using TeamsReportDashboard.Backend.Models.Configuration;
using TeamsReportDashboard.Backend.Models.ReportDto;
using TeamsReportDashboard.Backend.Models.UserDto;
using TeamsReportDashboard.Backend.Services;
using TeamsReportDashboard.Backend.Services.Report.Create;
using TeamsReportDashboard.Backend.Services.Report.Read;
using TeamsReportDashboard.Backend.Services.Report.Update;
using TeamsReportDashboard.Backend.Services.User.ChangeMyPassword;
using TeamsReportDashboard.Backend.Services.User.ForgotPassword;
using TeamsReportDashboard.Backend.Services.User.ResetForgottenPassword;
using TeamsReportDashboard.Backend.Services.User.ResetPassword;
using TeamsReportDashboard.Backend.Services.User.Update;
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
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
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
            builder.WithOrigins("http://localhost:60415")
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
                var accessToken = context.Request.Cookies["accessToken"];
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
builder.Services.AddScoped<IValidator<CreateReportDto>, CreateReportValidator>();  // Validador do CreateUserDto
builder.Services.AddScoped<IValidator<UpdateReportDto>, UpdateReportValidator>();  // Validador do UpdateUserDto
builder.Services.AddScoped<IValidator<ChangeMyPasswordDto>, ChangeMyPasswordValidator>();  // Validador do UpdateUserDto
builder.Services.AddScoped<IValidator<CreateReportDto>, CreateReportValidator>();
builder.Services.AddScoped<IValidator<UpdateReportDto>, UpdateReportValidator>();
builder.Services.AddScoped<IValidator<ResetPasswordDto>, ResetPasswordValidator>();
builder.Services.AddScoped<IValidator<ForgotPasswordDto>, ForgotPasswordValidator>();
builder.Services.AddScoped<IUnitOfWork,  UnitOfWork>();
builder.Services.AddTransient<IEmailService, EmailService>();

builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();

builder.Services.AddScoped<ICreateUserService, CreateUserService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IDeleteUserService, DeleteUserService>();
builder.Services.AddScoped<IGetUsersService, GetUsersService>();
builder.Services.AddScoped<IUpdateUserService, UpdateUserService>();
builder.Services.AddScoped<IChangeMyPasswordService, ChangeMyPasswordService>();
builder.Services.AddScoped<IResetPasswordService, ResetPasswordService>();
builder.Services.AddScoped<IForgotPasswordService, ForgotPasswordService>();
builder.Services.AddScoped<IResetForgottenPasswordService, ResetForgottenPasswordService>();


builder.Services.AddScoped<ICreateReportService, CreateReportService>();
builder.Services.AddScoped<IUpdateReportService, UpdateReportService>();
builder.Services.AddScoped<IGetReportService, GetReportService>();
builder.Services.AddScoped<IDeleteReportService, DeleteReportService>();


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

