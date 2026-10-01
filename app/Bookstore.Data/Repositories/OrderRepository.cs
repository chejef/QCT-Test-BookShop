using Bookstore.Domain;
using Bookstore.Domain.Books;
using Bookstore.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Data.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly ApplicationDbContext dbContext;

        public OrderRepository(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        async Task IOrderRepository.AddAsync(Order order)
        {
            await Task.Run(() => dbContext.Order.Add(order));
        }

        async Task<Order> IOrderRepository.GetAsync(int id)
        {
            return (await dbContext.Order
                .Include(x => x.Customer)
                .Include(x => x.Address)
                .Include(x => x.OrderItems)
                    .ThenInclude(y => y.Book)
                        .ThenInclude(b => b.BookType)
                .Include(x => x.OrderItems)
                    .ThenInclude(y => y.Book)
                        .ThenInclude(b => b.Condition)
                .Include(x => x.OrderItems)
                    .ThenInclude(y => y.Book)
                        .ThenInclude(b => b.Genre)
                .Include(x => x.OrderItems)
                    .ThenInclude(y => y.Book)
                        .ThenInclude(b => b.Publisher)
                .SingleOrDefaultAsync(x => x.Id == id))!;
        }

        async Task<Order> IOrderRepository.GetAsync(int id, string sub)
        {
            return (await dbContext.Order.SingleOrDefaultAsync(x => x.Id == id && x.Customer!.Sub == sub))!;
        }

        async Task<IEnumerable<Book>> IOrderRepository.ListBestSellingBooksAsync(int count)
        {
            var bestSellingBookIds = await dbContext.OrderItem
                .GroupBy(x => x.BookId)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .Take(count)
                .ToListAsync();

            var books = await dbContext.Book
                .Where(b => bestSellingBookIds.Contains(b.Id))
                .ToListAsync();

            return books.OrderBy(b => bestSellingBookIds.IndexOf(b.Id)).ToList();
        }

        async Task<OrderStatistics> IOrderRepository.GetStatisticsAsync()
        {
            var startOfMonth = DateTimeOffset.UtcNow.StartOfMonth();

            return new OrderStatistics
            {
                PendingOrders = await dbContext.Order.CountAsync(y => y.OrderStatus == OrderStatus.Pending),
                PastDueOrders = await dbContext.Order.CountAsync(y => y.OrderStatus == OrderStatus.Ordered && y.DeliveryDate < DateTimeOffset.UtcNow),
                OrdersThisMonth = await dbContext.Order.CountAsync(y => y.CreatedOn >= startOfMonth),
                OrdersTotal = await dbContext.Order.CountAsync()
            };
        }

        async Task<IPaginatedList<Order>> IOrderRepository.ListAsync(OrderFilters filters, int pageIndex, int pageSize)
        {
            var query = dbContext.Order.AsQueryable();

            if (filters.OrderStatusFilter.HasValue)
            {
                query = query.Where(x => x.OrderStatus == filters.OrderStatusFilter);
            }

            if (filters.OrderDateFromFilter.HasValue)
            {
                query = query.Where(x => x.CreatedOn >= filters.OrderDateFromFilter);
            }

            if (filters.OrderDateToFilter.HasValue)
            {
                var filterData = filters.OrderDateToFilter.Value.OneSecondToMidnight();
                query = query.Where(x => x.CreatedOn < filterData );
            }

            query = query
                .Include(x => x.Customer)
                .Include(x => x.OrderItems)
                    .ThenInclude(y => y.Book);

            var result = new PaginatedList<Order>(query, pageIndex, pageSize);

            await result.PopulateAsync();

            return result;
        }

        async Task<IEnumerable<Order>> IOrderRepository.ListAsync(string sub)
        {
            return await dbContext.Order
                .Include(x => x.OrderItems)
                    .ThenInclude(y => y.Book)
                .Where(x => x.Customer!.Sub == sub)
                .ToListAsync();
        }

        async Task IOrderRepository.SaveChangesAsync()
        {
            await dbContext.SaveChangesAsync();
        }
    }
}