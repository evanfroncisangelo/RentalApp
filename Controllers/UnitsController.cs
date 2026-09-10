using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalApp.Application.DTOs.Units;
using RentalApp.Application.Interfaces;

namespace RentalApp.Controllers;

[ApiController]
[ApiAuthorize]
[Route("api/[controller]")]
public class UnitsController(IUnitService unitService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UnitDto>>> GetAll([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var items = await unitService.GetAllAsync(search, cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UnitDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await unitService.GetByIdAsync(id, cancellationToken);
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<UnitDto>> Create([FromBody] CreateUnitRequestDto request, CancellationToken cancellationToken)
    {
        var created = await unitService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UnitDto>> Update(int id, [FromBody] UpdateUnitRequestDto request, CancellationToken cancellationToken)
    {
        var updated = await unitService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await unitService.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}/hard")]
    public async Task<IActionResult> HardDelete(int id, CancellationToken cancellationToken)
    {
        await unitService.HardDeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

