using Bookstore.Domain.Carts;

namespace Bookstore.Web.ViewModel.ShoppingCart
{
    public class ShoppingCartIndexViewModel
    {
        public decimal TotalPrice => ShoppingCartItems.Sum(x => x.Price);

        public List<ShoppingCartIndexItemViewModel> ShoppingCartItems { get; set; } = new List<ShoppingCartIndexItemViewModel>();

        public ShoppingCartIndexViewModel(Domain.Carts.ShoppingCart shoppingCart)
        {
            if (shoppingCart == null) return;

            ShoppingCartItems = shoppingCart
                .GetShoppingCartItems(ShoppingCartItemFilter.IncludeOutOfStockItems)
                .Select(c => new ShoppingCartIndexItemViewModel
                    {
                        BookId = c.Book?.Id ?? 0,
                        ImageUrl = c.Book?.CoverImageUrl ?? string.Empty,
                        Price = c.Book?.Price ?? 0,
                        BookName = c.Book?.Name ?? string.Empty,
                        ShoppingCartItemId = c.Id,
                        StockLevel = c.Book?.Quantity ?? 0
                    }).ToList();
        }
    }

    public class ShoppingCartIndexItemViewModel
    {
        public int ShoppingCartItemId { get; set; }

        public long BookId { get; set; }

        public string BookName { get; set; } = null!;

        public decimal Price { get; set; }

        public string ImageUrl { get; set; } = null!;

        public int StockLevel { get; set; }

        public bool HasLowStockLevels { get; set; }

        public bool IsOutOfStock { get; set; }
    }
}