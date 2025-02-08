namespace PoliticStatements.Models
{
    public class StatementsStats
    {
        public int ID { get; set; }
        public double AvgWords { get; set; }
        public double MedianWords { get; set; }
        public string MaxStatementsID { get; set; }
        public int MaxStatementsNumber { get; set; }
        public string MaxMentionsID { get; set; }
        public int MaxMentionssNumber { get; set; }
        public double AvgMentions { get; set; }
    }
}
