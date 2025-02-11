using PoliticStatements.Models;
using System.Data.SqlClient;

namespace PoliticStatements
{
    public class EmotionAnalysis
    {
       
        public async Task LoadEmotionFromDB(List<Statement> st)
        {
            
            Dictionary<string, Statement> statementsDict = st.ToDictionary(s => s.id);

            using (SqlConnection conn = new SqlConnection(GlobalConfig.connstring))
            {
                await conn.OpenAsync();

                string query = "SELECT * from Emotion ";
                              

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        
                        while (await reader.ReadAsync())
                        {
                            double score = reader.GetDouble(reader.GetOrdinal("Score"));
                            string emotion = reader.GetString(reader.GetOrdinal("Emotion"));
                            string statementId = reader.GetString(reader.GetOrdinal("StatementID"));

                            
                            if (statementsDict.TryGetValue(statementId, out Statement statement))
                            {
                                
                                statement.emotions ??= new List<EmotionData>();

                                
                                statement.emotions.Add(new EmotionData
                                {
                                    emotion = emotion,
                                    score = score
                                });
                            }
                        }
                    }
                }
            }
        }

        public List<EmotionDistribution> PrepareEmotionDistribution(List<Statement> statements)
        {
            var emotionCount = new Dictionary<string, int>();

            
            foreach (var statement in statements)
            {
               
                foreach (var emotionData in statement.emotions)
                {
                    if (emotionCount.ContainsKey(emotionData.emotion))
                    {
                        emotionCount[emotionData.emotion]++;
                    }
                    else
                    {
                        emotionCount[emotionData.emotion] = 1;
                    }
                }
            }

            
            var emotionDistribution = emotionCount
                .Select(ec => new EmotionDistribution
                {
                    Emotion = ec.Key,
                    Count = ec.Value
                })
                .ToList();

            return emotionDistribution;
        }
    }
}
