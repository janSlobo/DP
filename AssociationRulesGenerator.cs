using PoliticStatements.Models;
namespace PoliticStatements
{
    public class AssociationRulesGenerator
    {
        public class AssociationRule
        {
            public List<string> Antecedent { get; set; } = new List<string>();  // Předpoklad
            public List<string> Consequent { get; set; } = new List<string>();  // Důsledek
            public double Support { get; set; }
            public double Confidence { get; set; }
        }
       

        public List<AssociationRule> GenerateAssociationRules(List<Statement> statements, double minSupport, double minConfidence)
        {
            var entitySets = GetEntitySets(statements);

            var frequentItemsets = GetFrequentItemsets(entitySets, minSupport);

            var rules = new List<AssociationRule>();

            foreach (var itemset in frequentItemsets)
            {
                var ruleSets = GenerateRules(itemset);
                foreach (var rule in ruleSets)
                {
                    var support = CalculateSupport(statements, rule.Antecedent.Concat(rule.Consequent).ToList());
                    var confidence = CalculateConfidence(statements, rule);

                    if (support >= minSupport && confidence >= minConfidence)
                    {
                        rule.Support = support;
                        rule.Confidence = confidence;
                        rules.Add(rule);
                    }
                }
            }

            return rules.OrderByDescending(r => r.Confidence).ToList();
        }

        private List<List<string>> GetEntitySets(List<Statement> statements)
        {
            return statements
                .Select(s => s.Entities.Select(e => e.EntityText).ToList())
                .ToList();
        }

        // Generování častých itemsetů pomocí Apriori
        private List<List<string>> GetFrequentItemsets(List<List<string>> entitySets, double minSupport)
        {
            var frequentItemsets = new List<List<string>>();
            var candidateItemsets = GenerateCandidateItemsets(entitySets, 1); // Začínáme s 1-itemsety

            while (candidateItemsets.Count > 0)
            {
                var itemsetSupport = new Dictionary<List<string>, int>();

                // Počítáme support pro všechny kandidáty
                foreach (var entitySet in entitySets)
                {
                    foreach (var candidate in candidateItemsets)
                    {
                        if (candidate.All(c => entitySet.Contains(c)))
                        {
                            if (!itemsetSupport.ContainsKey(candidate))
                                itemsetSupport[candidate] = 0;
                            itemsetSupport[candidate]++;
                        }
                    }
                }

                // Filtrujeme časté itemsety, které mají dostatečný support
                var totalStatements = entitySets.Count;
                var frequentCandidates = itemsetSupport
                    .Where(kv => (double)kv.Value / totalStatements >= minSupport && kv.Key.Count()>1)
                    .Select(kv => kv.Key)
                    .ToList();

                frequentItemsets.AddRange(frequentCandidates);

                // Generujeme nové kandidátní itemsety pro další iteraci
                candidateItemsets = GenerateCandidateItemsets(frequentCandidates, candidateItemsets.FirstOrDefault()?.Count() + 1 ?? 1);
            }

            return frequentItemsets;
        }

        // Generování kandidátních itemsetů pro danou délku
        private List<List<string>> GenerateCandidateItemsets(List<List<string>> entitySets, int length)
        {
            var candidateItemsets = new List<List<string>>();

            if (length == 1)
            {
                // Generování 1-itemsetů (jednotlivé entity)
                var allEntities = entitySets.SelectMany(es => es).Distinct().ToList();
                foreach (var entity in allEntities)
                {
                    candidateItemsets.Add(new List<string> { entity });
                }
            }
            else
            {
                var previousItemsets = GenerateCandidateItemsets(entitySets, length - 1);
                foreach (var set1 in previousItemsets)
                {
                    foreach (var set2 in previousItemsets)
                    {
                        if (set1.Take(length - 1).SequenceEqual(set2.Take(length - 1)) && set1.Last() != set2.Last())
                        {
                            var newSet = set1.Concat(new[] { set2.Last() }).OrderBy(e => e).ToList();
                            if (!candidateItemsets.Contains(newSet))
                            {
                                candidateItemsets.Add(newSet);
                            }
                        }
                    }
                }
            }

            return candidateItemsets;
        }

        private List<AssociationRule> GenerateRules(List<string> itemset)
        {
            var rules = new List<AssociationRule>();

            for (int i = 1; i < itemset.Count; i++)
            {
                var antecedent = itemset.Take(i).ToList();
                var consequent = itemset.Skip(i).ToList();

                if (antecedent.Count > 0 && consequent.Count > 0)
                {
                    rules.Add(new AssociationRule
                    {
                        Antecedent = antecedent,
                        Consequent = consequent
                    });
                }
            }

            return rules;
        }

        private double CalculateSupport(List<Statement> statements, List<string> itemset)
        {
            var totalStatements = statements.Count;
            var count = statements.Count(s => itemset.All(e => s.Entities.Any(ent => ent.EntityText == e)));
            return (double)count / totalStatements;
        }

        private double CalculateConfidence(List<Statement> statements, AssociationRule rule)
        {
            var antecedentSupport = CalculateSupport(statements, rule.Antecedent);
            var combinedSupport = CalculateSupport(statements, rule.Antecedent.Concat(rule.Consequent).ToList());

            return antecedentSupport > 0 ? combinedSupport / antecedentSupport : 0;
        }
    }
}
