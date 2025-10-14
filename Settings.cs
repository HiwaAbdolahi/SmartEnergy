namespace SmartEnergy;

public class MqttSettings
{
    public MqttSettings() { }          // eksplisitt parameterløs ctor
    public string Host { get; set; } = "";
    public int Port { get; set; } = 1883;
    public string ClientId { get; set; } = "";
    public string User { get; set; } = "";
    public string Pass { get; set; } = "";
}

public class LoopSettings
{
    public LoopSettings() { }          //  eksplisitt parameterløs ctor
    public int IntervalSeconds { get; set; } = 15;
}
