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

        // --- RX: reagér på temp og publiser kommando -------------------------
        client.ApplicationMessageReceivedAsync += async e =>
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = e.ApplicationMessage.ConvertPayloadToString();
            _log.LogInformation("RX {Topic} => {Payload}", topic, payload);

            if (topic == "home/stue/temp" &&
                double.TryParse(payload, NumberStyles.Any, CultureInfo.InvariantCulture, out var temp))
            {
                var cmd = temp < 21.0 ? "ON" : "OFF";
                await client.PublishStringAsync(
                    "home/stue/heater/cmd",
                    cmd,
                    MqttQualityOfServiceLevel.AtLeastOnce,
                    retain: false,
                    ct
                );
                _log.LogInformation("TX home/stue/heater/cmd => {Cmd}", cmd);
            }
        };

        // --- LWT: status = offline om klienten dør uventet --------------------
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(_cfg.Host, _cfg.Port)
            .WithClientId(_cfg.ClientId)
            .WithWillTopic("home/edge/worker/status")
            .WithWillPayload("offline")
            .WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithWillRetain(true) 
            .Build(); ;

        // Hovedløkke: sikrer reconnect + heartbeat på fast intervall
        while (!ct.IsCancellationRequested)
        {
            if (!client.IsConnected)
            {
                try
                {
                    await client.ConnectAsync(options, ct);
                    _log.LogInformation("MQTT connected to {Host}:{Port}", _cfg.Host, _cfg.Port);

                    await client.SubscribeAsync("home/stue/temp", MqttQualityOfServiceLevel.AtLeastOnce, ct);
                    _log.LogInformation("Subscribed to home/stue/temp");

                    // Marker online (retained), så dashboard ser grønn prikk umiddelbart
                    await client.PublishStringAsync(
                        "home/edge/worker/status",
                        "online",
                        MqttQualityOfServiceLevel.AtLeastOnce,
                        retain: true,
                        ct
                    );
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Connect/subscribe failed, retrying in 2s...");
                    await Task.Delay(2000, ct);
                    continue; // prøv igjen i neste iterasjon
                }
            }

            // Publiser heartbeat med ISO-UTC (dashboard måler latency korrekt)
            try
            {
                var beat = DateTimeOffset.UtcNow.ToString("O");
                await client.PublishStringAsync(
                    "home/demo/heartbeat",
                    beat,
                    MqttQualityOfServiceLevel.AtLeastOnce,
                    retain: false,
                    ct
                );
                _log.LogInformation("TX home/demo/heartbeat => {Beat}", beat);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Publish heartbeat failed (vil forsøke reconnect neste runde).");
            }

            await Task.Delay(TimeSpan.FromSeconds(_loop.IntervalSeconds), ct);
        }
    }
}
