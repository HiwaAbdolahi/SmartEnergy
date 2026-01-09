using SmartEnergy.workerC;

var builder = Host.CreateApplicationBuilder(args);

// Hent også ENV-variabler (for MQTT_WS_URL m.m.)
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddOptions<MqttSettings>()
       .Bind(builder.Configuration.GetSection("Mqtt"));
builder.Services.AddOptions<LoopSettings>()
       .Bind(builder.Configuration.GetSection("Loop"));

builder.Services.AddHostedService<Worker>();
builder.Logging.AddConsole();

var app = builder.Build();
app.Run();
