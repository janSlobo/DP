using PoliticStatements.Models;
using System.Globalization;

namespace PoliticStatements
{
    public class RhetoricAnalysis
    {
        public  Dictionary<string, List<Similar>> LoadSimilarities()
        {
            var result = new Dictionary<string, List<Similar>>();

            try
            {
                foreach (var line in File.ReadLines("C:/Users/HONZA/Desktop/diplomka/politician_top_similarities.csv"))
                {
                    var parts = line.Split(',');
                    if (parts.Length < 11)
                    {
                        Console.WriteLine($"Varování: Přeskakuji řádek kvůli nesprávnému formátu -> {line}");
                        continue;
                    }

                    string politician = parts[0].Trim();
                    var similarList = new List<Similar>();

                    for (int i = 1; i < parts.Length; i += 2)
                    {
                        if (i + 1 >= parts.Length) break;

                        string name = parts[i].Trim();
                        if (!double.TryParse(parts[i + 1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double similarity))
                        {
                            Console.WriteLine($"Chyba parsování similarity: {parts[i + 1]} na řádku {line}");
                            continue;
                        }

                        similarList.Add(new Similar { Name = name, Similarity = similarity });
                    }

                    if (!result.ContainsKey(politician))
                    {
                        result[politician] = similarList;
                    }
                    else
                    {
                        Console.WriteLine($"Varování: Duplikát politika {politician}, přeskakuji.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba při čtení souboru: {ex.Message}");
            }

            return result;
        }
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
