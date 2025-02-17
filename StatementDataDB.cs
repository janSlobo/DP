using Microsoft.AspNetCore.Mvc;
using PoliticStatements.Models;
using System.Data.SqlClient;
using static PoliticStatements.SentimentAnalysis;
using System.Text.RegularExpressions;
using CsvHelper;
using System.Globalization;
using CsvHelper.Configuration;
using System.Text;
using static PoliticStatements.StatementDataDB;
namespace PoliticStatements
{
    public class StatementDataDB
    {
        public class StatementData
        {
            public string StatementId { get; set; }
            public double Logos { get; set; }
            public double Pathos { get; set; }
            public double Ethos { get; set; }
            public double Manipulace { get; set; }
            public double Populismus { get; set; }
        }
        public class ClusterData
        {
            public string id { get; set; }
            public int Cluster { get; set; }
        }

      
    public void UpdateClustersFromCsv(string csvFilePath, List<Statement> st)
        {
           
            var clusters = new List<ClusterData>();

            using (var reader = new StreamReader(csvFilePath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                clusters = csv.GetRecords<ClusterData>().ToList();
            }

         
            foreach (var clusterData in clusters)
            {
               
                var statement = st.FirstOrDefault(s => s.id == clusterData.id);
                if (statement != null)
                {
                    statement.cluster = clusterData.Cluster;
                }
            }

            
            Console.WriteLine("Aktualizace clusterů dokončena.");
        }

    public void UpdateClustersFromCsvDB(string csvFilePath)
        {

            var clusters = new List<ClusterData>();

            using (var reader = new StreamReader(csvFilePath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                clusters = csv.GetRecords<ClusterData>().ToList();
            }


            using (var connection = new SqlConnection(GlobalConfig.connstring))
            {
                connection.Open();

                foreach (var clusterData in clusters)
                {
                    string updateQuery = "UPDATE Statement SET cluster = @cluster WHERE id = @id";
                    using (var command = new SqlCommand(updateQuery, connection))
                    {

                        command.Parameters.AddWithValue("@cluster", clusterData.Cluster);
                        command.Parameters.AddWithValue("@id", clusterData.id);


                        command.ExecuteNonQuery();
                    }
                }
            }


        }
        private double ParseDouble(string value)
        {
            
            return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result) ? result : 0.0;
        }

        public async Task InsertTopicsFromFile(string filePath)
        {
            var lines = File.ReadAllLines(filePath);

            foreach (var line in lines)
            {
                
                var columns = ParseCsvLine(line);

                if (columns.Length < 3) continue; 

                string statementId = columns[0].Trim('"');
                string topics = columns[1].Trim('"'); 
                string keywords = columns[2].Trim('"'); 

                try
                {
                    
                    var topicList = topics.Split(',').Select(t => t.Trim()).ToList();

                    foreach (var topic in topicList)
                    {
                       await InsertTopic(statementId, topic);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Chyba při vkládání tématu pro StatementId {statementId}: {ex.Message}");
                }
                /*try
                {

                    var kwList = keywords.Split(',').Select(t => t.Trim()).ToList();

                    foreach (var kw in kwList)
                    {
                        await InsertKW(statementId, kw);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Chyba při vkládání tématu pro StatementId {statementId}: {ex.Message}");
                }*/
            }
        }

        
        private string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var currentField = new StringBuilder();
            bool insideQuote = false;

            foreach (var c in line)
            {
                if (c == '"')
                {
                    insideQuote = !insideQuote; 
                }
                else if (c == ',' && !insideQuote)
                {
                    result.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }

            result.Add(currentField.ToString()); 
            return result.ToArray();
        }

        private async Task InsertTopic(string statementId, string theme)
        {
            using (var connection = new SqlConnection(GlobalConfig.connstring))
            {
                await connection.OpenAsync();

                string query = "INSERT INTO Topic (StatementId, Topic) VALUES (@StatementId, @Topic)";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@StatementId", statementId);
                    command.Parameters.AddWithValue("@Topic", theme);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }
        private async Task InsertKW(string statementId, string kw)
        {
            using (var connection = new SqlConnection(GlobalConfig.connstring))
            {
                await connection.OpenAsync();

                string query = "INSERT INTO KeyWords (StatementId, KeyWord) VALUES (@StatementId, @KW)";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@StatementId", statementId);
                    command.Parameters.AddWithValue("@KW", kw);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }
        public async Task UpdateStatementsFromCsv(string csvFilePath)
        {

            var records = new List<StatementData>();

            try
            {
                // Načteme celý soubor jako text
                var lines = File.ReadAllLines(csvFilePath);

                // Předpokládáme, že první řádek obsahuje hlavičky
                var headers = lines[0].Split(';');

                // Procházení každým řádkem (počínaje druhým, protože první je hlavička)
                for (int i = 1; i < lines.Length; i++)
                {
                    var values = lines[i].Split(';');

                    // Ujistíme se, že máme správný počet sloupců
                    if (values.Length == headers.Length)
                    {
                        var record = new StatementData
                        {
                            StatementId = values[0],
                            Logos = ParseDouble(values[1]),
                            Pathos = ParseDouble(values[2]),
                            Ethos = ParseDouble(values[3]),
                            Manipulace = ParseDouble(values[4]),
                            Populismus = ParseDouble(values[5])
                        };

                        records.Add(record);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba při čtení CSV souboru: {ex.Message}");
            }

            using (var connection = new SqlConnection(GlobalConfig.connstring))
            {
                connection.Open();

                foreach (var r in records)
                {
                    string query = "UPDATE Statement SET logos=@logos,pathos=@pathos,ethos=@ethos,populism=@populism,manipulation=@manipulation WHERE id=@id";

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@id", r.StatementId);
                        cmd.Parameters.AddWithValue("@logos", r.Logos);
                        cmd.Parameters.AddWithValue("@pathos", r.Pathos);
                        cmd.Parameters.AddWithValue("@ethos", r.Ethos);
                        
                        cmd.Parameters.AddWithValue("@populism", r.Populismus);
                        cmd.Parameters.AddWithValue("@manipulation", r.Manipulace);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
        }
        static List<string> GetNamesWithNumbers(List<string> names)
        {
            List<string> result = new List<string>();
            Regex regex = new Regex(@"-\d+$"); // Regulární výraz pro číslo na konci

            foreach (var name in names)
            {
                if (regex.IsMatch(name)) // Pokud jméno končí číslem
                {
                    result.Add(name);
                }
            }

            return result;
        }

        
        static List<string> GetValidNames(List<string> allNames, List<string> namesWithNumbers)
        {
            List<string> validNames = new List<string>();
            foreach (var nameWithNumber in namesWithNumbers)
            {
                string baseName = Regex.Replace(nameWithNumber, @"-(\d+)$", "");

                if (allNames.Contains(baseName)) // Pokud základní jméno existuje v původním seznamu
                {
                    validNames.Add(nameWithNumber);
                }
            }

            return validNames;
        }
        public async Task  RemoveDuplicateMentions(List<Statement> statements)
        {
            foreach (var statement in statements)
            {

                List<string> namesWithNumbers = GetNamesWithNumbers(statement.politicizminky);

                
                List<string> validNames = GetValidNames(statement.politicizminky, namesWithNumbers);

               
                using (var conn = new SqlConnection(GlobalConfig.connstring))
                {
                    await conn.OpenAsync();
                    foreach (var s in validNames)
                    {


                        string query = "DELETE FROM Mentions where statement_id=@id and politic_id=@id_p";

                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", statement.id);
                            cmd.Parameters.AddWithValue("@id_p", s);


                            await cmd.ExecuteNonQueryAsync();
                        }

                    }
                }

            }
        }
        public async Task UpdateStatements(List<Statement> st)
        {
            var groups = st
            .GroupBy(s => new { s.osobaid, s.datum.Value.Date, text = s.text.Trim().ToLower() })
            .Where(g => g.Count() > 1)
            .OrderByDescending(g => g.Count())  
            .ToList();



            var deleteList = new List<Statement>();

            foreach (var group in groups)
            {
                var keep1 = group.Where(s => s.Sentiment != 666 && s.Entities.Count() != 0 && s.politicizminky.Count() != 0).ToList();
                if (keep1.Count() == 0)
                {
                    var keep = group.Where(s => s.Sentiment != 666 || s.Entities.Count() != 0 || s.politicizminky.Count() != 0).ToList();
                    if (!keep.Any() && group.Any())
                    {
                        keep.Add(group.First());
                    }
                    deleteList.AddRange(group.Where(s => !keep.Any(k => k.id == s.id)).ToList());
                }
                deleteList.AddRange(group.Where(s => !keep1.Any(k => k.id == s.id)).ToList());
            }


            using (SqlConnection conn = new SqlConnection(GlobalConfig.connstring))
            {
                await conn.OpenAsync();

                if (deleteList.Any())
                {
                    string ids = string.Join(",", deleteList.Select(d => $"'{d.id}'"));
                    string query = $"DELETE FROM Statement WHERE id IN ({ids})";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }


        }

        public async Task CalculateAndSaveStatementFrequency(List<Statement> st,bool mentions, string? server = null)
        {
           
            List<Statement> sortedStatements;
            if (server == null)
            {
                sortedStatements = st.OrderBy(s => s.osobaid).ToList();
            }
            else
            {
                sortedStatements = st.Where(s => s.server == server)
                                     .OrderBy(s => s.osobaid)
                                     .ToList();
            }

            var statementCountByPerson = new Dictionary<string, int>();

            foreach (var statement in sortedStatements)
            {
                if (!statementCountByPerson.ContainsKey(statement.osobaid))
                {
                    statementCountByPerson[statement.osobaid] = 0;
                }
                statementCountByPerson[statement.osobaid]++;
            }

            var frequencyData = statementCountByPerson.GroupBy(pair => pair.Value)
                                                      .Select(group => new
                                                      {
                                                          StatementCount = group.Key,
                                                          PersonCount = group.Count()
                                                      })
                                                      .OrderBy(data => data.StatementCount)
                                                      .ToList();

     
            using (var connection = new SqlConnection(GlobalConfig.connstring))
            {
                await connection.OpenAsync();

                foreach (var data in frequencyData)
                {
                    using (var command = new SqlCommand(
                        "INSERT INTO StatementFrequency (StatementCount, PersonCount, Server,type) VALUES (@StatementCount, @PersonCount, @Server,@Type)",
                        connection))
                    {
                        command.Parameters.AddWithValue("@StatementCount", data.StatementCount);
                        command.Parameters.AddWithValue("@PersonCount", data.PersonCount);
                        command.Parameters.AddWithValue("@Server", server ?? "all");
                        command.Parameters.AddWithValue("@Type", mentions? 1 : 0);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        public async Task SaveMentionsPerPoliticFrequency(List<Statement> statements)
        {
            // Slovník pro počítání zmínek
            Dictionary<string, int> mentionsCount = new Dictionary<string, int>();

            // Počítání zmínek
            foreach (var statement in statements)
            {
                if (statement.politicizminky != null && statement.politicizminky.Count() != 0)
                {
                    if (!mentionsCount.ContainsKey(statement.osobaid))
                    {
                        mentionsCount[statement.osobaid] = statement.politicizminky.Count();
                    }
                    else
                    {
                        mentionsCount[statement.osobaid] += statement.politicizminky.Count();
                    }
                }
            }

            // Slovník pro uložení počtu politiků na základě počtu zmínek
            Dictionary<int, int> politiciansCountByMentions = new Dictionary<int, int>();
            foreach (var count in mentionsCount.Values)
            {
                if (!politiciansCountByMentions.ContainsKey(count))
                    politiciansCountByMentions[count] = 0;

                politiciansCountByMentions[count]++;
            }

            // Uložení výsledků do databáze
            using (SqlConnection connection = new SqlConnection(GlobalConfig.connstring))
            {
                await connection.OpenAsync();

                // Uložení každého záznamu do databázové tabulky
                foreach (var kv in politiciansCountByMentions)
                {
                    string query = "INSERT INTO PoliticMentionsFrequency (CountOfPoliticians, CountOfMentions) VALUES (@CountOfPoliticians, @CountOfMentions)";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@CountOfPoliticians", kv.Value);
                        command.Parameters.AddWithValue("@CountOfMentions", kv.Key);

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
        }

       

        public async Task SaveMentionsOfPoliticianFrequency(List<Statement> statements, string? server=null)
        {
            // Příprava dat pro výpočet
            Dictionary<string, int> mentionCounts = new Dictionary<string, int>();

            // Filtr serveru
            if (server != null)
            {
                statements = statements.Where(s => s.server == server).ToList();
            }
            

            // Výpočet zmínek politiků
            foreach (var statement in statements)
            {
                foreach (var politician in statement.politicizminky)
                {
                    if (!mentionCounts.ContainsKey(politician))
                    {
                        mentionCounts[politician] = 1;
                    }
                    else
                    {
                        mentionCounts[politician]++;
                    }
                }
            }

            // Sestavení výsledného slovníku podle četností zmínek
            Dictionary<int, int> frequencyCounts = new Dictionary<int, int>();
            foreach (var count in mentionCounts.Values)
            {
                if (!frequencyCounts.ContainsKey(count))
                {
                    frequencyCounts[count] = 1;
                }
                else
                {
                    frequencyCounts[count]++;
                }
            }

            // Uložení výsledků do databáze
            using (var connection = new SqlConnection(GlobalConfig.connstring))
            {
                await connection.OpenAsync();

                foreach (var entry in frequencyCounts)
                {
                    var countOfMentions = entry.Key;
                    var countOfPoliticians = entry.Value;

                    using (var command = new SqlCommand("INSERT INTO MentionsOfPoliticians (CountOfMentions, CountOfPoliticians, Server) VALUES (@CountOfMentions, @CountOfPoliticians, @Server)", connection))
                    {
                        command.Parameters.AddWithValue("@CountOfMentions", countOfMentions);
                        command.Parameters.AddWithValue("@CountOfPoliticians", countOfPoliticians);
                        command.Parameters.AddWithValue("@Server", server ?? "all");

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        public async Task CalculateAndStoreWordsStatementFrequency(List<Statement> data,bool type, string? server = null)
        {
            var wordCountStatements = new List<object>();

            var groupedData = server != null
                ? data.Where(s => s.server == server)
                : data;

            var frequencyData = groupedData
                .GroupBy(s => s.pocetSlov ?? 0)
                .Select(g => new
                {
                    WordCount = g.Key,
                    StatementsCount = g.Count(),
                        
                });

            using (var connection = new SqlConnection(GlobalConfig.connstring))
            {
                await connection.OpenAsync();

                foreach (var item in frequencyData)
                {
                    var command = new SqlCommand("INSERT INTO WordsStatementFrequency (WordCount, StatementsCount, Server, Type) VALUES (@WordCount, @StatementsCount, @Server, @Type)", connection);
                    command.Parameters.AddWithValue("@WordCount", item.WordCount);
                    command.Parameters.AddWithValue("@StatementsCount", item.StatementsCount);
                    command.Parameters.AddWithValue("@Server", server ?? "all");
                    command.Parameters.AddWithValue("@Type", type? 1:0);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }


        
        public async Task SaveSentimentToDB()
        {
            List<SentimentRecordBert> sentimentRecords = LoadSentimentsFromCsv("C:/Users/HONZA/Desktop/diplomka/bert_sentiment.csv");

            

                using (SqlConnection conn = new SqlConnection(GlobalConfig.connstring))
                {
                    await conn.OpenAsync();

                    foreach (var s in sentimentRecords)
                    {
                       

                            string query = "UPDATE Statement SET SentimentBert=@Sentiment,posBert=@pos,negBert=@neg,neuBert=@neu WHERE id=@id";

                            using (SqlCommand cmd = new SqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@Sentiment", s.pos-s.neg);
                                cmd.Parameters.AddWithValue("@pos", s.pos);
                                cmd.Parameters.AddWithValue("@neu", s.neu);
                                cmd.Parameters.AddWithValue("@neg", s.neg);
                                cmd.Parameters.AddWithValue("@id", s.StatementId);

                                await cmd.ExecuteNonQueryAsync();
                            }
                        
                    }
                
               
            }

        }

        public async Task ProcessAndInsertMentions(List<Statement> statements, int type)
        {
            
            Dictionary<string, Dictionary<string, int>> allMentions = new Dictionary<string, Dictionary<string, int>>();

            
            foreach (var statement in statements)
            {
                if (statement.osobaid != null )
                {
                    string osobaID = statement.osobaid;

                    
                    if (!allMentions.ContainsKey(osobaID))
                    {
                        allMentions[osobaID] = new Dictionary<string, int>();
                    }
                    if (statement.politicizminky != null)
                    {
                        
                        foreach (var mentionedPolitician in statement.politicizminky)
                        {
                            if (!allMentions[osobaID].ContainsKey(mentionedPolitician))
                            {
                                allMentions[osobaID][mentionedPolitician] = 1;
                            }
                            else
                            {
                                allMentions[osobaID][mentionedPolitician]++;
                            }
                        }
                    }
                }
            }

            
            using (SqlConnection conn = new SqlConnection(GlobalConfig.connstring))
            {
                await conn.OpenAsync();

                foreach (var osobaEntry in allMentions)
                {
                    string osobaID = osobaEntry.Key;

                    foreach (var mention in osobaEntry.Value)
                    {
                        string mentionedPoliticianID = mention.Key;
                        int frequency = mention.Value;

                        string query = "INSERT INTO MentionsStats (OsobaID, MentionedPoliticianID, Frequency, Type) " +
                                       "VALUES (@OsobaID, @MentionedPoliticianID, @Frequency, @Type) ";
                                     

                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@OsobaID", osobaID);
                            cmd.Parameters.AddWithValue("@MentionedPoliticianID", mentionedPoliticianID);
                            cmd.Parameters.AddWithValue("@Frequency", frequency);
                            cmd.Parameters.AddWithValue("@Type", type);

                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
        }




        /* public async Task SavePoliticStats(List<Statement> st)
         {
             Dictionary<string, int> mentionCount = new Dictionary<string, int>();

             int sumZminky = 0;
             foreach (var statement in st)
             {

                 if (statement.politicizminky != null && statement.politicizminky.Any())
                 {
                     sumZminky += statement.politicizminky.Count;
                     foreach (var politik in statement.politicizminky)
                     {

                         if (mentionCount.ContainsKey(politik))
                         {
                             mentionCount[politik]++;
                         }
                         else
                         {
                             mentionCount[politik] = 1;
                         }
                     }
                 }
             }
             Dictionary<string, int> mentionCountReverse = new Dictionary<string, int>();

             var all_politicians = statementData.GetAllPoliticians(st);
             foreach (var p in all_politicians)
             {
                 mentionCountReverse[p] = 0;
             }
             foreach (var statement in st)
             {

                 if (statement.politicizminky != null && statement.politicizminky.Any())
                 {
                     mentionCountReverse[statement.osobaid] += statement.politicizminky.Count;

                 }
             }
         }*/

        public async Task SaveMentionsofPoliticiansStatsToDB(List<Statement> st)
        {
          

            await SaveMentionsOfPoliticianFrequency(st);
            await SaveMentionsOfPoliticianFrequency(st,"Facebook");
            await SaveMentionsOfPoliticianFrequency(st,"Twitter");



        }
        public async Task SaveWordsMentionsStatsToDB(List<Statement> st,bool mentions)
        {


            await CalculateAndStoreWordsStatementFrequency(st, mentions);
            await CalculateAndStoreWordsStatementFrequency(st, mentions,"Facebook");
            await CalculateAndStoreWordsStatementFrequency(st, mentions,"Twitter");


        }
        public async Task SaveStatsToDB(List<Statement> st, bool mentions )
        {
            await CalculateAndSaveStatementFrequency(st,mentions);
            await CalculateAndSaveStatementFrequency(st, mentions, "Facebook");
            await CalculateAndSaveStatementFrequency(st, mentions, "Twitter");

            


        }

      
    }
}
