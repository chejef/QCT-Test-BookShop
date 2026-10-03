using Amazon.Rekognition;
using Amazon.S3;
using BobsBookstoreClassic.Data;
using Bookstore.Data;
using Bookstore.Data.FileServices;
using Bookstore.Data.ImageResizeService;
using Bookstore.Data.ImageValidationServices;
using Bookstore.Data.Repositories;
using Bookstore.Domain;
using Bookstore.Domain.Addresses;
using Bookstore.Domain.Books;
using Bookstore.Domain.Carts;
using Bookstore.Domain.Customers;
using Bookstore.Domain.Offers;
using Bookstore.Domain.Orders;
using Bookstore.Domain.ReferenceData;
using Bookstore.Web.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;

namespace Bookstore.Web
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddBookstoreServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Application services (scoped — one per request, matching Autofac InstancePerRequest)
            services.AddScoped<IBookService, BookService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IReferenceDataService, ReferenceDataService>();
            services.AddScoped<IOfferService, OfferService>();
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<IAddressService, AddressService>();
            services.AddScoped<IShoppingCartService, ShoppingCartService>();
            services.AddScoped<IImageResizeService, ImageResizeService>();

            // DbContext (scoped — one per request)
            var connectionString = BookstoreConfiguration.GetConnectionString("BookstoreDatabaseConnection");
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            // Repositories (scoped)
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<IAddressRepository, AddressRepository>();
            services.AddScoped<IBookRepository, BookRepository>();
            services.AddScoped<IOfferRepository, OfferRepository>();
            services.AddScoped<IShoppingCartRepository, ShoppingCartRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IReferenceDataRepository, ReferenceDataRepository>();

            // Open generic registration (scoped — matching Autofac InstancePerLifetimeScope)
            services.AddScoped(typeof(IPaginatedList<>), typeof(PaginatedList<>));

            // Conditional file service registration
            if (BookstoreConfiguration.GetSetting("Services/FileService") == "aws")
            {
                services.AddSingleton<IAmazonS3, AmazonS3Client>();
                services.AddScoped<IFileService, S3FileService>();
            }
            else
            {
                services.AddScoped<IFileService>(sp =>
                {
                    var env = sp.GetRequiredService<IWebHostEnvironment>();
                    return new LocalFileService(env.WebRootPath);
                });
            }

            // Conditional image validation service registration
            if (BookstoreConfiguration.GetSetting("Services/ImageValidationService") == "aws")
            {
                services.AddSingleton<IAmazonRekognition, AmazonRekognitionClient>();
                services.AddScoped<IImageValidationService, RekognitionImageValidationService>();
            }
            else
            {
                services.AddScoped<IImageValidationService, LocalImageValidationService>();
            }

            return services;
        }
    }
}
