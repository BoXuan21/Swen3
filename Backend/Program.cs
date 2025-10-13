
using Swen3.API.DAL;
using Npgsql;

namespace Backend
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddSwenDal(builder.Configuration);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Execute init.sql to initialize database
            await InitializeDatabase(builder.Configuration);

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }

        private static async Task InitializeDatabase(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }

            var initSqlPath = Path.Combine(AppContext.BaseDirectory, "Database", "init.sql");
            
            if (!File.Exists(initSqlPath))
            {
                Console.WriteLine($"Warning: init.sql not found at {initSqlPath}. Skipping database initialization.");
                return;
            }

            var initSql = await File.ReadAllTextAsync(initSqlPath);

            try
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();
                await using var command = new NpgsqlCommand(initSql, connection);
                await command.ExecuteNonQueryAsync();
                Console.WriteLine("Database initialized successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing database: {ex.Message}");
                throw;
            }
        }
    }
}
