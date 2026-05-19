using ApiUsuarios.Data;
using ApiUsuarios.Model;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

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

        // GET api/<UsuariosController>/5
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            using var conexion = _db.ObtenerConexion();

            var usuario = await conexion.QueryFirstOrDefaultAsync<Usuarios>(
                "sp_ObtenerUsuarioId",
                new { Id = id },
                commandType: CommandType.StoredProcedure);

            if (usuario == null)
                return NotFound();

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

            return Ok("Usuario actualizado");
        }

        [HttpPatch("{id}/puntos")]
        [Authorize(Roles = "Administrador, Usuario")]
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
        [Authorize(Roles = "Administrador, Usuario")]
        public async Task<IActionResult> Delete(int id)
        {
            using var conexion = _db.ObtenerConexion();

            await conexion.ExecuteAsync(
                "sp_EliminarUsuario",
                new { Id = id },
                commandType: CommandType.StoredProcedure);

            return Ok("Usuario eliminado");
        }
    }
}
