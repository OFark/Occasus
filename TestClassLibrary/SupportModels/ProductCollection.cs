namespace TestClassLibrary.SupportModels;
public sealed class ProductCollection : Dictionary<string, ProductDetail>, IDictionary<string, ProductDetail>
{
    public ProductCollection() : base(StringComparer.OrdinalIgnoreCase)
    {
    }

    public Product GetProductFromProductname(string productName)
    {
        if (!ContainsKey(productName)) throw new Exception($"Product {productName} not found in the catalogue");

        return new(productName, this[productName]);
    }

    public bool HasProduct(string productName) => ContainsKey(productName);
}