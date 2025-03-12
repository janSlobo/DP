using Microsoft.AspNetCore.Mvc;

namespace PoliticStatements
{
    public class DataPreloadService : IHostedService
    {
        private readonly StatementData _statementData;

        public DataPreloadService(StatementData statementData)
        {
            _statementData = statementData;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var statements = await _statementData.LoadFromDatabase();  // Načteme data z DB
            await _statementData.LoadNERFromDB(statements);
            await _statementData.LoadEmotionFromDB(statements);
            _statementData.Statements = statements;

        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        
    }

}
