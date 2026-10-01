using System.Threading.Tasks;

namespace Bookstore.Domain.Books
{
    public  interface IBookRepository
    {
        Task<Book> GetAsync(int id);

        Task<IPagedResult<Book>> ListAsync(BookFilters filters, int pageIndex, int pageSize);

        Task<IPagedResult<Book>> ListAsync(string searchString, string sortBy, int pageIndex, int pageSize);

        Task AddAsync(Book book);

        Task UpdateAsync(Book book);

        Task SaveChangesAsync();

        Task<BookStatistics?> GetStatisticsAsync();
    }
}
