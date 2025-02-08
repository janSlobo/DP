using PoliticStatements.Models;

namespace PoliticStatements
{
    public class ClusterAnalysis
    {
        public Dictionary<int, Dictionary<string, int>> GetClusterEntities(List<Statement> statements)
        {
            var clusters = statements.GroupBy(s => s.cluster)
                .ToDictionary(g => g.Key, g => g.ToList());

            var clusterEntities = new Dictionary<int, Dictionary<string, int>>();

            foreach (var cluster in clusters)
            {
                int clusterId = cluster.Key;
                var statementsInCluster = cluster.Value;

                var entityCounts = new Dictionary<string, int>();
                foreach (var statement in statementsInCluster)
                {
                    foreach (var entity in statement.Entities)
                    {
                        if (!entityCounts.ContainsKey(entity.EntityText))
                            entityCounts[entity.EntityText] = 0;
                        entityCounts[entity.EntityText]++;
                    }
                }
                var topEntities = entityCounts.OrderByDescending(x => x.Value)
                                     .Take(20)
                                     .ToDictionary(x => x.Key, x => x.Value);

                clusterEntities[clusterId] = topEntities;
            }

            return clusterEntities;
        }

        public Dictionary<int, Dictionary<string, int>> GetClusterTopics(List<Statement> statements)
        {
            var clusters = statements.GroupBy(s => s.cluster)
                .ToDictionary(g => g.Key, g => g.ToList());

            var clusterTopics = new Dictionary<int, Dictionary<string, int>>();

            foreach (var cluster in clusters)
            {
                int clusterId = cluster.Key;
                var statementsInCluster = cluster.Value;

                var topicCounts = new Dictionary<string, int>();
                foreach (var statement in statementsInCluster)
                {
                    foreach (var topic in statement.topics)
                    {
                        if (!topicCounts.ContainsKey(topic))
                            topicCounts[topic] = 0;
                        topicCounts[topic]++;
                    }
                }
                var topTopics = topicCounts.OrderByDescending(x => x.Value)
                                  .Take(20)
                                  .ToDictionary(x => x.Key, x => x.Value);

                clusterTopics[clusterId] = topTopics;
            }

            return clusterTopics;
        }


        
        public Dictionary<int, List<double>> GetClusterSentiment(List<Statement> statements)
        {
            var clusters = statements.GroupBy(s => s.cluster)
                .ToDictionary(g => g.Key, g => g.ToList());

            var clusterSentiment = new Dictionary<int, List<double>>();

            foreach (var cluster in clusters)
            {
                int clusterId = cluster.Key;
                var statementsInCluster = cluster.Value;

                var sentiments = statementsInCluster.Select(s => s.Sentiment).ToList();
                clusterSentiment[clusterId] = sentiments;
            }

            return clusterSentiment;
        }
        public Dictionary<int, List<double>> GetClusterLogos(List<Statement> statements)
        {
            var clusters = statements.GroupBy(s => s.cluster)
                .ToDictionary(g => g.Key, g => g.ToList());

            var clusterSentiment = new Dictionary<int, List<double>>();

            foreach (var cluster in clusters)
            {
                int clusterId = cluster.Key;
                var statementsInCluster = cluster.Value;

                var sentiments = statementsInCluster.Select(s => s.logos).ToList();
                clusterSentiment[clusterId] = sentiments;
            }

            return clusterSentiment;
        }
        public Dictionary<int, List<double>> GetClusterEthos(List<Statement> statements)
        {
            var clusters = statements.GroupBy(s => s.cluster)
                .ToDictionary(g => g.Key, g => g.ToList());

            var clusterSentiment = new Dictionary<int, List<double>>();

            foreach (var cluster in clusters)
            {
                int clusterId = cluster.Key;
                var statementsInCluster = cluster.Value;

                var sentiments = statementsInCluster.Select(s => s.ethos).ToList();
                clusterSentiment[clusterId] = sentiments;
            }

            return clusterSentiment;
        }
        public Dictionary<int, List<double>> GetClusterPathos(List<Statement> statements)
        {
            var clusters = statements.GroupBy(s => s.cluster)
                .ToDictionary(g => g.Key, g => g.ToList());

            var clusterSentiment = new Dictionary<int, List<double>>();

            foreach (var cluster in clusters)
            {
                int clusterId = cluster.Key;
                var statementsInCluster = cluster.Value;

                var sentiments = statementsInCluster.Select(s => s.pathos).ToList();
                clusterSentiment[clusterId] = sentiments;
            }

            return clusterSentiment;
        }


    }
}
