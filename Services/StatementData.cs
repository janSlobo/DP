

using PoliticStatements.Models;
using System.Text.Json;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using CsvHelper;
using System.Globalization;
using static Plotly.NET.GenericChart;
using System.Text.RegularExpressions;

namespace PoliticStatements.Services
{
    public class StatementData
    {
        public List<Statement> Statements { get; set; }


        public StatementData()
        {
            
        }

        public List<MentionStats> GetPoliticMentionStats(List<Statement> statements)
        {
            var mentionedCount = new Dictionary<string, int>();
            var mentionedOthersCount = new Dictionary<string, int>();

            foreach (var statement in statements)
            {
                
                if (!mentionedOthersCount.ContainsKey(statement.osobaid.politic_id))
                    mentionedOthersCount[statement.osobaid.politic_id] = 0;

                mentionedOthersCount[statement.osobaid.politic_id] += statement.politicizminky?.Count ?? 0;

              
                if (statement.politicizminky != null)
                {
                    foreach (var zminka in statement.politicizminky)
                    {
                        if (!mentionedCount.ContainsKey(zminka.politic_id))
                            mentionedCount[zminka.politic_id] = 0;

                        mentionedCount[zminka.politic_id]++;
                    }
                }
            }

            
            var allPoliticians = new HashSet<string>(
                mentionedCount.Keys
                .Concat(mentionedOthersCount.Keys)
            );

           
            var result = new List<MentionStats>();
            foreach (var pid in allPoliticians)
            {
                result.Add(new MentionStats
                {
                    politic_id = pid,
                    MentionedCount = mentionedCount.ContainsKey(pid) ? mentionedCount[pid] : 0,
                    MentionOthersCount = mentionedOthersCount.ContainsKey(pid) ? mentionedOthersCount[pid] : 0
                });
            }

            return result;
        }
        public  Dictionary<string, List<double>> GetSentimentsByPolitic(List<Statement> statements,string model="BERT")
        {
            var sentimentDict = new Dictionary<string, List<double>>();

            foreach (var statement in statements)
            {
                foreach (var politik in statement.politicizminky)
                {
                    if (!sentimentDict.ContainsKey(politik.politic_id))
                    {
                        sentimentDict[politik.politic_id] = new List<double>();
                    }
                    if (model == "BERT") sentimentDict[politik.politic_id].Add(statement.SentimentBert);
                    if (model == "VADER") sentimentDict[politik.politic_id].Add(statement.Sentiment);
                }
            }

            return sentimentDict.OrderBy(x=>x.Key).ToDictionary(x=>x.Key,x=>x.Value);
        }
        
        public  List<PoliticianStats> CalculateStats(List<Statement> statements, int minLength)
        {
            
            var grouped = statements
                .GroupBy(s => s.osobaid.politic_id)
                .Select(g => new PoliticianStats
                {
                    PoliticId = g.Key,
                    NumberOfStatements = g.Count(),
                    AverageWordCount = g.Average(s => s.pocetSlov.Value),
                    MaxWordCount = g.Max(s => s.pocetSlov.Value),
                    NumberOfLongStatements = g.Count(s => s.pocetSlov.Value > minLength)
                })
                .ToList();

            
            foreach (var stat in grouped)
            {
                var wordCounts = statements.Where(s => s.osobaid.politic_id == stat.PoliticId).Select(s => s.pocetSlov.Value).OrderBy(x => x).ToList();
                stat.MedianWordCount = GetMedian(wordCounts);
            }

            return grouped;
        }

        private static int GetMedian(List<int> sortedList)
        {
            int count = sortedList.Count;
            if (count % 2 == 0)
            {
              
                return (sortedList[count / 2 - 1] + sortedList[count / 2]) / 2;
            }
            else
            {
                return sortedList[count / 2];
            }
        }
        public (List<string> labels, int[] binCounts) GetMentionIntervals(Dictionary<string, int> mentions, List<(int, int)> customIntervals)
        {
            
            int[] binCounts = new int[customIntervals.Count+1];

            
            int maxMention = customIntervals.Last().Item2;

            
            foreach (var mention in mentions.Values)
            {
                bool assigned = false;

                
                for (int i = 0; i < customIntervals.Count; i++)
                {
                    var interval = customIntervals[i];
                    if (mention >= interval.Item1 && mention <= interval.Item2)
                    {
                        binCounts[i]++;
                        assigned = true;
                        break;
                    }
                }

                
                if (!assigned && mention > customIntervals.Last().Item2)
                {
                    binCounts[customIntervals.Count]++;
                }
            }

           
            List<string> labels = customIntervals.Select(interval =>
    interval.Item1 == interval.Item2 ? $"{interval.Item1}" : $"{interval.Item1}-{interval.Item2}"
).ToList();


            
            labels.Add($"{maxMention}+");

            return (labels, binCounts);
        }
        public Dictionary<string, int> PocetZminekOsob(List<Statement> statements)
        {
            var zminkyPoOsobe = new Dictionary<string, int>();

            foreach (var statement in statements)
            {
                if (!zminkyPoOsobe.ContainsKey(statement.osobaid.politic_id))
                {
                    zminkyPoOsobe[statement.osobaid.politic_id] = 0;
                }
                zminkyPoOsobe[statement.osobaid.politic_id] += statement.politicizminky.Count;
            }

            return zminkyPoOsobe;
        }
        public Dictionary<string, int> PocetZminekPolitiku(List<Statement> statements)
        {
            var zminkyPolitiku = new Dictionary<string, int>();

            foreach (var statement in statements)
            {
                foreach (var politik in statement.politicizminky)
                {
                    if (!zminkyPolitiku.ContainsKey(politik.politic_id))
                    {
                        zminkyPolitiku[politik.politic_id] = 0;
                    }
                    zminkyPolitiku[politik.politic_id]++;
                }
            }

            return zminkyPolitiku;
        }

        public Dictionary<int, int> CreateDistribution(List<Statement> statements)
        {
            return statements
                .GroupBy(s => s.pocetSlov.Value)
                .ToDictionary(g => g.Key, g => g.Count());
        }
        public  Dictionary<string, int> CreateDistributionCategory(List<Statement> statements)
        {
            Dictionary<string, int> distribuce = new Dictionary<string, int>();
            List<(int Min, int Max)> intervaly = new List<(int, int)>
        {
            (0, 10),
            (11, 20),
            (21, 30),
            (31,40),
            (41,50),
            (51,60),
            (61,70),
            (71,100),
            (101,250),
            (251,500),
            (501, int.MaxValue) 
        };
            foreach (var (min, max) in intervaly)
            {
                string klic = max == int.MaxValue ? $"{min}+" : $"{min}-{max}";
                distribuce[klic] = statements.Count(s => s.pocetSlov.Value >= min && s.pocetSlov.Value <= max);
            }

            return distribuce;
        }
        public void ExportMentionsToCsvSentiment(List<Statement> statements, string filePath)
        {
            var mentionDict = new Dictionary<(string, string), (int count, double sentimentSum)>();

            foreach (var statement in statements)
            {
                string sourcePolitician = statement.osobaid.politic_id;
                double sentiment = statement.Sentiment; 

                foreach (var mentionedPolitician in statement.politicizminky)
                {
                    var key = (sourcePolitician, mentionedPolitician.politic_id);
                    if (mentionDict.ContainsKey(key))
                    {
                        mentionDict[key] = (mentionDict[key].count + 1, mentionDict[key].sentimentSum + sentiment);
                    }
                    else
                    {
                        mentionDict[key] = (1, sentiment);
                    }
                }
            }

            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine("Source,Target,Weight,AvgSentiment");
                foreach (var entry in mentionDict)
                {
                    double avgSentiment = entry.Value.count > 0 ? entry.Value.sentimentSum / entry.Value.count : 0;
                    writer.WriteLine($"{entry.Key.Item1},{entry.Key.Item2},{entry.Value.count},{avgSentiment.ToString("F2", CultureInfo.InvariantCulture)}");

                }
            }
        }

        public void ExportMentionsToCsv(List<Statement> statements, string filePath)
        {
           

            var mentionDict = new Dictionary<(string, string), int>();

            foreach (var statement in statements)
            {
                

                string sourcePolitician = statement.osobaid.politic_id;
                foreach (var mentionedPolitician in statement.politicizminky)
                {
                 

                    var key = (sourcePolitician, mentionedPolitician.politic_id);
                    if (mentionDict.ContainsKey(key))
                        mentionDict[key]++;
                    else
                        mentionDict[key] = 1;
                }
            }

            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine("Source,Target,Weight");
                foreach (var entry in mentionDict)
                {
                    writer.WriteLine($"{entry.Key.Item1},{entry.Key.Item2},{entry.Value}");
                }
            }
        }
        public ChartData GetChartData(List<Statement> statements)
        {
            var facebookCount = statements.Count(s => s.server == "Facebook");
            var twitterCount = statements.Count(s => s.server == "Twitter");
            var retweetCount = statements.Count(s => s.server == "Twitter" && s.RT == true);
            var normalTweetCount = statements.Count(s => s.server == "Twitter" && s.RT == false);

            return new ChartData
            {
                Facebook = facebookCount,
                Twitter = twitterCount,
                Retweets = retweetCount,
                NormalTweets = normalTweetCount
            };
        }
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

        
        public List<Statement> LoadDataWMentions(List<Statement> st)
        {
            return st.Where(s => s.politicizminky.Count != 0).ToList();
        }
        
       

        public Dictionary<string, Dictionary<string, int>> GetStatementsPerYear(List<Statement> statements)
        {
            var result = new Dictionary<string, Dictionary<string, int>>();

            var allStatements = new Dictionary<string, int>();

            foreach (var statement in statements)
            {
                int year = statement.datum.Value.Year;
                string osobaId = statement.osobaid.politic_id;

                if (!result.ContainsKey(year.ToString()))
                {
                    result[year.ToString()] = new Dictionary<string, int>();
                }

                if (!result[year.ToString()].ContainsKey(osobaId))
                {
                    result[year.ToString()][osobaId] = 0;
                }

                result[year.ToString()][osobaId]++;

                if (!allStatements.ContainsKey(osobaId))
                {
                    allStatements[osobaId] = 0;
                }

                allStatements[osobaId]++;
            }

            result["all"] = allStatements;

            return result;
        }
        public Dictionary<string, Dictionary<string, int>> GetStatementsPerYearAll(List<Statement> statements)
        {
            var result = new Dictionary<string, Dictionary<string, int>>();

            var allStatements = new Dictionary<string, int>();

            foreach (var statement in statements)
            {
            
                string osobaId = statement.osobaid.politic_id;

               
                if (!allStatements.ContainsKey(osobaId))
                {
                    allStatements[osobaId] = 0;
                }

                allStatements[osobaId]++;
            }

            allStatements=allStatements.OrderByDescending(x=>x.Value).ToDictionary(x => x.Key, x => x.Value);
            allStatements = allStatements.Take(15).ToDictionary(x => x.Key, x => x.Value);
            result["all"] = allStatements;

            return result;
        }
       


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

        public List<StatementCountDistribution> GetHistogramCountData(List<Statement> statements)
        {
            var statementCounts = statements
                .GroupBy(s => s.osobaid.politic_id) 
                .Select(group => new
                {
                    OsobaId = group.Key,      
                    Count = group.Count()   
                })
                .ToList();

            var histogramData = statementCounts
                .GroupBy(s => s.Count)  
                .Select(group => new StatementCountDistribution
                {
                    StatementCount = group.Key,  
                    NumOfPeople = group.Count()  
                })
                .ToList();

            return histogramData;
        }

       
      
      

        

        public  List<MonthlyStatementCount> GetMonthlyStatementCounts(List<Statement> statements)
        {
            return statements
                .GroupBy(s => s.datum.Value.Month)
                .Select(g => new MonthlyStatementCount { Month = g.Key, Count = g.Count() })
                .OrderBy(x => x.Month)
                .ToList();
        }

        public  Dictionary<string, int> GetPostsByWeekday(List<Statement> statements)
        {
            var daysOfWeekInCzech = new Dictionary<DayOfWeek, string>
            {
                { DayOfWeek.Sunday, "Neděle" },
                { DayOfWeek.Monday, "Pondělí" },
                { DayOfWeek.Tuesday, "Úterý" },
                { DayOfWeek.Wednesday, "Středa" },
                { DayOfWeek.Thursday, "Čtvrtek" },
                { DayOfWeek.Friday, "Pátek" },
                { DayOfWeek.Saturday, "Sobota" }
            };

            return statements
                .GroupBy(s => s.datum.Value.DayOfWeek)
                .ToDictionary(
                    g => daysOfWeekInCzech[g.Key], 
                    g => g.Count()
                );
        }



    }

}
