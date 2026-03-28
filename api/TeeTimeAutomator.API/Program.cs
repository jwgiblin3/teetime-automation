using System.Text;
using AutoMapper;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using TeeTimeAutomator.API.Adapters;
using TeeTimeAutomator.API.Data;
using TeeTimeAutomator.API.Models;
using TeeTimeAutomator.API.Models.DTOs;
using TeeTimeAutomator.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Hangfire
builder.Services.AddHangfire(configuration =>
    configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(c =>
            c.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"))));

builder.Services.AddHangfireServer();

// Add authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["Secret"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Log.Warning("JWT authentication failed: {Message}", context.Exception?.Message);
            return Task.CompletedTask;
        }
    };
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Google:ClientId"]!;
    options.ClientSecret = builder.Configuration["Google:ClientSecret"]!;
});

// Add authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireAssertion(context =>
        {
            var claim = context.User.FindFirst("admin");
            return claim?.Value == "true";
        }));
});

// Add CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "*" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Add services
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<ICalendarService, CalendarService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAdminService, AdminService>();

// Booking adapters
builder.Services.AddScoped<IBookingAdapterFactory, BookingAdapterFactory>();
builder.Services.AddScoped<CpsGolfAdapter>();
builder.Services.AddScoped<GolfNowAdapter>();
builder.Services.AddScoped<TeeSnapAdapter>();
builder.Services.AddScoped<ForeUpAdapter>();

// Named HttpClient for CPS Golf REST API calls
builder.Services.AddHttpClient("CpsGolf", client =>
{
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Add Controllers
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    });

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TeeTimeAutomator API",
        Version = "v1",
        Description = "Golf tee time booking automation system",
        Contact = new OpenApiContact
        {
            Name = "TeeTimeAutomator",
            Email = "support@teetimeautomator.com"
        },
        License = new OpenApiLicense
        {
            Name = "MIT",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "bearerAuth"
        }
    };

    c.AddSecurityDefinition("bearerAuth", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            securityScheme,
            new string[] { }
        }
    });

    c.EnableAnnotations();
});

var app = builder.Build();

// Apply migrations at startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    Log.Information("Database migrations applied");
}

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TeeTimeAutomator API V1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowSpecificOrigins");

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

// Configure Hangfire dashboard
// In development: open access for easy local debugging.
// In production: swap back to HangfireAuthorizationFilter.
var isDevelopment = app.Environment.IsDevelopment();
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    IsReadOnlyFunc = _ => false,
    Authorization = isDevelopment
        ? new[] { new HangfireAllowAllFilter() }
        : new IDashboardAuthorizationFilter[] { new HangfireAuthorizationFilter() }
});

app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();

/// <summary>
/// Allows all requests — used in local development only.
/// </summary>
public class HangfireAllowAllFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}

/// <summary>
/// Custom Hangfire authorization filter to ensure only admins can access the dashboard.
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var user = context.GetHttpContext().User;
        return user?.Identity?.IsAuthenticated == true &&
               user.IsInRole("Admin");
    }
}

/// <summary>
/// AutoMapper profile for entity/DTO mappings.
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User mappings
        CreateMap<User, UserProfileDto>().ReverseMap();

        // Course mappings
        CreateMap<Course, CourseDto>().ReverseMap();
        CreateMap<CreateCourseRequest, Course>();
        CreateMap<UpdateCourseRequest, Course>();

        // Booking mappings
        CreateMap<BookingRequest, BookingRequestDto>()
            .ForMember(d => d.CourseName, opt => opt.MapFrom(s => s.Course.CourseName))
            .ReverseMap();

        CreateMap<BookingResult, BookingResultDto>().ReverseMap();

        CreateMap<CreateBookingRequest, BookingRequest>();

        // Audit mappings
        CreateMap<AuditLog, AuditLogDto>().ReverseMap();
    }
}

/// <summary>
/// DTO for audit log entries.
/// </summary>
public class AuditLogDto
{
    public int LogId { get; set; }
    public int? UserId { get; set; }
    public int? RequestId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

