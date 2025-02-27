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


        public Dictionary<string, Dictionary<int, List<EmotionStats>>> GetEmotionPercentagesPerPoliticianAndYearQ(List<Statement> statements)
        {
            var emotionStats = new Dictionary<string, Dictionary<int, List<EmotionStats>>>();

            
            var groupedData = statements
                .GroupBy(s => new { s.osobaid, Year = s.datum.Value.Year })
                .OrderBy(g => g.Key.osobaid)
                .ThenBy(g => g.Key.Year)
                .ToList();

            foreach (var group in groupedData)
            {
                string politician = group.Key.osobaid;
                int year = group.Key.Year;

                
                if (!emotionStats.ContainsKey(politician))
                {
                    emotionStats[politician] = new Dictionary<int, List<EmotionStats>>();
                }

                
                if (!emotionStats[politician].ContainsKey(year))
                {
                    emotionStats[politician][year] = new List<EmotionStats>();
                }

                var totalStatementsInGroup = group.Count();
                var emotionsInGroup = new Dictionary<string, int>();

               
                foreach (var statement in group)
                {
                    foreach (var emotion in statement.emotions)
                    {
                        if (!emotionsInGroup.ContainsKey(emotion.emotion))
                        {
                            emotionsInGroup[emotion.emotion] = 0;
                        }
                        emotionsInGroup[emotion.emotion]++;
                    }
                }

                
                foreach (var emotion in emotionsInGroup)
                {
                    var emotionStat = new EmotionStats
                    {
                        Emotion = emotion.Key,
                        PercentagePerQuarter = CalculatePercentagesQ(group, emotion.Key)
                    };

                    
                    emotionStats[politician][year].Add(emotionStat);
                }
            }

            return emotionStats;
        }
        public Dictionary<string, SortedDictionary<string, List<EmotionStatsH>>> GetEmotionPercentagesPerPoliticianAndYearH(List<Statement> statements)
        {
            var emotionStats = new Dictionary<string, SortedDictionary<string, List<EmotionStatsH>>>();


            var groupedData = statements
                .GroupBy(s => new { s.osobaid, HalfYear = GetHalfYear(s.datum) })
                .ToList();
            
            foreach (var group in groupedData)
            {
                string politician = group.Key.osobaid;
                string half= group.Key.HalfYear;



                if (!emotionStats.ContainsKey(politician))
                {
                    emotionStats[politician] = new SortedDictionary<string, List<EmotionStatsH>>();
                }

                emotionStats[politician][half] = new List<EmotionStatsH>();



                var totalStatementsInGroup = group.Count();
               


                var emotionsInGroup = group
                .SelectMany(s => s.emotions)
                .GroupBy(e => e.emotion)
                .ToDictionary(g => g.Key, g => g.Count());


                foreach (var emotion in emotionsInGroup)
                {
                    var emotionStat = new EmotionStatsH
                    {
                        Emotion = emotion.Key,
                        Percentage = (double)emotionsInGroup[emotion.Key]/ totalStatementsInGroup*100
                    };


                    emotionStats[politician][half].Add(emotionStat);
                }
            }

            return emotionStats;
        }
        private List<double> CalculatePercentagesQ(IGrouping<dynamic, Statement> group, string emotion)
        {
           
            var percentages = new List<double>();

            var quarterGroups = group
                .GroupBy(s => GetQuarter(s.datum.Value))
                .OrderBy(g => g.Key)
                .ToList();

            foreach (var quarterGroup in quarterGroups)
            {
                int emotionCount = quarterGroup
                    .Count(s => s.emotions.Any(ed => ed.emotion == emotion));

                double percentage = (double)emotionCount / quarterGroup.Count() * 100;

                percentages.Add(percentage);
            }

            return percentages;
        }
        
        private int GetQuarter(DateTime date)
        {
            if (date.Month <= 3) return 1;
            if (date.Month <= 6) return 2;
            if (date.Month <= 9) return 3;
            return 4;
        }

        private static string GetHalfYear(DateTime? date)
        {
            if (!date.HasValue)
            {
                return string.Empty;
            }

            int year = date.Value.Year;
            int month = date.Value.Month;
            string halfYear = month <= 6 ? "H1" : "H2";

            return $"{year}-{halfYear}";
        }
    }

    

}
