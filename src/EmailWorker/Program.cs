using EmailWorker.Services;
using EmailWorker.Workers;
using Shared.Configuration;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .Configure<RabbitMqSettings>(builder.Configuration.GetSection(RabbitMqSettings.SectionName));

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddHostedService<EmailConsumerWorker>();

var host = builder.Build();
host.Run();
