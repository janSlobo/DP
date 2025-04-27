namespace PoliticStatements.Models
{
    public class SentimentResult
    {
        public string PoliticId { get; set; }
        public double AvgSentimentFB { get; set; }
        public double AvgSentimentTW { get; set; }
        public double AvgSentimentRTW { get; set; }
    }
    public class ExtremeSentiment
    {
        public double PositiveRatio { get; set; }
        public double NegativeRatio { get; set; }
    }
    class ServerSentiment
    {
        public string Server { get; set; }
        public List<double> Sentiments { get; set; }
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
}
