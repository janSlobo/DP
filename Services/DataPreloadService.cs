using Microsoft.AspNetCore.Mvc;
using PoliticStatements.Repositories;
namespace PoliticStatements.Services
{
    public class DataPreloadService : IHostedService
    {
        private readonly StatementData _statementData;
        private readonly StatementRepository statementRepository;
        private readonly EntityRepository entityRepository;
        private readonly EmotionRepository emotionRepository;

        public DataPreloadService(StatementData statementData, StatementRepository sr,EntityRepository er,EmotionRepository emr)
        {
            _statementData = statementData;
            statementRepository = sr;
            entityRepository = er;
            emotionRepository= emr;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var statements = await statementRepository.LoadFromDatabase();  
            await entityRepository.LoadNERFromDB(statements);
            await emotionRepository.LoadEmotionFromDB(statements);
            _statementData.Statements = statements;

        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        
    }

}
