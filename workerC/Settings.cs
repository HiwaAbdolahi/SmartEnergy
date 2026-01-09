namespace SmartEnergy.workerC;

public class MqttSettings
{
    public MqttSettings() { }
    public string Host { get; set; } = "";   // brukes lokalt (TCP)
    public int Port { get; set; } = 1883;    // 1883 lokalt, 8883 hvis TLS lokalt
    public string ClientId { get; set; } = "";
    public string User { get; set; } = "";
    public string Pass { get; set; } = "";

    // Nytt: WebSocket URL (Azure). Eks: "wss://smartenergy-mqtt.<hash>.norwayeast.azurecontainerapps.io/"
    // Kan settes i appsettings eller via ENV: MQTT_WS_URL
    public string WsUrl { get; set; } = "";
}

public class LoopSettings
{
    public LoopSettings() { }
    public int IntervalSeconds { get; set; } = 15;
}
