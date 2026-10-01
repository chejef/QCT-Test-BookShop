// PORT-TODO
// {"was": "System.Data.Entity.DropCreateDatabaseIfModelChanges", "note": "TR-13: Delete this file entirely. BookstoreDbInitializer inherits EF6 DropCreateDatabaseIfModelChanges which has no EF Core equivalent. Database.SetInitializer call was already removed from ApplicationDbContext. If seed data is still needed, migrate it to an IHostedService, a migration data seed, or DbContext.Database.EnsureCreated + HasData in OnModelCreating."}
namespace Bookstore.Data
{
    // This class is intentionally emptied per TR-13.
    // Delete this file once seed data has been migrated (if needed).
}