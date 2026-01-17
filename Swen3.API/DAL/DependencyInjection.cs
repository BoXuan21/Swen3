using Microsoft.EntityFrameworkCore;
using Swen3.API.DAL.Interfaces;
using Swen3.API.DAL.Repositories;

namespace Swen3.API.DAL
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddSwenDal(this IServiceCollection services, IConfiguration configuration)
        {
            var conn = configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("Missing DefaultConnection string.");

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(conn));

            services.AddScoped<IDocumentRepository, DocumentRepository>();
            services.AddScoped<IPriorityRepository, PriorityRepository>();

            return services;

        }
    }
}
