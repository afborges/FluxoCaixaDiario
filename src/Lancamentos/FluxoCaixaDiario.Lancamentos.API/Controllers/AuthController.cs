using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluxoCaixaDiario.Lancamentos.API.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace FluxoCaixaDiario.Lancamentos.API.Controllers;

[ApiController]
[Route("auth")]
[AllowAnonymous]
public sealed class AuthController(UserStore userStore, IConfiguration configuration) : ControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var user = userStore.Validar(request.Username, request.Password);
        if (user is null)
            return Unauthorized(new { erro = "Usuário ou senha inválidos." });

        var token = GerarJwt(user);
        return Ok(token);
    }

    private LoginResponse GerarJwt(UserInfo user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiracao = DateTime.UtcNow.AddHours(
            configuration.GetValue<int>("Jwt:ExpiracaoHoras", 8));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim(JwtRegisteredClaimNames.Name, user.NomeCompleto),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: expiracao,
            signingCredentials: credentials);

        return new LoginResponse(
            Token: new JwtSecurityTokenHandler().WriteToken(token),
            Username: user.Username,
            NomeCompleto: user.NomeCompleto,
            Role: user.Role,
            Expiracao: expiracao);
    }
}
