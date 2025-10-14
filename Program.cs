using SmartEnergy;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOptions<MqttSettings>()
       .Bind(builder.Configuration.GetSection("Mqtt"));
builder.Services.AddOptions<LoopSettings>()
       .Bind(builder.Configuration.GetSection("Loop"));

builder.Services.AddHostedService<Worker>();
builder.Logging.AddConsole();

var app = builder.Build();
app.Run();
