using PoliticStatements.Models;
using System.Data.SqlClient;
using System.Globalization;
using System.Text.RegularExpressions;

namespace PoliticStatements.Repositories
{
    public class EmotionRepository
    {

        public async Task LoadEmotionFromDB(List<Statement> st)
        {
            Dictionary<string, Statement> statementsDict = st.ToDictionary(s => s.id);

            using (SqlConnection conn = new SqlConnection(GlobalConfig.connstring))
            {
                await conn.OpenAsync();

                string query = @"
            SELECT se.StatementID, e.Emotion, se.Score
            FROM StatementEmotion se
            JOIN Emotion e ON se.EmotionID = e.ID";

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
                                statement.emotions ??= new List<Emotion>();

                                statement.emotions.Add(new Emotion
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
        static List<(string, float)> ParseToList(string input)
        {
            var result = new List<(string, float)>();


            var regex = new Regex(@"\('([^']+)',\s*(-?\d+(\.\d+)?)\)");

            foreach (Match match in regex.Matches(input))
            {
                string word = match.Groups[1].Value;
                string numberStr = match.Groups[2].Value;
                float number;


                if (float.TryParse(numberStr, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
                {
                    result.Add((word, number));
                }
                else
                {

                    Console.WriteLine($"Chyba při převodu čísla: {numberStr}");
                }
            }

            return result;
        }
        public List<Statement> LoadEmotionsFromFile(string filePath)
        {
            var statements = new List<Statement>();
            var check = new HashSet<string>();

            using (StreamReader reader = new StreamReader(filePath))
            {
                reader.ReadLine(); 
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var parts = line.Split(",", 2);
                    if (parts.Length < 2) continue;

                    string statementId = parts[0].Trim('"');
                    string jsonPart = parts[1].Trim('"');

                    if (!check.Contains(statementId))
                    {
                        check.Add(statementId);

                        var emotionsRaw = ParseToList(jsonPart); 
                        var emotions = new List<Emotion>();

                        foreach (var e in emotionsRaw)
                        {
                            emotions.Add(new Emotion
                            {
                                emotion = e.Item1,
                                score = e.Item2
                            });
                        }

                        statements.Add(new Statement
                        {
                            id = statementId,
                            emotions = emotions
                        });
                    }
                }
            }

            return statements;
        }


        public async Task SaveEmotionsToDB(List<Statement> statements)
        {
            using (SqlConnection conn = new SqlConnection(GlobalConfig.connstring))
            {
                await conn.OpenAsync();

                foreach (var statement in statements)
                {
                    foreach (var emotion in statement.emotions)
                    {
                        await InsertEmotion(conn, statement.id, emotion.emotion, (float)emotion.score);
                    }
                }
            }
        }

        private async Task InsertEmotion(SqlConnection conn, string statementId, string emotionName, float score)
        {
            int emotionId;


            string selectQuery = "SELECT ID FROM Emotion WHERE Emotion = @Emotion";
            using (SqlCommand selectCmd = new SqlCommand(selectQuery, conn))
            {
                selectCmd.Parameters.AddWithValue("@Emotion", emotionName);
                var result = await selectCmd.ExecuteScalarAsync();

                if (result != null)
                {
                    emotionId = Convert.ToInt32(result);
                }
                else
                {
                    string insertEmotionQuery = "INSERT INTO Emotion (Emotion)  VALUES (@Emotion)";
                    using (SqlCommand insertCmd = new SqlCommand(insertEmotionQuery, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@Emotion", emotionName);
                        emotionId = (int)await insertCmd.ExecuteScalarAsync();
                    }
                }
            }

            string insertStatementEmotionQuery = "INSERT INTO StatementEmotion (StatementID, EmotionID, Score) " +
                                                  "VALUES (@StatementID, @EmotionID, @Score)";
            using (SqlCommand insertStmtCmd = new SqlCommand(insertStatementEmotionQuery, conn))
            {
                insertStmtCmd.Parameters.AddWithValue("@StatementID", statementId);
                insertStmtCmd.Parameters.AddWithValue("@EmotionID", emotionId);
                insertStmtCmd.Parameters.AddWithValue("@Score", score);

                await insertStmtCmd.ExecuteNonQueryAsync();
            }
        }

    }


}
