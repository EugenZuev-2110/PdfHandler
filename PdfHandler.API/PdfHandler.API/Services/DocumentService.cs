using Microsoft.EntityFrameworkCore;
using PdfHandler.API.Data;
using PdfHandler.Common;

namespace PdfHandler.API.Services;

public class DocumentService
{
    private readonly AppDbContext _context;
    private readonly QueueService _queueService;

    public DocumentService(AppDbContext context, QueueService queueService)
    {
        _context = context;
        _queueService = queueService;
    }

    public async Task<Document> RegisterDocumentAsync(string originalFileName, string storedFilePath)
    {
        var document = new Document
        {
            Id = Guid.NewGuid(),
            FileName = originalFileName,
            FilePath = storedFilePath,
            Status = ProcessingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();
        await _queueService.PublishPdfTaskAsync(document.Id, document.FilePath);

        return document;
    }

    public async Task<List<Document>> GetAllDocumentsAsync()
    {
        return await _context.Documents
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<Document?> GetDocumentByIdAsync(Guid id)
    {
        return await _context.Documents.FindAsync(id);
    }
}