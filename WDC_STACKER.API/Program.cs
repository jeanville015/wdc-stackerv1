using WDC_STACKER.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddSingleton<CapacityConfigService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Preserve exact C# property names (no camelCase conversion)
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("ReactApp");          // ← 1st

app.UseHttpsRedirection();        // ← 2nd

app.UseAuthorization();           // ← 3rd

app.MapControllers();             // ← 4th

app.Run();
