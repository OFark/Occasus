namespace TestClassLibrary.SupportModels;
public sealed record Catalogue
{
    public BrandCollection? Brands { get; init; }
}