using System.Collections;

namespace TestClassLibrary.SupportModels;
public sealed class BrandCollection : Dictionary<string, BrandDetail>, IDictionary<string, BrandDetail>, IEnumerable
{
    public BrandCollection() : base(StringComparer.OrdinalIgnoreCase)
    {
    }

    public Brand GetBrand(string brandName)
    {
        if (!ContainsKey(brandName))
        {
            throw new KeyNotFoundException($"Brand {brandName} not found");
        }

        return new Brand(brandName, this[brandName]);
    }

    public Brand GetBrandFromProductName(string productName)
    {
        var brandKeyPairs = this.Where(x => x.Value.Products is not null && x.Value.Products.ContainsKey(productName));
        if (brandKeyPairs is null || !brandKeyPairs.Any()) throw new KeyNotFoundException($"Product {productName} not found in the catalogue");

        var brandKeyPair = brandKeyPairs.Single();

        return GetBrand(brandKeyPair.Key);
    }
}