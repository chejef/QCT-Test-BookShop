
namespace Bookstore.Web.ViewModel.Wishlist
{
    public class WishlistIndexViewModel
    {
        public List<WishlistIndexItemViewModel> WishlistItems { get; set; } = new List<WishlistIndexItemViewModel>();

        public WishlistIndexViewModel(Domain.Carts.ShoppingCart shoppingCart)
        {
            if (shoppingCart == null) return;

            WishlistItems = shoppingCart
                .GetWishListItems()
                .Select(x => new WishlistIndexItemViewModel
                {
                    ShoppingCartItemId = x.Id,
                    BookName = x.Book?.Name ?? string.Empty,
                    ImageUrl = x.Book?.CoverImageUrl ?? string.Empty,
                    Price = x.Book?.Price ?? 0
                }).ToList();
        }
    }

    public class WishlistIndexItemViewModel
    {
        public int ShoppingCartItemId { get; set; }

        public string BookName { get; set; } = null!;

        public string ImageUrl { get; set; } = null!;

        public decimal Price { get; set; }
    }
}
