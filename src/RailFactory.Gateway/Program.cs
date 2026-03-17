using RailFactory.Gateway;

var builder = WebApplication.CreateBuilder(args);

// Add standard Aspire service defaults
builder.AddServiceDefaults();

// Gateway specific configuration
builder.Services.AddGatewayConfiguration(builder.Configuration);

var app = builder.Build();

// Gateway middleware pipeline
app.UseGatewayMiddleware();

// Standard Aspire endpoints
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Map YARP Reverse Proxy
app.MapReverseProxy();

app.Run();
