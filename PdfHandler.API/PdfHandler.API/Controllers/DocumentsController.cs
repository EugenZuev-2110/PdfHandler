using Microsoft.AspNetCore.Mvc;
using PdfHandler.API.Services;
using PdfHandler.Common;

namespace PdfHandler.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly DocumentService _documentService;
    private readonly string _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "Storage");

    public DocumentsController(DocumentService documentService)
    {
        _documentService = documentService;
        if (!Directory.Exists(_storagePath)) Directory.CreateDirectory(_storagePath);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Файл не выбран");

        var fileId = Guid.NewGuid();
        var filePath = Path.Combine(_storagePath, $"{fileId}.pdf");

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var document = await _documentService.RegisterDocumentAsync(file.FileName, filePath);

        return Ok(new { document.Id, document.Status });
    }

    [HttpGet]
    public async Task<IActionResult> GetList()
    {
        var docs = await _documentService.GetAllDocumentsAsync();
        var result = docs.Select(d => new { d.Id, d.FileName, d.Status, d.CreatedAt });
        return Ok(result);
    }

    [HttpGet("{id}/text")]
    public async Task<IActionResult> GetText(Guid id)
    {
        var doc = await _documentService.GetDocumentByIdAsync(id);

        if (doc == null)
            return NotFound("Документ не найден");

        if (doc.Status != ProcessingStatus.Completed)
            return BadRequest($"Статус обработки: {doc.Status}. Текст еще не готов.");

        return Ok(new { doc.Id, doc.Content });
    }
}