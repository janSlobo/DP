using System.Globalization;

namespace PoliticStatements
{
    public class StylometryAnalysis
    {

        public List<AvgSentence> LoadAndSortPoliticians(string filePath)
        {
            var politicians = new List<AvgSentence>();

            foreach (var line in File.ReadLines(filePath).Skip(1)) // Přeskočí hlavičku
            {
                var columns = line.Split(','); // Změň na ';' pokud je třeba

                if (columns.Length < 2) continue; // Ochrana proti chybným řádkům

                string name = columns[0].Trim(); // Odstranění mezer kolem jména
                if (!double.TryParse(columns[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double avgLength))
                {
                    Console.WriteLine($"Chyba parsování: {columns[1]}"); // Debugovací výstup
                    continue;
                }

                politicians.Add(new AvgSentence
                {
                    Name = name,
                    AvgLength = avgLength
                });
            }
            return politicians.OrderByDescending(p => p.AvgLength).ToList();
        }

        public Dictionary<string, List<string>> LoadUniqueNGrams(string filePath)
        {
            var result = new Dictionary<string, List<string>>();

            foreach (var line in File.ReadLines(filePath).Skip(1)) // Přeskočení hlavičky
            {
                var parts = line.Split(',', 2);
                if (parts.Length < 2) continue;

                string politician = parts[0].Trim();
                string ngramsRaw = parts[1];

                var ngrams = ngramsRaw
                    .Split(';')
                    .Select(s => s.Trim().Split(" (")[0]) // Odebrání frekvence
                    .ToList();

                result[politician] = ngrams;
            }

            return result;
        }
        public Dictionary<string, Dictionary<string, List<string>>> LoadSimWords(string folderPath)
        {
            var result = new Dictionary<string, Dictionary<string, List<string>>>();

           
            var files = Directory.GetFiles(folderPath, "*_slova.csv");

            foreach (var file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                string[] parts = fileName.Split('_');

                if (parts.Length < 3) continue; 

                string politician = parts[0]; 
                string entity = parts[1];    

                if (!result.ContainsKey(politician))
                {
                    result[politician] = new Dictionary<string, List<string>>();
                }

                if (!result[politician].ContainsKey(entity))
                {
                    result[politician][entity] = new List<string>();
                }

               
                var lines = File.ReadAllLines(file).Skip(1); 

                foreach (var line in lines)
                {
                    var columns = line.Split(',');
                    if (columns.Length >= 1)
                    {
                        string word = columns[0].Trim();
                        result[politician][entity].Add(word);
                    }
                }
            }

            return result;
        }


    }
}