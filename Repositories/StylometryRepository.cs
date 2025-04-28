using PoliticStatements.Models;
using System.Globalization;

namespace PoliticStatements.Repositories
{
    public class StylometryRepository
    {

        public StylometryRepository() { }


        public Dictionary<string, List<SimilarPolitician>> LoadSimilarities(string file)
        {
            var result = new Dictionary<string, List<SimilarPolitician>>();
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", file);
            try
            {
                foreach (var line in File.ReadLines(filePath))
                {
                    var parts = line.Split(',');
                    if (parts.Length < 11)
                    {

                        continue;
                    }

                    string politician = parts[0].Trim();
                    var similarList = new List<SimilarPolitician>();

                    for (int i = 1; i < parts.Length; i += 2)
                    {
                        if (i + 1 >= parts.Length) break;

                        string name = parts[i].Trim();
                        if (!double.TryParse(parts[i + 1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double similarity))
                        {

                            continue;
                        }

                        similarList.Add(new SimilarPolitician { Name = name, Similarity = similarity });
                    }

                    if (!result.ContainsKey(politician))
                    {
                        result[politician] = similarList;
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba při čtení souboru: {ex.Message}");
            }

            return result;
        }

        public Dictionary<string, Dictionary<string, double>> LoadPoliticianData()
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "slovni_druhy_politici.csv");
            var dictionary = new Dictionary<string, Dictionary<string, double>>();

            var lines = File.ReadAllLines(filePath);
            var header = lines[0].Split(',');

            foreach (var line in lines.Skip(1))
            {
                var columns = line.Split(',');


                string[] nameParts = columns[0].Split('_');
                string politician = nameParts[0];
                string yearPart = nameParts[nameParts.Length - 1];

                if ((yearPart == "2019") || nameParts.Count() == 2)
                {
                    if (!dictionary.ContainsKey(politician))
                    {
                        dictionary[politician] = new Dictionary<string, double>();
                    }

                    for (int i = 1; i < columns.Length; i++)
                    {
                        string wordType = header[i];
                        double value = Math.Round((double.Parse(columns[i], CultureInfo.InvariantCulture)), 1);


                        if (!dictionary[politician].ContainsKey(wordType))
                        {
                            dictionary[politician][wordType] = 0;
                        }

                        dictionary[politician][wordType] += value;
                    }
                }
            }

            return dictionary;
        }

        public Dictionary<string, double> WordLengths()
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "avg_word_length.csv");
            var result = new Dictionary<string, double>();

            foreach (var line in File.ReadLines(filePath))
            {
                var parts = line.Split(',');

                if (parts.Length == 2)
                {
                    string name = parts[0].Trim();
                    if (double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                    {
                        result[parts[0]] = value;
                    }
                }
            }

            return result;
        }
        public Dictionary<string, List<int>> SentenceLengths()
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "avg_sentence_delky.csv");
            Dictionary<string, List<int>> data = new Dictionary<string, List<int>>();

            foreach (var line in File.ReadLines(filePath))
            {
                var parts = line.Split(new[] { ',' }, 2);

                string name = parts[0].Trim('\"');

                var values = parts[1].Trim('\"', '[', ']').Split(',');

                List<int> intValues = new List<int>();
                foreach (var value in values)
                {
                    if (int.TryParse(value.Trim(), out int num))
                    {
                        intValues.Add(num);
                    }
                }

                data[name] = intValues;
            }
            return data;
        }

        public List<string> LoadTopNgrams(string folderPath, string politicId, int topN = 30)
        {
            string fileName = Path.Combine(folderPath, $"{politicId}_ngrams.csv");

            if (!File.Exists(fileName))
                throw new FileNotFoundException($"Soubor pro politicId '{politicId}' nebyl nalezen.", fileName);

            var lines = File.ReadAllLines(fileName)
                            .Skip(1)
                            .Select(line => line.Split(','))
                            .Where(parts => parts.Length == 2)
                            .Select(parts => new { Ngram = parts[0], Freq = int.Parse(parts[1]) })
                            .OrderByDescending(x => x.Freq)
                            .Take(topN)
                            .Select(x => x.Ngram)
                            .ToList();

            return lines;
        }
        public List<AvgSentence> LoadAndSortPoliticians(string filePath)
        {
            var politicians = new List<AvgSentence>();

            foreach (var line in File.ReadLines(filePath).Skip(1))
            {
                var columns = line.Split(',');

                if (columns.Length < 2) continue;

                string name = columns[0].Trim();
                if (!double.TryParse(columns[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double avgLength))
                {
                    Console.WriteLine($"Chyba parsování: {columns[1]}");
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

            foreach (var line in File.ReadLines(filePath).Skip(1))
            {
                var parts = line.Split(',', 2);
                if (parts.Length < 2) continue;

                string politician = parts[0].Trim();
                string ngramsRaw = parts[1];

                var ngrams = ngramsRaw
                    .Split(';')
                    .Select(s => s.Trim().Split(" (")[0]).Take(30)
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

                int firstUnderscore = fileName.IndexOf('_');
                int lastUnderscore = fileName.LastIndexOf('_');

                if (firstUnderscore == -1 || lastUnderscore == -1 || firstUnderscore == lastUnderscore)
                    continue; 

                string politician = fileName.Substring(0, firstUnderscore);
                string entity = fileName.Substring(firstUnderscore + 1, lastUnderscore - firstUnderscore - 1);

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
