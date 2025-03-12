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
using System.Text.Json;
using System.Diagnostics;
namespace PoliticStatements.Controllers
{

    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private int Statement_count = 135621;
        private int Statement_mentions_count = 1305;

        private readonly StatementData _statementData;
        private readonly IWebHostEnvironment _env;
        public HomeController(ILogger<HomeController> logger, StatementData statementData, IWebHostEnvironment env)
        {
            _logger = logger;
            _statementData = statementData;
            _env = env;
        }

        public async Task<IActionResult> Index([FromServices]  StylometryAnalysis stylometryAnalysis,[FromServices] RhetoricAnalysis rhetoricAnalysis,[FromServices] EmotionAnalysis emotionAnalysis, [FromServices] SentimentAnalysis sentimentAnalysis, [FromServices] TopicAnalysis topicAnalysis, [FromServices] NetworkAnalysis networkAnalysis, [FromServices] TextAnalysis textAnalysis, [FromServices] NERAnalysis nerAnalysis, [FromServices] StatementDataDB statementDataDB)
        {
            //await statementDataDB.SaveSentimentToDB();
            //sentimentAnalysis.InsertEmotionsFromFile("C:/Users/HONZA/Desktop/diplomka/bert_dodelat_dnes_emoce.csv");
            //await nerAnalysis.UpdateNER();


            //_statementData.UpdateLanguageForIds("C:/Users/HONZA/Desktop/diplomka/non_czech_texts.csv");

            List<Statement> st = JsonSerializer.Deserialize<List<Statement>>(
            JsonSerializer.Serialize(_statementData.Statements)
        );

            //textAnalysis.ExportTexts(st, "texty_2019_dodelat.csv");

            /* st = _statementData.LoadDataWMentions(st);
              _statementData.ExportMentionsToCsvSentiment(st, "mentions_network_2019_sentiment.csv");*/
            /*List<Politic> politicians = await statementData.GetAllPoliticiansAsync();
            using (var writer = new StreamWriter("politici_strana.csv"))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                csv.WriteRecords(politicians);
            }*/
            /* textAnalysis.ExportTexts(st, "texty_2019.csv");

             var st_rt = st;
             await nerAnalysis.LoadNERFromDB(st);

             var result = nerAnalysis.CalculateCooccurrenceMatrixNormalizedJC(st);
             //nerAnalysis.SaveMatrixToCsv(result.TopEntities, result.Matrix, "coocurance_matrix_mix_jaccard.csv");
             /*
             string filePath = Path.Combine(Directory.GetCurrentDirectory(), "cooccurrence_net_names_jaccard_003.csv");
             nerAnalysis.SaveCooccurrenceMatrixToCsv(result.TopEntities, result.Matrix, filePath);*/



            //var result = nerAnalysis.CalculateCooccurrenceMatrixNormalizedJC(st);
            //nerAnalysis.SaveMatrixToCsv(result.TopEntities, result.Matrix, "coocurance_matrix_mix_cos.csv");

            /* string filePath = Path.Combine(Directory.GetCurrentDirectory(), "cooccurrence_net_names_jaccard_0_05.csv");
             nerAnalysis.SaveCooccurrenceMatrixToCsv(result.TopEntities, result.Matrix, filePath);*/

            var st_rt = st;
            st = st.Where(x => !x.RT).ToList();




            //await emotionAnalysis.LoadEmotionFromDB(st);

            List<string> names = new List<string> { "ps" };
            List<string> mix = new List<string> { "io", "if", "ic", "mn", "ms", "o_", "oa" };


            //await nerAnalysis.LoadNERFromDB(st);

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

        public static double CalculateMedian(List<int> values)
        {
            if (values.Count == 0) return 0; // Ochrana proti prázdnému seznamu

            values.Sort(); // Seřazení hodnot

            int mid = values.Count / 2;
            return values.Count % 2 == 0
                ? (values[mid - 1] + values[mid]) / 2.0 // Průměr dvou prostředních čísel
                : values[mid]; // Prostřední hodnota
        }
        public IActionResult StatementCount()
        {
            List<Statement> st = JsonSerializer.Deserialize<List<Statement>>(
           JsonSerializer.Serialize(_statementData.Statements)
       );
            //all
            Dictionary<string, int> statementCounts = st
                .GroupBy(s => s.datum.Value.Year.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            statementCounts["all"] = st.Count;
            ViewBag.statementCounts= statementCounts;

            Dictionary<string, int> uniquePersonsPerYear = st
                .GroupBy(s => s.datum.Value.Year.ToString())  
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(s => s.osobaid).Distinct().Count()
            );

            
            uniquePersonsPerYear["all"] = st.Select(s => s.osobaid).Distinct().Count();
            ViewBag.uniquePoliticians = uniquePersonsPerYear;

            //facebook
            var stFB = st.Where(x => x.server == "Facebook").ToList();
            Dictionary<string, int> statementCountsFB = stFB
                .GroupBy(s => s.datum.Value.Year.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            statementCountsFB["all"] = stFB.Count;
            ViewBag.statementCountsFB = statementCountsFB;

            Dictionary<string, int> uniquePersonsPerYearFB = stFB
                .GroupBy(s => s.datum.Value.Year.ToString())  
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(s => s.osobaid).Distinct().Count()
            );

            
            uniquePersonsPerYearFB["all"] = stFB.Select(s => s.osobaid).Distinct().Count();
            ViewBag.uniquePoliticiansFB = uniquePersonsPerYearFB;

            //twitter
            var stTW = st.Where(x => x.server == "Twitter").ToList();
            Dictionary<string, int> statementCountsTW = stTW
                .GroupBy(s => s.datum.Value.Year.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            statementCountsTW["all"] = stTW.Count;
            ViewBag.statementCountsTW = statementCountsTW;

            Dictionary<string, int> uniquePersonsPerYearTW = stTW
                .GroupBy(s => s.datum.Value.Year.ToString())  
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(s => s.osobaid).Distinct().Count()
            );

            
            uniquePersonsPerYearTW["all"] = stTW.Select(s => s.osobaid).Distinct().Count();
            ViewBag.uniquePoliticiansTW = uniquePersonsPerYearTW;


            Dictionary<string, double> avgStatementsPerPerson = st
            .GroupBy(s => s.datum.Value.Year.ToString())  // Skupinování podle roku jako string
            .ToDictionary(
                g => g.Key,
                g => (double)g.Count() / g.Select(s => s.osobaid).Distinct().Count()
            );

            // Celkový průměr: všechny vyjádření / počet unikátních osob
            int totalStatements = st.Count;
            int totalUniquePersons = st.Select(s => s.osobaid).Distinct().Count();
            avgStatementsPerPerson["all"] = totalUniquePersons > 0 ? (double)totalStatements / totalUniquePersons : 0;
            ViewBag.avgstatements = avgStatementsPerPerson;

            Dictionary<string, double> medianStatementsPerPerson = st
            .GroupBy(s => s.datum.Value.Year.ToString())  // Skupinování podle roku (string klíč)
            .ToDictionary(
                g => g.Key,
                g => CalculateMedian(g.GroupBy(s => s.osobaid).Select(gp => gp.Count()).ToList())
            );

            // Medián pro všechna vyjádření napříč roky
            medianStatementsPerPerson["all"] = CalculateMedian(
                st.GroupBy(s => s.osobaid).Select(g => g.Count()).ToList()
            );
            ViewBag.medianstatements = medianStatementsPerPerson;

            Dictionary<string, List<HistogramData>> distributionCount = new Dictionary<string, List<HistogramData>>();
            Dictionary<string, List<HistogramData>> distributionCountF = new Dictionary<string, List<HistogramData>>();
            Dictionary<string, List<HistogramData>> distributionCountT = new Dictionary<string, List<HistogramData>>();

            Dictionary<string, List<MonthlyStatementCount>> monthlyCount = new Dictionary<string, List<MonthlyStatementCount>>();
            Dictionary<string, Dictionary<string, int>> dayofweekCount = new Dictionary<string, Dictionary<string, int>>();


            Dictionary<string, Tuple<dynamic, dynamic>> maxminweek = new Dictionary<string, Tuple<dynamic, dynamic>>();

            for (int i = 2016; i <= 2023; i++)
            {
                var st_year= st.Where(x => x.datum.Value.Year == i).ToList();
                var st_f_year = st_year.Where(x => x.server == "Facebook").ToList();
                var st_t_year = st_year.Where(x => x.server == "Twitter").ToList();

                distributionCount[i.ToString()]= _statementData.GetHistogramCountData(st_year);
                distributionCountF[i.ToString()] = _statementData.GetHistogramCountData(st_f_year);
                distributionCountT[i.ToString()] = _statementData.GetHistogramCountData(st_t_year);

                monthlyCount[i.ToString()] = _statementData.GetMonthlyStatementCounts(st_year);
                dayofweekCount[i.ToString()] = _statementData.GetPostsByWeekday(st_year);

                var statementsByWeek = st_year
                    .GroupBy(s => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(s.datum.Value, CalendarWeekRule.FirstDay, DayOfWeek.Monday))
                    .Select(group => new
                    {
                        Week = group.Key,
                        Count = group.Count()
                    })
                    .ToList();

                var maxWeek = statementsByWeek.OrderByDescending(x => x.Count).First();
                var minWeek = statementsByWeek.OrderBy(x => x.Count).First();

                maxminweek[i.ToString()] = new Tuple<dynamic, dynamic>(maxWeek, minWeek);
            }

            var statementsByWeekall = st
                    .GroupBy(s => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(s.datum.Value, CalendarWeekRule.FirstDay, DayOfWeek.Monday))
                    .Select(group => new
                    {
                        Week = group.Key,
                        Count = group.Count()
                    })
                    .ToList();

            var maxWeekall = statementsByWeekall.OrderByDescending(x => x.Count).First();
            var minWeekall = statementsByWeekall.OrderBy(x => x.Count).First();

            distributionCount["all"]= _statementData.GetHistogramCountData(st);
            distributionCountF["all"] = _statementData.GetHistogramCountData(st.Where(x => x.server == "Facebook").ToList());
            distributionCountT["all"] = _statementData.GetHistogramCountData(st.Where(x => x.server == "Twitter").ToList());
            monthlyCount["all"] = _statementData.GetMonthlyStatementCounts(st);
            dayofweekCount["all"] = _statementData.GetPostsByWeekday(st);
            maxminweek["all"]= new Tuple<dynamic, dynamic>(maxWeekall, minWeekall);
            ViewBag.maxminweek = maxminweek;
            ViewBag.dayofweekCount = dayofweekCount;
            ViewBag.monthlyCount = monthlyCount;

            ViewBag.histogramData = distributionCount;
            ViewBag.histogramData_F = distributionCountF;
            ViewBag.histogramData_T = distributionCountT;


            var politicCounts = _statementData.GetStatementsPerYear(st);
            var politicCountsF = _statementData.GetStatementsPerYear(st.Where(x => x.server == "Facebook").ToList());
            var politicCountsT = _statementData.GetStatementsPerYear(st.Where(x => x.server == "Twitter").ToList());
            ViewBag.politicCountsAll = politicCounts;
            ViewBag.politicCountsFacebook = politicCountsF;
            ViewBag.politicCountsTwitter = politicCountsT;
            return View();
        }


        public async Task<IActionResult> PoliticDetail(string politic_id, [FromServices] StylometryAnalysis stylometryAnalysis, [FromServices] RhetoricAnalysis rhetoricAnalysis, [FromServices] EmotionAnalysis emotionAnalysis, [FromServices] SentimentAnalysis sentimentAnalysis, [FromServices] TopicAnalysis topicAnalysis, [FromServices] NetworkAnalysis networkAnalysis, [FromServices] TextAnalysis textAnalysis, [FromServices] NERAnalysis nerAnalysis, [FromServices] StatementDataDB statementDataDB)
        {
            List<Statement> st = JsonSerializer.Deserialize<List<Statement>>(
            JsonSerializer.Serialize(_statementData.Statements)
        );
            /*List<Politic> politicians = await statementData.GetAllPoliticiansAsync();
            using (var writer = new StreamWriter("politici_strana.csv"))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                csv.WriteRecords(politicians);
            }*/




            var st_rt = st;
            st = st.Where(x => !x.RT).ToList();




            //await emotionAnalysis.LoadEmotionFromDB(st);

            List<string> names = new List<string> { "ps" };
            List<string> mix = new List<string> { "io", "if", "ic", "mn", "ms", "o_", "oa" };


            //await nerAnalysis.LoadNERFromDB(st);

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
            List<Statement> st = JsonSerializer.Deserialize<List<Statement>>(
            JsonSerializer.Serialize(_statementData.Statements)
        );
            
            var st_sent_m = _statementData.LoadDataWMentions(st);

            Dictionary<string, Dictionary<string, List<double>>> pol_sentiments = new Dictionary<string, Dictionary<string, List<double>>>();
            Dictionary<string, double[]> p_counts = new Dictionary<string, double[]>();
            Dictionary<string, double[]> p_counts_m = new Dictionary<string, double[]>();
            Dictionary<string, double> classic_avg = new Dictionary<string, double>();
            Dictionary<string, double> rt_avg = new Dictionary<string, double>();
            Dictionary<string, List<ServerSentiment>> server_sent = new Dictionary<string, List<ServerSentiment>>();
            Dictionary<string, Dictionary<string, List<double>>> rtpol = new Dictionary<string, Dictionary<string, List<double>>>();

            Dictionary<string, Dictionary<string, ExtremeSentiment>> ex_s = new Dictionary<string, Dictionary<string, ExtremeSentiment>>();
            Dictionary<string, List<SentimentResult>> avg_rt = new Dictionary<string, List<SentimentResult>>();
            for (int i = 2016; i <= 2023; i++)
            {
                var st_year=st.Where(x=>x.datum.Value.Year==i).ToList();
                var st_year_m = st_sent_m.Where(x => x.datum.Value.Year == i).ToList();

                var politiciansentiments = sentimentAnalysis.PoliticianSentiments(st_year.Where(s => s.RT == false).ToList());
                pol_sentiments[i.ToString()]= politiciansentiments;
                var polaritycounts = st_year.Select(s => s.Sentiment).ToArray();
                p_counts[i.ToString()] = polaritycounts;
                var polaritycountsM = st_year_m.Select(s => s.Sentiment).ToArray();
                p_counts_m[i.ToString()] = polaritycountsM;
                var (classicAvgy, retweetAvgy) = sentimentAnalysis.RT_sentiment(st_year);
                classic_avg[i.ToString()] = classicAvgy;
                rt_avg[i.ToString()] = retweetAvgy;

                var server_sentimentY = st_year
                .GroupBy(s => s.server)
                .Select(g => new ServerSentiment{ Server = g.Key, Sentiments = g.Select(s => s.Sentiment).ToList() })
                .ToList();
                server_sent[i.ToString()] = server_sentimentY;

                var RTpolsentiments = sentimentAnalysis.PoliticianSentiments(st_year.Where(s => s.RT == true).ToList());
                rtpol[i.ToString()] = RTpolsentiments;

                var st_noRTY = st_year.Where(x => !x.RT).ToList();
                ex_s[i.ToString()] = sentimentAnalysis.CalculateSentimentRatios(st_noRTY);

                avg_rt[i.ToString()]= sentimentAnalysis.CalculateAvgSentimentRT(st_year);
            }
            var st_noRT = st.Where(x => !x.RT).ToList();
            pol_sentiments["all"]= sentimentAnalysis.PoliticianSentiments(st.Where(s => s.RT == false).ToList());
            p_counts["all"]= st.Select(s => s.Sentiment).ToArray();
            p_counts_m["all"] = st_sent_m.Select(s => s.Sentiment).ToArray();
            var (classicAvg, retweetAvg) = sentimentAnalysis.RT_sentiment(st);
            classic_avg["all"] = classicAvg;
            rt_avg["all"] = retweetAvg;
            server_sent["all"] = st
                .GroupBy(s => s.server)
                .Select(g => new ServerSentiment { Server = g.Key, Sentiments = g.Select(s => s.Sentiment).ToList() })
                .ToList();
            rtpol["all"]= sentimentAnalysis.PoliticianSentiments(st.Where(s => s.RT == true).ToList());
            ViewBag.pol_sentiments = pol_sentiments;
            ViewBag.polaritycounts = p_counts;
            ViewBag.polaritycountsM = p_counts_m;

            ViewBag.classicAvg = classic_avg;
            ViewBag.retweetAvg = rt_avg;
            ViewBag.server_sentiment = server_sent;

            ViewBag.RTpolsentiments = rtpol ;

            ex_s["all"]= sentimentAnalysis.CalculateSentimentRatios(st_noRT);
            ViewBag.extreme_s = ex_s;


            avg_rt["all"] = sentimentAnalysis.CalculateAvgSentimentRT(st);

            
            ViewBag.avgrt = avg_rt;


            //5 - Rozdělení sentimentu ve příspěvcích politiků
            /* var polarity = sentimentAnalysis.GetPolarity(st);
             ViewBag.polarity = polarity;*/













            var time_sentiment = sentimentAnalysis.CalculateAverageSentimentByHalfYear(st);
            ViewBag.time_sentiment = time_sentiment;







            //ViewBag.mcount_sentiment = sentimentAnalysis.MentionsAvgSentiment(st);


            var avgsentimentFMByYear = new Dictionary<string, List<PoliticianSentimentM>>();
            var avgCombinedByYear = new Dictionary<string, List<CombinedPoliticianSentiment>>();

            for (int year = 2016; year <= 2023; year++)
            {
                var yearlySt = st.Where(x => x.datum.Value.Year == year).ToList();
                var yearlyStSentM = st_sent_m.Where(x => x.datum.Value.Year == year).ToList();

                var avgsentimenty = sentimentAnalysis.CalculateAvgSentimentPolitician(yearlySt);
                avgsentimenty = avgsentimenty.OrderBy(x => x.AverageSentiment).ToList();

                var avgsentimentMentionsy = sentimentAnalysis.CalculateAvgSentimentPolitician(yearlyStSentM);
                var avgsentimentFMy = sentimentAnalysis.CalculateAvgSentimentPoliticianFromMentions(yearlySt);
                avgsentimentFMy = avgsentimentFMy.OrderBy(x => x.AverageSentiment).ToList();

                var combinedy = from item1 in avgsentimenty
                               join item2 in avgsentimentMentionsy
                               on item1.OsobaID equals item2.OsobaID into itemGroup
                               from item2 in itemGroup.DefaultIfEmpty()
                               select new CombinedPoliticianSentiment
                               {
                                   OsobaID = item1.OsobaID,
                                   AverageSentiment1 = item1.AverageSentiment,
                                   count = item1.Count,
                                   avgpos = item1.AveragePos,
                                   avgneu = item1.AverageNeu,
                                   avgneg = item1.AverageNeg,
                                   count_m = item2?.Count ?? 100,
                                   AverageSentiment2 = item2?.AverageSentiment ?? 100
                               };

                avgsentimentFMByYear[year.ToString()] = avgsentimentFMy.ToList();
                avgCombinedByYear[year.ToString()] = combinedy.OrderByDescending(x=>x.count).ToList();
            }

            

            var avgsentiment = sentimentAnalysis.CalculateAvgSentimentPolitician(st);
            avgsentiment = avgsentiment.OrderBy(x => x.AverageSentiment).ToList();

            var avgsentimentMentions = sentimentAnalysis.CalculateAvgSentimentPolitician(st_sent_m);
            var avgsentimentFM = sentimentAnalysis.CalculateAvgSentimentPoliticianFromMentions(st);
            avgsentimentFM = avgsentimentFM.OrderBy(x => x.AverageSentiment).ToList();

            var combined = from item1 in avgsentiment
                           join item2 in avgsentimentMentions
                           on item1.OsobaID equals item2.OsobaID into itemGroup
                           from item2 in itemGroup.DefaultIfEmpty()
                           select new CombinedPoliticianSentiment
                           {
                               OsobaID = item1.OsobaID,
                               AverageSentiment1 = item1.AverageSentiment,
                               count = item1.Count,
                               avgpos = item1.AveragePos,
                               avgneu = item1.AverageNeu,
                               avgneg = item1.AverageNeg,
                               count_m = item2?.Count ?? 100,
                               AverageSentiment2 = item2?.AverageSentiment ?? 100
                           };

            avgsentimentFMByYear["all"] = avgsentimentFM;
            avgCombinedByYear["all"] = combined.OrderByDescending(x => x.count).ToList();
            ViewBag.avgsentimentFM = avgsentimentFMByYear;
            ViewBag.avg_combined = avgCombinedByYear;


            return View();
        }



        public async Task<IActionResult> Emotions([FromServices] EmotionAnalysis emotionAnalysis,[FromServices] StylometryAnalysis stylometryAnalysis, [FromServices] ClusterAnalysis clusterAnalysis, [FromServices] RhetoricAnalysis rhetoricAnalysis, [FromServices] TopicAnalysis topicAnalysis, [FromServices] StatementData statementData, [FromServices] StatementDataDB statementDataDB, [FromServices] TextAnalysis textAnalysis, [FromServices] SentimentAnalysis sentimentAnalysis, [FromServices] NERAnalysis nerAnalysis, [FromServices] AssociationRulesGenerator association)
        {
            List<Statement> st = JsonSerializer.Deserialize<List<Statement>>(
            JsonSerializer.Serialize(_statementData.Statements)
        );
            st = st.Where(x => x.emotions.Any()).ToList();
            //await emotionAnalysis.LoadEmotionFromDB(st);
            Dictionary<string, List<EmotionDistribution>> emdistrib = new Dictionary<string, List<EmotionDistribution>>();
            Dictionary<string, Dictionary<string, Dictionary<string, double>>> coocm = new Dictionary<string, Dictionary<string, Dictionary<string, double>>>();
            Dictionary<string, Dictionary<string, double>> avgintens = new Dictionary<string, Dictionary<string, double>>();
            Dictionary<string, List<EmotionDistribution>> emdistribFB = new Dictionary<string, List<EmotionDistribution>>();
            Dictionary<string, List<EmotionDistribution>> emdistribTW = new Dictionary<string, List<EmotionDistribution>>();
            Dictionary<string, Dictionary<string, List<double>>> sentdata = new Dictionary<string, Dictionary<string, List<double>>>();
            for (int i = 2016; i < 2023; i++)
            {
                var st_year = st.Where(x => x.datum.Value.Year == i).ToList();
                var st_twittery = st_year.Where(s => s.server == "Twitter").ToList();
                var st_fby = st_year.Where(s => s.server == "Facebook").ToList();

               
                var emotionDistributionAlly = emotionAnalysis.PrepareEmotionDistribution(st_year);
                emdistrib[i.ToString()] = emotionDistributionAlly;
                var coOccurrenceMatrixy = emotionAnalysis.CalculateAndNormalizeCoOccurrence(st_year);
                coocm[i.ToString()] = coOccurrenceMatrixy;

                avgintens[i.ToString()]= emotionAnalysis.CalculateAverageIntensity(st_year);

                var emotionDistributionTwittery = emotionAnalysis.PrepareEmotionDistribution(st_twittery);
                var emotionDistributionFacebooky = emotionAnalysis.PrepareEmotionDistribution(st_fby);
                emdistribFB[i.ToString()] = emotionDistributionFacebooky;
                emdistribTW[i.ToString()] = emotionDistributionTwittery;
                var sentimentDatay = emotionAnalysis.GetSentimentDistribution(st_year);
                sentdata[i.ToString()] = sentimentDatay;

            }
            var emotionDistributionAll = emotionAnalysis.PrepareEmotionDistribution(st);
            emdistrib["all"] = emotionDistributionAll;
            ViewBag.emotionAll = emdistrib;


            var coOccurrenceMatrix = emotionAnalysis.CalculateAndNormalizeCoOccurrence(st);
            coocm["all"]= coOccurrenceMatrix;
            ViewBag.CoOccurrenceData = coocm;

            avgintens["all"]= emotionAnalysis.CalculateAverageIntensity(st);
            ViewBag.AverageIntensities = avgintens;


            var st_twitter = st.Where(s => s.server == "Twitter").ToList();
            var st_fb = st.Where(s => s.server == "Facebook").ToList();

            var emotionDistributionTwitter = emotionAnalysis.PrepareEmotionDistribution(st_twitter);
            emdistribTW["all"] = emotionDistributionTwitter;
            ViewBag.emotionTwitter = emdistribTW;

            var emotionDistributionFacebook= emotionAnalysis.PrepareEmotionDistribution(st_fb);
            emdistribFB["all"] = emotionDistributionFacebook;
            ViewBag.emotionFacebook = emdistribFB;


            var sentimentData = emotionAnalysis.GetSentimentDistribution(st);
            sentdata["all"] = sentimentData;


            ViewBag.SentimentData = sentdata;

            Dictionary<string, List<EntityFrequency>> emotionsNerMix = new Dictionary<string, List<EntityFrequency>>();
            Dictionary<string, List<EntityFrequency>> emotionsNerNames = new Dictionary<string, List<EntityFrequency>>();
            List<string> names = new List<string> { "ps" };
            List<string> mix = new List<string> { "io", "if", "ic", "mn", "ms", "o_", "oa" };
            var uniqueEmotions = st
              .SelectMany(s => s.emotions)
              .Select(e => e.emotion)
              .Distinct()
              .ToList();
            Dictionary<string, Dictionary<string, List<EntityFrequency>>> emonermix = new Dictionary<string, Dictionary<string, List<EntityFrequency>>>();
            Dictionary<string, Dictionary<string, List<EntityFrequency>>> emonernames = new Dictionary<string, Dictionary<string, List<EntityFrequency>>>();
            Dictionary<string, List<PoliticianEmotionData>> emostats = new Dictionary<string, List<PoliticianEmotionData>>();
            for (int i = 2016; i < 2023; i++)
            {
                var st_year = st.Where(x => x.datum.Value.Year == i).ToList();

                Dictionary<string, List<EntityFrequency>> emotionsNerMixy = new Dictionary<string, List<EntityFrequency>>();
                Dictionary<string, List<EntityFrequency>> emotionsNerNamesy = new Dictionary<string, List<EntityFrequency>>();

                var uniqueEmotionsy = st_year
               .SelectMany(s => s.emotions)
               .Select(e => e.emotion)
               .Distinct()
               .ToList();


                foreach (var emotion in uniqueEmotionsy)
                {
                    var st_e = st_year.Where(s => s.emotions.Any(x => x.emotion == emotion)).ToList();

                    var enm = nerAnalysis.PoliticEntityPieChartNormalized(st_e, st_year, mix);
                    var enn = nerAnalysis.PoliticEntityPieChartNormalized(st_e, st_year, names);

                    if (enm.Any())
                    {
                        emotionsNerMixy[emotion] = enm;
                    }
                    if (enn.Any())
                    {
                        emotionsNerNamesy[emotion] = enn;
                    }

                }
                emonermix[i.ToString()] = emotionsNerMixy;
                emonernames[i.ToString()] = emotionsNerNamesy;
                var resulty = emotionAnalysis.CalculateEmotionStats(st_year, uniqueEmotionsy);
                emostats[i.ToString()] = resulty;
            }

 

            foreach (var emotion in uniqueEmotions)
            {
                var st_e = st.Where(s => s.emotions.Any(x => x.emotion == emotion)).ToList();

                var enm = nerAnalysis.PoliticEntityPieChartNormalized(st_e, st, mix);
                var enn = nerAnalysis.PoliticEntityPieChartNormalized(st_e, st, names);

                if (enm.Any())
                {
                    emotionsNerMix[emotion] = enm;
                }
                if (enn.Any())
                {
                    emotionsNerNames[emotion] = enn;
                }

            }
            emonermix["all"] = emotionsNerMix;
            emonernames["all"] = emotionsNerNames;

            ViewBag.emotionsNerMix = emonermix;
            ViewBag.emotionsNerNames = emonernames;


            var result = emotionAnalysis.CalculateEmotionStats(st, uniqueEmotions);
            emostats["all"] = result;
            ViewBag.politicStats = emostats;

            return View();
        }
        public ActionResult ProcessText(string inputText)
        {
            
            // Cesta k adresáři wwwroot/App_Data
            string appDataPath = Path.Combine(_env.WebRootPath, "App_Data");
            Directory.CreateDirectory(appDataPath); // Vytvoří složku, pokud neexistuje

            string inputFilePath = Path.Combine(appDataPath, "input.txt");
            string scriptPath = Path.Combine(appDataPath, "script.py");
            string resultFilePath = Path.Combine(appDataPath, "output.csv");

            // Uložení vstupního textu do souboru
            System.IO.File.WriteAllText(inputFilePath, inputText);

            // Spuštění Python skriptu
            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"{scriptPath} {inputFilePath} {resultFilePath}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            string errorOutput = "";

            using (Process process = Process.Start(start))
            {
                string output = process.StandardOutput.ReadToEnd();
                
                process.WaitForExit();
            }

            ViewBag.Result = System.IO.File.ReadAllText(resultFilePath);
           

            return View("Privacy");
        }

        public async Task<IActionResult> NER([FromServices] StylometryAnalysis stylometryAnalysis, [FromServices] ClusterAnalysis clusterAnalysis, [FromServices] RhetoricAnalysis rhetoricAnalysis, [FromServices] TopicAnalysis topicAnalysis, [FromServices] StatementData statementData, [FromServices] StatementDataDB statementDataDB, [FromServices] TextAnalysis textAnalysis, [FromServices] SentimentAnalysis sentimentAnalysis, [FromServices] NERAnalysis nerAnalysis, [FromServices] AssociationRulesGenerator association)
        {
            List<Statement> st = JsonSerializer.Deserialize<List<Statement>>(
            JsonSerializer.Serialize(_statementData.Statements)
        );
            
            //await nerAnalysis.LoadNERFromDB(st);
            var st_noRT = st.Where(x => !x.RT).ToList();

            

            List<string> types = new List<string> { "io", "gc", "P", "ps", "gt", "om", "gu", "ty", "tm", "ic", "ms", "gl", "gr", "op", "oa", "if" };

            Dictionary<string, Dictionary<string, List<KeyValuePair<string, int>>>> nercountypes = new Dictionary<string, Dictionary<string, List<KeyValuePair<string, int>>>>();
            Dictionary<string, List<string>> enttypes = new Dictionary<string, List<string>>();
            Dictionary<string, List<int>> typecounts = new Dictionary<string, List<int>>();
            Dictionary<string, List<EntitySentimentData>> entsentdata = new Dictionary<string, List<EntitySentimentData>>();
            Dictionary<string, List<string>> allt = new Dictionary<string, List<string>>();
            Dictionary<string, Dictionary<string, double>> entratio = new Dictionary<string, Dictionary<string, double>>();
            for (int i = 2016; i <= 2023; i++)
            {
                

                var yearlySt = st.Where(x => x.datum.Value.Year == i).ToList();
                var st_noRT_y = yearlySt.Where(x => !x.RT).ToList();
                var groupedEntitiesy = nerAnalysis.GetTopEntitiesByType(yearlySt, types);
                nercountypes[i.ToString()] = groupedEntitiesy;
                var (entityTypesy, typeCountsy) = nerAnalysis.GetTopEntityTypes(yearlySt);
                enttypes[i.ToString()] = entityTypesy;
                typecounts[i.ToString()] = typeCountsy;

                var entitySentimentDatay = nerAnalysis.CalculateAverageSentimentType(yearlySt);
                entsentdata[i.ToString()] = entitySentimentDatay;
                allt[i.ToString()]= entitySentimentDatay
                .Select(x => x.EntityType)
                .Distinct()
                .ToList();
                entratio[i.ToString()]= nerAnalysis.CalculateEntityToWordRatio(st_noRT_y);
            }
            var entity_ratio = nerAnalysis.CalculateEntityToWordRatio(st_noRT);
            entratio["all"] = entity_ratio;
            ViewBag.PoliticianRatios = entratio;


            var groupedEntities = nerAnalysis.GetTopEntitiesByType(st, types);
            nercountypes["all"] = groupedEntities;
            ViewBag.nercounts_types = nercountypes;


            var (entityTypes, typeCounts) = nerAnalysis.GetTopEntityTypes(st);
            enttypes["all"] = entityTypes;
            typecounts["all"] = typeCounts;

            ViewBag.EntityTypes = enttypes;
            ViewBag.TypeCounts = typecounts;


            var entitySentimentData = nerAnalysis.CalculateAverageSentimentType(st);
            entsentdata["all"] = entitySentimentData;
            ViewBag.EntitySentiments = entsentdata;

            var alltypes = entitySentimentData
                .Select(x => x.EntityType)
                .Distinct()
                .ToList();
            allt["all"]= alltypes;
            ViewBag.Alltypes = allt;


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


            List<Statement> st = JsonSerializer.Deserialize<List<Statement>>(
            JsonSerializer.Serialize(_statementData.Statements)
        );
            List<Statement> st_m = statementData.LoadDataWMentions(st);
            //await nerAnalysis.LoadNERFromDB(st);
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
            /*var polarity = sentimentAnalysis.GetPolarity(st_sentiment);
            ViewBag.polarity = polarity;*/

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