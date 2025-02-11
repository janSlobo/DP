using System.Text.Json.Serialization;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using static PoliticStatements.StatementDataDB;
namespace PoliticStatements.Models
{
    public class Statement
    {
        public string? server { get; set; }
        public string? typserveru { get; set; }
        public string? osobaid { get; set; }
        public DateTime? datum { get; set; }

        public string? origid { get; set; }
        public string? id { get; set; }
        [Ignore]
        public string? url { get; set; }

        [Ignore]
        public string? text { get; set; }

        [Ignore]
        [JsonPropertyName("pocetslov")]
        public int? pocetslov { get; set; } = 0;

        [JsonPropertyName("pocetSlov")]
        public int? pocetSlov { get; set; } = 0;
        [Ignore]
        public DateTime? DbCreated { get; set; }
        [Ignore]
        public object? DbCreatedBy { get; set; }

        public List<string>? politicizminky { get; set; }
        public double Sentiment { get; set; }
        public double neg { get; set; }
        public double neu { get; set; }
        public double pos { get; set; }
        public string jazyk { get; set; }
        public bool RT { get; set; }

        public double logos { get; set; }
        public double pathos { get; set; }
        public double ethos { get; set; }
        public double manipulation { get; set; }
        public double populism { get; set; }

        public int cluster { get; set; }
        public List<EmotionData> emotions = new List<EmotionData>();
        public List<string> topics { get; set; } = new List<string>();
        public List<EntityData> Entities { get; set; } = new List<EntityData>();
        public Statement()
        {
            politicizminky = new List<string>();
        }
    }
    public class SentimentStats
    {
        public int Negative { get; set; }
        public int Neutral { get; set; }
        public int Positive { get; set; }
    }
    public class CombinedPoliticianSentiment
    {
        public string OsobaID { get; set; }
        public double AverageSentiment1 { get; set; }
        public double? AverageSentiment2 { get; set; }

        public double avgpos { get; set; }
        public double avgneg { get; set; }
        public double avgneu { get; set; }
        public int count { get; set; }
        public int count_m { get; set; }
    }

    public class EntityData
    {
        public string EntityText { get; set; }
        public string Type { get; set; }
    }
    public class EmotionData
    {
        public string emotion { get; set; }
        public double score { get; set; }
    }
    public class StatementNER
    {
        public string StatementId { get; set; }
        public List<EntityData> Entities { get; set; } = new List<EntityData>();
    }
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
    public int Frequency { get; set; }
}
public class EmotionDistribution
{
    public string Emotion { get; set; }
    public int Count { get; set; }
}