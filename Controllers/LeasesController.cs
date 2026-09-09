using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentalApp.Application.DTOs.Leases;
using RentalApp.Application.Interfaces;

namespace RentalApp.Controllers;

[ApiController]
[ApiAuthorize]
[Route("api/[controller]")]
public class LeasesController(ILeaseService leaseService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LeaseDto>>> GetAll(CancellationToken cancellationToken)
    {
        var items = await leaseService.GetAllAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LeaseDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await leaseService.GetByIdAsync(id, cancellationToken);
        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<LeaseDto>> Create([FromBody] CreateLeaseRequestDto request, CancellationToken cancellationToken)
    {
        var created = await leaseService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<LeaseDto>> Update(int id, [FromBody] UpdateLeaseRequestDto request, CancellationToken cancellationToken)
    {
        var updated = await leaseService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpPost("{id:int}/move-out")]
    public async Task<ActionResult<LeaseDto>> MoveOut(int id, [FromBody] MoveOutRequestDto request, CancellationToken cancellationToken)
    {
        var updated = await leaseService.MoveOutAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpPost("transfer")]
    public async Task<ActionResult<LeaseDto>> Transfer([FromBody] TransferLeaseRequestDto request, CancellationToken cancellationToken)
    {
        var updated = await leaseService.TransferAsync(request, cancellationToken);
        return Ok(updated);
    }

    [HttpGet("unit-info/{unitId:int}")]
    public async Task<ActionResult<UnitLeaseInfoDto>> GetUnitInfo(int unitId, CancellationToken cancellationToken)
    {
        var info = await leaseService.GetUnitLeaseInfoAsync(unitId, cancellationToken);
        return Ok(info);
    }
}

