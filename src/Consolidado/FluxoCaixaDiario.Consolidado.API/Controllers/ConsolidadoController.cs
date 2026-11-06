using MediatR;
using Microsoft.AspNetCore.Mvc;
using FluxoCaixaDiario.Consolidado.Application.Queries.ObterConsolidadoDiario;
using FluxoCaixaDiario.Consolidado.Application.Queries.ObterHistorico;

namespace FluxoCaixaDiario.Consolidado.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class ConsolidadoController(IMediator mediator) : ControllerBase
{
    [HttpGet("{data}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorData(
        [FromRoute] DateOnly data,
        CancellationToken ct)
    {
        var result = await mediator.Send(new ObterConsolidadoDiarioQuery(data), ct);
        if (result.IsFailure)
            return NotFound(new { erro = result.Error });

        Response.Headers.CacheControl = "public, max-age=300";
        return Ok(result.Value);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ObterHistorico(
        [FromQuery] DateOnly dataInicio,
        [FromQuery] DateOnly dataFim,
        CancellationToken ct)
    {
        if (dataFim < dataInicio)
            return BadRequest(new { erro = "A data fim deve ser maior ou igual à data início." });

        var result = await mediator.Send(new ObterHistoricoConsolidadoQuery(dataInicio, dataFim), ct);
        return Ok(result.Value);
    }
}