namespace Ecommerce.Model;

public class CarouselProduct
{
    public int Id { get; init; }
    public int ProductId { get; init; }
    public required Product Product { get; init; }
    public bool IsFlashSalesProduct { get; init; }
    public bool IsBestsellersProduct { get; init; }

    public CarouselProduct() { }

    public CarouselProduct(int productId, Product product, bool isFlashSalesProduct, bool isBestsellersProduct)
    {
        if (!isFlashSalesProduct && !isBestsellersProduct)
        {
            throw new ArgumentException("At least one of IsFlashSalesProduct or IsBestsellersProduct must be true.");
        }

        ProductId = productId;
        Product = product;
        IsFlashSalesProduct = isFlashSalesProduct;
        IsBestsellersProduct = isBestsellersProduct;
    }
}