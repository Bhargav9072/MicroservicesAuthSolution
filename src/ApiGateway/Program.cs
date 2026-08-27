var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// ADD THIS BLOCK
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactPolicy", policy =>
    {
        policy.WithOrigins(
            "https://calm-mushroom-073187200.7.azurestaticapps.net",
            "http://localhost:5173"
        )
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "ApiGateway v1");
    options.SwaggerEndpoint("/auth/swagger/v1/swagger.json", "AuthService v1 (via Gateway)");
    options.SwaggerEndpoint("/users/swagger/v1/swagger.json", "UserService v1 (via Gateway)");
    options.SwaggerEndpoint("/project/swagger/v1/swagger.json", "ProjectService v1 (via Gateway)");
});

// ADD THIS LINE
app.UseCors("ReactPolicy");

app.MapReverseProxy();

app.Run();
