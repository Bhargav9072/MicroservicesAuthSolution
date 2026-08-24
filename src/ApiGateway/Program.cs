var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ApiGateway v1");
        options.SwaggerEndpoint("/auth/swagger/v1/swagger.json", "AuthService v1 (via Gateway)");
        options.SwaggerEndpoint("/users/swagger/v1/swagger.json", "UserService v1 (via Gateway)");
        options.SwaggerEndpoint("/project/swagger/v1/swagger.json", "ProjectService v1 (via Gateway)");
    });
}

app.MapReverseProxy();

app.Run();
