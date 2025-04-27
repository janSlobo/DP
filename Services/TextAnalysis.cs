using PoliticStatements.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;
using CsvHelper.Configuration;
using CsvHelper;

namespace PoliticStatements.Services
{
    public class TextAnalysis
    {
        
        public void Shuffle<T>(List<T> list)
        {
            Random rng = new Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
        public void ExportTexts(List<Statement> statements, string filePath)
        {
            

            
            List<string> rows = new List<string>();

            foreach (var statement in statements)
            {
               
                    string text = statement.text;

                    
                    text = text.Replace("\"", " ");
                    text = text.Replace("\n", " ");    
                    text = text.Replace("\r", " ");   

                    
                    
                    text = $"\"{text}\"";
                    

                    
                    rows.Add($"{statement.id},{text}");
                
            }

            
            
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine("StatementId,Text");

                
                foreach (var row in rows)
                {
                    writer.WriteLine(row);
                }
            }

            Console.WriteLine($"CSV file saved to {filePath}");
        }

        

    }
}
