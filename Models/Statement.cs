using System.Text.Json.Serialization;
using CsvHelper;
using CsvHelper.Configuration.Attributes;

namespace PoliticStatements.Models
{
    public class Statement
    {
        public string? server { get; set; }
        public string? typserveru { get; set; }
        public Politic? osobaid { get; set; }
        public DateTime? datum { get; set; }

        public string? origid { get; set; }
        public string? id { get; set; }
      
        public string? url { get; set; }

    
        public string? text { get; set; }

  
        [JsonPropertyName("pocetslov")]
        public int? pocetslov { get; set; } = 0;

        [JsonPropertyName("pocetSlov")]
        public int? pocetSlov { get; set; } = 0;

        public List<Politic>? politicizminky { get; set; }
        public double Sentiment { get; set; }
        public double SentimentBert { get; set; }
        public double neg { get; set; }
        public double neu { get; set; }
        public double pos { get; set; }
        public string jazyk { get; set; }
        public bool RT { get; set; }
    
        public List<Emotion> emotions { get; set; } 

        public List<Entity> Entities { get; set; } 
        public Statement()
        {
            politicizminky = new List<Politic>();
            emotions  = new List<Emotion>();
            Entities  = new List<Entity>();
        }
    }
    public class MentionStats
    {
        public string politic_id { get; set; }
        public int MentionedCount { get; set; }  
        public int MentionOthersCount { get; set; } 
    }

    public class PoliticianStats
    {
        public string PoliticId { get; set; }
        public int NumberOfStatements { get; set; }
        public double AverageWordCount { get; set; }
        public int MedianWordCount { get; set; }
        public int MaxWordCount { get; set; }
        public int NumberOfLongStatements { get; set; }
    }

    public class StatementCountDistribution
    {
        public int StatementCount { get; set; }
        public int NumOfPeople { get; set; }
    }
    public class MonthlyStatementCount
    {
        public int Month { get; set; }
        public int Count { get; set; }
    }
    

    
   
    

    
    
    public class EmotionPStats
    {
        public double Percentage { get; set; }
        public double AverageIntensity { get; set; }
    }

    public class PoliticianEmotionData
    {
        public string OsobaId { get; set; }
        public Dictionary<string, EmotionPStats> EmotionStatistics { get; set; }

        public int count { get; set; }
    }
    public class EntitySentimentData
    {
        public string EntityName { get; set; }
        public string EntityType { get; set; }
        public double AverageSentiment { get; set; }

        public List<double> Sentiments { get; set; }
    }
    public class EntityFrequency
    {
        public string EntityText { get; set; }
        public double Frequency { get; set; }
    }
    public class EmotionDistribution
    {
        public string Emotion { get; set; }
        public double Count { get; set; }
    }

    public class AvgSentence
    {
        public string Name { get; set; }
        public double AvgLength { get; set; }
    }
    public class ChartData
    {
        public int Facebook { get; set; }
        public int Twitter { get; set; }
        public int Retweets { get; set; }
        public int NormalTweets { get; set; }
    }
    public class EmotionStats
    {
        public string Emotion { get; set; }
        public List<double> PercentagePerQuarter { get; set; }  // Procenta pro každé čtvrtletí
    }



    public class EmotionStatsH
    {
        public string Emotion { get; set; }
        public double Percentage { get; set; }  // Procenta pro každé čtvrtletí
    }

}
