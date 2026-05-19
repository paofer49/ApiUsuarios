using ApiUsuarios.Data;
using ApiUsuarios.Model;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ApiUsuarios.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsuariosController : ControllerBase
    {

        private readonly ConexionBD _db;

        public UsuariosController(ConexionBD db)
        {
            _db = db;
        }

        private int GetIdActual() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private bool EsAdmin() => User.IsInRole("Administrador");

        // GET: api/<UsuariosController>
        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Get()
        {
            using var conexion = _db.ObtenerConexion();

            var usuarios = await conexion.QueryAsync<Usuarios>(
                "sp_ObtenerUsuarios",
                commandType: CommandType.StoredProcedure);

            return Ok(usuarios);
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            using var conexion = _db.ObtenerConexion();

            var usuario = await conexion.QueryFirstOrDefaultAsync<Usuarios>(
                "sp_ObtenerUsuarioId",
                new { Id = GetIdActual() },
                commandType: CommandType.StoredProcedure);

            if (usuario == null)
                return NotFound();

            usuario.Contrasena = null; // nunca devolvemos la contraseña
            return Ok(usuario);
        }

        // GET api/<UsuariosController>/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {

            if (!EsAdmin() && GetIdActual() != id)
                return Forbid();

            using var conexion = _db.ObtenerConexion();

            var usuario = await conexion.QueryFirstOrDefaultAsync<Usuarios>(
                "sp_ObtenerUsuarioId",
                new { Id = id },
                commandType: CommandType.StoredProcedure);

            if (usuario == null)
                return NotFound();

            usuario.Contrasena = null;
            return Ok(usuario);
        }

        // POST api/<UsuariosController>
        //[HttpPost]
        //public async Task<IActionResult> Post([FromBody] Usuarios usuario)
        //{
        //    using var conexion = _db.ObtenerConexion();

        //    await conexion.ExecuteAsync(
        //        "sp_InsertarUsuario",
        //        new
        //        {
        //            usuario.Nombre,
        //            usuario.Apellido,
        //            usuario.Correo,
        //            usuario.Contrasena
        //        },
        //        commandType: CommandType.StoredProcedure);

        //    return Ok("Usuario insertado");
        //}

        // PUT api/<UsuariosController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Usuarios usuario)
        {
            if (!EsAdmin() && GetIdActual() != id)
                return Forbid();

            // Un usuario normal NO puede modificar sus propios puntos
            if (!EsAdmin())
            {
                using var cx = _db.ObtenerConexion();
                var actual = await cx.QueryFirstOrDefaultAsync<Usuarios>(
                    "sp_ObtenerUsuarioId",
                    new { Id = id },
                    commandType: CommandType.StoredProcedure);

                if (actual == null) return NotFound();
                usuario.PuntosActuales = actual.PuntosActuales;
            }

            using var conexion = _db.ObtenerConexion();

            await conexion.ExecuteAsync(
                "sp_ActualizarUsuario",
                new
                {
                    Id = id,
                    usuario.Nombre,
                    usuario.Apellido,
                    usuario.Correo,
                    usuario.Contrasena,
                    usuario.PuntosActuales
                },
                commandType: CommandType.StoredProcedure);

            return Ok(new { mensaje = "Usuario actualizado" });
        }

        [HttpPatch("{id}/puntos")]
        [Authorize(Roles = "Administrador,Usuario")]
        public async Task<IActionResult> PatchPuntos(int id, int puntos)
        {
            using var conexion = _db.ObtenerConexion();

            await conexion.ExecuteAsync(
                "sp_PatchPuntos",
                new
                {
                    Id = id,
                    Puntos = puntos
                },
                commandType: CommandType.StoredProcedure);

            return Ok("Puntos actualizados");
        }

        // DELETE api/<UsuariosController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!EsAdmin() && GetIdActual() != id)
                return Forbid();

            using var conexion = _db.ObtenerConexion();

            await conexion.ExecuteAsync(
                "sp_EliminarUsuario",
                new { Id = id },
                commandType: CommandType.StoredProcedure);

            return Ok(new { mensaje = "Usuario eliminado" });
        }
    }
}
