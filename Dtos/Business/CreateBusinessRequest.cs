namespace FinancialIntelligence.Api.Dtos.Businesses;

public sealed class CreateBusinessRequest
{
    public string BusinessName { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? Industry { get; set; }
    public string? Website { get; set; }
    public string? Phone { get; set; }
    public string? TaxId { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
}