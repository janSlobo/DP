namespace PoliticStatements.Models
{
    public class Politic
    {

        public string politic_id { get; set; }
        
        public double? AvgWords { get; set; }
        public double? MedianWords { get; set; }
        public int? SumWords { get; set; }
        public double? AvgMentions { get; set; }
        public double? AvgWordsM { get; set; }
        public double? MedianWordsM { get; set; }
        public int? SumWordsM { get; set; }
        public double? AvgMentionsM { get; set; }
    }
}
