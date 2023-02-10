namespace TestClassLibrary.SupportModels;
public sealed record Brand : BrandDetail
{
    public string Name { get; init; }

    public Brand(string name, BrandDetail detail)
    {
        Name = name;
               
        LongName = detail.LongName;
        Products = detail.Products;
        WebBaseURL = detail.WebBaseURL;
    }
}