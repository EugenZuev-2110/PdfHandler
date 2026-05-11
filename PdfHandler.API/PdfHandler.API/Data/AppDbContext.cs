using Microsoft.EntityFrameworkCore;
using PdfHandler.Common;

namespace PdfHandler.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Document> Documents => Set<Document>();
}