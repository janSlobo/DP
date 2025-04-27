namespace PoliticStatements.Models
{
    using CsvHelper.Configuration.Attributes;
    public class Politic
    {
        [Name("Id")]
        public string politic_id { get; set; }
        [Name("Organizace")]
        public string? organizace { get; set; }
        [Ignore]
        public int? count { get; set; }


        public Politic(string politicid)
        {
            politic_id = politicid;
        }

        public Politic(string politicid,string strana,int c)
        {
            politic_id = politicid;
            organizace = strana;
            count = c;
        }

    }
}
