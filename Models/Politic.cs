namespace PoliticStatements.Models
{
    using CsvHelper.Configuration.Attributes;
    public class Politic
    {
        [Name("Id")]
        public string politic_id { get; set; }
        [Name("Organizace")]
        public string organizace { get; set; }
        [Ignore]
        public double? AvgWords { get; set; }
        [Ignore]
        public double? MedianWords { get; set; }
        [Ignore]
        public int? SumWords { get; set; }
        [Ignore]
        public double? AvgMentions { get; set; }
        [Ignore]
        public double? AvgWordsM { get; set; }
        [Ignore]
        public double? MedianWordsM { get; set; }
        [Ignore]
        public int? SumWordsM { get; set; }
        [Ignore]
        public double? AvgMentionsM { get; set; }
    }
}
