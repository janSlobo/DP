using CsvHelper.Configuration;
using CsvHelper;
using PoliticStatements.Models;
using System.Globalization;
using System.Data.SqlClient;

namespace PoliticStatements.Repositories
{
    public class SentimentRepository
    {

        public  List<Statement> LoadSentimentsFromCsv(string filePath)
        {
            var sentimentRecords = new List<Statement>();

            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                sentimentRecords = csv.GetRecords<Statement>().ToList();
            }

            return sentimentRecords;
        }

        

        public void UpdateStatementsSentiment(List<Statement> statements)
        {
            

            using (SqlConnection connection = new SqlConnection(GlobalConfig.connstring))
            {
                connection.Open();

                foreach (var statement in statements)
                {
                    using (SqlCommand command = new SqlCommand(
                        "UPDATE Statement SET Sentiment = @Sentiment WHERE id = @Id", connection))
                    {
                        command.Parameters.AddWithValue("@Sentiment", statement.Sentiment);
                        command.Parameters.AddWithValue("@Id", statement.id);

                        command.ExecuteNonQuery();
                    }
                }
            }
        }

}
}
