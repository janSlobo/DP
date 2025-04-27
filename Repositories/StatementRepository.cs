using PoliticStatements.Models;
using System.Data.SqlClient;

namespace PoliticStatements.Repositories
{
    public class StatementRepository
    {
        public async Task<List<Statement>> LoadFromDatabase()
        {
            List<Statement> st = new List<Statement>();

            using (SqlConnection conn = new SqlConnection(GlobalConfig.connstring))
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("select  server,politic_id,datum,id,pocetSlov,RT,Sentiment,SentimentBert from Statement where jazyk='cs'  and SentimentBert!=666 and Sentiment!=666  ", conn))
                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        Statement s = new Statement
                        {
                            server = reader.GetString(reader.GetOrdinal("server")),
                            osobaid = new Politic(reader.GetString(reader.GetOrdinal("politic_id"))),
                            datum = reader.GetDateTime(reader.GetOrdinal("datum")),
                            id = reader.GetString(reader.GetOrdinal("id")),
                            pocetSlov = reader.GetInt32(reader.GetOrdinal("pocetSlov")),
                            RT = reader.GetBoolean(reader.GetOrdinal("RT")),
                            Sentiment = reader.GetDouble(reader.GetOrdinal("Sentiment")),
                            SentimentBert = reader.GetDouble(reader.GetOrdinal("SentimentBert"))

                        };

                        st.Add(s);
                    }
                }

                using (SqlCommand cmd = new SqlCommand("SELECT statement_id, politic_id FROM Mentions ", conn))
                {
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {

                            string statementId = reader.GetString(reader.GetOrdinal("statement_id"));
                            string politicId = reader.GetString(reader.GetOrdinal("politic_id"));
                            var politic = new Politic(politicId);

                            Statement statement = st.FirstOrDefault(s => s.id == statementId);
                            if (statement != null)
                            {
                                statement.politicizminky.Add(politic);
                            }


                        }
                    }
                }

            }

            return st;
        }
        public void UpdateLanguageForIds(string filePath)
        {



            using (SqlConnection connection = new SqlConnection(GlobalConfig.connstring))
            {
                connection.Open();

                using (StreamReader reader = new StreamReader(filePath))
                {
                    string line;
                    reader.ReadLine(); 

                    while ((line = reader.ReadLine()) != null)
                    {
                        var parts = line.Split(',');
                        if (parts.Length < 2) continue;

                        string id = parts[0].Trim();
                        string jazyk = parts[1].Trim();

                        string query = "UPDATE Statement SET jazyk = @jazyk WHERE id = @id";
                        using (SqlCommand cmd = new SqlCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("@jazyk", jazyk);
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }

            Console.WriteLine("Aktualizace dokončena.");
        }
        public async Task StoreToDatabase(List<Statement> st)
        {
            List<Statement> allData = st;


            using (SqlConnection conn = new SqlConnection(GlobalConfig.connstring))
            {
                await conn.OpenAsync();
                var errors = 0;
                List<string> errorlist = new List<string>();
                List<string> ids = new List<string>();

                for (int i = 0; i < allData.Count; i++)
                {
                    string osobaid = allData[i].osobaid.politic_id.ToLower();


                    using (SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM Politic WHERE politic_id = @id", conn))
                    {
                        checkCmd.Parameters.AddWithValue("@id", allData[i].osobaid);
                        int count = (int)await checkCmd.ExecuteScalarAsync();

                        if (count == 0)
                        {
                            using (SqlCommand cmd = new SqlCommand("INSERT INTO Politic(politic_id) VALUES(@id)", conn))
                            {
                                cmd.Parameters.AddWithValue("@id", allData[i].osobaid);
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                    }



                    using (SqlCommand cmd = new SqlCommand())
                    {

                        cmd.CommandText = "INSERT INTO Statement(server,typserveru,politic_id,datum,id,url,pocetSlov,text) VALUES(@server,@typserveru,@osobaid,@datum,@id,@url,@pocetSlov,@text)";
                        cmd.Parameters.AddWithValue("@server", allData[i].server);
                        cmd.Parameters.AddWithValue("@typserveru", allData[i].typserveru);
                        cmd.Parameters.AddWithValue("@osobaid", allData[i].osobaid);
                        cmd.Parameters.AddWithValue("@datum", allData[i].datum);

                        cmd.Parameters.AddWithValue("@id", allData[i].id);
                        cmd.Parameters.AddWithValue("@url", allData[i].url);
                        cmd.Parameters.AddWithValue("@text", allData[i].text);

                        cmd.Parameters.AddWithValue("@pocetSlov", allData[i].pocetSlov);


                        cmd.Connection = conn;
                        await cmd.ExecuteNonQueryAsync();


                    }


                    if (allData[i].politicizminky != null && allData[i].politicizminky.Count != 0)
                    {

                        for (int k = 0; k < allData[i].politicizminky.Count(); k++)
                        {





                            using (SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM Politic WHERE politic_id = @id", conn))
                            {
                                checkCmd.Parameters.AddWithValue("@id", allData[i].politicizminky[k]);
                                int count = (int)await checkCmd.ExecuteScalarAsync();

                                if (count == 0)
                                {
                                    using (SqlCommand cmd = new SqlCommand("INSERT INTO Politic(politic_id) VALUES(@id)", conn))
                                    {
                                        cmd.Parameters.AddWithValue("@id", allData[i].politicizminky[k]);
                                        await cmd.ExecuteNonQueryAsync();
                                    }
                                }
                            }

                            using (SqlCommand cmd = new SqlCommand())
                            {

                                try
                                {
                                    cmd.CommandText = "INSERT INTO Mentions(statement_id,politic_id) VALUES(@sid,@pid)";
                                    cmd.Parameters.AddWithValue("@sid", allData[i].id);
                                    cmd.Parameters.AddWithValue("@pid", allData[i].politicizminky[k]);

                                    cmd.Connection = conn;
                                    await cmd.ExecuteNonQueryAsync();
                                }
                                catch (Exception ex)
                                {
                                    errorlist.Add(allData[i].politicizminky[k].politic_id);
                                    errors++;
                                    Console.WriteLine($"Chyba při vkládání dat: {ex.Message}");

                                }

                            }
                        }
                    }
                }
                await conn.CloseAsync();
            }


        }
    }
}
