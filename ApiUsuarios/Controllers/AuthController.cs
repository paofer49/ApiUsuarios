using ApiUsuarios.Data;
using ApiUsuarios.Model;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ApiUsuarios.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ConexionBD _db;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthController(ConexionBD db, IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _db = db;
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("registrar")]
        public async Task<IActionResult> Registrar([FromBody] Usuarios usuario)
        {
            using var conexion = _db.ObtenerConexion();

            // Usamos el nuevo SP que asigna el rol 'Usuario' automáticamente
            int nuevoId = await conexion.ExecuteScalarAsync<int>(
                "sp_InsertarUsuario",
                new { usuario.Nombre, usuario.Apellido, usuario.Correo, usuario.Contrasena },
                commandType: CommandType.StoredProcedure);

            // Notifica a ApiLogros en segundo plano (no bloquea la respuesta)
            _ = NotificarNuevoUsuario(nuevoId);

            return Ok(new { mensaje = "Usuario registrado exitosamente" });
        }

        private async Task NotificarNuevoUsuario(int usuarioId)
        {
            try
            {
                var cliente = _httpClientFactory.CreateClient("ApiLogros");
                await cliente.PostAsync($"Asignacion/asignar-nuevo-usuario/{usuarioId}", null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Registro] Error al notificar ApiLogros: {ex.Message}");
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest login)
        {
            using var conexion = _db.ObtenerConexion();

            var usuario = await conexion.QueryFirstOrDefaultAsync<UsuarioLogueadoDto>(
                "sp_Login",
                new { login.Correo, login.Contrasena },
                commandType: CommandType.StoredProcedure);

            if (usuario == null)
                return Unauthorized(new { mensaje = "Correo o contraseña incorrectos" });

            // Generar Token JWT
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtKey = _config["Jwt:Key"] ?? "TuClaveSecretaSuperSeguraDeAlMenos32Caracteres!";
            var key = Encoding.ASCII.GetBytes(jwtKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                    new Claim(ClaimTypes.Email, usuario.Correo),
                    new Claim(ClaimTypes.Role, usuario.Rol) // Aquí guardamos el Rol (Administrador o Usuario)
                }),
                Expires = DateTime.UtcNow.AddHours(2), // Duración del token
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new
            {
                Token = tokenString,
                Usuario = usuario
            });
        }
    }
}
