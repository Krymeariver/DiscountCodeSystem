var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();

builder.Services.AddSingleton<DiscountServiceImpl>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<DiscountServiceImpl>();
app.MapGet("/", () => "Use a gRPC client to communicate.");

app.Run();
