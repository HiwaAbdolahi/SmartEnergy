namespace SmartEnergy.workerC;

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
                await client.PublishStringAsync(
                    "home/stue/heater/cmd",
                    cmd,
                    MqttQualityOfServiceLevel.AtLeastOnce,
                    retain: true,
                    ct
                );
                _log.LogInformation("TX home/stue/heater/cmd => {Cmd}", cmd);
            }
        };

        client.DisconnectedAsync += e =>
        {
            _log.LogWarning("MQTT disconnected: {Reason}", e.ReasonString);
            return Task.CompletedTask;
        };

        // WebSocket i Azure (wss) hvis WsUrl er satt, ellers lokal TCP
        var options = BuildClientOptions(_cfg);

        while (!ct.IsCancellationRequested)
        {
            if (!client.IsConnected)
            {
                try
                {
                    await client.ConnectAsync(options, ct);
                    _log.LogInformation("MQTT connected ({Mode})",
                        string.IsNullOrWhiteSpace(_cfg.WsUrl) ? $"tcp://{_cfg.Host}:{_cfg.Port}" : _cfg.WsUrl);

                    await client.SubscribeAsync("home/stue/temp", MqttQualityOfServiceLevel.AtLeastOnce, ct);
                    _log.LogInformation("Subscribed to home/stue/temp");

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
                    continue;
                }
            }

            try
            {
                // Send epoch-ms som tekst + retain, som dashboardet kan parse stabilt
                var epochMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);

                await client.PublishStringAsync(
                    "home/demo/heartbeat",
                    epochMs,
                    MqttQualityOfServiceLevel.AtLeastOnce,
                    retain: true,
                    ct
                );

                _log.LogInformation("TX home/demo/heartbeat => {EpochMs}", epochMs);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Publish heartbeat failed (will try reconnect next loop).");
            }



            await Task.Delay(TimeSpan.FromSeconds(_loop.IntervalSeconds), ct);
        }
    }

    private static MqttClientOptions BuildClientOptions(MqttSettings cfg)
    {
        var b = new MqttClientOptionsBuilder()
            .WithClientId(string.IsNullOrWhiteSpace(cfg.ClientId)
                ? "smartenergy-worker-" + Guid.NewGuid().ToString("N")[..6]
                : cfg.ClientId)
            .WithCleanSession()
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(30))
            .WithWillTopic("home/edge/worker/status")
            .WithWillPayload("offline")
            .WithWillQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithWillRetain(true);

        if (!string.IsNullOrWhiteSpace(cfg.User))
            b = b.WithCredentials(cfg.User, cfg.Pass ?? string.Empty);

        var wsUrl = string.IsNullOrWhiteSpace(cfg.WsUrl)
            ? Environment.GetEnvironmentVariable("MQTT_WS_URL")
            : cfg.WsUrl;

        if (!string.IsNullOrWhiteSpace(wsUrl))
        {
            // WSS (Azure)
            b = b.WithWebSocketServer(wsUrl).WithTls();
        }
        else
        {
            // Lokal TCP
            b = b.WithTcpServer(cfg.Host, cfg.Port);
            if (cfg.Port == 8883) b = b.WithTls();
        }

        return b.Build();
    }
}
