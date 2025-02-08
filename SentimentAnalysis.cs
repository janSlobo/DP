using PoliticStatements.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Data.SqlClient;
namespace PoliticStatements
{
    public class SentimentAnalysis
    {

        public class SentimentRecord
        {
            public string StatementId { get; set; }
            public double Sentiment { get; set; }

            public double neg { get; set; }
            public double neu { get; set; }
            public double pos { get; set; }
        }

        public class PoliticianSentiment
        {
            public string OsobaID { get; set; }
            public double AverageSentiment { get; set; }
            public double AveragePos { get; set; }
            public double AverageNeu { get; set; }
            public double AverageNeg { get; set; }
            public int Count { get; set; }
        }
        public class MentionsSentiment
        {
           
            public double AverageSentiment { get; set; }
            public int Count { get; set; }
        }
        public class PoliticianSentimentM
        {
            public string OsobaID { get; set; }
            public double AverageSentiment { get; set; }

            public double AveragePos { get; set; }
            public double AverageNeu { get; set; }
            public double AverageNeg { get; set; }

            public int Count_m { get; set; }
        }
        public class SentimentResult
        {
            public string PoliticId { get; set; }
            public double AvgSentimentOriginal { get; set; }
            public double AvgSentimentRetweet { get; set; }
        }

        public class ExtremeSentiment
        {
            public double PositiveRatio { get; set; }
            public double NegativeRatio { get; set; }
        }

        public Dictionary<string, ExtremeSentiment> CalculateSentimentRatios(List<Statement> statements)
        {

            var groupedByPolitician = statements
                .GroupBy(s => s.osobaid)
                .ToDictionary(
                    group => group.Key, 
                    group => group.ToList() 
                );

            
            var sentimentRatios = new Dictionary<string, ExtremeSentiment>();

            foreach (var group in groupedByPolitician)
            {
                string? osobaId = group.Key;
                var politicianStatements = group.Value;

                
                int positiveCount = politicianStatements.Count(s => s.Sentiment > 0.7);               
                int negativeCount = politicianStatements.Count(s => s.Sentiment < -0.7);               
                int totalStatements = politicianStatements.Count;

               
                double positiveRatio = totalStatements > 0 ? (double)positiveCount / totalStatements : 0;
                double negativeRatio = totalStatements > 0 ? (double)negativeCount / totalStatements : 0;

                if (!sentimentRatios.ContainsKey(osobaId))
                {
                    sentimentRatios[osobaId] = new ExtremeSentiment();
                }

                sentimentRatios[osobaId].PositiveRatio = positiveRatio;
                sentimentRatios[osobaId].NegativeRatio = negativeRatio;
            }

            return sentimentRatios;
        }
        public  Dictionary<string, Dictionary<string, double>> CalculateAverageSentimentByHalfYear(List<Statement> statements)
        {
            var result = new Dictionary<string, Dictionary<string, double>>();

            var groupedStatements = statements.GroupBy(s => new { s.osobaid, HalfYear = GetHalfYear(s.datum) });

            foreach (var group in groupedStatements)
            {
                if (!result.ContainsKey(group.Key.osobaid))
                {
                    result[group.Key.osobaid] = new Dictionary<string, double>();
                }

                // Vypočítáme průměrný sentiment pro daný půlrok
                double averageSentiment = group.Average(s => s.Sentiment);
                result[group.Key.osobaid][group.Key.HalfYear] = averageSentiment;
            }
            foreach (var person in result.Keys.ToList())
            {
                result[person] = result[person]
                    .OrderBy(p => p.Key, StringComparer.Ordinal) // Seřadí půlroky abecedně (H1 před H2)
                    .ToDictionary(p => p.Key, p => p.Value);
            }

            return result;
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


        public List<SentimentResult> CalculateAvgSentimentRT(List<Statement> statements)
        {
            var sentimentResults = statements
                .GroupBy(s => s.osobaid)  // Skupina podle PoliticId
                .Where(group => group.Any(s => s.RT))  // Filtrace, aby skupina obsahovala alespoň jeden retweet
                .Select(group => new SentimentResult
                {
                    PoliticId = group.Key,
                    AvgSentimentOriginal = group.Where(s => !s.RT).Any() ? group.Where(s => !s.RT).Average(s => s.Sentiment) : 0,
                    AvgSentimentRetweet = group.Where(s => s.RT).Any() ? group.Where(s => s.RT).Average(s => s.Sentiment) : 0
                })
                .ToList();

            return sentimentResults;
        }


        public static List<SentimentRecord> LoadSentimentsFromCsv(string filePath)
        {
            var sentimentRecords = new List<SentimentRecord>();

            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                sentimentRecords = csv.GetRecords<SentimentRecord>().ToList();
            }

            return sentimentRecords;
        }
        
        public List<Statement> GetUpdatedStatements(List<Statement> st)
        {
            List<SentimentRecord> sentimentRecords = LoadSentimentsFromCsv("C:/Users/HONZA/Desktop/diplomka/texts_with_sentiment2.csv");
            List<Statement> updatedStatements = new List<Statement>();
            foreach (var i in sentimentRecords)
            {
                Statement s = st.FirstOrDefault(stat => stat.id == i.StatementId);
                if (s != null)
                {
                    s.Sentiment = i.Sentiment;
                    s.neg=i.neg;
                    s.neu = i.neu;
                    s.pos = i.pos;
                    updatedStatements.Add(s);
                }
            }

            return updatedStatements;
        }

        public Tuple<double, double> RT_sentiment(List<Statement> st)
        {
            // Filtrace pro klasické příspěvky (RT == false) a server "Twitter"
            var classicSentiments = st.Where(s => s.RT == false && s.server == "Twitter").Select(s => s.Sentiment);
            var classicAverage = classicSentiments.Any() ? classicSentiments.Average() : 0;

            // Filtrace pro retweety (RT == true) a server "Twitter"
            var retweetSentiments = st.Where(s => s.RT == true && s.server == "Twitter").Select(s => s.Sentiment);
            var retweetAverage = retweetSentiments.Any() ? retweetSentiments.Average() : 0;

            // Vrácení průměrného sentimentu pro klasické příspěvky a retweety
            return new Tuple<double, double>(classicAverage, retweetAverage);
        }

        public List<MentionsSentiment> MentionsAvgSentiment(List<Statement> st){

            List<MentionsSentiment> groupedData = st
            .GroupBy(s => s.politicizminky.Count())
            .Select(g => new MentionsSentiment
            {
                Count = g.Key,
                AverageSentiment = g.Average(s => s.Sentiment)
            })
            .OrderBy(d => d.Count) // Seřadíme podle počtu zmínek
            .ToList();

            return groupedData;

        }

        public Dictionary<int, double> AvgSentimentMonth(List<Statement> st)
        {
            
           // List<Statement> statements = GetUpdatedStatements(st);

            
            Dictionary<int, List<double>> months = new Dictionary<int, List<double>>();

           
            foreach (var s in st)
            {
                int month = s.datum.Value.Month;
                if (!months.ContainsKey(month))
                {
                    months[month] = new List<double>();
                }

                months[month].Add(s.Sentiment);
            }

            // Výpočet průměrných sentimentů pro každý měsíc
            Dictionary<int, double> avgSentiments = new Dictionary<int, double>();
            foreach (var month in months)
            {
                avgSentiments[month.Key] = month.Value.Average();
            }

            return avgSentiments;
        }
        public List<PoliticianSentiment> CalculateAvgSentimentPolitician(List<Statement> st)
        {
            //List<Statement> statements = GetUpdatedStatements(st);
            

            var averageSentiments = st.GroupBy(x => x.osobaid).Select(g => new PoliticianSentiment
              {
                  OsobaID = g.Key,
                  AverageSentiment = Math.Round(g.Average(x => x.Sentiment),2),
                  AveragePos = Math.Round(g.Average(x => x.pos),2),
                AverageNeu = Math.Round(g.Average(x => x.neu), 2),
                AverageNeg= Math.Round(g.Average(x => x.neg), 2),
                Count =g.Count()
              })
              .ToList();

            return averageSentiments;
        }
       
        public List<PoliticianSentimentM> CalculateAvgSentimentPoliticianFromMentions(List<Statement> st)
        {

            //List<Statement> statements = GetUpdatedStatements(st);

            var politicianSentiments = st
            .SelectMany(s => s.politicizminky
                .Select(p => new { Politician = p, Sentiment = s.Sentiment,s.pos,s.neu,s.neg }))
            .GroupBy(x => x.Politician)
            .Select(g => new PoliticianSentimentM
            {
                OsobaID = g.Key,
                AverageSentiment = Math.Round(g.Average(x => x.Sentiment),2),
                AveragePos = Math.Round(g.Average(x => x.pos), 2),
                AverageNeu = Math.Round(g.Average(x => x.neu), 2),
                AverageNeg = Math.Round(g.Average(x => x.neg), 2),
                Count_m =g.Count()
            })
            .ToList();

            return politicianSentiments;
        }

        public Dictionary<string, List<double>> PoliticianSentiments(List<Statement> st)
        {

            //List<Statement> statements = GetUpdatedStatements(st);
            var politicianSentiments = new Dictionary<string, List<double>>();

            foreach (var statement in st)
            {
               
                    if (!politicianSentiments.ContainsKey(statement.osobaid))
                    {
                        politicianSentiments[statement.osobaid] = new List<double>();
                    }
                    politicianSentiments[statement.osobaid].Add(statement.Sentiment);
                
            }


            return politicianSentiments; 
        }

        public object PolarityCounts(List<Statement> st)
        {
            //List<Statement> statements = GetUpdatedStatements(st);
            int countNegative = st.Count(s => s.Sentiment < 0);
            int countNeutral = st.Count(s => s.Sentiment == 0);
            int countPositive = st.Count(s => s.Sentiment > 0);

            var sentimentData = new
            {
                Negative = countNegative,
                Neutral = countNeutral,
                Positive = countPositive
            };

            return sentimentData;
        }

        public class SentimentStats
        {
            public double Negative { get; set; }
            public double Neutral { get; set; }
            public double Positive { get; set; }
        }
        public Dictionary<string, SentimentStats> GetMentionsPolarity(List<Statement> st)
        {
            
            var politicianMentions = new Dictionary<string, SentimentStats>();
            //List<Statement> statements = GetUpdatedStatements(st);
            
            foreach (var statement in st)
            {
                if (statement.politicizminky != null)
                {
                    foreach (var politician in statement.politicizminky)
                    {
                        
                        if (!politicianMentions.ContainsKey(politician))
                        {
                            politicianMentions[politician] = new SentimentStats(); // Inicializace počtu zmínek
                        }

                        var currentStats = politicianMentions[politician];

                        if (statement.Sentiment < 0)
                        {
                            politicianMentions[politician] = new SentimentStats
                            {
                                Negative = currentStats.Negative + 1,
                                Neutral = currentStats.Neutral,
                                Positive = currentStats.Positive
                            };
                        }
                        else if (statement.Sentiment == 0)
                        {
                            politicianMentions[politician] = new SentimentStats
                            {
                                Negative = currentStats.Negative,
                                Neutral = currentStats.Neutral + 1,
                                Positive = currentStats.Positive
                            };
                        }
                        else
                        {
                            politicianMentions[politician] = new SentimentStats
                            {
                                Negative = currentStats.Negative,
                                Neutral = currentStats.Neutral,
                                Positive = currentStats.Positive + 1
                            };
                        }
                    }
                }
            }

            return politicianMentions;
        }

        public Dictionary<string, SentimentStats> GetPolarity(List<Statement> st)
        {
            
            var polarity = new Dictionary<string, SentimentStats>();
            //List<Statement> statements = GetUpdatedStatements(st);

            foreach (var statement in st)
            {
                var politician = statement.osobaid;
               
                if (!polarity.ContainsKey(politician))
                {
                    polarity[politician] = new SentimentStats(); // Inicializace počtu zmínek
                }

                var currentStats = polarity[politician];

                polarity[politician] = new SentimentStats
                {
                    Negative = currentStats.Negative + statement.neg,
                    Neutral = currentStats.Neutral + statement.neu,
                    Positive = currentStats.Positive +statement.pos
                };
               
                
                    
                
            }

            return polarity;
        }

        public Dictionary<string, double> GetRTPolarity(List<Statement> st)
        {
            st = st.Where(x => x.RT == true).ToList();

            var polarity = new Dictionary<string, double>();
            //List<Statement> statements = GetUpdatedStatements(st);

            foreach (var statement in st)
            {
                var politician = statement.osobaid;

                if (!polarity.ContainsKey(politician))
                {
                    polarity[politician] = 0; // Inicializace počtu zmínek
                }



                polarity[politician] += statement.Sentiment;




            }

            return polarity;
        }


       
    }
}
