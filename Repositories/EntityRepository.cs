using PoliticStatements.Models;
using System.Data.SqlClient;

namespace PoliticStatements.Repositories
{
    public class EntityRepository
    {


        public async Task LoadNERFromDB(List<Statement> st)
        {
            Dictionary<string, Statement> statementsDict = st.ToDictionary(s => s.id);

            using (SqlConnection conn = new SqlConnection(GlobalConfig.connstring))
            {
                await conn.OpenAsync();

                string query =
             " SELECT e1.EntityName, se.EntityType, se.StatementID " +
             " FROM StatementEntity se " +
             " INNER JOIN Statement s ON s.id = se.StatementID " +
             " INNER JOIN Entity e1 ON e1.EntityID = se.EntityID " +
             " WHERE s.jazyk LIKE 'cs' " +
             "  AND se.EntityType IN ('io','ps','P','pf','gc','gu', 'ic', 'ms', 'op', 'oa', 'if','tm','ty') " +
             " ORDER BY se.EntityType; ";




                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string entityName = reader.GetString(reader.GetOrdinal("EntityName"));
                            string entityType = reader.GetString(reader.GetOrdinal("EntityType"));
                            string statementId = reader.GetString(reader.GetOrdinal("StatementID"));

                            if (statementsDict.TryGetValue(statementId, out Statement statement))
                            {
                                statement.Entities ??= new List<Entity>();

                                statement.Entities.Add(new Entity
                                {
                                    EntityText = entityName,
                                    Type = entityType
                                });
                            }
                        }
                    }
                }
            }
        }

        public async Task UpdateNER(string fileLemmaEntities)
        {
            using (SqlConnection connection = new SqlConnection(GlobalConfig.connstring))
            {
                connection.Open();

                foreach (string line in File.ReadLines(fileLemmaEntities))
                {
                    var parts = line.Split(',');
                    if (parts.Length == 2)
                    {
                        string oldWord = parts[0].Trim();
                        string newWord = parts[1].Trim();

                        string query = $"UPDATE Entity SET EntityName = @newWord WHERE EntityName = @oldWord";

                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@newWord", newWord);
                            command.Parameters.AddWithValue("@oldWord", oldWord);

                            int rowsAffected = command.ExecuteNonQuery();

                        }
                    }
                }


            }
        }


        public List<Statement> LoadNERCsv(string statementEntityFile)
        {
            var statements = new List<Statement>();

            foreach (var line in File.ReadLines(statementEntityFile))
            {
                var parts = line.Split(';');
                if (parts.Length < 2) continue;

                var statementId = parts[0];
                var entities = new List<Entity>();

                for (int i = 1; i < parts.Length; i++)
                {
                    var entityParts = parts[i].Split(',');
                    if (entityParts.Length == 2)
                    {
                        entities.Add(new Entity
                        {
                            EntityText = entityParts[0].Trim(),
                            Type = entityParts[1].Trim()
                        });
                    }
                }

                statements.Add(new Statement
                {
                    id = statementId,
                    Entities = entities
                });
            }

            return statements;
        }
        public async Task SaveNERToDB(List<Statement> statementNERs)
        {
            using (SqlConnection conn = new SqlConnection(GlobalConfig.connstring))
            {
                await conn.OpenAsync();

                foreach (var st in statementNERs)
                {
                    foreach (var entity in st.Entities)
                    {
                        string st_id = st.id;
                        string entity_name = entity.EntityText;
                        string type = entity.Type;

                        if (entity_name.Length > 1 && entity_name.Length < 100)
                        {
                            int entityId;

                            string selectQuery = "SELECT EntityID FROM Entity WHERE EntityName = @EntityName";

                            using (SqlCommand selectCmd = new SqlCommand(selectQuery, conn))
                            {
                                selectCmd.Parameters.AddWithValue("@EntityName", entity_name);
                                var result = await selectCmd.ExecuteScalarAsync();

                                if (result != null)
                                {
                                    entityId = Convert.ToInt32(result);
                                }
                                else
                                {
                                    string insertEntityQuery = "INSERT INTO Entity (EntityName) OUTPUT INSERTED.EntityID VALUES (@EntityName)";
                                    using (SqlCommand insertCmd = new SqlCommand(insertEntityQuery, conn))
                                    {
                                        insertCmd.Parameters.AddWithValue("@EntityName", entity_name);
                                        entityId = (int)await insertCmd.ExecuteScalarAsync();
                                    }
                                }
                            }

                            string insertStatementEntityQuery = "INSERT INTO StatementEntity (EntityID, StatementID, EntityType) " +
                                                                 "VALUES (@EntityID, @StatementID, @EntityType)";

                            using (SqlCommand insertStmtCmd = new SqlCommand(insertStatementEntityQuery, conn))
                            {
                                insertStmtCmd.Parameters.AddWithValue("@EntityID", entityId);
                                insertStmtCmd.Parameters.AddWithValue("@StatementID", st_id);
                                insertStmtCmd.Parameters.AddWithValue("@EntityType", type);

                                await insertStmtCmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }
            }
        }


    }
}
