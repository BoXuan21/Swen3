using Microsoft.EntityFrameworkCore;
using Swen3.API.DAL.Interfaces;
using Swen3.API.DAL.Repositories;
using System;

namespace Swen3.API.DAL
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddSwenDal(this IServiceCollection services, IConfiguration configuration)
        {
            var conn = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Missing DefaultConnection");

            services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(conn));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            return services;
        }
    }
}
