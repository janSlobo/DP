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
    }
}