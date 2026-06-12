using MediatR;
using Microsoft.AspNetCore.Mvc;
using FluxoCaixaDiario.Lancamentos.Application.Commands.CancelarLancamento;
using FluxoCaixaDiario.Lancamentos.Application.Commands.CriarLancamento;
using FluxoCaixaDiario.Lancamentos.Application.Queries.ObterLancamentoPorId;
using FluxoCaixaDiario.Lancamentos.Application.Queries.ObterLancamentos;

namespace FluxoCaixaDiario.Lancamentos.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class LancamentosController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Criar([FromBody] CriarLancamentoCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        if (result.IsFailure)
            return UnprocessableEntity(new { erro = result.Error });

        return CreatedAtAction(nameof(ObterPorId), new { id = result.Value.Id }, result.Value);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar(
        [FromQuery] DateOnly? dataInicio,
        [FromQuery] DateOnly? dataFim,
        [FromQuery] string? tipo,
        [FromQuery] string? status,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoPagina = 20,
        CancellationToken ct = default)
    {
        var query = new ObterLancamentosQuery(dataInicio, dataFim, tipo, status, pagina, tamanhoPagina);
        var result = await mediator.Send(query, ct);
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorId(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new ObterLancamentoPorIdQuery(id), ct);
        if (result.IsFailure)
            return NotFound(new { erro = result.Error });

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Cancelar(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new CancelarLancamentoCommand(id), ct);
        if (result.IsFailure)
        {
            if (result.Error.Contains("não encontrado"))
                return NotFound(new { erro = result.Error });
            return UnprocessableEntity(new { erro = result.Error });
        }

        return NoContent();
    }
}