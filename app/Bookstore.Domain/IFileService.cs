namespace Bookstore.Domain
{
    public interface IFileService
    {
        Task<string> SaveAsync(Stream? contents, string? filename);

        Task DeleteAsync(string? filePath);
    }
}
