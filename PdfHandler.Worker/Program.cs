using PdfHandler.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<PdfWorker>();

var host = builder.Build();
host.Run();
