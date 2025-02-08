using PoliticStatements.Models;
using System.Data.SqlClient;
namespace PoliticStatements
{
    public class TopicAnalysis
    {

      
        public async Task LoadTopics(List<Statement> st)
        {
            
            Dictionary<string, Statement> statementsDict = st.ToDictionary(s => s.id);

            using (SqlConnection conn = new SqlConnection(GlobalConfig.connstring))
            {
                await conn.OpenAsync();

                string query = "select  topic,StatementId  from Topic  where topic in (select topic from Topic group by topic having count(*)>3 )";
                              

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        
                        while (await reader.ReadAsync())
                        {
                            string topic = reader.GetString(reader.GetOrdinal("topic"));
                           
                            string statementId = reader.GetString(reader.GetOrdinal("StatementId"));

                            
                            if (statementsDict.TryGetValue(statementId, out Statement statement))
                            {
                               
                                statement.topics ??= new List<string>();

                               
                                statement.topics.Add(topic);
                            }
                        }
                    }
                }
            }
        }

        public void PrepareStatementClusteringData(List<Statement> statements, string filePath)
        {
            var uniqueTopics = statements.SelectMany(s => s.topics).Distinct().ToList();

            using (StreamWriter writer = new StreamWriter(filePath))
            {
               
                writer.WriteLine("StatementID," + string.Join(";", uniqueTopics));

                
                foreach (var statement in statements)
                {
                    var row = new List<string> { statement.id.ToString() };
                    row.AddRange(uniqueTopics.Select(topic => statement.topics.Contains(topic) ? "1" : "0"));
                    writer.WriteLine(string.Join(";", row));
                }
            }
        }

     
        public void PrepareTopicClusteringData(List<Statement> statements, string filePath)
        {
            var uniqueTopics = statements.SelectMany(s => s.topics).Distinct().ToList();

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                
                writer.WriteLine("Topic," + string.Join(";", statements.Select(s => s.id)));

             
                foreach (var topic in uniqueTopics)
                {
                    var row = new List<string> { topic };
                    row.AddRange(statements.Select(s => s.topics.Contains(topic) ? "1" : "0"));
                    var pocet = row.Where(x => x == "1").ToList().Count();
                    writer.WriteLine(string.Join(";", row));
                }
            }
        }
    }
}
