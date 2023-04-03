var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<WebApiAspNetCore.Config>();
builder.Services.AddSingleton<WebApiAspNetCore.Logger>();
builder.Services.AddSingleton<WebApiAspNetCore.JiraBus.Main>();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
