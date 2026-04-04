public sealed class UploadCsvRequest
{
    public Guid BusinessId { get; set; } = Guid.Empty;
    public IFormFile File { get; set; } = default!;
}