using Microsoft.Data.SqlClient;

namespace ApiUsuarios.Data
{
    public class ConexionBD
    {
        private readonly string _cadenaConexion;

        public ConexionBD(IConfiguration configuration)
        {
            _cadenaConexion = configuration.GetConnectionString("CadenaSQL");
        }

        public SqlConnection ObtenerConexion()
        {
            return new SqlConnection(_cadenaConexion);
        }
    }
}
