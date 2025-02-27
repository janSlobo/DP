using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Microsoft.AspNetCore.Mvc;
using PoliticStatements.Models;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Web.Mvc.Html;

using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System.Reflection.Emit;
using static PoliticStatements.NERAnalysis;
using System.Collections.Generic;

namespace PoliticStatements.Controllers
{

    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private int Statement_count = 135621;
        private int Statement_mentions_count = 1305;

        private readonly StatementData _statementData;
        public HomeController(ILogger<HomeController> logger, StatementData statementData)
        {
            _logger = logger;
            _statementData = statementData;
        }

        public async Task<IActionResult> Index([FromServices]  StylometryAnalysis stylometryAnalysis,[FromServices] RhetoricAnalysis rhetoricAnalysis,[FromServices] EmotionAnalysis emotionAnalysis, [FromServices] SentimentAnalysis sentimentAnalysis, [FromServices] TopicAnalysis topicAnalysis, [FromServices] NetworkAnalysis networkAnalysis, [FromServices] TextAnalysis textAnalysis, [FromServices] NERAnalysis nerAnalysis, [FromServices] StatementDataDB statementDataDB)
        {
            //await statementDataDB.SaveSentimentToDB();
            //sentimentAnalysis.InsertEmotionsFromFile("C:/Users/HONZA/Desktop/diplomka/bert_dodelat_result.csv");
            //await nerAnalysis.UpdateNER();
            List<Statement> st = _statementData.Statements;
            /*List<Politic> politicians = await statementData.GetAllPoliticiansAsync();
            using (var writer = new StreamWriter("politici_strana.csv"))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                csv.WriteRecords(politicians);
            }*/
            

            var st_rt = st;
            

            //var result = nerAnalysis.CalculateCooccurrenceMatrixNormalizedJC(st);
            //nerAnalysis.SaveMatrixToCsv(result.TopEntities, result.Matrix, "coocurance_matrix_mix_cos.csv");

           /* string filePath = Path.Combine(Directory.GetCurrentDirectory(), "cooccurrence_net_names_jaccard_0_05.csv");
            nerAnalysis.SaveCooccurrenceMatrixToCsv(result.TopEntities, result.Matrix, filePath);*/


            st = st.Where(x => !x.RT).ToList();




            await emotionAnalysis.LoadEmotionFromDB(st);

            List<string> names = new List<string> { "ps" };
            List<string> mix = new List<string> { "io", "if", "ic", "mn", "ms", "o_", "oa" };


            await nerAnalysis.LoadNERFromDB(st);

            var uniquePoliticIds = st.Select(s => s.osobaid).Distinct();

            var uniqueEmotions = st
           .SelectMany(s => s.emotions)
           .Select(e => e.emotion)
           .Distinct()
           .ToList();

            var unique_ngrams= stylometryAnalysis.LoadUniqueNGrams("C:/Users/HONZA/Desktop/diplomka/n_grams/unique_ngrams/unique_ngrams_all_2019.csv");
            ViewBag.unique_ngrams= unique_ngrams;
            var simWords = stylometryAnalysis.LoadSimWords("C:/Users/HONZA/Desktop/diplomka/texty_csv");
            ViewBag.simWords = simWords;
            /* foreach (var politicId in uniquePoliticIds)
             {

                 foreach (var emotion in uniqueEmotions)
                 {
                     var statementsForPolitician = st.Where(s => s.osobaid == politicId).ToList();
                     var emotion_st = statementsForPolitician.Where(s => s.emotions.Any(x => x.emotion == emotion && x.score>0.85)).ToList();
                     if (emotion_st.Count() == 0)
                     {
                         continue;
                     }
                     string fileName = $"{politicId}_texty_{emotion}.csv";
                     textAnalysis.ExportTexts(emotion_st, fileName);
                 }


             }
            */


            st = st.Where(x => x.osobaid == "petr-fiala").ToList();
            st_rt = st_rt.Where(x => x.osobaid == "petr-fiala").ToList();

            var time_sentiment = sentimentAnalysis.CalculateAverageSentimentByHalfYear(st);
            var time_sentiment_q = sentimentAnalysis.CalculateAverageSentimentByQuarter(st);
            ViewBag.time_sentiment = time_sentiment;
            ViewBag.time_sentiment_q = time_sentiment_q;
            Dictionary<string, ChartData> server_count = new Dictionary<string, ChartData>();
            foreach(var p in uniquePoliticIds)
            {
                var sc = _statementData.GetChartData(st_rt);
                server_count[p] = sc;
            }
            

            
            ViewBag.server_count = server_count;

            foreach (var statement in st)
            {
                statement.emotions = statement.emotions
                    .Where(e => e.score > 0.7)
                    .ToList();

                if (!statement.emotions.Any())
                {
                    statement.emotions.Add(new EmotionData
                    {
                        emotion = "Neutral", 
                        score = 0.9 
                    });
                }

                statement.emotions=statement.emotions.OrderByDescending(e => e.score).Take(1).ToList();
            }

            var emotionStatsQ = emotionAnalysis.GetEmotionPercentagesPerPoliticianAndYearQ(st);

            ViewBag.EmotionStatsQ = emotionStatsQ;

            var emotionStatsH = emotionAnalysis.GetEmotionPercentagesPerPoliticianAndYearH(st);

            ViewBag.EmotionStatsH = emotionStatsH;

            Dictionary<string, int> st_count = st
            .GroupBy(s => s.osobaid)
            .ToDictionary(g => g.Key, g => g.Count());

            ViewBag.st_count=st_count;

            Dictionary<string, Dictionary<int, int>> st_count_years = st
                .GroupBy(s => s.osobaid)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(s => s.datum.Value.Year)
                          .ToDictionary(gg => gg.Key, gg => gg.Count())
                );
            ViewBag.st_count_years = st_count_years;

            var topentities = nerAnalysis.GetTopEntitiesPerPerson(st);
            var statementsPerEntity = nerAnalysis.GetStatementsPerTopEntity(st, topentities);

            var allPieChartData = new Dictionary<string, Dictionary<string, List<EntityFrequency>>>();
            var sentimentHist = new Dictionary<string, Dictionary<string, double[]>>();
            var emotionData = new Dictionary<string, Dictionary<string, List<EmotionDistribution>>>();
            var sentiment_half_pe = new Dictionary<string, Dictionary<string, Dictionary<string, double>>>();
            var year_count_pe = new Dictionary<string, Dictionary<string, Dictionary<int,int>>>();
            foreach (var kvp in statementsPerEntity)
            {
                
                if (!allPieChartData.ContainsKey(kvp.Key))
                {
                    allPieChartData[kvp.Key] = new Dictionary<string, List<EntityFrequency>>();
                }
                if (!sentimentHist.ContainsKey(kvp.Key))
                {
                    sentimentHist[kvp.Key] = new Dictionary<string, double[]>();
                }
                if (!emotionData.ContainsKey(kvp.Key))
                {
                    emotionData[kvp.Key] = new Dictionary<string, List<EmotionDistribution>>();
                }
                if (!sentiment_half_pe.ContainsKey(kvp.Key))
                {
                    sentiment_half_pe[kvp.Key] = new Dictionary<string, Dictionary<string, double>>();
                }
                if (!year_count_pe.ContainsKey(kvp.Key))
                {
                    year_count_pe[kvp.Key] = new Dictionary<string, Dictionary<int, int>>();
                }
                for (int i = 0; i < kvp.Value.Count; i++)
                {


                    //string fileName = $"{kvp.Key}_{topentities[kvp.Key][i]}.csv";
                    //textAnalysis.ExportTexts(kvp.Value[i], fileName);

                    var res = nerAnalysis.PoliticEntityPieChart(kvp.Value[i],mix ,topentities[kvp.Key][i]);
                    allPieChartData[kvp.Key][topentities[kvp.Key][i]] = res;


                    sentimentHist[kvp.Key][topentities[kvp.Key][i]] = kvp.Value[i].Select(s => s.Sentiment).ToArray();

                    var emotionDistribution = emotionAnalysis.PrepareEmotionDistribution(kvp.Value[i]);
                    emotionData[kvp.Key][topentities[kvp.Key][i]] = emotionDistribution;

                    var time_sentiment_pe = sentimentAnalysis.CalculateAverageSentimentByHalfYear(kvp.Value[i])[kvp.Key];


                    if (!sentiment_half_pe[kvp.Key].ContainsKey(topentities[kvp.Key][i]))
                    {
                        sentiment_half_pe[kvp.Key][topentities[kvp.Key][i]] = new Dictionary<string, double>();
                    }
                    sentiment_half_pe[kvp.Key][topentities[kvp.Key][i]] = time_sentiment_pe;

                    if (!year_count_pe[kvp.Key].ContainsKey(topentities[kvp.Key][i]))
                    {
                        year_count_pe[kvp.Key][topentities[kvp.Key][i]] = new Dictionary<int, int>();
                    }

                    var count_years = nerAnalysis.GetStatementsPerYear(kvp.Value[i]);
                    year_count_pe[kvp.Key][topentities[kvp.Key][i]] = count_years;

                }
            }

            ViewBag.allpiecharts = allPieChartData;

            ViewBag.sentHist = sentimentHist;
            ViewBag.emotionData = emotionData;
            ViewBag.sentiment_half_pe = sentiment_half_pe;
            ViewBag.year_count_pe = year_count_pe;
            
            var sentimentAll = st.Select(s => s.Sentiment).ToArray();
            ViewBag.sentimentAll = sentimentAll;
            var piechartAll = nerAnalysis.PoliticEntityPieChart(st,mix);
            ViewBag.piechartAll = piechartAll;
            var piechartAll_names = nerAnalysis.PoliticEntityPieChart(st, names);
            ViewBag.piechartAll_names = piechartAll_names;
            var emotionDistributionAll = emotionAnalysis.PrepareEmotionDistribution(st);
            ViewBag.emotionAll = emotionDistributionAll;


            var st_negative = st.Where(s => s.Sentiment < -0.5).ToList();
            var st_positive= st.Where(s => s.Sentiment >0.5).ToList();

            var negative_ner= nerAnalysis.PoliticEntityPieChart(st_negative,mix);
            var negative_names = nerAnalysis.PoliticEntityPieChart(st_negative,names);
            ViewBag.neg_mix = negative_ner;
            ViewBag.neg_names = negative_names;
            var positive_ner = nerAnalysis.PoliticEntityPieChart(st_positive, mix);
            var positive_names = nerAnalysis.PoliticEntityPieChart(st_positive, names);
            ViewBag.pos_mix = positive_ner;
            ViewBag.pos_names = positive_names;

            Dictionary<string, List<EntityFrequency>> emotionsNerMix = new Dictionary<string, List<EntityFrequency>>();
            Dictionary<string, List<EntityFrequency>> emotionsNerNames = new Dictionary<string, List<EntityFrequency>>();
                       

            foreach(var emotion in uniqueEmotions)
            {
                var st_e = st.Where(s => s.emotions.Any(x => x.emotion == emotion)).ToList();

                var enm= nerAnalysis.PoliticEntityPieChart(st_e, mix);
                var enn= nerAnalysis.PoliticEntityPieChart(st_e, names);

                if(enm.Any())
                {
                    emotionsNerMix[emotion] = enm;
                }
                if (enn.Any())
                {
                    emotionsNerNames[emotion] = enn;
                }
                
            }
            ViewBag.emotionsNerMix = emotionsNerMix;
            ViewBag.emotionsNerNames = emotionsNerNames;


            var top_sim = rhetoricAnalysis.LoadSimilarities();
            ViewBag.top_sim=top_sim;


            //await statementDataDB.SaveSentimentToDB();
            //await nerAnalysis.SaveNERToDB();
            /*List<string> pol = new List<string> {"karel-havlicek", "lubomir-volny"};
            foreach(var p in pol)
            {
                var data1 = await statementData.GetDataFromAPI("2016-01-01","2018-12-31",p);
                var data2 = await statementData.GetDataFromAPI("2020-01-02", "2023-12-31", p);
                data1.AddRange(data2);
                await statementData.StoreToDatabase(data1);
            }

            var i = 1;*/
            /*List<Statement> st = await statementData.LoadFromDatabase();
           /*textAnalysis.ExportTexts(st, "sentiment_mix.csv");
           //await statementDataDB.RemoveDuplicateMentions(st);
           /* await nerAnalysis.LoadNERFromDB(st);
            await topicAnalysis.LoadTopics(st);
           statementDataDB.UpdateClustersFromCsv("C:/Users/HONZA/Desktop/diplomka/clusters.csv",st);*/

            /*
            string filePath = "statements_vectors.csv";
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine("server,osobaid,datum,id,pocetSlov,Sentiment,ethos,pathos,logos,neg,neu,pos,manipulation,populism,topics,entities");

                foreach (var s in st)
                {
                    string topics = s.topics != null && s.topics.Any() ? $"\"{string.Join(";", s.topics)}\"" : "\"\"";
                    string entities = s.Entities != null && s.Entities.Any() ? $"\"{string.Join(";", s.Entities.Select(e => e.EntityText))}\"" : "\"\"";

                    string SentimentStr = s.Sentiment.ToString().Replace(",", ".") ?? "";
                    string EthosStr = s.ethos.ToString().Replace(",", ".") ?? "";
                    string PathosStr = s.pathos.ToString().Replace(",", ".") ?? "";
                    string LogosStr = s.logos.ToString().Replace(",", ".") ?? "";
                    string NegStr = s.neg.ToString().Replace(",", ".") ?? "";
                    string NeuStr = s.neu.ToString().Replace(",", ".") ?? "";
                    string PosStr = s.pos.ToString().Replace(",", ".") ?? "";
                    string ManipulationStr = s.manipulation.ToString().Replace(",", ".") ?? "";
                    string PopulismStr = s.populism.ToString().Replace(",", ".") ?? "";

                    // Poté ulož hodnoty místo původních
                    writer.WriteLine($"{s.server},{s.osobaid},{s.datum},{s.id},{s.pocetSlov},{SentimentStr},{EthosStr},{PathosStr},{LogosStr},{NegStr},{NeuStr},{PosStr},{ManipulationStr},{PopulismStr},{topics},{entities}");

                }
            }*/


            /*List<Statement> st_m = statementData.LoadDataWMentions(st);
            await nerAnalysis.LoadNERFromDB(st);
            await topicAnalysis.LoadTopics(st);
            List<Statement> st_topic = st.Where(x => x.topics.Any()).ToList();

            topicAnalysis.PrepareStatementClusteringData(st_topic, "statements_matrix.csv");
            topicAnalysis.PrepareTopicClusteringData(st_topic, "topics_matrix.csv");
            //await statementDataDB.UpdateStatementsFromCsv("C:/Users/HONZA/Desktop/diplomka/output_retorika_oprava.csv");

            //await statementDataDB.InsertTopicsFromFile("C:/Users/HONZA/Desktop/diplomka/analyza_temata1.csv");
            //await statementDataDB.SaveSentimentToDB();
            /*List<Statement> st = await statementData.GetDataFromAPI();
            await statementData.StoreToDatabase(st);*/
            //await statementData.UpdateLanguageForIds("C:/Users/HONZA/Desktop/diplomka/english_ids.csv");


            //await nerAnalysis.UpdateNER();
            // await nerAnalysis.LoadNERFromDB(st);

            /* var ner_year = nerAnalysis.GetTopEntitiesPoliticPerYear(st);
             var babis = ner_year["andrej-babis"];
             ViewBag.babis_year_ent = babis;
             var i = 1;*/
           
            //await nerAnalysis.SaveNERToDB();
            //List<Statement> st_m = statementData.LoadDataWMentions(st);
            /*List<Statement> st = await statementData.LoadFromDatabase();
            textAnalysis.ExportTexts(st, "babis_ceske.csv");
            textAnalysis.ExportTexts(st.Where(x=>x.datum.Value.Year==2016).ToList(), "babis_ceske_2016.csv");
            textAnalysis.ExportTexts(st.Where(x => x.datum.Value.Year == 2017).ToList(), "babis_ceske_2017.csv");
            textAnalysis.ExportTexts(st.Where(x => x.datum.Value.Year == 2018).ToList(), "babis_ceske_2018.csv");
            textAnalysis.ExportTexts(st.Where(x => x.datum.Value.Year == 2019).ToList(), "babis_ceske_2019.csv");
            textAnalysis.ExportTexts(st.Where(x => x.datum.Value.Year == 2020).ToList(), "babis_ceske_2020.csv");
            textAnalysis.ExportTexts(st.Where(x => x.datum.Value.Year == 2021).ToList(), "babis_ceske_2021.csv");
            textAnalysis.ExportTexts(st.Where(x => x.datum.Value.Year == 2022).ToList(), "babis_ceske_2022.csv");
            textAnalysis.ExportTexts(st.Where(x => x.datum.Value.Year == 2023).ToList(), "babis_ceske_2023.csv");*/

            /*networkAnalysis.SaveMatrixToCsv(st_m, "matrix_avg_slova.csv");
            var st_m_t = st_m.Where(x => x.server == "Twitter").ToList();
            var st_m_f = st_m.Where(x => x.server == "Facebook").ToList();
            networkAnalysis.SaveMatrixToCsv(st_m_t, "matrix_avg_slova_Twitter.csv");
            networkAnalysis.SaveMatrixToCsv(st_m_f, "matrix_avg_slova_Facebook.csv");*/

            /*
            List<Statement> st = await statementData.LoadFromDatabase();
            await nerAnalysis.LoadNERFromDB(st);
            var result = nerAnalysis.CalculateCooccurrenceMatrix(st);

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "cooccurrence_net_all_new.csv");
            nerAnalysis.SaveCooccurrenceMatrixToCsv(result.TopEntities, result.Matrix, filePath);*/
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }



        public async Task<IActionResult> PoliticDetail(string politic_id, [FromServices] StylometryAnalysis stylometryAnalysis, [FromServices] RhetoricAnalysis rhetoricAnalysis, [FromServices] EmotionAnalysis emotionAnalysis, [FromServices] SentimentAnalysis sentimentAnalysis, [FromServices] TopicAnalysis topicAnalysis, [FromServices] NetworkAnalysis networkAnalysis, [FromServices] TextAnalysis textAnalysis, [FromServices] NERAnalysis nerAnalysis, [FromServices] StatementDataDB statementDataDB)
        {
            List<Statement> st = _statementData.Statements;
            /*List<Politic> politicians = await statementData.GetAllPoliticiansAsync();
            using (var writer = new StreamWriter("politici_strana.csv"))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                csv.WriteRecords(politicians);
            }*/


            var st_rt = st;


            //var result = nerAnalysis.CalculateCooccurrenceMatrixNormalizedJC(st);
            //nerAnalysis.SaveMatrixToCsv(result.TopEntities, result.Matrix, "coocurance_matrix_mix_cos.csv");

            /* string filePath = Path.Combine(Directory.GetCurrentDirectory(), "cooccurrence_net_names_jaccard_0_05.csv");
             nerAnalysis.SaveCooccurrenceMatrixToCsv(result.TopEntities, result.Matrix, filePath);*/


            st = st.Where(x => !x.RT).ToList();




            await emotionAnalysis.LoadEmotionFromDB(st);

            List<string> names = new List<string> { "ps" };
            List<string> mix = new List<string> { "io", "if", "ic", "mn", "ms", "o_", "oa" };


            await nerAnalysis.LoadNERFromDB(st);

            var uniquePoliticIds = st.Select(s => s.osobaid).Distinct();

            var uniqueEmotions = st
           .SelectMany(s => s.emotions)
           .Select(e => e.emotion)
           .Distinct()
           .ToList();

            var unique_ngrams = stylometryAnalysis.LoadUniqueNGrams("C:/Users/HONZA/Desktop/diplomka/n_grams/unique_ngrams/unique_ngrams_all_2019.csv");
            ViewBag.unique_ngrams = unique_ngrams;
            var simWords = stylometryAnalysis.LoadSimWords("C:/Users/HONZA/Desktop/diplomka/texty_csv");
            ViewBag.simWords = simWords;
            


            st = st.Where(x => x.osobaid == politic_id).ToList();
            st_rt = st_rt.Where(x => x.osobaid == politic_id).ToList();

            var time_sentiment = sentimentAnalysis.CalculateAverageSentimentByHalfYear(st);
            var time_sentiment_q = sentimentAnalysis.CalculateAverageSentimentByQuarter(st);
            ViewBag.time_sentiment = time_sentiment;
            ViewBag.time_sentiment_q = time_sentiment_q;
            Dictionary<string, ChartData> server_count = new Dictionary<string, ChartData>();
            foreach (var p in uniquePoliticIds)
            {
                var sc = _statementData.GetChartData(st_rt);
                server_count[p] = sc;
            }



            ViewBag.server_count = server_count;

            foreach (var statement in st)
            {
                statement.emotions = statement.emotions
                    .Where(e => e.score > 0.7)
                    .ToList();

                if (!statement.emotions.Any())
                {
                    statement.emotions.Add(new EmotionData
                    {
                        emotion = "Neutral",
                        score = 0.9
                    });
                }

                statement.emotions = statement.emotions.OrderByDescending(e => e.score).Take(1).ToList();
            }

            var emotionStatsQ = emotionAnalysis.GetEmotionPercentagesPerPoliticianAndYearQ(st);

            ViewBag.EmotionStatsQ = emotionStatsQ;

            var emotionStatsH = emotionAnalysis.GetEmotionPercentagesPerPoliticianAndYearH(st);

            ViewBag.EmotionStatsH = emotionStatsH;

            Dictionary<string, int> st_count = st
            .GroupBy(s => s.osobaid)
            .ToDictionary(g => g.Key, g => g.Count());

            ViewBag.st_count = st_count;

            Dictionary<string, Dictionary<int, int>> st_count_years = st
                .GroupBy(s => s.osobaid)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(s => s.datum.Value.Year)
                          .ToDictionary(gg => gg.Key, gg => gg.Count())
                );
            ViewBag.st_count_years = st_count_years;

            var topentities = nerAnalysis.GetTopEntitiesPerPerson(st);
            var statementsPerEntity = nerAnalysis.GetStatementsPerTopEntity(st, topentities);

            var allPieChartData = new Dictionary<string, Dictionary<string, List<EntityFrequency>>>();
            var sentimentHist = new Dictionary<string, Dictionary<string, double[]>>();
            var emotionData = new Dictionary<string, Dictionary<string, List<EmotionDistribution>>>();
            var sentiment_half_pe = new Dictionary<string, Dictionary<string, Dictionary<string, double>>>();
            var year_count_pe = new Dictionary<string, Dictionary<string, Dictionary<int, int>>>();
            foreach (var kvp in statementsPerEntity)
            {

                if (!allPieChartData.ContainsKey(kvp.Key))
                {
                    allPieChartData[kvp.Key] = new Dictionary<string, List<EntityFrequency>>();
                }
                if (!sentimentHist.ContainsKey(kvp.Key))
                {
                    sentimentHist[kvp.Key] = new Dictionary<string, double[]>();
                }
                if (!emotionData.ContainsKey(kvp.Key))
                {
                    emotionData[kvp.Key] = new Dictionary<string, List<EmotionDistribution>>();
                }
                if (!sentiment_half_pe.ContainsKey(kvp.Key))
                {
                    sentiment_half_pe[kvp.Key] = new Dictionary<string, Dictionary<string, double>>();
                }
                if (!year_count_pe.ContainsKey(kvp.Key))
                {
                    year_count_pe[kvp.Key] = new Dictionary<string, Dictionary<int, int>>();
                }
                for (int i = 0; i < kvp.Value.Count; i++)
                {


                    //string fileName = $"{kvp.Key}_{topentities[kvp.Key][i]}.csv";
                    //textAnalysis.ExportTexts(kvp.Value[i], fileName);

                    var res = nerAnalysis.PoliticEntityPieChart(kvp.Value[i], mix, topentities[kvp.Key][i]);
                    allPieChartData[kvp.Key][topentities[kvp.Key][i]] = res;


                    sentimentHist[kvp.Key][topentities[kvp.Key][i]] = kvp.Value[i].Select(s => s.Sentiment).ToArray();

                    var emotionDistribution = emotionAnalysis.PrepareEmotionDistribution(kvp.Value[i]);
                    emotionData[kvp.Key][topentities[kvp.Key][i]] = emotionDistribution;

                    var time_sentiment_pe = sentimentAnalysis.CalculateAverageSentimentByHalfYear(kvp.Value[i])[kvp.Key];


                    if (!sentiment_half_pe[kvp.Key].ContainsKey(topentities[kvp.Key][i]))
                    {
                        sentiment_half_pe[kvp.Key][topentities[kvp.Key][i]] = new Dictionary<string, double>();
                    }
                    sentiment_half_pe[kvp.Key][topentities[kvp.Key][i]] = time_sentiment_pe;

                    if (!year_count_pe[kvp.Key].ContainsKey(topentities[kvp.Key][i]))
                    {
                        year_count_pe[kvp.Key][topentities[kvp.Key][i]] = new Dictionary<int, int>();
                    }

                    var count_years = nerAnalysis.GetStatementsPerYear(kvp.Value[i]);
                    year_count_pe[kvp.Key][topentities[kvp.Key][i]] = count_years;

                }
            }

            ViewBag.allpiecharts = allPieChartData;

            ViewBag.sentHist = sentimentHist;
            ViewBag.emotionData = emotionData;
            ViewBag.sentiment_half_pe = sentiment_half_pe;
            ViewBag.year_count_pe = year_count_pe;

            var sentimentAll = st.Select(s => s.Sentiment).ToArray();
            ViewBag.sentimentAll = sentimentAll;
            var piechartAll = nerAnalysis.PoliticEntityPieChart(st, mix);
            ViewBag.piechartAll = piechartAll;
            var piechartAll_names = nerAnalysis.PoliticEntityPieChart(st, names);
            ViewBag.piechartAll_names = piechartAll_names;
            var emotionDistributionAll = emotionAnalysis.PrepareEmotionDistribution(st);
            ViewBag.emotionAll = emotionDistributionAll;


            var st_negative = st.Where(s => s.Sentiment < -0.5).ToList();
            var st_positive = st.Where(s => s.Sentiment > 0.5).ToList();

            var negative_ner = nerAnalysis.PoliticEntityPieChart(st_negative, mix);
            var negative_names = nerAnalysis.PoliticEntityPieChart(st_negative, names);
            ViewBag.neg_mix = negative_ner;
            ViewBag.neg_names = negative_names;
            var positive_ner = nerAnalysis.PoliticEntityPieChart(st_positive, mix);
            var positive_names = nerAnalysis.PoliticEntityPieChart(st_positive, names);
            ViewBag.pos_mix = positive_ner;
            ViewBag.pos_names = positive_names;

            Dictionary<string, List<EntityFrequency>> emotionsNerMix = new Dictionary<string, List<EntityFrequency>>();
            Dictionary<string, List<EntityFrequency>> emotionsNerNames = new Dictionary<string, List<EntityFrequency>>();


            foreach (var emotion in uniqueEmotions)
            {
                var st_e = st.Where(s => s.emotions.Any(x => x.emotion == emotion)).ToList();

                var enm = nerAnalysis.PoliticEntityPieChart(st_e, mix);
                var enn = nerAnalysis.PoliticEntityPieChart(st_e, names);

                if (enm.Any())
                {
                    emotionsNerMix[emotion] = enm;
                }
                if (enn.Any())
                {
                    emotionsNerNames[emotion] = enn;
                }

            }
            ViewBag.emotionsNerMix = emotionsNerMix;
            ViewBag.emotionsNerNames = emotionsNerNames;


            var top_sim = rhetoricAnalysis.LoadSimilarities();
            ViewBag.top_sim = top_sim;



            return View();
        }
        public async Task<IActionResult> PoliticianList()
        {

            List<string> top_politics = new() { "andrej-babis", "tomio-okamura", "lubomir-volny", "adam-vojtech", "miroslav-kalousek",
            "alena-schillerova", "pavel-belobradek", "petr-fiala", "karel-havlicek", "milos-zeman" };

            ViewBag.politician_list = top_politics;


            return View();
        }
        public IActionResult Sentiment([FromServices] StylometryAnalysis stylometryAnalysis, [FromServices] ClusterAnalysis clusterAnalysis, [FromServices] RhetoricAnalysis rhetoricAnalysis, [FromServices] TopicAnalysis topicAnalysis , [FromServices] StatementDataDB statementDataDB, [FromServices] TextAnalysis textAnalysis, [FromServices] SentimentAnalysis sentimentAnalysis, [FromServices] NERAnalysis nerAnalysis, [FromServices] AssociationRulesGenerator association)
        {
            List<Statement> st = _statementData.Statements;
            st = st.Where(x => x.Sentiment != 666).ToList();
            var st_sent_m = _statementData.LoadDataWMentions(st);
            var politiciansentiments = sentimentAnalysis.PoliticianSentiments(st.Where(s => s.RT == false).ToList());
            ViewBag.pol_sentiments = politiciansentiments;


            var polaritycounts = st.Select(s => s.Sentiment).ToArray();
            ViewBag.polaritycounts = polaritycounts;

            var polaritycountsM = st_sent_m.Select(s => s.Sentiment).ToArray();
            ViewBag.polaritycountsM = polaritycountsM;

            //4 - počet příspěvků s nějakým sentimentem ve kterých je daný politik zmíněn
            var mentionspolarity = sentimentAnalysis.GetMentionsPolarity(st);
            ViewBag.mentionspolarity = mentionspolarity;

            //5 - Rozdělení sentimentu ve příspěvcích politiků
            var polarity = sentimentAnalysis.GetPolarity(st);
            ViewBag.polarity = polarity;
            var (classicAvg, retweetAvg) = sentimentAnalysis.RT_sentiment(st);
            ViewBag.classicAvg = classicAvg;
            ViewBag.retweetAvg = retweetAvg;


            var server_sentiment = st
                .GroupBy(s => s.server)
                .Select(g => new { Server = g.Key, Sentiments = g.Select(s => s.Sentiment).ToList() })
                .ToList();
            ViewBag.server_sentiment = server_sentiment;

            var RTpolsentiments = sentimentAnalysis.PoliticianSentiments(st.Where(s => s.RT == true).ToList());
            ViewBag.RTpolsentiments = RTpolsentiments;
            var time_sentiment = sentimentAnalysis.CalculateAverageSentimentByHalfYear(st);
            ViewBag.time_sentiment = time_sentiment;

            var st_noRT = st.Where(x => !x.RT).ToList();

         
            var extreme_s = sentimentAnalysis.CalculateSentimentRatios(st_noRT);
            ViewBag.extreme_s = extreme_s;

            var avgrt = sentimentAnalysis.CalculateAvgSentimentRT(st);
            ViewBag.avgrt = avgrt;
            ViewBag.mcount_sentiment = sentimentAnalysis.MentionsAvgSentiment(st);

            var avgsentiment = sentimentAnalysis.CalculateAvgSentimentPolitician(st);

            avgsentiment = avgsentiment.OrderBy(x => x.AverageSentiment).ToList();

            var avgsentimentMentions = sentimentAnalysis.CalculateAvgSentimentPolitician(st_sent_m);
            var avgsentimentFM = sentimentAnalysis.CalculateAvgSentimentPoliticianFromMentions(st);
            avgsentimentFM = avgsentimentFM.OrderBy(x => x.AverageSentiment).ToList();
            ViewBag.avgsentimentFM = avgsentimentFM.OrderByDescending(x => x.Count_m).ToList();

            ViewBag.avgsentiment = avgsentiment;
            ViewBag.avgsentimentMentions = avgsentimentMentions;
            var combined = from item1 in avgsentiment
                           join item2 in avgsentimentMentions
                           on item1.OsobaID equals item2.OsobaID into itemGroup
                           from item2 in itemGroup.DefaultIfEmpty() // DefaultIfEmpty provede left join
                           select new CombinedPoliticianSentiment
                           {
                               OsobaID = item1.OsobaID,
                               AverageSentiment1 = item1.AverageSentiment,
                               count = item1.Count,
                               avgpos = item1.AveragePos,
                               avgneu = item1.AverageNeu,
                               avgneg = item1.AverageNeg,
                               count_m = item2?.Count ?? 100,
                               AverageSentiment2 = item2?.AverageSentiment ?? 100 // Pokud item2 neexistuje, použije se null
                           };

            ViewBag.avg_combined = combined.OrderByDescending(x => x.count).ToList();
            return View();
        }




        public async Task<IActionResult> NER([FromServices] StylometryAnalysis stylometryAnalysis, [FromServices] ClusterAnalysis clusterAnalysis, [FromServices] RhetoricAnalysis rhetoricAnalysis, [FromServices] TopicAnalysis topicAnalysis, [FromServices] StatementData statementData, [FromServices] StatementDataDB statementDataDB, [FromServices] TextAnalysis textAnalysis, [FromServices] SentimentAnalysis sentimentAnalysis, [FromServices] NERAnalysis nerAnalysis, [FromServices] AssociationRulesGenerator association)
        {
            List<Statement> st = _statementData.Statements;
            await nerAnalysis.LoadNERFromDB(st);
            var st_noRT = st.Where(x => !x.RT).ToList();

            var entity_ratio = nerAnalysis.CalculateEntityToWordRatio(st_noRT);
            ViewBag.PoliticianRatios = entity_ratio;

            List<string> types = new List<string> { "io", "gc", "P", "ps", "gt", "om", "gu", "ty", "tm", "ic", "ms", "gl", "gr", "op", "oa", "if" };
            var groupedEntities = nerAnalysis.GetTopEntitiesByType(st, types);
            ViewBag.nercounts_types = groupedEntities;

            var (entityTypes, typeCounts) = nerAnalysis.GetTopEntityTypes(st);

           
            ViewBag.EntityTypes = entityTypes;
            ViewBag.TypeCounts = typeCounts;
            var entitySentimentData = nerAnalysis.CalculateAverageSentimentType(st);

            ViewBag.EntitySentiments = entitySentimentData;

            var alltypes = entitySentimentData
                .Select(x => x.EntityType)
                .Distinct()
                .ToList();
            ViewBag.Alltypes = alltypes;


            return View();
        }


        public async Task<IActionResult> Statement([FromServices] StylometryAnalysis stylometryAnalysis,[FromServices] ClusterAnalysis clusterAnalysis, [FromServices] RhetoricAnalysis rhetoricAnalysis, [FromServices] TopicAnalysis topicAnalysis, [FromServices] StatementData statementData, [FromServices] StatementDataDB statementDataDB, [FromServices] TextAnalysis textAnalysis, [FromServices] SentimentAnalysis sentimentAnalysis, [FromServices] NERAnalysis nerAnalysis, [FromServices] AssociationRulesGenerator association)
        {

            //List<Statement> st_m = statementData.LoadDataWMentions(st);

            //await statementData.UpdateLanguageForIds("C:/Users/HONZA/Desktop/diplomka/detections.txt");
            //await statementDataDB.SaveSentimentToDB();
            //List<Statement> st = await statementData.LoadFromDatabaseWSentiment();
            //textAnalysis.ExportTexts(st, "texts_new2.csv");

            List<string> top_politics = new() { "andrej-babis", "tomio-okamura", "lubomir-volny", "adam-vojtech", "miroslav-kalousek",
            "alena-schillerova", "pavel-belobradek", "petr-fiala", "karel-havlicek", "milos-zeman", "alena-gajduskova",
            "tomas-prouza", "jiri-dolejs", "marek-zenisek", "marian-jurecka", "michaela-sojdrova", "miroslav-antl",
            "jan-zahradil", "martin-kolovratnik", "mikulas-peksa", "zdenek-hrib", "jan-skopecek", "patrik-nacher",
            "jan-hamacek", "jan-bartosek", "zuzana-majerova-zahradnikova", "tomas-martinek", "tomas-zdechovsky",
            "lukas-wagenknecht", "barbora-koranova", "radim-fiala", "jan-lipavsky", "dominik-feri", "marek-vyborny",
            "jan-cizinsky", "katerina-valachova", "ludek-niedermayer", "alexandra-udzenija", "jan-kobza", "marketa-adamova" };


            var avg_sentence = stylometryAnalysis.LoadAndSortPoliticians("C:/Users/HONZA/Desktop/diplomka/avg_sentence_length.csv");

            avg_sentence = avg_sentence.Where(x => top_politics.Contains(x.Name)).ToList();
            ViewBag.avg_sentence = avg_sentence;

            var avg_sentence_emoce = stylometryAnalysis.LoadAndSortPoliticians("C:/Users/HONZA/Desktop/diplomka/prumerna_delka_vet_emoce.csv");
            ViewBag.avg_sentence_emoce = avg_sentence_emoce;


            List<Statement> st = _statementData.Statements;
            List<Statement> st_m = statementData.LoadDataWMentions(st);
            await nerAnalysis.LoadNERFromDB(st);
            await topicAnalysis.LoadTopics(st);
            List<Statement> st_topic = st.Where(x => x.topics.Any()).ToList();

            statementDataDB.UpdateClustersFromCsv("C:/Users/HONZA/Desktop/diplomka/clusters.csv", st_topic);

            var clusterEntities = clusterAnalysis.GetClusterEntities(st_topic);
            var clusterTopics = clusterAnalysis.GetClusterTopics(st_topic);


            ViewBag.ClusterEntities = clusterEntities;
            ViewBag.ClusterTopics = clusterTopics;
            var clusterSentiment = clusterAnalysis.GetClusterSentiment(st_topic); // Funkce pro získání sentimentu
            var clusterEthos = clusterAnalysis.GetClusterEthos(st_topic); // Funkce pro získání ethos
            var clusterPathos = clusterAnalysis.GetClusterPathos(st_topic); // Funkce pro získání pathos
            var clusterLogos = clusterAnalysis.GetClusterLogos(st_topic); // Funkce pro získání logos

            // Uložení do ViewBag pro zobrazení ve View
            ViewBag.ClusterEntities = clusterEntities;
            ViewBag.ClusterTopics = clusterTopics;
            ViewBag.ClusterSentiment = clusterSentiment;
            ViewBag.ClusterEthos = clusterEthos;
            ViewBag.ClusterPathos = clusterPathos;
            ViewBag.ClusterLogos = clusterLogos;


            List<Statement> st_rh = st.Where(x => x.logos != 666).ToList();
            var rh_hist = rhetoricAnalysis.GroupedHistogramRH(st_rh);
            ViewBag.rh_hist = rh_hist;
            var boxplotData = rhetoricAnalysis.BOXRH(st_rh);
            ViewBag.rh_box = boxplotData;

            var ner_year = nerAnalysis.GetTopEntitiesPoliticPerYear(st);

            var babis = ner_year["andrej-babis"].OrderByDescending(x => x.Key).ToDictionary(x => x.Key, x => x.Value); ;
            ViewBag.babis_year_ent = babis;

            //var rules = association.GenerateAssociationRules(st, minSupport: 0.0000001, minConfidence: 0);

            /* foreach (var rule in rules)
             {
                 Debug.WriteLine($"Rule: {string.Join(",", rule.Antecedent)} -> {string.Join(",", rule.Consequent)}");
                 Debug.WriteLine($"Support: {rule.Support}, Confidence: {rule.Confidence}");
             }*/


            //nerAnalysis.GenerateVectors(st, "clustering.csv");
            //var politic_topentities = nerAnalysis.GetTopEntitiesPolitic(st);
            var st_sentiment = st.Where(x => x.Sentiment != 666).ToList();
            var st_sent_m = statementData.LoadDataWMentions(st_sentiment);

            var st_noRT = st_sentiment.Where(x => !x.RT).ToList();

            var entity_ratio = nerAnalysis.CalculateEntityToWordRatio(st_noRT);
            ViewBag.PoliticianRatios = entity_ratio;
            var extreme_s = sentimentAnalysis.CalculateSentimentRatios(st_noRT);
            ViewBag.extreme_s = extreme_s;

            var result = nerAnalysis.CalculateCooccurrenceMatrix(st_sentiment);

            /*string filePath = Path.Combine(Directory.GetCurrentDirectory(), "cooccurrence_net1.csv");
            nerAnalysis.SaveCooccurrenceMatrixToCsv(result.TopEntities, result.Matrix, filePath);*/


            ViewBag.TopEntities = result.TopEntities;
            ViewBag.CooccurrenceMatrix = result.Matrix;



            var time_sentiment = sentimentAnalysis.CalculateAverageSentimentByHalfYear(st_sentiment);
            ViewBag.time_sentiment = time_sentiment;
            var entitySentimentData = nerAnalysis.CalculateAverageSentimentType(st_sentiment);

            ViewBag.EntitySentiments = entitySentimentData;


            var alltypes = entitySentimentData
                .Select(x => x.EntityType)
                .Distinct()
                .ToList();
            ViewBag.Alltypes = alltypes;

            //await nerAnalysis.SaveNERToDB();


            /*List<KeyValuePair<string, int>> ner_counts =nerAnalysis.GetEntityNamesCount(st);
            ViewBag.ner_counts = ner_counts;*/



            List<string> types = new List<string> { "io", "gc", "P", "ps", "gt", "om", "gu", "ty", "tm", "ic", "ms", "gl", "gr", "op", "oa", "if" };
            var groupedEntities = nerAnalysis.GetTopEntitiesByType(st_sentiment, types);
            ViewBag.nercounts_types = groupedEntities;

            var (entityTypes, typeCounts) = nerAnalysis.GetTopEntityTypes(st_sentiment);

            // Uložení do ViewBag
            ViewBag.EntityTypes = entityTypes;
            ViewBag.TypeCounts = typeCounts;

            var ner_sentiment = nerAnalysis.CalculateAverageSentiment(st_sentiment);
            ViewBag.ner_sentiment = ner_sentiment;



            var avgrt = sentimentAnalysis.CalculateAvgSentimentRT(st_sentiment);
            ViewBag.avgrt = avgrt;

            var (classicAvg, retweetAvg) = sentimentAnalysis.RT_sentiment(st_sentiment);
            ViewBag.classicAvg = classicAvg;
            ViewBag.retweetAvg = retweetAvg;

            //ViewBag.st_sentiment =st_m.Select(s => new { Mentions = s.politicizminky.Count(), Sentiment = s.Sentiment }).ToList();

            ViewBag.rt_polarity = sentimentAnalysis.GetRTPolarity(st_sentiment);
            var server_sentiment = st_sentiment
                .GroupBy(s => s.server)
                .Select(g => new { Server = g.Key, Sentiments = g.Select(s => s.Sentiment).ToList() })
                .ToList();
            ViewBag.server_sentiment = server_sentiment
;
            ViewBag.mcount_sentiment = sentimentAnalysis.MentionsAvgSentiment(st_sentiment);

            //textAnalysis.ExportTexts(st, "texts_new.csv");
            var avgsentiment = sentimentAnalysis.CalculateAvgSentimentPolitician(st_sentiment);

            avgsentiment = avgsentiment.OrderBy(x => x.AverageSentiment).ToList();

            var avgsentimentMentions = sentimentAnalysis.CalculateAvgSentimentPolitician(st_sent_m);
            var avgsentimentFM = sentimentAnalysis.CalculateAvgSentimentPoliticianFromMentions(st);
            avgsentimentFM = avgsentimentFM.OrderBy(x => x.AverageSentiment).ToList();
            var politiciansentiments = sentimentAnalysis.PoliticianSentiments(st.Where(s => s.RT == false).ToList());
            ViewBag.pol_sentiments = politiciansentiments;
            var polaritycounts = sentimentAnalysis.PolarityCounts(st);
            ViewBag.polaritycounts = polaritycounts;
            var polaritycountsM = sentimentAnalysis.PolarityCounts(st_sent_m);
            ViewBag.polaritycountsM = polaritycountsM;

            //4 - počet příspěvků s nějakým sentimentem ve kterých je daný politik zmíněn
            var mentionspolarity = sentimentAnalysis.GetMentionsPolarity(st_sentiment);
            ViewBag.mentionspolarity = mentionspolarity;

            //5 - Rozdělení sentimentu ve příspěvcích politiků
            var polarity = sentimentAnalysis.GetPolarity(st_sentiment);
            ViewBag.polarity = polarity;

            /* var avgSentiments = sentimentAnalysis.AvgSentimentMonth(st);
             avgSentiments=avgSentiments.OrderBy(x => x.Key).ToDictionary(entry => entry.Key, entry => entry.Value);


             ViewBag.Months = avgSentiments.Keys.Select(m => new DateTime(2024, m, 1).ToString("MMMM")).ToList(); // Názvy měsíců
             ViewBag.Sentiments = avgSentiments.Values.ToList();*/

            //textAnalysis.ExportTexts(st, "texts.csv");
            //statementData.StoreToCSV(st);
            //List<Statement> st_mentions = statementData.LoadDataWMentions(st);
            //await statementDataDB.SaveStatsToDB(st,false);
            //await statementDataDB.SaveStatsToDB(st_mentions,true);
            ViewBag.avgsentimentFM = avgsentimentFM.OrderByDescending(x => x.Count_m).ToList();

            ViewBag.avgsentiment = avgsentiment;
            ViewBag.avgsentimentMentions = avgsentimentMentions;

            var RTpolsentiments = sentimentAnalysis.PoliticianSentiments(st_sentiment.Where(s => s.RT == true).ToList());
            ViewBag.RTpolsentiments = RTpolsentiments;

            var combined = from item1 in avgsentiment
                           join item2 in avgsentimentMentions
                           on item1.OsobaID equals item2.OsobaID into itemGroup
                           from item2 in itemGroup.DefaultIfEmpty() // DefaultIfEmpty provede left join
                           select new CombinedPoliticianSentiment
                           {
                               OsobaID = item1.OsobaID,
                               AverageSentiment1 = item1.AverageSentiment,
                               count = item1.Count,
                               avgpos = item1.AveragePos,
                               avgneu = item1.AverageNeu,
                               avgneg = item1.AverageNeg,
                               count_m = item2?.Count ?? 100,
                               AverageSentiment2 = item2?.AverageSentiment ?? 100 // Pokud item2 neexistuje, použije se null
                           };

            ViewBag.avg_combined = combined.OrderByDescending(x => x.count).ToList();


            ViewBag.histogramData = await statementData.GetStatementFrequencyFromDatabase(false);
            ViewBag.histogramData_F = await statementData.GetStatementFrequencyFromDatabase(false, "Facebook");
            ViewBag.histogramData_T = await statementData.GetStatementFrequencyFromDatabase(false, "Twitter");

            ViewBag.histogramDataM = await statementData.GetStatementFrequencyFromDatabase(true);
            ViewBag.histogramData_FM = await statementData.GetStatementFrequencyFromDatabase(true, "Facebook");
            ViewBag.histogramData_TM = await statementData.GetStatementFrequencyFromDatabase(true, "Twitter");





            //getmentions
            //await statementDataDB.SaveMentionsPerPoliticFrequency(st);
            ViewBag.mentionsByPolitican = await statementData.GetMentionsPerPoliticFrequencyFromDatabase();


            //await statementDataDB.SaveMentionsofPoliticiansStatsToDB(st);

            List<dynamic> mentionsOfPoliticians = await statementData.GetMentionsOfPoliticianFrequencyFromDatabase();
            List<dynamic> mentionsOfPoliticiansF = await statementData.GetMentionsOfPoliticianFrequencyFromDatabase("Facebook");
            List<dynamic> mentionsOfPoliticiansT = await statementData.GetMentionsOfPoliticianFrequencyFromDatabase("Twitter");

            ViewBag.mentionsOfPoliticians = mentionsOfPoliticians;
            ViewBag.mentionsOfPoliticiansF = mentionsOfPoliticiansF;
            ViewBag.mentionsOfPoliticiansT = mentionsOfPoliticiansT;

            //get frekvency      
            ViewBag.pmfrequency = mentionsOfPoliticians.SelectMany(obj => Enumerable.Repeat((int)obj.CountOfMentions, (int)obj.CountOfPoliticians)).ToList();
            ViewBag.pmfrequencyF = mentionsOfPoliticiansF.SelectMany(obj => Enumerable.Repeat((int)obj.CountOfMentions, (int)obj.CountOfPoliticians)).ToList();
            ViewBag.pmfrequencyT = mentionsOfPoliticiansT.SelectMany(obj => Enumerable.Repeat((int)obj.CountOfMentions, (int)obj.CountOfPoliticians)).ToList();



            //await statementDataDB.SaveWordsMentionsStatsToDB(st, false);
            //await statementDataDB.SaveWordsMentionsStatsToDB(st_mentions, true);

            //get wordsfrequency
            List<dynamic> wordsstatement = await statementData.GetWordsStatementFrequencyFromDatabase(false);
            List<dynamic> wordsstatementF = await statementData.GetWordsStatementFrequencyFromDatabase(false, "Facebook");
            List<dynamic> wordsstatementT = await statementData.GetWordsStatementFrequencyFromDatabase(false, "Twitter");


            ViewBag.wordsstatement = wordsstatement;
            ViewBag.wordsstatementF = wordsstatementF;
            ViewBag.wordsstatementT = wordsstatementT;


            ViewBag.numberWords = wordsstatement.SelectMany(obj => Enumerable.Repeat((int)obj.WordCount, (int)obj.StatementsCount)).ToList();
            ViewBag.numberWordsF = wordsstatementF.SelectMany(obj => Enumerable.Repeat((int)obj.WordCount, (int)obj.StatementsCount)).ToList();
            ViewBag.numberWordsT = wordsstatementT.SelectMany(obj => Enumerable.Repeat((int)obj.WordCount, (int)obj.StatementsCount)).ToList();


            //get words with mentions




            List<dynamic> wordsstatementM = await statementData.GetWordsStatementFrequencyFromDatabase(true);
            List<dynamic> wordsstatementFM = await statementData.GetWordsStatementFrequencyFromDatabase(true, "Facebook");
            List<dynamic> wordsstatementTM = await statementData.GetWordsStatementFrequencyFromDatabase(true, "Twitter");

            ViewBag.wordsstatementM = wordsstatementM;
            ViewBag.wordsstatementFM = wordsstatementFM;
            ViewBag.wordsstatementTM = wordsstatementTM;

            ViewBag.numberWordsM = wordsstatementM.SelectMany(obj => Enumerable.Repeat((int)obj.WordCount, (int)obj.StatementsCount)).ToList();
            ViewBag.numberWordsFM = wordsstatementFM.SelectMany(obj => Enumerable.Repeat((int)obj.WordCount, (int)obj.StatementsCount)).ToList();
            ViewBag.numberWordsTM = wordsstatementTM.SelectMany(obj => Enumerable.Repeat((int)obj.WordCount, (int)obj.StatementsCount)).ToList();



            Dictionary<string, Dictionary<string, int>> mentions = new Dictionary<string, Dictionary<string, int>>();




            var allpoliticians = await statementData.GetAllPoliticiansAsync();
            var all_politicians = allpoliticians.Where(p => p.SumWords > 0).ToList();

            //await statementDataDB.ProcessAndInsertMentions(st, 0);
            //await statementDataDB.ProcessAndInsertMentions(st_mentions, 1);


            mentions = await statementData.GetMentionsStatsFromDatabase(0);
            Dictionary<string, Dictionary<string, int>> mentionsM = new Dictionary<string, Dictionary<string, int>>(mentions);

            foreach (var p in all_politicians.Select(x => x.politic_id))
            {
                if (!mentions.ContainsKey(p))
                {
                    mentions[p] = new Dictionary<string, int>();
                }

            }

            //without mentions
            Dictionary<string, int> mentionCount = new Dictionary<string, int>();

            int sumZminky = 0;

            foreach (var ment in mentions.Values)
            {
                foreach (var p in ment.Keys)
                {

                    if (!mentionCount.ContainsKey(p))
                    {
                        mentionCount[p] = ment[p];
                    }
                    else
                    {
                        mentionCount[p] += ment[p];
                    }
                }


            }
            /*foreach (var statement in st)
            {

                if (statement.politicizminky != null && statement.politicizminky.Any())
                {
                   
                    foreach (var politik in statement.politicizminky)
                    {
                        sumZminky++;
                        if (mentionCount.ContainsKey(politik))
                        {
                            mentionCount[politik]++;
                        }
                        else
                        {
                            mentionCount[politik] = 1;
                        }
                    }
                }
            }*/


            Dictionary<string, int> mentionCountReverse = new Dictionary<string, int>();



            foreach (var politicid in mentions.Keys)
            {
                mentionCountReverse[politicid] = 0;
                foreach (var ment_count in mentions[politicid].Values)
                {
                    sumZminky += ment_count;
                    mentionCountReverse[politicid] += ment_count;
                }
            }




            //with mentions







            string maxMentionsP = mentionCount.FirstOrDefault(x => x.Value == mentionCount.Values.Max()).Key;
            int maxMentionsV = mentionCount[maxMentionsP];
            Tuple<string, int> maxMentions = new Tuple<string, int>(maxMentionsP, maxMentionsV);
            float avg = sumZminky / (float)Statement_count;

            string maxMentionsPM = mentionCount.FirstOrDefault(x => x.Value == mentionCount.Values.Max()).Key;
            int maxMentionsVM = mentionCount[maxMentionsPM];
            Tuple<string, int> maxMentionsM = new Tuple<string, int>(maxMentionsPM, maxMentionsVM);
            float avgM = sumZminky / (float)Statement_mentions_count;

            var StatementStats = await statementData.GetStatementsStats(0);
            var StatementStatsM = await statementData.GetStatementsStats(1);

            //await statementData.UpdatePoliticTableAsync(st);
            //await statementData.UpdatePoliticTableAsync(st);


            var avgwords = allpoliticians.ToDictionary(p => p.politic_id, p => p.AvgWords);
            var avgwordsM = allpoliticians.ToDictionary(p => p.politic_id, p => p.AvgWordsM);

            var medianwords = allpoliticians.ToDictionary(p => p.politic_id, p => p.MedianWords);
            var medianwordsM = allpoliticians.ToDictionary(p => p.politic_id, p => p.MedianWordsM);

            var sumwords = allpoliticians.ToDictionary(p => p.politic_id, p => p.SumWords);
            var sumwordsM = allpoliticians.ToDictionary(p => p.politic_id, p => p.SumWordsM);

            var avgmentions = allpoliticians.ToDictionary(p => p.politic_id, p => p.AvgMentions);
            var avgmentionsM = allpoliticians.ToDictionary(p => p.politic_id, p => p.SumWordsM);

            var avgwordso = StatementStats.AvgWords;
            var avgwordsoM = StatementStatsM.AvgWords;

            var medianwordso = StatementStats.MedianWords;
            var medianwordsoM = StatementStatsM.MedianWords;


            //var mentionsN = statementData.MentionsNetwork(st);


            ViewBag.avgwordsoverall = avgwordso;
            ViewBag.avgwordsoverallM = avgwordsoM;
            ViewBag.medianwordsoverall = medianwordso;
            ViewBag.medianwordsoverallM = medianwordsoM;
            ViewBag.avgmentions = avgmentions;
            ViewBag.avgmentionsM = avgmentionsM;
            ViewBag.avgwords = avgwords;
            ViewBag.avgwordsM = avgwordsM;
            ViewBag.medianwords = medianwords;
            ViewBag.medianwordsM = medianwordsM;
            ViewBag.sumwords = sumwords;
            ViewBag.sumwordsM = sumwordsM;
            ViewBag.avg = avg;
            ViewBag.max = Tuple.Create(StatementStats.MaxMentionsID, StatementStats.MaxMentionssNumber);
            ViewBag.avgM = avgM;


            var personWithMostStatements = Tuple.Create(StatementStats.MaxStatementsID, StatementStats.MaxStatementsNumber);
            var personWithMostStatementsM = Tuple.Create(StatementStatsM.MaxStatementsID, StatementStatsM.MaxStatementsNumber);

            ViewBag.maxstatements = personWithMostStatements;
            ViewBag.maxstatementsM = personWithMostStatementsM;
            ViewBag.mentionsCount = mentionCount;
            ViewBag.mentionsCountM = mentionCount;

            ViewBag.mentionsCountR = mentionCountReverse;
            ViewBag.mentionsCountRM = mentionCountReverse;
            ViewBag.mentions = mentions;
            ViewBag.mentionsM = mentionsM;

            return View();
        }


    }
}