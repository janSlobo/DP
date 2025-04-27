using Microsoft.AspNetCore.Mvc;
using PoliticStatements.Models;
using System.Data.SqlClient;

using Accord.Math;
using Accord.Statistics.Analysis;
using System.Text;
using System.Globalization;
using PoliticStatements.Repositories;
namespace PoliticStatements.Services
{
    public class NERAnalysis
    {
        private readonly EntityRepository entityRepository;

        public NERAnalysis(EntityRepository er)
        {

            entityRepository = er;
            entity_mapping = new Dictionary<string, string>
            {
                { "io", "Politické instituce" },
                {"ps","Příjmení" },
                {"pf","Křestní jméno" },
                {"gc","Státy" },
                {"mn","Periodical" },
                {"gu","Města" },
                {"ty","Roky" },
                {"ic","Kult./Vzděl./Vědec. instituce" },
                {"if","Firmy" },
                {"P","Celá jména osob" },
                {"gt","Kontinenty" },
                {"om" ,"Měny"},
                {"tm","Měsíce" },
                {"ms" ,"TV a rádio st."},
                {"gl","Přírodní oblasti" },
                {"gr","Území" },
                {"op","Produkty" },
                {"oa","Filmy,knihy atd." },
                {"o_","Různé" },
                {"p_","Jména" }

            };
        }
        public Dictionary<string, string> entity_mapping { get; set; }

        public async Task ImportNER(string statementEntityFile)
        {
            List<Statement> statementNERs = entityRepository.LoadNERCsv(statementEntityFile);
            await entityRepository.SaveNERToDB(statementNERs);
        }

        public Dictionary<int, int> GetStatementsPerYear(List<Statement> statements)
        {
            return statements
                .GroupBy(s => s.datum.Value.Year)
                .ToDictionary(g => g.Key, g => g.Count());
        }


        public List<EntityFrequency> PoliticEntityPieChart(List<Statement> st_emotion, List<string> nertypes, string currentEntity = "")
        {
            var entityFrequency = new Dictionary<string, int>();


            foreach (var statement in st_emotion)
            {
                foreach (var entity in statement.Entities)
                {
                    if (entity.EntityText == currentEntity) continue;
                    if (!nertypes.Contains(entity.Type)) continue;
                    if (entityFrequency.ContainsKey(entity.EntityText))
                    {
                        entityFrequency[entity.EntityText]++;
                    }
                    else
                    {

                        entityFrequency.Add(entity.EntityText, 1);
                    }
                }
            }


            List<EntityFrequency> pieChartData = entityFrequency
                .Select(e => new EntityFrequency { EntityText = e.Key, Frequency = e.Value })
                .OrderByDescending(e => e.Frequency).Take(15)
                .ToList();

            return pieChartData;
        }
        public List<EntityFrequency> PoliticEntityPieChartNormalized2(List<Statement> st_emotion, List<Statement> st_all, List<string> nertypes, string currentEntity = "")
        {
            var entityFrequencyEmotion = new Dictionary<string, int>();
            var totalEntityFrequency = new Dictionary<string, int>();
            int totalStatements = st_all.Count;


            foreach (var statement in st_emotion)
            {
                var uniqueEntities = new HashSet<string>();
                foreach (var entity in statement.Entities)
                {
                    if (entity.EntityText == currentEntity) continue;
                    if (!nertypes.Contains(entity.Type)) continue;

                    if (entityFrequencyEmotion.ContainsKey(entity.EntityText))
                        entityFrequencyEmotion[entity.EntityText]++;
                    else
                        entityFrequencyEmotion.Add(entity.EntityText, 1);

                    uniqueEntities.Add(entity.EntityText);
                }
            }


            foreach (var statement in st_all)
            {
                foreach (var entity in statement.Entities)
                {
                    if (!nertypes.Contains(entity.Type)) continue;

                    if (totalEntityFrequency.ContainsKey(entity.EntityText))
                        totalEntityFrequency[entity.EntityText]++;
                    else
                        totalEntityFrequency.Add(entity.EntityText, 1);
                }
            }

            var filteredTotalEntityFrequency = totalEntityFrequency
                .Where(e => e.Value > 100)
                .ToDictionary(e => e.Key, e => e.Value);

            var weightedScores = new Dictionary<string, double>();

            foreach (var entity in entityFrequencyEmotion)
            {
                string entityText = entity.Key;
                double emotionFrequency = entity.Value;
                double globalFrequency = filteredTotalEntityFrequency.ContainsKey(entityText) ? filteredTotalEntityFrequency[entityText] : 0;


                if (globalFrequency > 0)
                {
                    double score = emotionFrequency / globalFrequency;
                    weightedScores[entityText] = score;
                }
            }

            List<EntityFrequency> pieChartData = weightedScores
                .OrderByDescending(e => e.Value)
                .Take(15)
                .Select(e => new EntityFrequency { EntityText = e.Key, Frequency = e.Value })
                .ToList();

            return pieChartData;
        }

        public List<EntityFrequency> PoliticEntityPieChartNormalized3(
    List<Statement> st_emotion,
    List<Statement> st_all,
    List<string> nertypes,
    int mincount = 100,
    string currentEntity = "")
        {
            var entityFrequencyEmotion = new Dictionary<string, int>();
            var totalEntityFrequency = new Dictionary<string, int>();
            var emotionFrequency = 0;
            int totalStatements = st_all.Count;


            foreach (var statement in st_all)
            {
                foreach (var entity in statement.Entities)
                {
                    if (!nertypes.Contains(entity.Type)) continue;

                    if (totalEntityFrequency.ContainsKey(entity.EntityText))
                        totalEntityFrequency[entity.EntityText]++;
                    else
                        totalEntityFrequency.Add(entity.EntityText, 1);
                }
            }

            foreach (var statement in st_emotion)
            {
                emotionFrequency++;
                var uniqueEntities = new HashSet<string>();
                foreach (var entity in statement.Entities)
                {
                    if (entity.EntityText == currentEntity) continue;
                    if (!nertypes.Contains(entity.Type)) continue;

                    if (entityFrequencyEmotion.ContainsKey(entity.EntityText))
                        entityFrequencyEmotion[entity.EntityText]++;
                    else
                        entityFrequencyEmotion.Add(entity.EntityText, 1);

                    uniqueEntities.Add(entity.EntityText);
                }
            }


            var filteredEntities = totalEntityFrequency.Where(e => e.Value > mincount).ToDictionary(e => e.Key, e => e.Value);

            var pmiScores = new Dictionary<string, double>();

            foreach (var entity in entityFrequencyEmotion)
            {
                string entityText = entity.Key;

                if (!filteredEntities.ContainsKey(entityText)) continue;

                double entityEmotionFrequency = entity.Value;
                double entityFrequencyGlobal = filteredEntities[entityText];
                double entityProbability = entityFrequencyGlobal / totalStatements;

                double emotionProbability = (double)emotionFrequency / totalStatements;

                double jointProbability = entityEmotionFrequency / totalStatements;


                if (entityProbability > 0 && emotionProbability > 0 && jointProbability > 0)
                {
                    double pmi = Math.Log(jointProbability / (entityProbability * emotionProbability));
                    pmiScores[entityText] = pmi;
                }
            }


            var pieChartData = pmiScores
                .OrderByDescending(e => e.Value)
                .Take(15)
                .Select(e => new EntityFrequency { EntityText = e.Key, Frequency = e.Value })
                .ToList();

            if (pieChartData.Count < 10)
            {

                var remainingEntities = totalEntityFrequency
                    .Where(e => e.Value > 0)
                    .ToDictionary(e => e.Key, e => e.Value);


                var additionalPmiScores = new Dictionary<string, double>();

                foreach (var entity in entityFrequencyEmotion)
                {
                    string entityText = entity.Key;

                    if (!remainingEntities.ContainsKey(entityText)) continue;

                    double entityEmotionFrequency = entity.Value;
                    double entityFrequencyGlobal = remainingEntities[entityText];
                    double entityProbability = entityFrequencyGlobal / totalStatements;
                    double emotionProbability = (double)emotionFrequency / totalStatements;
                    double jointProbability = entityEmotionFrequency / totalStatements;

                    if (entityProbability > 0 && emotionProbability > 0 && jointProbability > 0)
                    {
                        double pmi = Math.Log(jointProbability / (entityProbability * emotionProbability));
                        additionalPmiScores[entityText] = pmi;
                    }
                }

                pieChartData.AddRange(additionalPmiScores
                    .OrderByDescending(e => e.Value)
                    .Take(15 - pieChartData.Count)
                    .Select(e => new EntityFrequency { EntityText = e.Key, Frequency = e.Value }));

                pieChartData = pieChartData.Take(15).ToList();
            }

            return pieChartData;
        }



        public List<EntityFrequency> PoliticEntityPieChartNormalized(List<Statement> st_emotion, List<Statement> st_all, List<string> nertypes, string currentEntity = "")
        {
            var entityFrequency = new Dictionary<string, int>();
            var totalEntityFrequency = new Dictionary<string, int>();
            var documentFrequency = new Dictionary<string, int>();
            int totalStatements = st_all.Count;

            foreach (var statement in st_emotion)
            {
                var uniqueEntities = new HashSet<string>();
                foreach (var entity in statement.Entities)
                {
                    if (entity.EntityText == currentEntity) continue;
                    if (!nertypes.Contains(entity.Type)) continue;

                    if (entityFrequency.ContainsKey(entity.EntityText))
                        entityFrequency[entity.EntityText]++;
                    else
                        entityFrequency.Add(entity.EntityText, 1);

                    uniqueEntities.Add(entity.EntityText);
                }
                foreach (var entity in uniqueEntities)
                {
                    if (documentFrequency.ContainsKey(entity))
                        documentFrequency[entity]++;
                    else
                        documentFrequency.Add(entity, 1);
                }
            }

            foreach (var statement in st_all)
            {
                foreach (var entity in statement.Entities)
                {
                    if (!nertypes.Contains(entity.Type)) continue;

                    if (totalEntityFrequency.ContainsKey(entity.EntityText))
                        totalEntityFrequency[entity.EntityText]++;
                    else
                        totalEntityFrequency.Add(entity.EntityText, 1);
                }
            }

            var weightedScores = new Dictionary<string, double>();

            foreach (var entity in entityFrequency)
            {
                string entityText = entity.Key;
                double termFrequency = (double)entity.Value / entityFrequency.Values.Sum();

                if (documentFrequency.ContainsKey(entityText) && documentFrequency[entityText] > 0)
                {
                    double idf = Math.Pow(Math.Log((double)totalStatements / documentFrequency[entityText]), 3);

                    weightedScores[entityText] = termFrequency * idf;
                }
            }

            List<EntityFrequency> pieChartData = weightedScores
                .OrderByDescending(e => e.Value)
                .Take(15)
                .Select(e => new EntityFrequency { EntityText = e.Key, Frequency = e.Value })
                .ToList();

            return pieChartData;
        }

        public Dictionary<string, List<string>> GetTopEntitiesPerPerson(List<Statement> statements)
        {
            return statements
                .Where(s => s.Entities != null)
                .SelectMany(s => s.Entities
                    .Where(e => e.Type == "ps")
                    .Select(e => new { s.osobaid.politic_id, e.EntityText }))
                .GroupBy(x => new { x.politic_id, x.EntityText })
                .Select(g => new { g.Key.politic_id, g.Key.EntityText, Count = g.Count() })
                .Where(x => x.Count > 5)
                .OrderByDescending(x => x.Count)
                .GroupBy(x => x.politic_id)
                .ToDictionary(
                    g => g.Key,
                    g => g.Take(10).Select(x => x.EntityText).ToList()
                );
        }

        public Dictionary<string, List<List<Statement>>> GetStatementsPerTopEntity(List<Statement> statements, Dictionary<string, List<string>> topEntities)
        {
            return topEntities.ToDictionary(
               kvp => kvp.Key,
               kvp => kvp.Value
           .Select(entity => statements
               .Where(s => s.osobaid.politic_id == kvp.Key && s.Entities.Any(e => e.EntityText == entity))
               .ToList()
           )
           .ToList()
   );
        }



        public Dictionary<string, double> CalculateEntityToWordRatio(List<Statement> statements)
        {
            var politicianRatios = new Dictionary<string, double>();


            var groupedStatements = statements.GroupBy(s => s.osobaid.politic_id);

            foreach (var group in groupedStatements)
            {
                string politicianId = group.Key;


                double totalWords = group.Sum(s => s.pocetSlov ?? 0);
                double totalEntities = group.Sum(s => s.Entities.Count);


                double ratio = totalWords > 0 ? (totalEntities / (totalWords / 10)) : 0;



                politicianRatios[politicianId] = ratio;
            }

            return politicianRatios;
        }




        public List<KeyValuePair<string, int>> GetEntityNamesCount(List<Statement> statementNERs)
        {

            Dictionary<string, int> entityCounts = new Dictionary<string, int>();

            foreach (var st in statementNERs)
            {
                foreach (var entity in st.Entities)
                {
                    string entityName = entity.EntityText;


                    if (entityCounts.ContainsKey(entityName))
                    {
                        entityCounts[entityName]++;
                    }
                    else
                    {
                        entityCounts[entityName] = 1;
                    }
                }
            }


            return entityCounts
                .OrderByDescending(e => e.Value)
                .Take(50)
                .ToList();
        }

        public Dictionary<string, List<KeyValuePair<string, int>>> GetTopEntitiesByType(
    List<Statement> statementNERs,
    List<string> selectedEntityTypes,
    int minCount = 20)
        {
            var allEntities = statementNERs
        .SelectMany(st => st.Entities)
        .Where(e => selectedEntityTypes.Contains(e.Type))
        .GroupBy(e => e.EntityText)
        .Select(e => new KeyValuePair<string, int>(e.Key, e.Count()))
        .Where(e => e.Value >= minCount)
        .OrderByDescending(e => e.Value)
        .ToList();

            var groupedEntities = statementNERs
                .SelectMany(st => st.Entities)
                .Where(e => selectedEntityTypes.Contains(e.Type))
                .GroupBy(e => e.Type)
                .ToDictionary(
                    g => entity_mapping[g.Key],
                    g => g
                        .GroupBy(e => e.EntityText)
                        .Select(e => new KeyValuePair<string, int>(e.Key, e.Count()))
                        .Where(e => e.Value >= minCount)
                        .OrderByDescending(e => e.Value)
                        .ToList()
                );

            groupedEntities.Add("Všechny", allEntities);

            return groupedEntities;
        }



        public (List<string> EntityTypes, List<int> TypeCounts) GetTopEntityTypes(List<Statement> statementNERs, int topCount = 10)
        {

            var topEntities = statementNERs
                .SelectMany(st => st.Entities)
                .GroupBy(e => e.Type)
                .Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
                .OrderByDescending(kvp => kvp.Value)
                .Take(topCount)
                .ToList();


            var entityTypes = topEntities.Select(kvp => entity_mapping[kvp.Key]).ToList();
            var typeCounts = topEntities.Select(kvp => kvp.Value).ToList();

            return (entityTypes, typeCounts);
        }

        public Dictionary<string, double> CalculateAverageSentiment(List<Statement> statements)
        {
            return statements
                .SelectMany(statement => statement.Entities.Select(entity => new
                {
                    EntityText = entity.EntityText,
                    Sentiment = statement.Sentiment
                }))
                .GroupBy(e => e.EntityText)
                .ToDictionary(
                    group => group.Key,
                    group => group.Average(e => e.Sentiment)
                );
        }

        public List<EntitySentimentData> CalculateAverageSentimentType(List<Statement> statements, List<string> selectedEntityTypes, int minCount = 40)
        {
            var result = new List<EntitySentimentData>();

            var entitySentiments = new Dictionary<(string, string), List<double>>();
            var entityCounts = new Dictionary<(string, string), int>();

            foreach (var statement in statements)
            {
                foreach (var entity in statement.Entities)
                {
                    if (!selectedEntityTypes.Contains(entity.Type))
                    {
                        continue;
                    }

                    var entityKey = (entity.EntityText, entity.Type);

                    if (!entitySentiments.ContainsKey(entityKey))
                    {
                        entitySentiments[entityKey] = new List<double>();
                    }
                    entitySentiments[entityKey].Add(statement.SentimentBert);

                    if (!entityCounts.ContainsKey(entityKey))
                    {
                        entityCounts[entityKey] = 0;
                    }
                    entityCounts[entityKey]++;
                }
            }

            var groupedByType = entitySentiments
                .GroupBy(e => e.Key.Item2)
                .ToList();

            foreach (var entityGroup in groupedByType)
            {
                var topEntities = entityCounts
                    .Where(e => e.Key.Item2 == entityGroup.Key)
                    .Where(e => e.Value >= minCount)
                    .OrderByDescending(e => e.Value)
                    .Select(e => e.Key)
                    .ToList();

                foreach (var entityKey in topEntities)
                {
                    var averageSentiment = entitySentiments[entityKey].Average();

                    result.Add(new EntitySentimentData
                    {
                        EntityName = entityKey.Item1,
                        EntityType = entity_mapping[entityKey.Item2],
                        AverageSentiment = averageSentiment,
                        Sentiments = entitySentiments[entityKey]
                    });
                }
            }

            var allEntityCounts = entityCounts
                .GroupBy(e => e.Key.Item1)
                .Select(g => new
                {
                    EntityName = g.Key,
                    TotalCount = g.Sum(e => e.Value),
                    Sentiments = g.SelectMany(e => entitySentiments[e.Key]).ToList()
                })
                .Where(e => e.TotalCount >= minCount)
                .OrderByDescending(e => e.TotalCount)
                .ToList();

            foreach (var entity in allEntityCounts)
            {
                var averageSentiment = entity.Sentiments.Average();

                result.Add(new EntitySentimentData
                {
                    EntityName = entity.EntityName,
                    EntityType = "Všechny",
                    AverageSentiment = averageSentiment,
                    Sentiments = entity.Sentiments
                });
            }

            return result;
        }






        public (List<string> TopEntities, int[,] Matrix) CalculateCooccurrenceMatrix(
          List<Statement> statements,
          int topN = 500)
        {

            var entityFrequencies = statements
                .SelectMany(s => s.Entities)
                .GroupBy(e => e.EntityText)
                .Select(g => new { Entity = g.Key, Count = g.Count() })
                .OrderByDescending(e => e.Count)
                .Take(topN)
                .ToList();

            var topEntities = entityFrequencies.Select(e => e.Entity).ToList();


            var matrixSize = topEntities.Count;
            var cooccurrenceMatrix = new int[matrixSize, matrixSize];

            foreach (var statement in statements)
            {
                var entitiesInStatement = statement.Entities
                    .Select(e => e.EntityText)
                    .Where(e => topEntities.Contains(e))
                    .Distinct()
                    .ToList();

                for (int i = 0; i < entitiesInStatement.Count; i++)
                {
                    for (int j = i + 1; j < entitiesInStatement.Count; j++)
                    {
                        int row = topEntities.IndexOf(entitiesInStatement[i]);
                        int col = topEntities.IndexOf(entitiesInStatement[j]);
                        cooccurrenceMatrix[row, col]++;
                        cooccurrenceMatrix[col, row]++;
                    }
                }
            }

            return (topEntities, cooccurrenceMatrix);
        }

        public (List<string> TopEntities, double[,] Matrix) CalculateCooccurrenceMatrixNormalized(
    List<Statement> statements,
    int topN = 500)
        {

            var entityFrequencies = statements
                .SelectMany(s => s.Entities)
                .GroupBy(e => e.EntityText)
                .Select(g => new { Entity = g.Key, Count = g.Count() })
                .OrderByDescending(e => e.Count)
                .Take(topN)
                .ToList();

            var topEntities = entityFrequencies.Select(e => e.Entity).ToList();


            var matrixSize = topEntities.Count;
            var cooccurrenceMatrix = new double[matrixSize, matrixSize];


            foreach (var statement in statements)
            {
                var entitiesInStatement = statement.Entities
                    .Select(e => e.EntityText)
                    .Where(e => topEntities.Contains(e))
                    .Distinct()
                    .ToList();

                for (int i = 0; i < entitiesInStatement.Count; i++)
                {
                    for (int j = i + 1; j < entitiesInStatement.Count; j++)
                    {
                        int row = topEntities.IndexOf(entitiesInStatement[i]);
                        int col = topEntities.IndexOf(entitiesInStatement[j]);


                        double frequencyI = entityFrequencies.First(e => e.Entity == entitiesInStatement[i]).Count;
                        double frequencyJ = entityFrequencies.First(e => e.Entity == entitiesInStatement[j]).Count;


                        cooccurrenceMatrix[row, col] += 1.0 / (Math.Sqrt(frequencyI * frequencyJ));
                        cooccurrenceMatrix[col, row] += 1.0 / (Math.Sqrt(frequencyI * frequencyJ));
                    }
                }
            }

            return (topEntities, cooccurrenceMatrix);
        }



        public (List<string> TopEntities, double[,] Matrix) CalculateCooccurrenceMatrixNormalizedJC(
    List<Statement> statements,
    int topN = 500)
        {
            var entityFrequencies = statements
                .SelectMany(s => s.Entities)
                .GroupBy(e => e.EntityText)
                .Select(g => new { Entity = g.Key, Count = g.Count() })
                .OrderByDescending(e => e.Count)
                .Take(topN)
                .ToList();

            var topEntities = entityFrequencies.Select(e => e.Entity).ToList();
            var matrixSize = topEntities.Count;
            var cooccurrenceMatrix = new double[matrixSize, matrixSize];


            var cooccurrenceCounts = new Dictionary<(int, int), int>();
            var entityStatementCounts = new Dictionary<int, int>();

            foreach (var statement in statements)
            {
                var entitiesInStatement = statement.Entities
                    .Select(e => e.EntityText)
                    .Where(e => topEntities.Contains(e))
                    .Distinct()
                    .ToList();

                var entityIndices = entitiesInStatement
                    .Select(e => topEntities.IndexOf(e))
                    .ToList();


                foreach (var idx in entityIndices)
                {
                    if (!entityStatementCounts.ContainsKey(idx))
                        entityStatementCounts[idx] = 0;
                    entityStatementCounts[idx]++;
                }

                for (int i = 0; i < entityIndices.Count; i++)
                {
                    for (int j = i + 1; j < entityIndices.Count; j++)
                    {
                        var key = (entityIndices[i], entityIndices[j]);

                        if (!cooccurrenceCounts.ContainsKey(key))
                            cooccurrenceCounts[key] = 0;

                        cooccurrenceCounts[key]++;
                    }
                }
            }

            foreach (var pair in cooccurrenceCounts)
            {
                int i = pair.Key.Item1;
                int j = pair.Key.Item2;
                double intersection = pair.Value;
                double union = entityStatementCounts[i] + entityStatementCounts[j] - intersection;

                cooccurrenceMatrix[i, j] = intersection / union;
                cooccurrenceMatrix[j, i] = cooccurrenceMatrix[i, j];
            }

            return (topEntities, cooccurrenceMatrix);
        }
        public void SaveMatrixToCsv(
    List<string> topEntities, int[,] matrix, string filePath)
        {
            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {

                writer.Write(";");
                writer.WriteLine(string.Join(";", topEntities));

                for (int i = 0; i < topEntities.Count; i++)
                {

                    writer.Write(topEntities[i] + ";");
                    for (int j = 0; j < topEntities.Count; j++)
                    {
                        writer.Write(matrix[i, j]);
                        if (j < topEntities.Count - 1) writer.Write(";");
                    }
                    writer.WriteLine();
                }
            }
        }
        public void SaveCooccurrenceMatrixToCsv(List<string> topEntities, int[,] cooccurrenceMatrix, string filePath)
        {
            using (var writer = new StreamWriter(filePath))
            {

                writer.WriteLine("Source;Target;Weight");


                for (int i = 0; i < topEntities.Count; i++)
                {
                    for (int j = i + 1; j < topEntities.Count; j++)
                    {

                        if (cooccurrenceMatrix[i, j] > 20)
                        {
                            writer.WriteLine($"{topEntities[i]};{topEntities[j]};{cooccurrenceMatrix[i, j].ToString(CultureInfo.InvariantCulture)}");
                        }
                    }
                }
            }
        }


        public Dictionary<string, Dictionary<string, Dictionary<string, int>>> GetTopEntitiesPoliticPerYear(List<Statement> statements)
        {
            var result = new Dictionary<string, Dictionary<string, Dictionary<string, int>>>();

            foreach (var statement in statements)
            {
                if (statement.Entities != null && statement.datum.HasValue)
                {
                    int year = statement.datum.Value.Year;
                    int month = statement.datum.Value.Month;


                    string quarter = year + "Q" + ((month - 1) / 6 + 1);

                    foreach (var entity in statement.Entities)
                    {
                        if (!string.IsNullOrEmpty(entity.EntityText))
                        {
                            if (!result.ContainsKey(statement.osobaid.politic_id))
                            {
                                result[statement.osobaid.politic_id] = new Dictionary<string, Dictionary<string, int>>();
                            }

                            if (!result[statement.osobaid.politic_id].ContainsKey(quarter))
                            {
                                result[statement.osobaid.politic_id][quarter] = new Dictionary<string, int>();
                            }


                            if (!result[statement.osobaid.politic_id][quarter].ContainsKey(entity.EntityText))
                            {
                                result[statement.osobaid.politic_id][quarter][entity.EntityText] = 0;
                            }

                            result[statement.osobaid.politic_id][quarter][entity.EntityText]++;
                        }
                    }
                }
            }

            foreach (var osobaId in result.Keys.ToList())
            {
                foreach (var quarter in result[osobaId].Keys.ToList())
                {
                    result[osobaId][quarter] = result[osobaId][quarter]
                        .OrderByDescending(kv => kv.Value)
                        .Take(20)
                        .ToDictionary(kv => kv.Key, kv => kv.Value);
                }
            }

            return result;
        }


        public Dictionary<string, List<string>> GetTopEntitiesPolitic(List<Statement> statements, string politician)
        {

            var result = new Dictionary<string, List<string>>();


            foreach (var statement in statements)
            {

                if (statement.Entities != null)
                {

                    foreach (var entity in statement.Entities)
                    {
                        if (!string.IsNullOrEmpty(entity.EntityText))
                        {

                            if (!result.ContainsKey(statement.osobaid.politic_id))
                            {
                                result[statement.osobaid.politic_id] = new List<string>();
                            }


                            result[statement.osobaid.politic_id].Add(entity.EntityText);
                        }
                    }
                }
            }


            foreach (var key in result.Keys.ToList())
            {
                result[key] = result[key]
                    .GroupBy(e => e)
                    .OrderByDescending(g => g.Count())
                    .Take(20)
                    .Select(g => g.Key)
                    .ToList();
            }

            return result;
        }









    }

}

