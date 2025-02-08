using PoliticStatements.Models;

namespace PoliticStatements
{
    public class RhetoricAnalysis
    {
        public Dictionary<string, List<int>> GroupedHistogramRH(List<Statement> statements)
        {
            var bins = new double[] { 0.0, 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0 };
            var categories = new string[] { "ethos", "pathos", "logos", "populism", "manipulation" };

            var histogram = new Dictionary<string, List<int>>();

            foreach (var category in categories)
            {
                histogram[category] = new List<int>(new int[bins.Length - 1]);

                foreach (var statement in statements)
                {
                    double value = category switch
                    {
                        "ethos" => statement.ethos,
                        "pathos" => statement.pathos,
                        "logos" => statement.logos,
                        "populism" => statement.populism,
                        "manipulation" => statement.manipulation,
                        _ => 0
                    };

                    for (int i = 0; i < bins.Length - 1; i++)
                    {
                        if (value >= bins[i] && value < bins[i + 1])
                        {
                            histogram[category][i]++;
                            break;
                        }
                    }
                }
            }

            return histogram;
        }
        public Dictionary<string, List<double>> BOXRH(List<Statement> statements)
        {
            var categories = new string[] { "ethos", "pathos", "logos", "populism", "manipulation" };
            var boxplotData = new Dictionary<string, List<double>>();

            foreach (var category in categories)
            {
                var values = new List<double>();

                foreach (var statement in statements)
                {
                    double value = category switch
                    {
                        "ethos" => statement.ethos,
                        "pathos" => statement.pathos,
                        "logos" => statement.logos,
                        "populism" => statement.populism,
                        "manipulation" => statement.manipulation,
                        _ => 0
                    };
                    values.Add(value);
                }

                boxplotData[category] = values;
            }

            return boxplotData;
        }
    }

    

}
