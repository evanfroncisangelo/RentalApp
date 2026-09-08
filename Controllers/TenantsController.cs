using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalApp.Application.DTOs.Tenants;
using RentalApp.Application.Interfaces;

namespace RentalApp.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TenantsController(ITenantService tenantService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TenantDto>>> GetAll([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var items = await tenantService.GetAllAsync(search, cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TenantDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await tenantService.GetByIdAsync(id, cancellationToken);
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<TenantDto>> Create([FromBody] CreateTenantRequestDto request, CancellationToken cancellationToken)
    {
        var created = await tenantService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TenantDto>> Update(int id, [FromBody] UpdateTenantRequestDto request, CancellationToken cancellationToken)
    {
        var updated = await tenantService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await tenantService.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }
}
