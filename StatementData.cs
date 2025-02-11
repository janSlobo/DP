

using PoliticStatements.Models;
using System.Text.Json;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using CsvHelper;
using System.Globalization;

namespace PoliticStatements
{
    public class StatementData
    {

        public async Task<List<Statement>> GetDataFromAPI(string start,string end,string politician)
        {
            List<Statement> allData = new List<Statement>();
            int page = 1;
            bool set = true;
            string datum_start = start;
            string datum_end = end;
            string origid = "";
            string apiUrl = "";
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    while (true)
                    {
                        if (origid != "")
                        {
                             apiUrl = $"https://api.hlidacstatu.cz/api/v2/datasety/vyjadreni-politiku/hledat?dotaz=osobaid%3A{politician}%2A%20AND%20datum%3A%5B{datum_start}%20TO%20{datum_end}%5D%20AND%20%28server%3ATwitter%20OR%20server%3AFacebook%29%20AND%20origid%3A%3E{origid}&strana={page}&sort=origid&desc=0";

                        }
                        else
                        {
                             apiUrl = $"https://api.hlidacstatu.cz/api/v2/datasety/vyjadreni-politiku/hledat?dotaz=osobaid%3A{politician}%2A%20AND%20datum%3A%5B{datum_start}%20TO%20{datum_end}%5D%20AND%20%28server%3ATwitter%20OR%20server%3AFacebook%29&strana={page}&sort=origid&desc=0";

                        }
                        if (set)
                        {
                            client.DefaultRequestHeaders.Add("accept", "text/plain");
                            client.DefaultRequestHeaders.Add("Authorization", "2bf06054a2854816afeaafa26781a4a2");
                            set = false;
                        }
                        

                        HttpResponseMessage response = await client.GetAsync(apiUrl);
                        string resultsJson = "";
                        if (response.IsSuccessStatusCode)
                        {
                            string jsonResponse = await response.Content.ReadAsStringAsync();
                            using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                            {
                             
                                JsonElement root = doc.RootElement;

                             
                                JsonElement results = root.GetProperty("results");

                                if (results.GetArrayLength()==0)
                                {
                                    break;
                                }
                               
                                resultsJson = results.ToString();

                               
                            }

                            List<Statement> data = JsonSerializer.Deserialize<List<Statement>>(resultsJson);
                            allData.AddRange(data);


                            if (page == 200)
                            {
                               
                               origid = allData[allData.Count() - 1].origid;
                               page = 0;
                            }
                            
                            page++;

                        }
                        else
                        {
                          
                            break; 
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Chyba: " + ex.Message);
                }
            }
            allData = allData.GroupBy(obj => obj.id.ToLower()).Select(group => group.First()).ToList();
            foreach (Statement obj in allData)
            {
                if (obj.pocetSlov == 0)
                {
                    obj.pocetSlov = obj.pocetslov;
                }
                if (obj.pocetSlov == 0 && obj.text.Length != 0)
                {
                    var pocet_slov = await CountWords(obj.text);
                    obj.pocetSlov = pocet_slov;

                }
                
            }
            allData = allData.Where(s => s.pocetSlov > 1).ToList();
            return allData;
        }

        //database
        public List<Statement> LoadDataWMentions(List<Statement> st)
        {
            return st.Where(s => s.politicizminky.Count != 0).ToList();
        }
        public async Task<List<Statement>> LoadFromDatabase()
        {
            List<Statement> st = new List<Statement>();

            using (SqlConnection conn = new SqlConnection(GlobalConfig.connstring))
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("select * from Statement where politic_id IN ('andrej-babis','karel-havlicek', 'lubomir-volny','alena-schillerova','tomio-okamura','petr-fiala','adam-vojtech','milos-zeman','pavel-belobradek','miroslav-kalousek') and jazyk='cs' and RT=0 and pocetSlov>5 and Sentiment!=666 ", conn))
                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        Statement s = new Statement
                        {
                            server = reader.GetString(reader.GetOrdinal("server")),
                            //typserveru = reader.GetString(reader.GetOrdinal("typserveru")),
                            osobaid = reader.GetString(reader.GetOrdinal("politic_id")),
                            datum = reader.GetDateTime(reader.GetOrdinal("datum")),
                            id = reader.GetString(reader.GetOrdinal("id")),
                            //url = reader.GetString(reader.GetOrdinal("url")),
                            pocetSlov = reader.GetInt32(reader.GetOrdinal("pocetSlov")),
                            RT = reader.GetBoolean(reader.GetOrdinal("RT")),
                            text = reader.GetString(reader.GetOrdinal("text")),
                            Sentiment = reader.GetDouble(reader.GetOrdinal("Sentiment")),
                            pos = reader.GetDouble(reader.GetOrdinal("pos")),
                            neu = reader.GetDouble(reader.GetOrdinal("neu")),
                            neg = reader.GetDouble(reader.GetOrdinal("neg")),
                            logos = reader.GetDouble(reader.GetOrdinal("logos")),
                            pathos = reader.GetDouble(reader.GetOrdinal("pathos")),
                            ethos = reader.GetDouble(reader.GetOrdinal("ethos")),
                            manipulation = reader.GetDouble(reader.GetOrdinal("manipulation")),
                            populism= reader.GetDouble(reader.GetOrdinal("populism")),
                            cluster= reader.GetInt32(reader.GetOrdinal("cluster"))

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

                           
                            Statement statement = st.FirstOrDefault(s => s.id == statementId);
                            if (statement != null)
                            {
                                statement.politicizminky.Add(politicId);
                            }

                            
                        }
                    }
                }

            }

            return st;
        }

        public async Task<List<Statement>> LoadFromDatabaseWSentiment()
        {
            List<Statement> st = new List<Statement>();

            using (SqlConnection conn = new SqlConnection(GlobalConfig.connstring))
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("SELECT server,politic_id,datum,id,pocetSlov,Sentiment,pos,neu,neg,RT,text FROM Statement WHERE Sentiment IS NOT NULL and jazyk like 'cs' ", conn))
                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        Statement s = new Statement
                        {
                            server = reader.GetString(reader.GetOrdinal("server")),
                            //typserveru = reader.GetString(reader.GetOrdinal("typserveru")),
                            osobaid = reader.GetString(reader.GetOrdinal("politic_id")),
                            datum = reader.GetDateTime(reader.GetOrdinal("datum")),
                            id = reader.GetString(reader.GetOrdinal("id")),
                            //url = reader.GetString(reader.GetOrdinal("url")),
                            pocetSlov = reader.GetInt32(reader.GetOrdinal("pocetSlov")),
                            Sentiment= reader.GetDouble(reader.GetOrdinal("Sentiment")),
                            pos = reader.GetDouble(reader.GetOrdinal("pos")),
                            neu = reader.GetDouble(reader.GetOrdinal("neu")),
                            neg = reader.GetDouble(reader.GetOrdinal("neg")),
                            RT = reader.GetBoolean(reader.GetOrdinal("RT")),
                            text = reader.GetString(reader.GetOrdinal("text"))
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


                            Statement statement = st.FirstOrDefault(s => s.id == statementId);
                            if (statement != null)
                            {
                                statement.politicizminky.Add(politicId);
                            }


                        }
                    }
                }

            }

            return st;
        }
        public async Task StoreToDatabase(List<Statement> st)
        {
            List<Statement> allData = st;


            using (SqlConnection conn = new SqlConnection(GlobalConfig.connstring))
            {
                await conn.OpenAsync();
                var errors = 0;
                List<string> errorlist=new List<string>();
                List<string> ids = new List<string>();

                for (int i = 0; i < allData.Count; i++)
                {
                    string osobaid = allData[i].osobaid.ToLower();


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
                                    errorlist.Add(allData[i].politicizminky[k]);
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

        //csv
        public void StoreToCSV(List<Statement> st)
        {
            string statementPath = "data.csv";
            string mentionsPath = "mentions.csv";

            using (var writer = new StreamWriter(statementPath))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {

                csv.WriteRecords(st);
            }

            StringBuilder csvContent = new StringBuilder();
            csvContent.AppendLine("Osoba ID,Politik");

            foreach (var statement in st)
            {
                if (statement.politicizminky != null)
                {
                    foreach (var politician in statement.politicizminky)
                    {
                        csvContent.AppendLine($"{statement.osobaid},{politician}");
                    }
                }
            }


            using (StreamWriter writer = new StreamWriter(mentionsPath))
            {
                writer.Write(csvContent.ToString());
            }
        }

        public void PoliticMentionsCSV(Dictionary<string, int> mentionCount, Dictionary<string, int> mentionCountReverse)
        {

            string filePath = "politic_mentions.csv";

            using (StreamWriter writer = new StreamWriter(filePath))
            {

                writer.WriteLine("id,Zmínil n-krát,Zmíněn n-krát");


                foreach (var id in mentionCountReverse.Keys)
                {
                    var mention = mentionCount.ContainsKey(id) ? mentionCount[id] : 0;
                    var mentionR = mentionCountReverse[id];


                    writer.WriteLine($"{id},{mentionR},{mention}");
                }
            }
        }


        public async Task<StatementsStats> GetStatementsStats(int type)
        {
            StatementsStats data = null;

            string query = "SELECT * FROM StatementsStats WHERE Type=@type";

            using (SqlConnection connection = new SqlConnection(GlobalConfig.connstring))
            {
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@type", type);
                await connection.OpenAsync();

                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        data = new StatementsStats
                        {
                            ID = reader.GetInt32(reader.GetOrdinal("ID")),
                            AvgWords = reader.GetDouble(reader.GetOrdinal("AvgWords")),
                            MedianWords = reader.GetDouble(reader.GetOrdinal("MedianWords")),
                            MaxStatementsID = reader.GetString(reader.GetOrdinal("MaxStatementsID")),
                            MaxStatementsNumber = reader.GetInt32(reader.GetOrdinal("MaxStatementsNumber")),
                            MaxMentionsID = reader.GetString(reader.GetOrdinal("MaxMentionsID")),
                            MaxMentionssNumber = reader.GetInt32(reader.GetOrdinal("MaxMentionssNumber")),
                            AvgMentions = reader.GetDouble(reader.GetOrdinal("AvgMentions"))
                        };
                    }
                }
            }

            return data;
        }

        //
        double CalculateMedian(List<int> values)
        {
            if (values == null || values.Count == 0)
                return 0;
            var sortedValues = values.OrderBy(v => v).ToList();
            int count = sortedValues.Count;
            if (count % 2 == 1)
            {         
                return sortedValues[count / 2];
            }
            else
            {              
                return (double)(sortedValues[(count / 2) - 1] + sortedValues[count / 2]) / 2.0;
            }
        }

        public async Task<List<Politic>> GetAllPoliticiansAsync()
        {
            var politicians = new List<Politic>();

            string query = "SELECT politic_id, avg_words, median_words, sum_words, avg_mentions,avg_wordsM, median_wordsM, sum_wordsM, avg_mentionsM FROM Politic";

            using (SqlConnection connection = new SqlConnection(GlobalConfig.connstring))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        Politic politic = new Politic
                        {
                            politic_id = reader["politic_id"].ToString(),
                            AvgWords = reader["avg_words"] as double?,
                            MedianWords = reader["median_words"] as double?,
                            SumWords = reader["sum_words"] as int?,
                            AvgMentions = reader["avg_mentions"] as double?,
                            AvgWordsM = reader["avg_wordsM"] as double?,
                            MedianWordsM = reader["median_wordsM"] as double?,
                            SumWordsM = reader["sum_wordsM"] as int?,
                            AvgMentionsM = reader["avg_mentionsM"] as double?
                        };

                        politicians.Add(politic);
                    }
                }
            }

            return politicians;
        }
        public List<string> GetAllPoliticians(List<Statement> statements)
        {
            HashSet<string> politicians = new HashSet<string>();

            foreach (var statement in statements)
            {
                if (!string.IsNullOrEmpty(statement.osobaid))
                {
                    politicians.Add(statement.osobaid);
                }

               /* if (statement.politicizminky != null)
                {
                    politicians.UnionWith(statement.politicizminky);
                }*/
            }

            return politicians.ToList();
        }
       
        public  Dictionary<string, double> AvgWords(List<Statement> st)
        {
            Dictionary<string, List<int>> politicianWords = new Dictionary<string, List<int>>();

            foreach (var statement in st)
            {
                if (statement.osobaid != null && statement.pocetSlov != null)
                {
                    if (!politicianWords.ContainsKey(statement.osobaid))
                    {
                        politicianWords[statement.osobaid] = new List<int>();
                    }
                    politicianWords[statement.osobaid].Add((int)statement.pocetSlov);
                }
            }

            Dictionary<string, double> avgWords = politicianWords.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Average()
            );

            return avgWords;
        }
        public double AvgWordsOverall(List<Statement> st)
        {
            int sum = 0;

            foreach (var statement in st)
            {
                sum += (int)statement.pocetSlov;
            }

           

            return sum/(double)st.Count;
        }

        public double MedianWordsOverall(List<Statement> st)
        {
            var orderedSt=st.OrderBy(s => s.pocetSlov).Select(s=> s.pocetSlov).ToList();

            double median = 0;
            if(orderedSt.Count % 2 == 0)
            {
                median = (double)((orderedSt[orderedSt.Count / 2] + orderedSt[(orderedSt.Count / 2) - 1]) / 2);
            }
            else
            {
                median = (double)(orderedSt[orderedSt.Count / 2]);
            }


            return median;
        }
        public Dictionary<string, double> AvgMentions(List<Statement> st)
        {
            Dictionary<string, List<int>> politicianMentions = new Dictionary<string, List<int>>();

            foreach (var statement in st)
            {
                if (statement.osobaid != null )
                {
                    if (!politicianMentions.ContainsKey(statement.osobaid))
                    {
                        politicianMentions[statement.osobaid] = new List<int>();
                    }
                    if (statement.politicizminky != null)
                    {
                        politicianMentions[statement.osobaid].Add((int)statement.politicizminky.Count);
                    }
                }
            }

            Dictionary<string, double> avgMentions = politicianMentions.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Average()
            );

            return avgMentions;
        }
        public Dictionary<string, int> SumWords(List<Statement> st)
        {
            Dictionary<string, List<int>> politicianWords = new Dictionary<string, List<int>>();

            foreach (var statement in st)
            {
                if (statement.osobaid != null && statement.pocetSlov != null)
                {
                    if (!politicianWords.ContainsKey(statement.osobaid))
                    {
                        politicianWords[statement.osobaid] = new List<int>();
                    }
                    politicianWords[statement.osobaid].Add((int)statement.pocetSlov);
                }
            }

            Dictionary<string, int> sumWords = politicianWords.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Sum()
            );

            return sumWords;
        }

        public Dictionary<string, List<string>> MentionsNetwork(List<Statement> st)
        {
            Dictionary<string, List<string>> politicMentions = new Dictionary<string, List<string>>();
            var allPoliticians = GetAllPoliticians(st);

            foreach(var p in allPoliticians)
            {

            }


            foreach (var statement in st)
            {
                if (!politicMentions.ContainsKey(statement.osobaid))
                {
                    politicMentions[statement.osobaid] = new List<string>();
                }
                
                    politicMentions[statement.osobaid].AddRange(statement.politicizminky);
                
               
            }
            return politicMentions;
        }

        public Dictionary<string, double> MedianWords(List<Statement> st)
        {
            Dictionary<string, List<int>> politicianWords = new Dictionary<string, List<int>>();

            foreach (var statement in st)
            {
                if (statement.osobaid != null && statement.pocetSlov != null)
                {
                    if (!politicianWords.ContainsKey(statement.osobaid))
                    {
                        politicianWords[statement.osobaid] = new List<int>();
                    }
                    politicianWords[statement.osobaid].Add((int)statement.pocetSlov);
                }
            }

            Dictionary<string, double> medianWords = politicianWords.ToDictionary(
                kvp => kvp.Key,
                kvp => CalculateMedian(kvp.Value)
            );

            return medianWords;
        }



        public async Task UpdatePoliticTableAsync(List<Statement> statements)
        {
            var avgWordsData = AvgWords(statements);
            var medianWordsData = MedianWords(statements);
            var sumWordsData = SumWords(statements);
            var avgMentionsData = AvgMentions(statements);

            using (SqlConnection connection = new SqlConnection(GlobalConfig.connstring))
            {
                await connection.OpenAsync();

                foreach (var politicId in avgWordsData.Keys)
                {
                    double avgWords = avgWordsData.ContainsKey(politicId) ? avgWordsData[politicId] : 0;
                    double medianWords = medianWordsData.ContainsKey(politicId) ? medianWordsData[politicId] : 0;
                    int sumWords = sumWordsData.ContainsKey(politicId) ? sumWordsData[politicId] : 0;
                    double avgMentions = avgMentionsData.ContainsKey(politicId) ? avgMentionsData[politicId] : 0;
                    
                    string query = @"
                    UPDATE Politic
                    SET avg_words = @AvgWords,
                        median_words = @MedianWords,
                        sum_words = @SumWords,
                        avg_mentions = @AvgMentions
                    WHERE politic_id = @PoliticId";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@AvgWords", avgWords);
                        command.Parameters.AddWithValue("@MedianWords", medianWords);
                        command.Parameters.AddWithValue("@SumWords", sumWords);
                        command.Parameters.AddWithValue("@AvgMentions", avgMentions);
                        command.Parameters.AddWithValue("@PoliticId", politicId);
                        
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        public async Task<Dictionary<string, int>> GetFrequencyOfPoliticiansMentions(List<Statement> data, string? server = null)
        {
            var frequency = new Dictionary<string, int>();

            if (server != null)
            {
                foreach (var statement in data.Where(s => s.server == server))
                {
                    foreach (var politician in statement.politicizminky ?? Enumerable.Empty<string>())
                    {
                        if (frequency.ContainsKey(politician))
                        {
                            frequency[politician]++;
                        }
                        else
                        {
                            frequency[politician] = 1;
                        }
                    }
                }
            }
            else
            {
                foreach (var statement in data)
                {
                    foreach (var politician in statement.politicizminky ?? Enumerable.Empty<string>())
                    {
                        if (frequency.ContainsKey(politician))
                        {
                            frequency[politician]++;
                        }
                        else
                        {
                            frequency[politician] = 1;
                        }
                    }
                }
            }
            return frequency;
        }
       

        

        
        public async Task<List<int>> GetWords(List<Statement> st, string? server = null)
        {
            List<int> words = new List<int>();

            if (server != null)
            {
                foreach (var s in st.Where(stat=>stat.server==server))
                {
                    words.Add(s.pocetSlov.GetValueOrDefault());
                }
            }
            else
            {
                foreach (var s in st)
                {
                    words.Add(s.pocetSlov.GetValueOrDefault());
                }
            }
            return words;
        }
        public async Task<List<int>> GetStCount(List<Statement> st)
        {
            // Vytvoření slovníku pro ukládání počtů vyjádření pro každou osobu
            Dictionary<string, int> counts = new Dictionary<string, int>();

            // Procházení všech vyjádření a aktualizace počtů pro každou osobu
            foreach (var statement in st)
            {
                if (counts.ContainsKey(statement.osobaid))
                {
                    counts[statement.osobaid]++;
                }
                else
                {
                    counts[statement.osobaid] = 1;
                }
            }

            // Převod hodnot slovníku do List<int>
            List<int> stcount = counts.Values.ToList();

            return stcount;
        }

        


        

       

        public async Task< int> CountWords(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return 0;

            int wordCount = 0;
            bool inWord = false;

            foreach (char c in input)
            {
                if (char.IsWhiteSpace(c))
                {
                    if (inWord)
                    {
                        wordCount++;
                        inWord = false;
                    }
                }
                else
                {
                    inWord = true;
                }
            }

            if (inWord)
                wordCount++;

            return wordCount;
        }

        //From Database
        public async Task<List<object>> GetStatementFrequencyFromDatabase(bool mentions,string? server = null)
        {
            var frequencyData = new List<object>();

            using (var connection = new SqlConnection(GlobalConfig.connstring))
            {
                await connection.OpenAsync();

                // SQL dotaz pro načtení dat s volitelným filtrem podle serveru
                var query = "SELECT StatementCount, PersonCount FROM StatementFrequency";
                if (mentions)
                {
                    query += " WHERE type = 1";
                }
                else
                {
                    query += " WHERE type = 0";
                }
               
                query += " AND Server = @Server";
               
                

                using (var command = new SqlCommand(query, connection))
                {
                    if (server != null)
                    {
                        command.Parameters.AddWithValue("@Server", server);
                    }
                    else
                    {
                        command.Parameters.AddWithValue("@Server", "all");
                    }

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var data = new
                            {
                                StatementCount = reader.GetInt32(0),  // Získá hodnotu z prvního sloupce
                                PersonCount = reader.GetInt32(1)      // Získá hodnotu z druhého sloupce
                            };
                            frequencyData.Add(data);
                        }
                    }
                }
            }

            return frequencyData.Cast<object>().ToList();
        }

        public async Task<List<object>> GetMentionsOfPoliticianFrequencyFromDatabase(string? server=null)
        {
            List<object> result = new List<object>();

            using (var connection = new SqlConnection(GlobalConfig.connstring))
            {
                await connection.OpenAsync();

                // SQL dotaz s volitelným filtrováním podle serveru
                var query = "SELECT CountOfMentions, CountOfPoliticians FROM MentionsOfPoliticians";
                
                query += " WHERE Server = @Server";
                

                using (var command = new SqlCommand(query, connection))
                {
                    if (server != null)
                    {
                        command.Parameters.AddWithValue("@Server", server);
                    }
                    else
                    {
                        command.Parameters.AddWithValue("@Server", "all");
                    }

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var obj = new
                            {
                                CountOfMentions = reader.GetInt32(0),
                                CountOfPoliticians = reader.GetInt32(1)
                            };
                            result.Add(obj);
                        }
                    }
                }
            }

            return result;
        }

        public async Task<List<object>> GetMentionsPerPoliticFrequencyFromDatabase()
        {
            List<object> result = new List<object>();

            using (SqlConnection connection = new SqlConnection(GlobalConfig.connstring))
            {
                await connection.OpenAsync();

                string query = "SELECT CountOfPoliticians, CountOfMentions FROM PoliticMentionsFrequency";
                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int countOfPoliticians = reader.GetInt32(0);
                        int countOfMentions = reader.GetInt32(1);

                        result.Add(new { CountOfPoliticians = countOfPoliticians, CountOfMentions = countOfMentions });
                    }
                }
            }

            return result;
        }


        public async Task<List<object>> GetWordsStatementFrequencyFromDatabase(bool type,string? server = null)
        {
            var wordCountStatementsCount = new List<object>();

            using (var connection = new SqlConnection(GlobalConfig.connstring))
            {
                await connection.OpenAsync();

                // Vytvoření základního SQL dotazu
                var query = "SELECT WordCount, StatementsCount FROM WordsStatementFrequency"; // 1=1 je pro snadné přidávání podmínek

                if (type)
                {
                    query += " WHERE Type = 1";
                }
                else
                {
                    query += " WHERE Type = 0";
                }
                

                
                    query += " AND Server = @Server";
                
               
                    
                

                var command = new SqlCommand(query, connection);

                // Přidání parametrů
                if (!string.IsNullOrEmpty(server))
                {
                    command.Parameters.AddWithValue("@Server", server);
                }
                else
                {
                    command.Parameters.AddWithValue("@Server", "all");
                }
                

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        wordCountStatementsCount.Add(new
                        {
                            WordCount = reader.GetInt32(0),
                            StatementsCount = reader.GetInt32(1),
                          
                        });
                    }
                }
            }

            return wordCountStatementsCount;
        }

        public async Task<Dictionary<string, Dictionary<string, int>>> GetMentionsStatsFromDatabase(int type)
        {
            var mentions = new Dictionary<string, Dictionary<string, int>>();

           

            using (SqlConnection conn = new SqlConnection(GlobalConfig.connstring))
            {
                conn.Open();

                string query = "SELECT OsobaID, MentionedPoliticianID, Frequency FROM MentionsStats WHERE type = @type";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@type", type);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string osobaID = reader["OsobaID"].ToString();
                            string mentionedPoliticianID = reader["MentionedPoliticianID"].ToString();
                            int frequency = Convert.ToInt32(reader["Frequency"]);

                          
                            if (!mentions.ContainsKey(osobaID))
                            {
                                mentions[osobaID] = new Dictionary<string, int>();
                            }

                            
                            mentions[osobaID][mentionedPoliticianID] = frequency;
                        }
                    }
                }
            }

            return mentions;
        }



        //old functions
        public async Task<List<object>> GetStatementFrequency(List<Statement> st, string? server = null)
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


            var frequency = statementCountByPerson.GroupBy(pair => pair.Value)
                                         .Select(group => new { StatementCount = group.Key, PersonCount = group.Count() })
                                         .OrderBy(data => data.StatementCount)
                                         .Select(data => new { StatementCount = data.StatementCount, PersonCount = data.PersonCount })
                                         .Cast<object>()
                                         .ToList();

            return frequency;
        }

        public async Task<List<object>> GetMentionsOfPoliticianFrequency(List<Statement> st, string? server = null)
        {
            List<object> res = new List<object>();
            Dictionary<string, int> mentionCounts = new Dictionary<string, int>();
            if (server != null)
            {
                st = st.Where(s => s.server == server).ToList();
            }
            // Projdeme všechny výroky
            foreach (var statement in st)
            {
                // Projdeme všechny politiky v daném výroku
                foreach (var politician in statement.politicizminky)
                {
                    // Zvýšíme počet zmínek pro daného politika
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

            // Vytvoříme výsledný seznam
            Dictionary<int, int> result = new Dictionary<int, int>();

            // Projdeme všechny zmíněné politiky a spočítáme, kolikrát byli zmíněni
            foreach (var count in mentionCounts.Values)
            {

                if (!result.ContainsKey(count))
                {
                    // Pokud není, přidáme ho do seznamu
                    result[count] = 1;
                }
                else
                {
                    result[count]++;
                }
            }

            foreach (var it in result)
            {
                var obj = new
                {
                    CountOfPoliticians = it.Value,
                    CountOfMentions = it.Key
                };
                res.Add(obj);
            }
            return res;



        }
        public async Task<List<object>> GetMentionsPerPoliticFrequency(List<Statement> statements)
        {
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

            // Počet politiků pro každý počet zmínek
            Dictionary<int, int> politiciansCountByMentions = new Dictionary<int, int>();
            foreach (var count in mentionsCount.Values)
            {
                if (!politiciansCountByMentions.ContainsKey(count))
                    politiciansCountByMentions[count] = 0;

                politiciansCountByMentions[count]++;
            }

            // Vytvoření výstupního seznamu
            List<object> result = politiciansCountByMentions.Select(kv => (object)new { CountOfPoliticians = kv.Value, CountOfMentions = kv.Key }).ToList();

            return result;
        }

        public async Task<List<object>> GetWordsStatementFrequency(List<Statement> data, string? server = null)
        {
            var wordCountStatementsCount = new List<object>();
            if (server != null)
            {
                var groupedData = data.Where(s => s.server == server).GroupBy(s => s.pocetSlov ?? 0)
                                 .Select(g => new { WordCount = g.Key, StatementsCount = g.Count() });

                foreach (var item in groupedData)
                {
                    wordCountStatementsCount.Add(new { WordCount = item.WordCount, StatementsCount = item.StatementsCount });
                }
            }
            else
            {
                var groupedData = data.GroupBy(s => s.pocetSlov ?? 0)
                                 .Select(g => new { WordCount = g.Key, StatementsCount = g.Count() });

                foreach (var item in groupedData)
                {
                    wordCountStatementsCount.Add(new { WordCount = item.WordCount, StatementsCount = item.StatementsCount });
                }
            }


            return wordCountStatementsCount;
        }

        public async Task UpdateLanguageForIds(string filePath)
        {
            // Načíst ID z textového souboru
            List<string> ids = new List<string>();
            try
            {
                foreach (var line in File.ReadLines(filePath))
                {
                    ids.Add(line.Trim());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba při čtení souboru: {ex.Message}");
                return;
            }

            // Pokud soubor není prázdný, pokračovat
            if (ids.Count > 0)
            {
                string inClause = string.Join(",", ids.ConvertAll(id => $"'{id}'"));  // Sestavení IN() části
                string query = $"UPDATE Statement SET jazyk='en' WHERE id IN ({inClause})";

                // Spustit SQL dotaz
                try
                {
                    using (SqlConnection connection = new SqlConnection(GlobalConfig.connstring))
                    {
                        connection.Open();
                        SqlCommand command = new SqlCommand(query, connection);
                        int rowsAffected = command.ExecuteNonQuery();
                        
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Chyba při provádění dotazu: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Soubor neobsahuje žádná ID.");
            }
        }


    }

}
