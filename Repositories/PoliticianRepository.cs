using PoliticStatements.Models;
using System.Data.SqlClient;

namespace PoliticStatements.Repositories
{
    public class PoliticianRepository
    {


        public async Task<List<Politic>> GetAllPoliticiansAsync()
        {
            var politicians = new List<Politic>();

            string query = "SELECT p.politic_id, p.strana,count(s.id) as c FROM Politic p inner join Statement s on s.politic_id=p.politic_id  where s.jazyk='cs' group by  p.politic_id, p.strana order by count(s.id) desc";

            using (SqlConnection connection = new SqlConnection(GlobalConfig.connstring))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        Politic politic = new Politic
                        (
                            reader["politic_id"].ToString(),
                             reader["strana"].ToString(),
                           int.Parse(reader["c"].ToString())
                        );

                        politicians.Add(politic);
                    }
                }
            }

            return politicians;
        }
    }
}
