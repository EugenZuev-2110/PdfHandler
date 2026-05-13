using Microsoft.AspNetCore.Mvc;
using PdfHandler.API.Data;
using Microsoft.EntityFrameworkCore;
using PdfHandler.Common;

namespace PdfHandler.API.Controllers;

public class DocumentController : ControllerBase
{
    private readonly AppDbContext _context;
    public DocumentController(AppDbContext appDbContext)
    {
        _context = appDbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetList()
    {
        var docs = await _context.Documents
            .Select(d => new { d.Id, d.FileName, d.Status, d.CreatedAt })
            .ToListAsync();

        return Ok(docs);
    }

    [HttpGet("{id}/text")]
    public async Task<IActionResult> GetText(Guid id)
    {
        var doc = await _context.Documents.FindAsync(id);

        if (doc == null) return NotFound("Документ не найден");

        if (doc.Status != ProcessingStatus.Completed)
            return BadRequest($"Статус обработки: {doc.Status}. Текст еще не готов.");

        return Ok(new { doc.Id, doc.Content });
    }
}