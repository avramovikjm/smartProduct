using Microsoft.AspNetCore.Mvc;
using SmartProduct.Services;

namespace SmartProduct.Controllers;

[ApiController]
[Route("api/recommend")]
public class RecommendController : ControllerBase
{
    private readonly IRecommendationService _service;
    public RecommendController(IRecommendationService service) => _service = service;

    public record QueryRequest(string Query, int Count = 5);

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] QueryRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Query)) return BadRequest("Query required");
        var recs = await _service.RecommendAsync(request.Query, request.Count, ct);
        return Ok(recs);
    }
}
