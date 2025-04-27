using PoliticStatements.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text.Json;

using System.Data.SqlClient;
using System.Text.RegularExpressions;
using PoliticStatements.Repositories;
namespace PoliticStatements.Services
{
    public class SentimentAnalysis
    {


        private readonly SentimentRepository sentimentRepository;

        public SentimentAnalysis(SentimentRepository sr)
        {

            sentimentRepository = sr;
        }



        public Dictionary<string, double> GetAverageSentimentPerMonth(List<Statement> statements,string model="BERT")
        {
            if (model == "BERT")
            {
                var result = statements
                    .GroupBy(s => new { s.datum.Value.Year, s.datum.Value.Month })
                    .ToDictionary(
                        g => $"{g.Key.Month:00}",
                        g => g.Average(s => s.SentimentBert)
                    );

                return result;
            }
            else
            {
                var result = statements
                   .GroupBy(s => new { s.datum.Value.Year, s.datum.Value.Month })
                   .ToDictionary(
                       g => $"{g.Key.Month:00}",
                       g => g.Average(s => s.Sentiment)
                   );

                return result;
            }
        }
        public Dictionary<string, double> GetAverageSentimentPerHalfYear(List<Statement> statements, string model = "BERT")
        {
            var result = statements
                .GroupBy(s => new
                {
                    Year = s.datum.Value.Year,
                    Half = s.datum.Value.Month <= 6 ? 1 : 2
                })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Half)
                .ToDictionary(
                    g => $"{g.Key.Year}-H{g.Key.Half}",
                    g => model == "BERT" ? g.Average(s => s.SentimentBert) : g.Average(s => s.Sentiment)
                );

            return result;
        }



        
       

        
        public Dictionary<string, int> PrepareSentimentHistogram(List<Statement> statements)
        {
            Dictionary<string, int> histogram = new Dictionary<string, int>();

            for (double i = -1.0; i < 1.0; i += 0.2)
            {
                string rangeKey = $"[{i.ToString("F1", CultureInfo.InvariantCulture)}, {(i + 0.2).ToString("F1", CultureInfo.InvariantCulture)})";
                histogram[rangeKey] = 0;
            }

            
             foreach (var statement in statements)
            {
                double sentiment = Math.Clamp(statement.Sentiment, -1.0, 0.9999); 
                double lowerBound = Math.Floor((sentiment + 1) / 0.2) * 0.2 - 1.0; 
                string rangeKey = $"[{lowerBound.ToString("F1", CultureInfo.InvariantCulture)}, {(lowerBound + 0.2).ToString("F1", CultureInfo.InvariantCulture)})";
                histogram[rangeKey]++;
            }

            return histogram;
        }


        public Dictionary<string, ExtremeSentiment> CalculateSentimentRatios(List<Statement> statements, string model="BERT")
        {

            
            var groupedByPolitician = statements
                .GroupBy(s => s.osobaid.politic_id)
                .ToDictionary(
                    group => group.Key, 
                    group => group.ToList() 
                );

            
            var sentimentRatios = new Dictionary<string, ExtremeSentiment>();

            foreach (var group in groupedByPolitician)
            {
                string? osobaId = group.Key;
                var politicianStatements = group.Value;
                int positiveCount = 0;
                int negativeCount = 0;
                if (model == "BERT")
                {
                     positiveCount = politicianStatements.Count(s => s.SentimentBert > 0.8);
                     negativeCount = politicianStatements.Count(s => s.SentimentBert < -0.8);
                }
                else
                {
                     positiveCount = politicianStatements.Count(s => s.Sentiment> 0.8);
                     negativeCount = politicianStatements.Count(s => s.Sentiment < -0.8);
                }
                             
                int totalStatements = politicianStatements.Count;

                if (totalStatements > 50)
                {
                    double positiveRatio = totalStatements > 0 ? (double)positiveCount / totalStatements : 0;
                    double negativeRatio = totalStatements > 0 ? (double)negativeCount / totalStatements : 0;

                    if (!sentimentRatios.ContainsKey(osobaId))
                    {
                        sentimentRatios[osobaId] = new ExtremeSentiment();
                    }

                    sentimentRatios[osobaId].PositiveRatio = positiveRatio;
                    sentimentRatios[osobaId].NegativeRatio = negativeRatio;
                }
            }

            return sentimentRatios
        .OrderBy(entry => entry.Key) 
        .ToDictionary(entry => entry.Key, entry => entry.Value);
        }
        public  Dictionary<string, Dictionary<string, double>> CalculateAverageSentimentByHalfYear(List<Statement> statements,int treshold)
        {
            var result = new Dictionary<string, Dictionary<string, double>>();

            var groupedStatements = statements.GroupBy(s => new { s.osobaid.politic_id, HalfYear = GetHalfYear(s.datum) });

            foreach (var group in groupedStatements)
            {
                if (!result.ContainsKey(group.Key.politic_id))
                {
                    result[group.Key.politic_id] = new Dictionary<string, double>();
                }

                
                double averageSentiment = group.Average(s => s.SentimentBert);
                if (group.Count() > treshold)
                {
                    result[group.Key.politic_id][group.Key.HalfYear] = averageSentiment;
                }
                
            }
            foreach (var person in result.Keys.ToList())
            {
                result[person] = result[person]
                    .OrderBy(p => p.Key, StringComparer.Ordinal) 
                    .ToDictionary(p => p.Key, p => p.Value);
            }

            return result;
        }
        public Dictionary<string, Dictionary<string, double>> CalculateMedianSentimentByHalfYear(List<Statement> statements, int treshold)
        {
            var result = new Dictionary<string, Dictionary<string, double>>();

            var groupedStatements = statements.GroupBy(s => new { s.osobaid.politic_id, HalfYear = GetHalfYear(s.datum) });

            foreach (var group in groupedStatements)
            {
                if (!result.ContainsKey(group.Key.politic_id))
                {
                    result[group.Key.politic_id] = new Dictionary<string, double>();
                }

                
                var sortedSentiments = group.Select(s => s.SentimentBert).OrderBy(s => s).ToList();
                double medianSentiment = CalculateMedian(sortedSentiments);

                if (group.Count() > treshold)
                {
                    result[group.Key.politic_id][group.Key.HalfYear] = medianSentiment;
                }
            }

            foreach (var person in result.Keys.ToList())
            {
                result[person] = result[person]
                    .OrderBy(p => p.Key, StringComparer.Ordinal) 
                    .ToDictionary(p => p.Key, p => p.Value);
            }

            return result;
        }

        
        private double CalculateMedian(List<double> sortedList)
        {
            int count = sortedList.Count;
            if (count % 2 == 1)
            {
                return sortedList[count / 2]; 
            }
            else
            {
                return (sortedList[count / 2 - 1] + sortedList[count / 2]) / 2.0;
            }
        }

        public Dictionary<string, Dictionary<int, Dictionary<string, double>>> CalculateAverageSentimentByQuarter(List<Statement> statements)
        {
            var result = new Dictionary<string, Dictionary<int, Dictionary<string, double>>>();

            var groupedStatements = statements.GroupBy(s => new { s.osobaid.politic_id, Year = s.datum.Value.Year, Quarter = GetQuarter(s.datum.Value) });

            foreach (var group in groupedStatements)
            {
                if (!result.ContainsKey(group.Key.politic_id))
                {
                    result[group.Key.politic_id] = new Dictionary<int, Dictionary<string, double>>();
                }

                if (!result[group.Key.politic_id].ContainsKey(group.Key.Year))
                {
                    result[group.Key.politic_id][group.Key.Year] = new Dictionary<string, double>();
                }

               
                double averageSentiment = group.Average(s => s.SentimentBert);
                result[group.Key.politic_id][group.Key.Year][group.Key.Quarter.ToString()] = averageSentiment;
            }

           
            foreach (var person in result.Keys.ToList())
            {
                foreach (var year in result[person].Keys.ToList())
                {
                    result[person][year] = result[person][year]
                        .OrderBy(p => p.Key) 
                        .ToDictionary(p => p.Key, p => p.Value);
                }
            }

            return result;
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



        public List<SentimentResult> CalculateAvgSentimentRT(List<Statement> statements,string model="BERT")
        {
            if (model == "BERT")
            {
                var sentimentResults = statements
                    .GroupBy(s => s.osobaid.politic_id)   
                    .Select(group => new SentimentResult
                    {
                        PoliticId = group.Key,
                        AvgSentimentFB = group.Where(s => !s.RT && s.server == "Facebook").Any() ? group.Where(s => !s.RT && s.server=="Facebook").Average(s => s.SentimentBert) : 0,
                        AvgSentimentTW = group.Where(s => !s.RT && s.server == "Twitter").Any() ? group.Where(s => !s.RT && s.server=="Twitter").Average(s => s.SentimentBert) : 0,
                        AvgSentimentRTW = group.Where(s => s.RT && s.server == "Twitter").Any() ? group.Where(s => s.RT && s.server == "Twitter").Average(s => s.SentimentBert) : 0
                    })
                    .OrderBy(result => result.PoliticId) 
                    .ToList();

                return sentimentResults;
            }
            else
            {
                var sentimentResults = statements
                    .GroupBy(s => s.osobaid.politic_id) 
                    .Select(group => new SentimentResult
                    {
                        PoliticId = group.Key,
                        AvgSentimentFB = group.Where(s => !s.RT && s.server == "Facebook").Any() ? group.Where(s => !s.RT && s.server == "Facebook").Average(s => s.Sentiment) : 0,
                        AvgSentimentTW = group.Where(s => !s.RT && s.server == "Twitter").Any() ? group.Where(s => !s.RT && s.server == "Twitter").Average(s => s.Sentiment) : 0,
                        AvgSentimentRTW = group.Where(s => s.RT && s.server == "Twitter").Any() ? group.Where(s => s.RT && s.server == "Twitter").Average(s => s.Sentiment) : 0
                    })
                    .OrderBy(result => result.PoliticId) 
                    .ToList();

                return sentimentResults;
            }
        }



        
        
      

        public Tuple<double, double> RT_sentiment(List<Statement> st,string model="BERT")
        {
            if (model == "BERT")
            {

                var classicSentiments = st.Where(s => s.RT == false && s.server == "Twitter").Select(s => s.SentimentBert);

                var classicAverage = classicSentiments.Any() ? classicSentiments.Average() : 0;


                var retweetSentiments = st.Where(s => s.RT == true && s.server == "Twitter").Select(s => s.SentimentBert);
                var retweetAverage = retweetSentiments.Any() ? retweetSentiments.Average() : 0;


                return new Tuple<double, double>(classicAverage, retweetAverage);
            }
            else
            {
                var classicSentiments = st.Where(s => s.RT == false && s.server == "Twitter").Select(s => s.Sentiment);

                var classicAverage = classicSentiments.Any() ? classicSentiments.Average() : 0;


                var retweetSentiments = st.Where(s => s.RT == true && s.server == "Twitter").Select(s => s.Sentiment);
                var retweetAverage = retweetSentiments.Any() ? retweetSentiments.Average() : 0;


                return new Tuple<double, double>(classicAverage, retweetAverage);
            }
        }

        public List<MentionsSentiment> MentionsAvgSentiment(List<Statement> st){

            List<MentionsSentiment> groupedData = st
            .GroupBy(s => s.politicizminky.Count())
            .Select(g => new MentionsSentiment
            {
                Count = g.Key,
                AverageSentiment = g.Average(s => s.Sentiment)
            })
            .OrderBy(d => d.Count) 
            .ToList();

            return groupedData;

        }

        public Dictionary<int, double> AvgSentimentMonth(List<Statement> st)
        {
            

            
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

            Dictionary<int, double> avgSentiments = new Dictionary<int, double>();
            foreach (var month in months)
            {
                avgSentiments[month.Key] = month.Value.Average();
            }

            return avgSentiments;
        }
        public List<PoliticianSentiment> CalculateAvgSentimentPolitician(List<Statement> st,string model="BERT")
        {

            if (model == "BERT")
            {
                var averageSentiments = st.GroupBy(x => x.osobaid.politic_id).Select(g => new PoliticianSentiment
                {
                    OsobaID = g.Key,
                    AverageSentiment = Math.Round(g.Average(x => x.SentimentBert), 2),
                    AveragePos = Math.Round(g.Average(x => x.pos), 2),
                    AverageNeu = Math.Round(g.Average(x => x.neu), 2),
                    AverageNeg = Math.Round(g.Average(x => x.neg), 2),
                    Count = g.Count()
                })
                .ToList()
                .Select(ps =>
                {
                    ps.AverageSentiment = (ps.AverageSentiment == 0 ? 0 : ps.AverageSentiment);
                    ps.AveragePos = (ps.AveragePos == 0 ? 0 : ps.AveragePos);
                    ps.AverageNeu = (ps.AverageNeu == 0 ? 0 : ps.AverageNeu);
                    ps.AverageNeg = (ps.AverageNeg == 0 ? 0 : ps.AverageNeg);
                    return ps;
                })
                .ToList();


                return averageSentiments;
            }
            else
            {
                var averageSentiments = st.GroupBy(x => x.osobaid.politic_id).Select(g => new PoliticianSentiment
                {
                    OsobaID = g.Key,
                    AverageSentiment = Math.Round(g.Average(x => x.Sentiment), 2),
                    AveragePos = Math.Round(g.Average(x => x.pos), 2),
                    AverageNeu = Math.Round(g.Average(x => x.neu), 2),
                    AverageNeg = Math.Round(g.Average(x => x.neg), 2),
                    Count = g.Count()
                })
                .ToList()
                .Select(ps =>
                {
                    ps.AverageSentiment = (ps.AverageSentiment == 0 ? 0 : ps.AverageSentiment);
                    ps.AveragePos = (ps.AveragePos == 0 ? 0 : ps.AveragePos);
                    ps.AverageNeu = (ps.AverageNeu == 0 ? 0 : ps.AverageNeu);
                    ps.AverageNeg = (ps.AverageNeg == 0 ? 0 : ps.AverageNeg);
                    return ps;
                })
                .ToList();


                return averageSentiments;
            }
        }
       
        public List<PoliticianSentimentM> CalculateAvgSentimentPoliticianFromMentions(List<Statement> st,string model="BERT")
        {


            if (model == "BERT")
            {
                var politicianSentiments = st
                .SelectMany(s => s.politicizminky
                    .Select(p => new { Politician = p.politic_id, Sentiment = s.SentimentBert, s.pos, s.neu, s.neg }))
                .GroupBy(x => x.Politician)
                .Select(g => new PoliticianSentimentM
                {
                    OsobaID = g.Key,
                    AverageSentiment = Math.Round(g.Average(x => x.Sentiment), 2),
                    AveragePos = Math.Round(g.Average(x => x.pos), 2),
                    AverageNeu = Math.Round(g.Average(x => x.neu), 2),
                    AverageNeg = Math.Round(g.Average(x => x.neg), 2),
                    Count_m = g.Count()
                })
                .ToList();

                return politicianSentiments;
            }
            else
            {
                var politicianSentiments = st
                .SelectMany(s => s.politicizminky
                    .Select(p => new { Politician = p.politic_id, Sentiment = s.Sentiment, s.pos, s.neu, s.neg }))
                .GroupBy(x => x.Politician)
                .Select(g => new PoliticianSentimentM
                {
                    OsobaID = g.Key,
                    AverageSentiment = Math.Round(g.Average(x => x.Sentiment), 2),
                    AveragePos = Math.Round(g.Average(x => x.pos), 2),
                    AverageNeu = Math.Round(g.Average(x => x.neu), 2),
                    AverageNeg = Math.Round(g.Average(x => x.neg), 2),
                    Count_m = g.Count()
                })
                .ToList();

                return politicianSentiments;
            }
        }

        public Dictionary<string, List<double>> PoliticianSentiments(List<Statement> st,string sent="BERT")
        {

            var politicianSentiments = new Dictionary<string, List<double>>();

            foreach (var statement in st)
            {
               
                    if (!politicianSentiments.ContainsKey(statement.osobaid.politic_id))
                    {
                        politicianSentiments[statement.osobaid.politic_id] = new List<double>();
                    }
                    if(sent=="BERT") politicianSentiments[statement.osobaid.politic_id].Add(statement.SentimentBert);
                    if (sent == "VADER") politicianSentiments[statement.osobaid.politic_id].Add(statement.Sentiment);

            }


            return politicianSentiments.OrderBy(x=>x.Key).ToDictionary(x=>x.Key,x=>x.Value); 
        }

        public object PolarityCounts(List<Statement> st)
        {
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
        public void LoadSentimentsAndUpdateDatabase(string filePath)
        {
            List<Statement> statements = sentimentRepository.LoadSentimentsFromCsv(filePath);
            sentimentRepository.UpdateStatementsSentiment(statements);
        }










    }
}
