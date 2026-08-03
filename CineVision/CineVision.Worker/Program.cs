using CineVision.Worker;
using CineVision.Worker.Services;

// Same .env as the API (next to docker-compose.yml). Docker Compose injects env already.
EnvFileLoader.Load(
    Path.Combine(Directory.GetCurrentDirectory(), ".env"),
    Path.Combine(Directory.GetCurrentDirectory(), "..", ".env"),
    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".env")));

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<EmailConsumerWorker>();

var host = builder.Build();
host.Run();
