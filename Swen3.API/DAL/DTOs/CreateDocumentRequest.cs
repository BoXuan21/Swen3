using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Swen3.API.DAL.DTOs;

public class CreateDocumentRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Upload)]
    public IFormFile? File { get; set; }
}

public class UpdateDocumentRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;

    public int? PriorityId { get; set; }
}

