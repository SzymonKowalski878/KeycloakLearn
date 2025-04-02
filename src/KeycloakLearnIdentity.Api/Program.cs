using KeycloakLearnIdentity.Api.Services;
using KeycloakLearnIdentity.Api.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using KeycloakLearnIdentity.Api.Database;
using Microsoft.EntityFrameworkCore;
using KeycloakLearnIdentity.Api.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<KeycloakSettings>(builder.Configuration.GetSection("Authentication:Keycloak"));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var keycloakSettings = builder.Configuration.GetSection("Authentication:Keycloak").Get<KeycloakSettings>();

        options.Authority = keycloakSettings.Authority;
        options.Audience = keycloakSettings.Audience;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, // Ensure the token is from your Keycloak
            ValidateAudience = true, // Validate the "aud" claim matches your client ID
            ValidateLifetime = true, // Ensure the token is not expired
            ValidateIssuerSigningKey = true // Verify the signature of the token
        };
        //TODO: take this value from env config
        options.RequireHttpsMetadata = false; // Use HTTPS for metadata fetching
    });

builder.Services.AddAuthorization();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Keycloak Integration API",
        Version = "v1",
        Description = "API demonstrating Keycloak integration with .NET 9"
    });

    // Add Bearer token authentication in Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter 'Bearer' followed by a space and the JWT token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddScoped<IKeycloakService, KeycloakService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Keycloak Integration API v1");
    options.RoutePrefix = string.Empty; // Swagger will be available at the root URL
});

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();