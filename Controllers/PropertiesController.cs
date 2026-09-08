using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalApp.Application.DTOs.Properties;
using RentalApp.Application.Interfaces;

namespace RentalApp.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class PropertiesController(IPropertyService propertyService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PropertyDto>>> GetAll([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var items = await propertyService.GetAllAsync(search, cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PropertyDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await propertyService.GetByIdAsync(id, cancellationToken);
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<PropertyDto>> Create([FromBody] CreatePropertyRequestDto request, CancellationToken cancellationToken)
    {
        var created = await propertyService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PropertyDto>> Update(int id, [FromBody] UpdatePropertyRequestDto request, CancellationToken cancellationToken)
    {
        var updated = await propertyService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await propertyService.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }
}
