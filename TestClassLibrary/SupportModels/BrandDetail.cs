namespace TestClassLibrary.SupportModels;
public record BrandDetail

{
    public string? LongName { get; init; }
    public string? PartName { get; init; }
    public string? CompanyID { get; init; }
    public string? WebBaseURL { get; init; }


    public ProductCollection? Products { get; init; }
}