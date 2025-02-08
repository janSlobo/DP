using Microsoft.AspNetCore.Mvc;
using PoliticStatements.Models;
using System.Data.SqlClient;
using static PoliticStatements.SentimentAnalysis;
using Accord.Math;
using Accord.Statistics.Analysis;
namespace PoliticStatements
{
    public class NetworkAnalysis
    {

        public List<string> GetAllPoliticians(List<Statement> st_m)
        {
            List<string> politicians=st_m.Select(st_m=>st_m.osobaid).Distinct().ToList();
            return politicians;
        }

        public List<string> GetAllMPoliticians(List<Statement> st_m)
        {
            List<string> politicians = st_m.SelectMany(st_m => st_m.politicizminky).Distinct().ToList();
            return politicians;
        }

        public int[,] CreateMatrix(List<Statement> st_m, List<string> politiciansA, List<string> politiciansB)
        {
            

            int rows = politiciansA.Count;
            int cols = politiciansB.Count;

            
            int[,] matrix = new int[rows, cols];


            for (int i = 0; i < rows; i++)
            {
                List<Statement> A = st_m.Where(x => x.osobaid == politiciansA[i]).ToList();
                for (int j = 0; j < cols; j++)
                {
                   int count = A.Where(y => y.politicizminky.Contains(politiciansB[j])).Count();
                    matrix[i, j] = count;
                }
               
            }

            return matrix;
        }
        public int[,] CreateMatrixWords(List<Statement> st_m, List<string> politiciansA, List<string> politiciansB)
        {


            int rows = politiciansA.Count;
            int cols = politiciansB.Count;


            int[,] matrix = new int[rows, cols];


            for (int i = 0; i < rows; i++)
            {
                List<Statement> A = st_m.Where(x => x.osobaid == politiciansA[i]).ToList();
                for (int j = 0; j < cols; j++)
                {
                    var list = A.Where(y => y.politicizminky.Contains(politiciansB[j])).ToList();
                    int sum_words = list.Sum(x => x.pocetSlov).Value;
                    matrix[i, j] = sum_words;
                }

            }

            return matrix;
        }
        public double[,] CreateMatrixAvg(List<Statement> st_m, List<string> politiciansA, List<string> politiciansB)
        {
            int rows = politiciansA.Count;
            int cols = politiciansB.Count;


            double[,] matrix = new double[rows, cols];


            for (int i = 0; i < rows; i++)
            {
                List<Statement> A = st_m.Where(x => x.osobaid == politiciansA[i]).ToList();
                for (int j = 0; j < cols; j++)
                {
                    int count = A.Where(y => y.politicizminky.Contains(politiciansB[j])).Count();
                    matrix[i, j] = Math.Round( (double)count/A.Count(),2);
                }

            }

            return matrix;
        }
        public double[,] CreateMatrixAvgWords(List<Statement> st_m, List<string> politiciansA, List<string> politiciansB)
        {
            int rows = politiciansA.Count;
            int cols = politiciansB.Count;


            double[,] matrix = new double[rows, cols];


            for (int i = 0; i < rows; i++)
            {
                List<Statement> A = st_m.Where(x => x.osobaid == politiciansA[i]).ToList();
                for (int j = 0; j < cols; j++)
                {
                    var list = A.Where(y => y.politicizminky.Contains(politiciansB[j])).ToList();
                    int sum_words = list.Sum(x => x.pocetSlov).Value;
                    matrix[i, j] = Math.Round( (double)sum_words/A.Sum(x=>x.pocetSlov).Value,2);
                }

            }

            return matrix;
        }
        public void SaveMatrixToCsv(List<Statement> st_m,string filePath)
        {
            List<string> politiciansA = GetAllPoliticians(st_m);
            List<string> politiciansB = GetAllMPoliticians(st_m);
            double[,] matrix = CreateMatrixAvgWords(st_m,politiciansA,politiciansB);
            using (var writer = new StreamWriter(filePath))
            {
                
                writer.Write(";");
                writer.WriteLine(string.Join(";", politiciansB));

               
                for (int i = 0; i < politiciansA.Count; i++)
                {
                    List<string> row = new List<string> { politiciansA[i] };
                    for (int j = 0; j < politiciansB.Count; j++)
                    {
                        row.Add(matrix[i, j].ToString());
                    }
                    writer.WriteLine(string.Join(";", row));
                }
            }
        }
    }
}
