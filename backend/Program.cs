using backend.Data;
using backend.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<RecaptchaOptions>(
    builder.Configuration.GetSection(RecaptchaOptions.SectionName));

builder.Services.AddHttpClient();
builder.Services.AddScoped<IRecaptchaService, RecaptchaService>();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString);
});

var allowedOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ??
                     new[] { "http://localhost:4200" };

const string corsPolicyName = "AllowFrontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicyName, policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

var enableHttpsRedirection = app.Configuration.GetValue<bool?>("EnableHttpsRedirection") ?? false;

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (enableHttpsRedirection)
{
    app.UseHttpsRedirection();
}
app.UseCors(corsPolicyName);

app.MapControllers();

app.Run();
