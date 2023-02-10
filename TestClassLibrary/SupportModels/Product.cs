namespace TestClassLibrary.SupportModels;
public sealed record Product : ProductDetail
{
    public string Name { get; init; }

    public Product(string name, ProductDetail detail)
    {
        Name = name;

        ProductName = detail.ProductName;
        Description = detail.Description;
    }
}