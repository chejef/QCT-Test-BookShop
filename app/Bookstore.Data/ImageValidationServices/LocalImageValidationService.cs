using Bookstore.Domain;

namespace Bookstore.Data.ImageValidationServices
{
    public class LocalImageValidationService : IImageValidationService
    {
        public async Task<bool> IsSafeAsync(Stream? image)
        {
            return await Task.Run(() => true);
        }
    }
}