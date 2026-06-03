using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using WDC_STACKER.API.Aggregate;
using WDC_STACKER.API.Services;
using WDC_STACKER.API.Swagger;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.





// Auth services — order matters: AD service first, then the aggregation layer
builder.Services.AddScoped<ActiveDirectoryService>();

// Aggregate layer (sits between service and controller)
builder.Services.AddScoped<AuthProjectionAggregate>();

builder.Services.AddScoped<FeatsService>();
builder.Services.AddScoped<UserPrivilegesService>();
builder.Services.AddScoped<StackerAggregate>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddSingleton<CapacityConfigService>();
builder.Services.AddSingleton<FeatsCredentialStore>();
builder.Services.AddScoped<StackerSqlService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Preserve exact C# property names (no camelCase conversion)
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// builder.Services.AddOpenApi();
// Replace AddOpenApi() with these two:
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.OperationFilter<AddRequiredHeaderParameter>();
});





// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", policy =>
    {
        policy.WithOrigins(
            "http://localhost:5173",   // React dev server
            "https://localhost:5173"
        )
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SigningKey"]!))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.MapOpenApi();
    // Replace MapOpenApi() with these two:
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("ReactApp");          // 1st

app.UseHttpsRedirection();        // 2nd

app.UseAuthentication();          // 3rd

app.UseAuthorization();           // 4th

app.MapControllers();             // 5th

app.Run();
