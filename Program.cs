using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Events;
using EduPulse.Api.Data;
using EduPulse.Api.Middleware;
using EduPulse.Api.Services;

namespace EduPulse.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Configure Serilog bootstrap logger
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("logs/edupulse-audit-.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Log.Information("Starting EduPulse Enterprise EdTech Platform Web API...");

                var builder = WebApplication.CreateBuilder(args);

                // Attach Serilog
                builder.Host.UseSerilog();

                // 1. Configure EF Core with SQLite
                string dbPath = Path.Combine(builder.Environment.ContentRootPath, "edupulse.db");
                builder.Services.AddDbContext<EduPulseDbContext>(options =>
                    options.UseSqlite($"Data Source={dbPath}"));

                // 2. Register Application Services
                builder.Services.AddScoped<IAuditService, AuditService>();
                builder.Services.AddScoped<ICourseService, CourseService>();
                builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
                builder.Services.AddScoped<IAssessmentService, AssessmentService>();
                builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
                builder.Services.AddScoped<IPaymentService, PaymentService>();

                // 3. Controllers & JSON Options
                builder.Services.AddControllers()
                    .AddJsonOptions(options =>
                    {
                        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                    });

                // 4. OpenAPI / Swagger Configuration
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Title = "EduPulse Enterprise EdTech Platform API",
                        Version = "v1",
                        Description = "RESTful API platform for Learning Management, Interactive Quizzes, Analytics, Serilog Audit Logging, and IIS Enterprise Hosting.",
                        Contact = new OpenApiContact
                        {
                            Name = "EduPulse Core Engineering",
                            Email = "support@edupulse.org"
                        }
                    });
                });

                // 5. CORS for web integration
                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("AllowAll", policy =>
                    {
                        policy.AllowAnyOrigin()
                              .AllowAnyHeader()
                              .AllowAnyMethod();
                    });
                });

                var app = builder.Build();

                // Seed database on startup
                using (var scope = app.Services.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<EduPulseDbContext>();
                    DbInitializer.Initialize(db);
                }

                // Middleware Pipeline
                app.UseCors("AllowAll");

                // Enable Swagger UI in development & production for easy evaluation
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "EduPulse REST API v1");
                    c.RoutePrefix = "swagger";
                });

                // Custom Serilog Request Logging
                app.UseMiddleware<SerilogRequestLoggingMiddleware>();

                // Static files for Web UI
                app.UseDefaultFiles();
                app.UseStaticFiles();

                app.UseRouting();
                app.UseAuthorization();
                app.MapControllers();

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "EduPulse Web API terminated unexpectedly.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
