using Asp.Versioning;
using DevolveFacill.Core.Ports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevolveFacill.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/uploads")]
[Authorize]
public class UploadsController(IStorageService storage) : ControllerBase
{
    private static readonly string[] AllowedTypes = ["image/jpeg", "image/png", "image/webp"];
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    [HttpPost("damage-images")]
    [Authorize(Roles = "QualityInspector,Supervisor")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UploadDamageImages(
        [FromForm] IFormFileCollection files, CancellationToken ct)
    {
        if (files.Count == 0) return BadRequest(new { error = "No files provided." });
        if (files.Count > 5) return BadRequest(new { error = "Maximum 5 files allowed." });

        var keys = new List<string>();

        foreach (var file in files)
        {
            if (!AllowedTypes.Contains(file.ContentType))
                return BadRequest(new { error = $"File type {file.ContentType} not allowed." });

            if (file.Length > MaxFileSizeBytes)
                return BadRequest(new { error = $"File {file.FileName} exceeds 10 MB limit." });

            var key = $"damage-proofs/uploads/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            using var stream = file.OpenReadStream();
            await storage.UploadFileAsync(key, stream, file.ContentType, ct);
            keys.Add(key);
        }

        return Ok(new { imageKeys = keys });
    }
}
