namespace Asn.Diplomski.Application.UseCases.RequestReprovision
{
    public class RequestReprovisionResult
    {
        public bool IsNotFound { get; set; }

        public static RequestReprovisionResult NotFound() => new() { IsNotFound = true };
        public static RequestReprovisionResult Success() => new();
    }
}
