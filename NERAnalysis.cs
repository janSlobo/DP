using Microsoft.AspNetCore.Mvc;
using PoliticStatements.Models;
using System.Data.SqlClient;
using static PoliticStatements.SentimentAnalysis;
using Accord.Math;
using Accord.Statistics.Analysis;
using System.Text;
using System.Globalization;
namespace PoliticStatements
{
    public class NERAnalysis
    {

        public Dictionary<string, string> entity_mapping { get; set; }


        public Dictionary<int, int> GetStatementsPerYear(List<Statement> statements)
        {
            return statements
                .GroupBy(s => s.datum.Value.Year) // Seskupíme podle roku
                .ToDictionary(g => g.Key, g => g.Count()); // Vytvoříme slovník: rok → počet vyjádření
        }


        public List<EntityFrequency> PoliticEntityPieChart(List<Statement> st,List<string> nertypes, string  currentEntity="")
        {
            var entityFrequency = new Dictionary<string, int>();

           
            foreach (var statement in st)
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


        public  Dictionary<string, List<string>> GetTopEntitiesPerPerson(List<Statement> statements)
        {
            return statements
                .Where(s => s.Entities != null) 
                .SelectMany(s => s.Entities.Where(e=>e.Type=="ps").Select(e => new { s.osobaid, e.EntityText }))
                .GroupBy(x => new { x.osobaid, x.EntityText })
                .Select(g => new { g.Key.osobaid, g.Key.EntityText, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .GroupBy(x => x.osobaid)
                .ToDictionary(
                    g => g.Key,
                    g => g.Take(10).Select(x => x.EntityText).ToList()
                );
        }
        public  Dictionary<string, List<List<Statement>>> GetStatementsPerTopEntity(List<Statement> statements, Dictionary<string, List<string>> topEntities)
        {
            return topEntities.ToDictionary(
               kvp => kvp.Key, 
               kvp => kvp.Value
           .Select(entity => statements
               .Where(s => s.osobaid == kvp.Key && s.Entities.Any(e => e.EntityText == entity))
               .ToList()
           )
           .ToList()
   );
        }

        
        public async Task UpdateNER()
        {
            using (SqlConnection connection = new SqlConnection(GlobalConfig.connstring))
            {
                connection.Open();

                foreach (string line in File.ReadLines("C:/Users/HONZA/Desktop/diplomka/lemma_entities_1.txt"))
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
        public Dictionary<string, double> CalculateEntityToWordRatio(List<Statement> statements)
        {
            var politicianRatios = new Dictionary<string, double>();

            
            var groupedStatements = statements.GroupBy(s => s.osobaid);

            foreach (var group in groupedStatements)
            {
                string politicianId = group.Key;

                
                double totalWords = group.Sum(s => s.pocetSlov ?? 0); 
                double totalEntities = group.Sum(s => s.Entities.Count); 

                
                double ratio = totalWords > 0 ? totalEntities/totalWords : 0;

                
                politicianRatios[politicianId] = ratio;
            }

            return politicianRatios;
        }

        public List<StatementNER> LoadNERCsv()
        {
            var statements = new List<StatementNER>();

            foreach (var line in File.ReadLines("C:/Users/HONZA/Desktop/diplomka/NER_mix.csv"))
            {

                var parts = line.Split(';');
                if (parts.Length < 2) continue;

                var statementId = parts[0];
                var entities = new List<EntityData>();

                for (int i = 1; i < parts.Length; i++)
                {
                    var entityParts = parts[i].Split(',');
                    if (entityParts.Length == 2)
                    {
                        entities.Add(new EntityData
                        {
                            EntityText = entityParts[0].Trim(),
                            Type = entityParts[1].Trim()
                        });
                    }
                }

                statements.Add(new StatementNER
                {
                    StatementId = statementId,
                    Entities = entities
                });
            }

            return statements;
        }
        public async Task SaveNERToDB()
        {
            List<StatementNER> statementNERs = LoadNERCsv();
            using (SqlConnection conn = new SqlConnection(GlobalConfig.connstring))
            {
                await conn.OpenAsync();

                foreach (var st in statementNERs)
                {

                    foreach (var entity in st.Entities)
                    {
                        string st_id = st.StatementId;
                        string entity_name = entity.EntityText;
                        string type=entity.Type;
                        

                        string query = "INSERT INTO Entity (EntityName, EntityType, StatementID) " +
                                       "VALUES (@EntityName, @EntityType, @StatementID) ";


                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@EntityName", entity_name);
                            cmd.Parameters.AddWithValue("@EntityType", type);
                            cmd.Parameters.AddWithValue("@StatementID", st_id);
                            

                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }
            }

        }


        public async Task DELETENERToDB()
        {
            List<StatementNER> statementNERs = LoadNERCsv();
            using (SqlConnection conn = new SqlConnection(GlobalConfig.connstring))
            {
                await conn.OpenAsync();

                foreach (var st in statementNERs)
                {
                    string query = "DELETE FROM Entity where StatementID=@id ";
                                      


                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                       
                        cmd.Parameters.AddWithValue("@id", st.StatementId);


                        await cmd.ExecuteNonQueryAsync();
                    }
                   
                }
            }

        }
        public async Task LoadNERFromDB(List<Statement> st)
        {
            // Vytvoření slovníku pro rychlý přístup k Statement podle ID
            Dictionary<string, Statement> statementsDict = st.ToDictionary(s => s.id);

            using (SqlConnection conn = new SqlConnection(GlobalConfig.connstring))
            {
                await conn.OpenAsync();

                string query = "SELECT e.EntityName, e.EntityType, e.StatementID " +
                               "FROM Entity e " +
                               "INNER JOIN Statement s ON s.id = e.StatementID " +
                               "WHERE s.jazyk LIKE 'cs' " +
                               "AND e.EntityType IN ('ps','io','if','ic','mn','ms','o_','oa','gl','gu')";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        // Hromadné přidávání entit do statementů
                        while (await reader.ReadAsync())
                        {
                            string entityName = reader.GetString(reader.GetOrdinal("EntityName"));
                            string entityType = reader.GetString(reader.GetOrdinal("EntityType"));
                            string statementId = reader.GetString(reader.GetOrdinal("StatementID"));

                            // Rychlý přístup ke statementu pomocí slovníku
                            if (statementsDict.TryGetValue(statementId, out Statement statement))
                            {
                                // Inicializace seznamu entit, pokud je null
                                statement.Entities ??= new List<EntityData>();

                                // Přidání entity
                                statement.Entities.Add(new EntityData
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
            List<string> selectedEntityTypes)
        {
            var groupedEntities = statementNERs
                .SelectMany(st => st.Entities)
                .Where(e => selectedEntityTypes.Contains(e.Type)) 
                .GroupBy(e => e.Type) 
                .ToDictionary(
                    g => entity_mapping[g.Key],  
                    g => g
                        .GroupBy(e => e.EntityText)  
                        .Select(e => new KeyValuePair<string, int>(e.Key, e.Count()))  
                        .OrderByDescending(e => e.Value) 
                        .Take(20)  
                        .ToList() 
                );

            return groupedEntities;
        }

        public (List<string> EntityTypes, List<int> TypeCounts) GetTopEntityTypes(List<Statement> statementNERs, int topCount = 6)
        {
            // Počítáme výskyt EntityType
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

        public  Dictionary<string, double> CalculateAverageSentiment(List<Statement> statements)
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

        public List<EntitySentimentData> CalculateAverageSentimentType(List<Statement> statements)
        {
            
            var result = new List<EntitySentimentData>();

           
            var entitySentiments = new Dictionary<(string, string), List<double>>();

            
            var entityCounts = new Dictionary<(string, string), int>();

           
            foreach (var statement in statements)
            {
                foreach (var entity in statement.Entities)
                {
                    var entityKey = (entity.EntityText, entity.Type);

                    
                    if (!entitySentiments.ContainsKey(entityKey))
                    {
                        entitySentiments[entityKey] = new List<double>();
                    }
                    entitySentiments[entityKey].Add(statement.Sentiment);

                    
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
                    .OrderByDescending(e => e.Value)
                    .Take(20) 
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

            return result;
        }


        public  (List<string> TopEntities, int[,] Matrix) CalculateCooccurrenceMatrix(
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

                        
                        cooccurrenceMatrix[row, col] += 1.0 / (Math.Sqrt( frequencyI * frequencyJ));
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

            // Vypočítání Jaccardova koeficientu
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
        public  void SaveMatrixToCsv(
    List<string> topEntities, double[,] matrix, string filePath)
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
                        writer.Write(Math.Round(matrix[i, j],5));
                        if (j < topEntities.Count - 1) writer.Write(";");
                    }
                    writer.WriteLine();
                }
            }
        }
        public void SaveCooccurrenceMatrixToCsv(List<string> topEntities, double[,] cooccurrenceMatrix, string filePath)
        {
            using (var writer = new StreamWriter(filePath))
            {
               
                writer.WriteLine("Source;Target;Weight");

                
                for (int i = 0; i < topEntities.Count; i++)
                {
                    for (int j = i + 1; j < topEntities.Count; j++)
                    {
                        
                        if (cooccurrenceMatrix[i, j] > 0.05)
                        {
                            writer.WriteLine($"{topEntities[i]};{topEntities[j]};{cooccurrenceMatrix[i, j].ToString(CultureInfo.InvariantCulture)}");
                        }
                    }
                }
            }
        }


        public Dictionary<string, Dictionary<string, Dictionary<string, int>>> GetTopEntitiesPoliticPerYear(List<Statement> statements)
        {
            // Výsledek: osobaID -> rok -> {entita -> počet výskytů}
            var result = new Dictionary<string, Dictionary<string, Dictionary<string, int>>>();

            foreach (var statement in statements)
            {
                if (statement.Entities != null && statement.datum.HasValue)
                {
                    int year = statement.datum.Value.Year;
                    int month = statement.datum.Value.Month; // Předpokládáme, že klíčem je měsíc

                    
                    string quarter = year + "Q" + ((month - 1) / 6 + 1);

                    foreach (var entity in statement.Entities)
                    {
                        if (!string.IsNullOrEmpty(entity.EntityText))
                        {
                            // Inicializace struktury, pokud ještě neexistuje
                            if (!result.ContainsKey(statement.osobaid))
                            {
                                result[statement.osobaid] = new Dictionary<string, Dictionary<string, int>>();
                            }

                            if (!result[statement.osobaid].ContainsKey(quarter))
                            {
                                result[statement.osobaid][quarter] = new Dictionary<string, int>();
                            }

                            // Zvýšení počtu výskytů dané entity
                            if (!result[statement.osobaid][quarter].ContainsKey(entity.EntityText))
                            {
                                result[statement.osobaid][quarter][entity.EntityText] = 0;
                            }

                            result[statement.osobaid][quarter][entity.EntityText]++;
                        }
                    }
                }
            }

            // Zpracování výsledků: uchování pouze top 20 entit podle počtu výskytů
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


        public Dictionary<string, List<string>> GetTopEntitiesPolitic(List<Statement> statements,string politician)
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
                            
                            if (!result.ContainsKey(statement.osobaid))
                            {
                                result[statement.osobaid] = new List<string>();
                            }

                            
                            result[statement.osobaid].Add(entity.EntityText);
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

        [Obsolete]
        public  void GenerateVectors(List<Statement> statements, string outputFile)
        {

            var allEntities = statements
        .SelectMany(s => s.Entities)
        .Select(e => e.EntityText)
        .Distinct()
        .OrderBy(e => e)
        .ToList();

            // Krok 2: Generování vektorů pro každého politika
            var politicianVectors = new Dictionary<string, List<int>>();
            foreach (var statement in statements)
            {
                if (string.IsNullOrEmpty(statement.osobaid)) continue;

                if (!politicianVectors.ContainsKey(statement.osobaid))
                    politicianVectors[statement.osobaid] = new List<int>(new int[allEntities.Count]);

                foreach (var entity in statement.Entities)
                {
                    var index = allEntities.IndexOf(entity.EntityText);
                    if (index >= 0)
                    {
                        politicianVectors[statement.osobaid][index]++;
                    }
                }
            }

            var matrix = politicianVectors.Values
           .Select(v => v.Select(i => (double)i).ToArray()) // Konverze z int na double
           .ToArray();

            // Krok 4: Aplikace PCA
            var pca = new PrincipalComponentAnalysis(matrix, AnalysisMethod.Center);
            pca.Compute();

            // Redukce dimenzí
            var reducedMatrix = pca.Transform(matrix, dimensions: 50);

            // Krok 5: Zapsání redukovaných vektorů do souboru
            using (var writer = new StreamWriter(outputFile))
            {
                int i = 0;
                foreach (var kvp in politicianVectors)
                {
                    var reducedVector = reducedMatrix.GetRow(i);
                    var vectorString = string.Join(",", reducedVector.Select(v => v.ToString("F4")));
                    writer.WriteLine($"{kvp.Key};{vectorString}");
                    i++;
                }
            }
        }

        
    public NERAnalysis()
        {
            entity_mapping = new Dictionary<string, string>
            {
                { "io", "Politické instituce" },
                {"ps","Příjmení" },
                {"gc","Státy" },
                {"mn","Periodical" },
                {"gu","Města" },
                {"ty","Roky" },
                {"ic","Kult./Vzděl./Vědec. instituce" },
                {"if","Firmy" },
                {"P","Osoba" },
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
    }


    




}

