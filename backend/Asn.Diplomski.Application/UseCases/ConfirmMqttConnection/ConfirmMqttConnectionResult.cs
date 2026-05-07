namespace Asn.Diplomski.Application.UseCases.ConfirmMqttConnection
{
    public class ConfirmMqttConnectionResult
    {
        public bool IsNotFound { get; set; }
        public bool IsInvalidState { get; set; }

        public static ConfirmMqttConnectionResult Success() => new();
        public static ConfirmMqttConnectionResult NotFound() => new() { IsNotFound = true };
        public static ConfirmMqttConnectionResult InvalidState() => new() { IsInvalidState = true };
    }
}
