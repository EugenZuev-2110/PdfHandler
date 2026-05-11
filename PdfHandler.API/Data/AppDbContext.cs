using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata;

namespace PdfHandler.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Document> Documents => Set<Document>();
}