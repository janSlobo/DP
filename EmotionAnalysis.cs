using PoliticStatements.Models;
using System.Data.SqlClient;

namespace PoliticStatements
{
    public class EmotionAnalysis
    {
        public List<PoliticianEmotionData> CalculateEmotionStats(List<Statement> statements, List<string> emotionsToAnalyze)
        {
            var politicianStats = new Dictionary<string, PoliticianEmotionData>();

            foreach (var statement in statements)
            {
                // Get the politician's data or initialize if not found
                if (!politicianStats.ContainsKey(statement.osobaid))
                {
                    politicianStats[statement.osobaid] = new PoliticianEmotionData
                    {
                        OsobaId = statement.osobaid,
                        EmotionStatistics = emotionsToAnalyze.ToDictionary(emotion => emotion, emotion => new EmotionPStats())
                    };
                }

                var politicianData = politicianStats[statement.osobaid];

                // For each emotion in the statement, update statistics
                foreach (var emotionData in statement.emotions)
                {
                    if (emotionsToAnalyze.Contains(emotionData.emotion))
                    {
                        var emotionStat = politicianData.EmotionStatistics[emotionData.emotion];

                        // Update average intensity
                        if (emotionData.score > 0)
                        {
                            emotionStat.AverageIntensity += emotionData.score;
                            emotionStat.Percentage += 1;
                        }
                    }
                }
            }

            
            foreach (var politicianData in politicianStats.Values)
            {
                foreach (var emotion in politicianData.EmotionStatistics.Keys.ToList())
                {
                    var emotionStat = politicianData.EmotionStatistics[emotion];

                    if (emotionStat.Percentage > 0) 
                    {
                        emotionStat.AverageIntensity /= emotionStat.Percentage; 
                    }
                    
                    emotionStat.Percentage = emotionStat.Percentage / statements.Count(s => s.osobaid == politicianData.OsobaId) * 100;

                    
                    
                }
            }

            return politicianStats.Values.ToList();
        }
        public Dictionary<string, List<double>> GetSentimentDistribution(List<Statement> statements)
        {
            var sentimentData = new Dictionary<string, List<double>>();

            foreach (var statement in statements)
            {
              
                double sentiment = statement.Sentiment;

               
                foreach (var emotion in statement.emotions)
                {
                    
                    if (!sentimentData.ContainsKey(emotion.emotion))
                    {
                        sentimentData[emotion.emotion] = new List<double>();
                    }

                    
                    sentimentData[emotion.emotion].Add(sentiment);
                }
            }

            return sentimentData;
        }
        public Dictionary<string, double> CalculateAverageIntensity(List<Statement> statements)
        {
            
            var emotionSums = new Dictionary<string, double>();
            var emotionCounts = new Dictionary<string, int>();

           
            foreach (var statement in statements)
            {
              
                foreach (var emotionData in statement.emotions)
                {
                    if (!emotionSums.ContainsKey(emotionData.emotion))
                    {
                        emotionSums[emotionData.emotion] = 0;
                        emotionCounts[emotionData.emotion] = 0;
                    }

                    
                    emotionSums[emotionData.emotion] += emotionData.score;
                    emotionCounts[emotionData.emotion] += 1;
                }
            }

         
            var averageIntensities = new Dictionary<string, double>();

            foreach (var emotion in emotionSums.Keys)
            {
                
                averageIntensities[emotion] = emotionSums[emotion] / emotionCounts[emotion];
            }

            return averageIntensities;
        }
        public Dictionary<string, Dictionary<string, double>> CalculateAndNormalizeCoOccurrence(List<Statement> statements)
        {
            var emotionPairs = new Dictionary<string, Dictionary<string, int>>();

            // Krok 1: Výpočet spoluvýskytu emocí
            foreach (var statement in statements)
            {
                var uniqueEmotions = statement.emotions
                    .Select(e => e.emotion)
                    .Distinct()
                    .ToList();

                for (int i = 0; i < uniqueEmotions.Count; i++)
                {
                    for (int j = i; j < uniqueEmotions.Count; j++)
                    {
                        string e1 = uniqueEmotions[i];
                        string e2 = uniqueEmotions[j];

                        if (!emotionPairs.ContainsKey(e1))
                            emotionPairs[e1] = new Dictionary<string, int>();

                        if (!emotionPairs[e1].ContainsKey(e2))
                            emotionPairs[e1][e2] = 0;

                        emotionPairs[e1][e2]++;

                        // Zrcadlově vyplníme matici
                        if (e1 != e2)
                        {
                            if (!emotionPairs.ContainsKey(e2))
                                emotionPairs[e2] = new Dictionary<string, int>();

                            if (!emotionPairs[e2].ContainsKey(e1))
                                emotionPairs[e2][e1] = 0;

                            emotionPairs[e2][e1]++;
                        }
                    }
                }
            }

            // Krok 2: Normalizace matice
            var normalizedMatrix = new Dictionary<string, Dictionary<string, double>>();

            // Nejprve získáme maximální hodnoty na diagonále
            var diagValues = emotionPairs.ToDictionary(kv => kv.Key, kv => kv.Value.ContainsKey(kv.Key) ? kv.Value[kv.Key] : 1);

            foreach (var row in emotionPairs)
            {
                string emotion1 = row.Key;
                normalizedMatrix[emotion1] = new Dictionary<string, double>();

                foreach (var col in row.Value)
                {
                    string emotion2 = col.Key;
                    int count = col.Value;

                    // Kontrola, zda hodnoty na diagonále nejsou nula, aby se zabránilo dělení nulou
                    double normValue = 0;
                    double diag1 = diagValues[emotion1];
                    double diag2 = diagValues[emotion2];

                    if (diag1 > 0 && diag2 > 0) // Pokud jsou diagonální hodnoty větší než nula
                    {
                        normValue = (double)count / Math.Sqrt(diag1 * diag2);
                    }

                    // Pokud je některá diagonální hodnota 0, přiřadíme hodnotu 0
                    normalizedMatrix[emotion1][emotion2] = normValue;
                }
            }

            return normalizedMatrix;
        }

        public Dictionary<string, Dictionary<string, int>> CalculateCoOccurrence(List<Statement> statements)
        {
            var emotionPairs = new Dictionary<string, Dictionary<string, int>>();

            foreach (var statement in statements)
            {
                var uniqueEmotions = statement.emotions
                    .Select(e => e.emotion)
                    .Distinct()
                    .ToList();

                for (int i = 0; i < uniqueEmotions.Count; i++)
                {
                    for (int j = i; j < uniqueEmotions.Count; j++)
                    {
                        string e1 = uniqueEmotions[i];
                        string e2 = uniqueEmotions[j];

                        if (!emotionPairs.ContainsKey(e1))
                            emotionPairs[e1] = new Dictionary<string, int>();

                        if (!emotionPairs[e1].ContainsKey(e2))
                            emotionPairs[e1][e2] = 0;

                        emotionPairs[e1][e2]++;

                        // Zrcadlově vyplníme matici
                        if (e1 != e2)
                        {
                            if (!emotionPairs.ContainsKey(e2))
                                emotionPairs[e2] = new Dictionary<string, int>();

                            if (!emotionPairs[e2].ContainsKey(e1))
                                emotionPairs[e2][e1] = 0;

                            emotionPairs[e2][e1]++;
                        }
                    }
                }
            }

            return emotionPairs;
        }
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
                }).OrderByDescending(x=>x.Count)
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
