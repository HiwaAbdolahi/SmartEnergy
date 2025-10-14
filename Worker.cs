namespace SmartEnergy;

using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using System.Globalization;

public class Worker(ILogger<Worker> log, IOptions<MqttSettings> mqtt, IOptions<LoopSettings> loop)
    : BackgroundService
{
    private readonly ILogger<Worker> _log = log;
    private readonly MqttSettings _cfg = mqtt.Value;
    private readonly LoopSettings _loop = loop.Value;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var factory = new MqttFactory();
        var client = factory.CreateMqttClient();

        client.ApplicationMessageReceivedAsync += async e =>
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = e.ApplicationMessage.ConvertPayloadToString();
            _log.LogInformation("RX {Topic} => {Payload}", topic, payload);

            if (topic == "home/stue/temp" &&
                double.TryParse(payload, NumberStyles.Any, CultureInfo.InvariantCulture, out var temp))
            {
                var cmd = temp < 21.0 ? "ON" : "OFF";
                await client.PublishStringAsync("home/stue/heater/cmd", cmd, MqttQualityOfServiceLevel.AtLeastOnce, false, ct);
                _log.LogInformation("TX home/stue/heater/cmd => {Cmd}", cmd);
            }
        };



        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(_cfg.Host, _cfg.Port)
            .WithClientId(_cfg.ClientId)
            .Build();

        // enkel retry på connect + subscribe
        while (!ct.IsCancellationRequested && !client.IsConnected)
        {
            try
            {
                await client.ConnectAsync(options, ct);
                _log.LogInformation("MQTT connected to {Host}:{Port}", _cfg.Host, _cfg.Port);
                await client.SubscribeAsync("home/stue/temp", MqttQualityOfServiceLevel.AtLeastOnce, ct);
                _log.LogInformation("Subscribed to home/stue/temp");
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Connect/subscribe failed, retrying in 2s...");
                await Task.Delay(2000, ct);
            }
        }

        while (!ct.IsCancellationRequested)
        {
            var beat = DateTimeOffset.UtcNow.ToString("O");
            await client.PublishStringAsync("home/demo/heartbeat", beat, MqttQualityOfServiceLevel.AtLeastOnce, false, ct);
            _log.LogInformation("TX home/demo/heartbeat => {Beat}", beat);
            await Task.Delay(TimeSpan.FromSeconds(_loop.IntervalSeconds), ct);
        }
    }
}
