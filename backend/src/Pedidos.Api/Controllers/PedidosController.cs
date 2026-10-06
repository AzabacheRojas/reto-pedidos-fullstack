using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pedidos.Api.Extensions;
using Pedidos.Application.Common.Models;
using Pedidos.Application.Pedidos;

namespace Pedidos.Api.Controllers;

/// <summary>
/// Controlador delgado: solo traduce HTTP ⇄ casos de uso. La lógica vive en Application/Domain
/// y los errores se convierten a ProblemDetails en el manejador global de excepciones.
/// </summary>
[ApiController]
[Route("api/pedidos")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
public sealed class PedidosController(IPedidoService pedidoService) : ControllerBase
{
    /// <summary>Lista pedidos con búsqueda, filtro por estado, orden y paginación.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<PedidoDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PedidoDto>>> GetAll([FromQuery] PedidoQuery query, CancellationToken ct)
        => Ok(await pedidoService.GetAllAsync(query, ct));

    [HttpGet("{id:int}", Name = nameof(GetById))]
    [ProducesResponseType<PedidoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PedidoDto>> GetById(int id, CancellationToken ct)
        => Ok(await pedidoService.GetByIdAsync(id, ct));

    [HttpPost]
    [ProducesResponseType<PedidoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PedidoDto>> Create([FromBody] CrearPedidoRequest request, CancellationToken ct)
    {
        var pedido = await pedidoService.CreateAsync(request, ct);
        return CreatedAtRoute(nameof(GetById), new { id = pedido.Id }, pedido);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<PedidoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PedidoDto>> Update(int id, [FromBody] ActualizarPedidoRequest request, CancellationToken ct)
        => Ok(await pedidoService.UpdateAsync(id, request, ct));

    /// <summary>Eliminación lógica. Requiere rol Admin.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await pedidoService.DeleteAsync(id, ct);
        return NoContent();
    }
}