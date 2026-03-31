using Consolidado.Worker.Data;
using Consolidado.Worker.Consumers;
using Microsoft.EntityFrameworkCore;
using MassTransit;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<ConsolidadoDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<LancamentoCriadoConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rmqHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        var rmqVHost = builder.Configuration["RabbitMQ:VirtualHost"] ?? "/";
        var rmqUser = builder.Configuration["RabbitMQ:Username"] ?? "guest";
        var rmqPass = builder.Configuration["RabbitMQ:Password"] ?? "guest";

        var rmqPort = builder.Configuration.GetValue<ushort>("RabbitMQ:Port", 5672);

        cfg.Host(rmqHost, rmqPort, rmqVHost, h => {
            h.Username(rmqUser);
            h.Password(rmqPass);
        });

        cfg.ReceiveEndpoint("fluxo-caixa-consolidado", e =>
        {
            e.UseMessageRetry(r => r.Exponential(3, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(2)));

            e.ConfigureConsumer<LancamentoCriadoConsumer>(context);
        });
    });
});

var host = builder.Build();
host.Run();
