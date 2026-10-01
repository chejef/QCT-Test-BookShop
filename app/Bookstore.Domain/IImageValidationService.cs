namespace Bookstore.Domain
{
    public interface IImageValidationService
    {
        Task<bool> IsSafeAsync(Stream? image);
    }
}