using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Microsoft.AspNetCore.Mvc;
using PoliticStatements.Models;
using PoliticStatements.Services;
using PoliticStatements.Repositories;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Web.Mvc.Html;

using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Text.Json;
using System.Diagnostics;

using System.IO;

namespace PoliticStatements.Controllers
{

    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
     
        private string fileDirectory = "data/";
        private readonly StatementData _statementData;
        private readonly PoliticianRepository _politicianRepository;
        private readonly StylometryRepository _stylometryRepository;
        private readonly IWebHostEnvironment _env;
        public HomeController(ILogger<HomeController> logger, StatementData statementData, PoliticianRepository polR, StylometryRepository stylometryRepository, IWebHostEnvironment env)
        {
            _logger = logger;
            _statementData = statementData;
            _env = env;
            _politicianRepository = polR;
            _stylometryRepository = stylometryRepository;
        }

        

        public async Task<IActionResult> Stylometry([FromServices] StylometryAnalysis stylometryAnalysis, [FromServices] EmotionAnalysis emotionAnalysis, [FromServices] SentimentAnalysis sentimentAnalysis ,  [FromServices] TextAnalysis textAnalysis, [FromServices] NERAnalysis nerAnalysis)
        {
            

            var st = _statementData.Statements;
            string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "avg_sentence_delka.csv");
            var avg_sentence = _stylometryRepository.LoadAndSortPoliticians(path);

            var result = st
            .GroupBy(s => s.osobaid.politic_id) 
            .ToDictionary(g => g.Key, g => g.Count());
            ViewBag.st_count = result;
            ViewBag.avg_sentence = avg_sentence;

            ViewBag.sentencelengths = _stylometryRepository.SentenceLengths();

            ViewBag.word_length = _stylometryRepository.WordLengths();


            var sldruhy = _stylometryRepository.LoadPoliticianData();
            ViewBag.slovnidruhy = sldruhy;
            return View();
        }

        
        public async Task<IActionResult> ExportGexf(string fileName)
        {
            
            var filePath = Path.Combine(_env.WebRootPath, fileDirectory, fileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(); 
            }

            var fileBytes = System.IO.File.ReadAllBytes(filePath);

            
            return File(fileBytes, "application/xml", fileName);
        }
       
        public async Task<IActionResult> Index([FromServices]  StylometryAnalysis stylometryAnalysis,[FromServices] EmotionAnalysis emotionAnalysis, [FromServices] SentimentAnalysis sentimentAnalysis, [FromServices] TextAnalysis textAnalysis, [FromServices] NERAnalysis nerAnalysis)
        {
           
            List<string> top_politics = new()
    {
        "andrej-babis", "tomio-okamura", "lubomir-volny", "adam-vojtech", "miroslav-kalousek",
        "alena-schillerova", "pavel-belobradek", "petr-fiala", "karel-havlicek", "milos-zeman"
    };

            var st = _statementData.Statements;

            var top_sim = _stylometryRepository.LoadSimilarities("politician_top_similarities.csv");
           

           
           


            var politicians = await _politicianRepository.GetAllPoliticiansAsync();


            Dictionary<string, int> check = new Dictionary<string, int>();
            foreach (var u in politicians)
            {
                if (top_sim.ContainsKey(u.politic_id))
                {
                    check[u.politic_id] = 1;
                }

            }
            ViewBag.check = check;

            ViewBag.politici = politicians;


            var partyCounts = politicians
               .GroupBy(p => p.organizace)
               .Select(g => new { Party = g.Key, Count = g.Count() })
               .OrderByDescending(g => g.Count).Take(10)
               .ToList();

            ViewBag.partyCounts = partyCounts;

            var groupedByYear = st
            .Where(s => s.datum.HasValue)
            .GroupBy(s => s.datum.Value.Year.ToString())
            .ToDictionary(g => g.Key, g => g.ToList());


            var statementCounts = groupedByYear.ToDictionary(kv => kv.Key, kv => kv.Value.Count);


            statementCounts["all"] = st.Count;
            ViewBag.statementCounts = statementCounts;

            Dictionary<string, int> uniquePersonsPerYear = st
                .GroupBy(s => s.datum.Value.Year.ToString())
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(s => s.osobaid.politic_id).Distinct().Count()
            );


            uniquePersonsPerYear["all"] = st.Select(s => s.osobaid.politic_id).Distinct().Count();
            ViewBag.uniquePoliticians = uniquePersonsPerYear;

            //facebook
            var stFB = st.Where(x => x.server == "Facebook");
            Dictionary<string, int> statementCountsFB = stFB
                .GroupBy(s => s.datum.Value.Year.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            statementCountsFB["all"] = stFB.Count();
            ViewBag.statementCountsFB = statementCountsFB;

            Dictionary<string, int> uniquePersonsPerYearFB = stFB
                .GroupBy(s => s.datum.Value.Year.ToString())
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(s => s.osobaid.politic_id).Distinct().Count()
            );


            uniquePersonsPerYearFB["all"] = stFB.Select(s => s.osobaid.politic_id).Distinct().Count();
            ViewBag.uniquePoliticiansFB = uniquePersonsPerYearFB;

            //twitter
            var stTW = st.Where(x => x.server == "Twitter");
            Dictionary<string, int> statementCountsTW = stTW
                .GroupBy(s => s.datum.Value.Year.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            statementCountsTW["all"] = stTW.Count();
            ViewBag.statementCountsTW = statementCountsTW;

            Dictionary<string, int> uniquePersonsPerYearTW = stTW
                .GroupBy(s => s.datum.Value.Year.ToString())
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(s => s.osobaid.politic_id).Distinct().Count()
            );


            uniquePersonsPerYearTW["all"] = stTW.Select(s => s.osobaid.politic_id).Distinct().Count();
            ViewBag.uniquePoliticiansTW = uniquePersonsPerYearTW;


            var avgStatementsPerPerson = groupedByYear.ToDictionary(
                kv => kv.Key,
                kv => (double)kv.Value.Count / kv.Value.Select(s => s.osobaid.politic_id).Distinct().Count()
            );

            int totalStatements = st.Count;
            int totalUniquePersons = st.Select(s => s.osobaid.politic_id).Distinct().Count();
            avgStatementsPerPerson["all"] = totalUniquePersons > 0 ? (double)totalStatements / totalUniquePersons : 0;
            ViewBag.avgstatements = avgStatementsPerPerson;

            Dictionary<string, double> medianStatementsPerPerson = st
            .GroupBy(s => s.datum.Value.Year.ToString()) 
            .ToDictionary(
                g => g.Key,
                g => CalculateMedian(g.GroupBy(s => s.osobaid.politic_id).Select(gp => gp.Count()).ToList())
            );

            medianStatementsPerPerson["all"] = CalculateMedian(
                st.GroupBy(s => s.osobaid.politic_id).Select(g => g.Count()).ToList()
            );
            ViewBag.medianstatements = medianStatementsPerPerson;

            Dictionary<string, double[]> p_counts = new Dictionary<string, double[]>();

            p_counts["all"] = st.Select(s => s.SentimentBert).ToArray();
            ViewBag.polaritycounts = p_counts;

            Dictionary<int, int> st_count_years = st
              .GroupBy(s => s.datum.Value.Year)
                        .ToDictionary(gg => gg.Key, gg => gg.Count());

            ViewBag.st_count_years = st_count_years;




            var server_count = _statementData.GetChartData(st);
            ViewBag.server_count = server_count;

            List<string> names = new List<string> { "ps" };
            List<string> mix = new List<string> { "io", "if", "ic", "mn", "ms", "o_", "oa" };
            var piechartAll = nerAnalysis.PoliticEntityPieChart(st, mix);
            ViewBag.piechartAll = piechartAll;
            var piechartAll_names = nerAnalysis.PoliticEntityPieChart(st, names);
            ViewBag.piechartAll_names = piechartAll_names;
            var emotionDistributionAll = emotionAnalysis.PrepareEmotionDistribution(st);
            ViewBag.emotionAll = emotionDistributionAll;
            var politicCounts = _statementData.GetStatementsPerYearAll(st);

            ViewBag.politicCountsAll = politicCounts;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
        public async Task<IActionResult> Mentions(int year,[FromServices] StylometryAnalysis stylometryAnalysis, [FromServices] EmotionAnalysis emotionAnalysis, [FromServices] SentimentAnalysis sentimentAnalysis, [FromServices] TextAnalysis textAnalysis, [FromServices] NERAnalysis nerAnalysis)
        {
          
            var st = _statementData.Statements;
            var st_year = st.Where(s => s.datum.Value.Year == 2019).ToList();
                List<(int, int)> customIntervals = new List<(int, int)>
                {
                    
                    (1,1),
                    (2, 5),
                    (6, 10),
                    (11, 15),
                    (16, 20),
                    (21, 25),
                    (26, 30),
                    (31,35),
                    (36,40),
                    (41,45),
                    (46,50)
                };
                List<(int, int)> customIntervals1 = new List<(int, int)>
                {

                    (1,1),
                    (2, 5),
                    (6, 10),
                    (11, 15),
                    (16, 20),
                    (21, 25),
                    (26, 30),
                    (31,35),
                    (36,40)
                };
                var zminkyPoOsobe = _statementData.PocetZminekOsob(st_year);
                var result = _statementData.GetMentionIntervals(zminkyPoOsobe, customIntervals);
                ViewBag.Labels = result.labels;
                ViewBag.BinCounts = result.binCounts;


                var zminkyPolitiku = _statementData.PocetZminekPolitiku(st_year);
                var result1 = _statementData.GetMentionIntervals(zminkyPolitiku, customIntervals);
                ViewBag.Labels1 = result1.labels;
                ViewBag.BinCounts1 = result1.binCounts;

               
                var distribution = st_year
                    .Where(s => s.politicizminky.Count > 0) 
                    .GroupBy(s => s.politicizminky.Count)
                    .Select(g => new { PocetZmínek = g.Key, PocetStatementů = g.Count() })
                    .OrderBy(x => x.PocetZmínek)
                    .ToList();

               
                ViewBag.PoliticZminkyDistribuce = Newtonsoft.Json.JsonConvert.SerializeObject(distribution);
                ViewBag.politic_stats = _statementData.GetPoliticMentionStats(st_year);




            return View();
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> Length([FromServices] StylometryAnalysis stylometryAnalysis, [FromServices] EmotionAnalysis emotionAnalysis, [FromServices] SentimentAnalysis sentimentAnalysis , [FromServices] TextAnalysis textAnalysis, [FromServices] NERAnalysis nerAnalysis)
        {
            
            var st = _statementData.Statements;
            var st_f= st.Where(x => x.server == "Facebook").ToList();
            var st_t = st.Where(x => x.server == "Twitter").ToList();
            Dictionary<string, Dictionary<int, int>> distribution = new Dictionary<string, Dictionary<int, int>>();
            Dictionary<string, Dictionary<string, int>> distributinInter = new Dictionary<string, Dictionary<string, int>>();

            Dictionary<string, Dictionary<int, int>> distributionFB = new Dictionary<string, Dictionary<int, int>>();
            Dictionary<string, Dictionary<string, int>> distributinInterFB = new Dictionary<string, Dictionary<string, int>>();

            Dictionary<string, Dictionary<int, int>> distributionTW = new Dictionary<string, Dictionary<int, int>>();
            Dictionary<string, Dictionary<string, int>> distributinInterTW = new Dictionary<string, Dictionary<string, int>>();

            Dictionary<string, List<int>> distribBox = new Dictionary<string, List<int>>();
            Dictionary<string, List<int>> distribBoxFB = new Dictionary<string, List<int>>();
            Dictionary<string, List<int>> distribBoxTW = new Dictionary<string, List<int>>();

            Dictionary<string, List<PoliticianStats>> polTable = new Dictionary<string, List<PoliticianStats>>();
            Dictionary<string, List<PoliticianStats>> polTableFB = new Dictionary<string, List<PoliticianStats>>();
            Dictionary<string, List<PoliticianStats>> polTableTW = new Dictionary<string, List<PoliticianStats>>();
            for (int i = 2016; i <= 2022; i++)
            {
                var st_year = st.Where(x => x.datum.Value.Year == i).ToList();
                var st_f_year = st_year.Where(x => x.server == "Facebook").ToList();
                var st_t_year = st_year.Where(x => x.server == "Twitter").ToList();

                distribution[i.ToString()]= _statementData.CreateDistribution(st_year);
                distributinInter[i.ToString()] = _statementData.CreateDistributionCategory(st_year);

                distributionFB[i.ToString()] = _statementData.CreateDistribution(st_f_year);
                distributinInterFB[i.ToString()] = _statementData.CreateDistributionCategory(st_f_year);

                distributionTW[i.ToString()] = _statementData.CreateDistribution(st_t_year);
                distributinInterTW[i.ToString()] = _statementData.CreateDistributionCategory(st_t_year);

                distribBox[i.ToString()] = st_year.Select(x => x.pocetSlov.Value).ToList();
                distribBoxFB[i.ToString()] = st_f_year.Select(x => x.pocetSlov.Value).ToList();
                distribBoxTW[i.ToString()] = st_t_year.Select(x => x.pocetSlov.Value).ToList();

                polTable[i.ToString()] = _statementData.CalculateStats(st_year,500);
                polTableFB[i.ToString()] = _statementData.CalculateStats(st_f_year,500);
                polTableTW[i.ToString()] = _statementData.CalculateStats(st_t_year,500);
            }

            distribution["all"] = _statementData.CreateDistribution(st);
            distributinInter["all"]= _statementData.CreateDistributionCategory(st);

            distributionFB["all"] = _statementData.CreateDistribution(st_f);
            distributinInterFB["all"] = _statementData.CreateDistributionCategory(st_f);

            distributionTW["all"] = _statementData.CreateDistribution(st_t);
            distributinInterTW["all"] = _statementData.CreateDistributionCategory(st_t);

            distribBox["all"] = st.Select(x => x.pocetSlov.Value).ToList();
            distribBoxFB["all"] = st_f.Select(x => x.pocetSlov.Value).ToList();
            distribBoxTW["all"] = st_t.Select(x => x.pocetSlov.Value).ToList();

            polTable["all"] = _statementData.CalculateStats(st, 500);
            polTableFB["all"] = _statementData.CalculateStats(st_f, 500);
            polTableTW["all"] = _statementData.CalculateStats(st_t, 500);

            ViewBag.HistogramDistribuce = distribution;
            ViewBag.HistogramIntervaly = distributinInter;

            ViewBag.HistogramDistribuceFB = distributionFB;
            ViewBag.HistogramIntervalyFB = distributinInterFB;

            ViewBag.HistogramDistribuceTW = distributionTW;
            ViewBag.HistogramIntervalyTW = distributinInterTW;

            ViewBag.distribbox = distribBox;
            ViewBag.distribboxFB = distribBoxFB;
            ViewBag.distribboxTW = distribBoxTW;

            ViewBag.table = polTable;
            ViewBag.tableF = polTableFB;
            ViewBag.tableT = polTableTW;
            return View();
        }

        public static double CalculateMedian(List<int> values)
        {
            if (values.Count == 0) return 0; 

            values.Sort(); 
            int mid = values.Count / 2;
            return values.Count % 2 == 0
                ? (values[mid - 1] + values[mid]) / 2.0 
                : values[mid]; 
        }
        public IActionResult StatementCount()
        {
            
            var st = _statementData.Statements;
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
                    g => g.Select(s => s.osobaid.politic_id).Distinct().Count()
            );

            
            uniquePersonsPerYear["all"] = st.Select(s => s.osobaid.politic_id).Distinct().Count();
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
                    g => g.Select(s => s.osobaid.politic_id).Distinct().Count()
            );

            
            uniquePersonsPerYearFB["all"] = stFB.Select(s => s.osobaid.politic_id).Distinct().Count();
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
                    g => g.Select(s => s.osobaid.politic_id).Distinct().Count()
            );

            
            uniquePersonsPerYearTW["all"] = stTW.Select(s => s.osobaid.politic_id).Distinct().Count();
            ViewBag.uniquePoliticiansTW = uniquePersonsPerYearTW;


            Dictionary<string, double> avgStatementsPerPerson = st
            .GroupBy(s => s.datum.Value.Year.ToString()) 
            .ToDictionary(
                g => g.Key,
                g => (double)g.Count() / g.Select(s => s.osobaid.politic_id).Distinct().Count()
            );

            
            int totalStatements = st.Count;
            int totalUniquePersons = st.Select(s => s.osobaid.politic_id).Distinct().Count();
            avgStatementsPerPerson["all"] = totalUniquePersons > 0 ? (double)totalStatements / totalUniquePersons : 0;
            ViewBag.avgstatements = avgStatementsPerPerson;

            Dictionary<string, double> medianStatementsPerPerson = st
            .GroupBy(s => s.datum.Value.Year.ToString())  
            .ToDictionary(
                g => g.Key,
                g => CalculateMedian(g.GroupBy(s => s.osobaid.politic_id).Select(gp => gp.Count()).ToList())
            );

          
            medianStatementsPerPerson["all"] = CalculateMedian(
                st.GroupBy(s => s.osobaid.politic_id).Select(g => g.Count()).ToList()
            );
            ViewBag.medianstatements = medianStatementsPerPerson;

            Dictionary<string, List<StatementCountDistribution>> distributionCount = new Dictionary<string, List<StatementCountDistribution>>();
            Dictionary<string, List<StatementCountDistribution>> distributionCountF = new Dictionary<string, List<StatementCountDistribution>>();
            Dictionary<string, List<StatementCountDistribution>> distributionCountT = new Dictionary<string, List<StatementCountDistribution>>();

            Dictionary<string, List<MonthlyStatementCount>> monthlyCount = new Dictionary<string, List<MonthlyStatementCount>>();
            Dictionary<string, Dictionary<string, int>> dayofweekCount = new Dictionary<string, Dictionary<string, int>>();


            Dictionary<string, Tuple<dynamic, dynamic>> maxminweek = new Dictionary<string, Tuple<dynamic, dynamic>>();

            for (int i = 2016; i <= 2022; i++)
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


        public async Task<IActionResult> PoliticDetail(string politic_id, [FromServices] StylometryAnalysis stylometryAnalysis, [FromServices] EmotionAnalysis emotionAnalysis, [FromServices] SentimentAnalysis sentimentAnalysis ,  [FromServices] TextAnalysis textAnalysis, [FromServices] NERAnalysis nerAnalysis)
        {

            var st_rt = _statementData.Statements.Where(x => x.osobaid.politic_id == politic_id).ToList();
            var st = st_rt.Where(x => !x.RT).ToList();



            List<string> names = new List<string> { "ps" };
            List<string> mix = new List<string> { "io", "if", "ic", "mn", "ms", "o_", "oa","op" };


            var uniquePoliticIds = st.Select(s => s.osobaid.politic_id).Distinct();

            var uniqueEmotions = st
           .SelectMany(s => s.emotions)
           .Select(e => e.emotion)
           .Distinct()
           .ToList();

            var unique_ngrams = _stylometryRepository.LoadUniqueNGrams("wwwroot/data/unique_ngrams_all_2019_4.csv");
            ViewBag.unique_ngrams = unique_ngrams;
            var topngrams = _stylometryRepository.LoadTopNgrams("wwwroot/data/normal",politic_id);
            ViewBag.topngrams = topngrams;
            var simWords = _stylometryRepository.LoadSimWords("wwwroot/data/texty_topentity");
            ViewBag.simWords = simWords;


              
            var time_sentiment = sentimentAnalysis.CalculateMedianSentimentByHalfYear(st,50);
            var time_sentiment_avg = sentimentAnalysis.CalculateAverageSentimentByHalfYear(st, 50);
            var time_sentiment_q = sentimentAnalysis.CalculateAverageSentimentByQuarter(st);
            ViewBag.time_sentiment = time_sentiment;
            ViewBag.time_sentiment_q = time_sentiment_q;
            ViewBag.time_sentimentavg = time_sentiment_avg;
            Dictionary<string, ChartData> server_count = new Dictionary<string, ChartData>();
           
            var sc = _statementData.GetChartData(st_rt);
            server_count[politic_id] = sc;
            
            ViewBag.server_count = server_count;


            var emotionStatsQ = emotionAnalysis.GetEmotionPercentagesPerPoliticianAndYearQ(st);

            ViewBag.EmotionStatsQ = emotionStatsQ;

            var emotionStatsH = emotionAnalysis.GetEmotionPercentagesPerPoliticianAndYearH(st);

            ViewBag.EmotionStatsH = emotionStatsH;

            Dictionary<string, int> st_count = st_rt
            .GroupBy(s => s.osobaid.politic_id)
            .ToDictionary(g => g.Key, g => g.Count());

            ViewBag.st_count = st_count;

            Dictionary<string, Dictionary<int, int>> st_count_years = st_rt
                .GroupBy(s => s.osobaid.politic_id)
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


              

                    var res = nerAnalysis.PoliticEntityPieChart(kvp.Value[i], mix, topentities[kvp.Key][i]);
                    allPieChartData[kvp.Key][topentities[kvp.Key][i]] = res;


                    sentimentHist[kvp.Key][topentities[kvp.Key][i]] = kvp.Value[i].Select(s => s.SentimentBert).ToArray();

                    var emotionDistribution = emotionAnalysis.PrepareEmotionDistribution(kvp.Value[i]);
                    emotionData[kvp.Key][topentities[kvp.Key][i]] = emotionDistribution;

                    var time_sentiment_pe = sentimentAnalysis.CalculateMedianSentimentByHalfYear(kvp.Value[i],0)[kvp.Key];


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

            var sentimentAll = st.Select(s => s.SentimentBert).ToArray();
            ViewBag.sentimentAll = sentimentAll;
            var piechartAll = nerAnalysis.PoliticEntityPieChart(st, mix);
            ViewBag.piechartAll = piechartAll;
            var piechartAll_names = nerAnalysis.PoliticEntityPieChart(st, names);
            ViewBag.piechartAll_names = piechartAll_names;
            var emotionDistributionAll = emotionAnalysis.PrepareEmotionDistribution(st);
            ViewBag.emotionAll = emotionDistributionAll;


            var st_negative = st.Where(s => s.SentimentBert < -0.7).ToList();
            var st_positive = st.Where(s => s.SentimentBert > 0.7).ToList();

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

                var enm = nerAnalysis.PoliticEntityPieChartNormalized3(st_e,st, mix,20);
                var enn = nerAnalysis.PoliticEntityPieChartNormalized3(st_e,st, names,20);

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


            var top_sim = _stylometryRepository.LoadSimilarities("politician_top_similarities.csv");
            ViewBag.top_sim = top_sim;
            var top_sim_ST = _stylometryRepository.LoadSimilarities("nejpodobnejsi_politici_summary_2019.csv");
            ViewBag.top_sim_ST = top_sim_ST;



            return View();
        }
        public async Task<IActionResult> PoliticDetailLight(string politic_id, [FromServices] StylometryAnalysis stylometryAnalysis,  [FromServices] EmotionAnalysis emotionAnalysis, [FromServices] SentimentAnalysis sentimentAnalysis  ,  [FromServices] TextAnalysis textAnalysis, [FromServices] NERAnalysis nerAnalysis)
        {
           
            var st_rt = _statementData.Statements.Where(x => x.osobaid.politic_id == politic_id).ToList();
            var st = st_rt.Where(x => !x.RT).ToList();

            List<string> names = new List<string> { "ps" };
            List<string> mix = new List<string> { "io", "if", "ic", "mn", "ms", "o_", "oa", "op" };



            var uniquePoliticIds = st.Select(s => s.osobaid.politic_id).Distinct();

            var uniqueEmotions = st
           .SelectMany(s => s.emotions)
           .Select(e => e.emotion)
           .Distinct()
           .ToList();

            var unique_ngrams = _stylometryRepository.LoadUniqueNGrams("wwwroot/data/unique_ngrams_all_2019_4.csv");
            ViewBag.unique_ngrams = unique_ngrams;
            var simWords = _stylometryRepository.LoadSimWords("wwwroot/data/texty_topentity");
            ViewBag.simWords = simWords;


            var time_sentiment = sentimentAnalysis.CalculateMedianSentimentByHalfYear(st, 10);
            var time_sentiment_avg = sentimentAnalysis.CalculateAverageSentimentByHalfYear(st, 10);
            var time_sentiment_q = sentimentAnalysis.CalculateAverageSentimentByQuarter(st);
            ViewBag.time_sentiment = time_sentiment;
            ViewBag.time_sentiment_q = time_sentiment_q;
            ViewBag.time_sentimentavg = time_sentiment_avg;
            Dictionary<string, ChartData> server_count = new Dictionary<string, ChartData>();
            
            var sc = _statementData.GetChartData(st_rt);
            server_count[politic_id] = sc;
            



            ViewBag.server_count = server_count;


            var emotionStatsQ = emotionAnalysis.GetEmotionPercentagesPerPoliticianAndYearQ(st);

            ViewBag.EmotionStatsQ = emotionStatsQ;

            var emotionStatsH = emotionAnalysis.GetEmotionPercentagesPerPoliticianAndYearH(st);

            ViewBag.EmotionStatsH = emotionStatsH;

            Dictionary<string, int> st_count = st_rt
            .GroupBy(s => s.osobaid.politic_id)
            .ToDictionary(g => g.Key, g => g.Count());

            ViewBag.st_count = st_count;

            Dictionary<string, Dictionary<int, int>> st_count_years = st_rt
                .GroupBy(s => s.osobaid.politic_id)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(s => s.datum.Value.Year)
                          .ToDictionary(gg => gg.Key, gg => gg.Count())
                );
            ViewBag.st_count_years = st_count_years;

            
            var sentimentAll = st.Select(s => s.SentimentBert).ToArray();
            ViewBag.sentimentAll = sentimentAll;
            var piechartAll = nerAnalysis.PoliticEntityPieChart(st, mix);
            ViewBag.piechartAll = piechartAll;
            var piechartAll_names = nerAnalysis.PoliticEntityPieChart(st, names);
            ViewBag.piechartAll_names = piechartAll_names;
            var emotionDistributionAll = emotionAnalysis.PrepareEmotionDistribution(st);
            ViewBag.emotionAll = emotionDistributionAll;

            ViewBag.politic_id = politic_id;
            var st_negative = st.Where(s => s.SentimentBert < -0.7).ToList();
            var st_positive = st.Where(s => s.SentimentBert > 0.7).ToList();

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

                var enm = nerAnalysis.PoliticEntityPieChartNormalized3(st_e, st, mix, 20);
                var enn = nerAnalysis.PoliticEntityPieChartNormalized3(st_e, st, names, 20);

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


            var top_sim = _stylometryRepository.LoadSimilarities("politician_top_similarities.csv");
            ViewBag.top_sim = top_sim;
            var top_sim_ST = _stylometryRepository.LoadSimilarities("nejpodobnejsi_politici_summary_2019.csv");
            ViewBag.top_sim_ST = top_sim_ST;


            return View();
        }
        public async Task<IActionResult> PoliticianList()
        {
            var uniquePoliticIds = await _politicianRepository.GetAllPoliticiansAsync();
            uniquePoliticIds = uniquePoliticIds.Where(x => x.count >0).ToList();
            var top_sim = _stylometryRepository.LoadSimilarities("politician_top_similarities.csv");
            //var unique_ngrams = _stylometryRepository.LoadUniqueNGrams("wwwroot/data/unique_ngrams_all_2019_4.csv");

            ViewBag.politician_list = uniquePoliticIds.OrderByDescending(x=>x.count).ToList();
            Dictionary<string, int> check = new Dictionary<string, int>();
            foreach (var u in uniquePoliticIds)
            {
                if(top_sim.ContainsKey( u.politic_id))
                {
                    check[u.politic_id] = 1;
                }
                
            }
            ViewBag.check = check;
            return View();
        }
        public IActionResult Sentiment([FromServices] StylometryAnalysis stylometryAnalysis ,[FromServices] TextAnalysis textAnalysis, [FromServices] SentimentAnalysis sentimentAnalysis, [FromServices] NERAnalysis nerAnalysis, string model = "BERT")
        {
           
            var st = _statementData.Statements;
            var st_sent_m = _statementData.LoadDataWMentions(st);

            Dictionary<string, Dictionary<string, List<double>>> pol_sentiments = new Dictionary<string, Dictionary<string, List<double>>>();
            Dictionary<string, double[]> p_counts = new Dictionary<string, double[]>();
            Dictionary<string, double[]> p_countsB = new Dictionary<string, double[]>();

            Dictionary<string, double[]> p_counts_m = new Dictionary<string, double[]>();
            Dictionary<string, double> classic_avg = new Dictionary<string, double>();
            Dictionary<string, double> rt_avg = new Dictionary<string, double>();
            Dictionary<string, List<ServerSentiment>> server_sent = new Dictionary<string, List<ServerSentiment>>();

            Dictionary<string, Dictionary<string, ExtremeSentiment>> ex_s = new Dictionary<string, Dictionary<string, ExtremeSentiment>>();
            Dictionary<string, List<SentimentResult>> avg_rt = new Dictionary<string, List<SentimentResult>>();
            Dictionary<string, Dictionary<string, List<double>>> mentions_sentiment = new Dictionary<string, Dictionary<string, List<double>>>();
            Dictionary<string, Dictionary<string, double>> time_sent = new Dictionary<string, Dictionary<string, double>>();

            for (int i = 2016; i <= 2022; i++)
            {

                
                var st_year=st.Where(x=>x.datum.Value.Year==i).ToList();
                var st_year_m = st_sent_m.Where(x => x.datum.Value.Year == i).ToList();

                time_sent[i.ToString()] = sentimentAnalysis.GetAverageSentimentPerMonth(st_year,model);

                mentions_sentiment[i.ToString()] = _statementData.GetSentimentsByPolitic(st_year,model);

                var politiciansentiments = sentimentAnalysis.PoliticianSentiments(st_year.Where(s => s.RT == false).ToList(),model);
                pol_sentiments[i.ToString()]= politiciansentiments;

                if (model == "BERT")
                {
                    var polaritycounts = st_year.Select(s => s.SentimentBert).ToArray();
                    p_counts[i.ToString()] = polaritycounts;
                    var polaritycountsM = st_year_m.Select(s => s.SentimentBert).ToArray();
                    p_counts_m[i.ToString()] = polaritycountsM;
                }
                else
                {
                    var polaritycounts = st_year.Select(s => s.Sentiment).ToArray();
                    p_counts[i.ToString()] = polaritycounts;
                    var polaritycountsM = st_year_m.Select(s => s.Sentiment).ToArray();
                    p_counts_m[i.ToString()] = polaritycountsM;
                }

                var (classicAvgy, retweetAvgy) = sentimentAnalysis.RT_sentiment(st_year,model);
                classic_avg[i.ToString()] = classicAvgy;
                rt_avg[i.ToString()] = retweetAvgy;

                if (model == "BERT")
                {
                    var server_sentimentY = st_year
                        .GroupBy(s => s.server == "Twitter"
                            ? (s.RT ? "Retweet" : "Twitter")
                            : "Facebook")
                        .Select(g => new ServerSentiment
                        {
                            Server = g.Key,
                            Sentiments = g.Select(s => s.SentimentBert).ToList()
                        })
                        .ToList();

                    server_sent[i.ToString()] = server_sentimentY;
                }
                else
                {
                    var server_sentimentY = st_year
                       .GroupBy(s => s.server == "Twitter"
                           ? (s.RT ? "Retweet" : "Twitter")
                           : "Facebook")
                       .Select(g => new ServerSentiment
                       {
                           Server = g.Key,
                           Sentiments = g.Select(s => s.Sentiment).ToList()
                       })
                       .ToList();
                    server_sent[i.ToString()] = server_sentimentY;
                }

                var st_noRTY = st_year.Where(x => !x.RT).ToList();
                ex_s[i.ToString()] = sentimentAnalysis.CalculateSentimentRatios(st_noRTY,model);

                avg_rt[i.ToString()]= sentimentAnalysis.CalculateAvgSentimentRT(st_year,model);
            }
            var st_noRT = st.Where(x => !x.RT).ToList();

            time_sent["all"] = sentimentAnalysis.GetAverageSentimentPerHalfYear(st, model);
            ViewBag.MonthlySentiment = time_sent;
            pol_sentiments["all"]= sentimentAnalysis.PoliticianSentiments(st_noRT,model);
            if (model == "BERT") { p_counts["all"] = st.Select(s => s.SentimentBert).ToArray(); p_counts_m["all"] = st_sent_m.Select(s => s.SentimentBert).ToArray(); }

            if (model == "VADER") { p_counts["all"] = st.Select(s => s.Sentiment).ToArray(); p_counts_m["all"] = st_sent_m.Select(s => s.Sentiment).ToArray(); }

            
            var (classicAvg, retweetAvg) = sentimentAnalysis.RT_sentiment(st,model);
            classic_avg["all"] = classicAvg;
            rt_avg["all"] = retweetAvg;
            if (model == "BERT")
            {
                server_sent["all"] = st
                .GroupBy(s => s.server == "Twitter"
                    ? (s.RT ? "Retweet" : "Twitter")
                    : "Facebook")
                .Select(g => new ServerSentiment
                {
                    Server = g.Key,
                    Sentiments = g.Select(s => s.SentimentBert).ToList()
                })
                .ToList();
            }

            if (model == "VADER")
            {
                server_sent["all"] = st
                .GroupBy(s => s.server == "Twitter"
                    ? (s.RT ? "Retweet" : "Twitter")
                    : "Facebook")
                .Select(g => new ServerSentiment
                {
                    Server = g.Key,
                    Sentiments = g.Select(s => s.Sentiment).ToList()
                })
                .ToList();
            }

            mentions_sentiment["all"] = _statementData.GetSentimentsByPolitic(st, model);
            ViewBag.mentions_sent = mentions_sentiment;
            ViewBag.pol_sentiments = pol_sentiments;
            ViewBag.polaritycounts = p_counts;
            ViewBag.polaritycountsB = p_countsB;
            ViewBag.polaritycountsM = p_counts_m;

            ViewBag.classicAvg = classic_avg;
            ViewBag.retweetAvg = rt_avg;
            ViewBag.server_sentiment = server_sent;


            ex_s["all"]= sentimentAnalysis.CalculateSentimentRatios(st_noRT,model);
            ViewBag.extreme_s = ex_s;


            avg_rt["all"] = sentimentAnalysis.CalculateAvgSentimentRT(st,model);

            
            ViewBag.avgrt = avg_rt;


            var avgsentimentFMByYear = new Dictionary<string, List<PoliticianSentimentM>>();
            var avgCombinedByYear = new Dictionary<string, List<CombinedPoliticianSentiment>>();

           for (int year = 2016; year <= 2022; year++)
            {
                var yearlySt = st.Where(x => x.datum.Value.Year == year).ToList();
                var yearlyStSentM = st_sent_m.Where(x => x.datum.Value.Year == year).ToList();

                var avgsentimenty = sentimentAnalysis.CalculateAvgSentimentPolitician(yearlySt.Where(s => s.RT == false).ToList(), model);
                avgsentimenty = avgsentimenty.OrderBy(x => x.AverageSentiment).ToList();

                var avgsentimentMentionsy = sentimentAnalysis.CalculateAvgSentimentPolitician(yearlyStSentM,model);
                var avgsentimentFMy = sentimentAnalysis.CalculateAvgSentimentPoliticianFromMentions(yearlySt,model);
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
           
            

            var avgsentiment = sentimentAnalysis.CalculateAvgSentimentPolitician(st_noRT, model);
            avgsentiment = avgsentiment.OrderBy(x => x.AverageSentiment).ToList();

            var avgsentimentMentions = sentimentAnalysis.CalculateAvgSentimentPolitician(st_sent_m,model);
            var avgsentimentFM = sentimentAnalysis.CalculateAvgSentimentPoliticianFromMentions(st,model);
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
            ViewBag.model = model;

            return View();
        }



        public async Task<IActionResult> Emotions([FromServices] EmotionAnalysis emotionAnalysis,
                                            [FromServices] StylometryAnalysis stylometryAnalysis,                                       
                                            
                                            
                                            [FromServices] StatementData statementData,
                                           
                                            [FromServices] TextAnalysis textAnalysis,
                                            [FromServices] SentimentAnalysis sentimentAnalysis,
                                            [FromServices] NERAnalysis nerAnalysis
                                           )
        {
            
            
            var st1 = _statementData.Statements;
            var st = st1.Where(x => x.emotions.Any()).ToList();

            var statementsByYear = st
                .GroupBy(x => x.datum?.Year)
                .ToDictionary(g => g.Key, g => g.ToList());

            var emdistrib = new Dictionary<string, List<EmotionDistribution>>();
            var coocm = new Dictionary<string, Dictionary<string, Dictionary<string, double>>>();
            var avgintens = new Dictionary<string, Dictionary<string, double>>();
            var emdistribFB = new Dictionary<string, List<EmotionDistribution>>();
            var emdistribTW = new Dictionary<string, List<EmotionDistribution>>();
            var sentdata = new Dictionary<string, Dictionary<string, List<double>>>();

            var tasks = new List<Task>();

            for (int i = 2016; i <= 2022; i++)
            {
                var year = i;
                tasks.Add(Task.Run(() =>
                {
                    if (statementsByYear.TryGetValue(year, out var st_year))
                    {
                        var emotionDistribution = emotionAnalysis.PrepareEmotionDistribution(st_year);
                        var coOccurrenceMatrix = emotionAnalysis.CalculateAndNormalizeCoOccurrence(st_year);
                        var avgIntensity = emotionAnalysis.CalculateAverageIntensity(st_year);
                        var emotionDistributionTwitter = emotionAnalysis.PrepareEmotionDistribution(st_year.Where(s => s.server == "Twitter").ToList());
                        var emotionDistributionFacebook = emotionAnalysis.PrepareEmotionDistribution(st_year.Where(s => s.server == "Facebook").ToList());
                        var sentimentData = emotionAnalysis.GetSentimentDistribution(st_year);

                        lock (emdistrib) { emdistrib[year.ToString()] = emotionDistribution; }
                        lock (coocm) { coocm[year.ToString()] = coOccurrenceMatrix; }
                        lock (avgintens) { avgintens[year.ToString()] = avgIntensity; }
                        lock (emdistribFB) { emdistribFB[year.ToString()] = emotionDistributionFacebook; }
                        lock (emdistribTW) { emdistribTW[year.ToString()] = emotionDistributionTwitter; }
                        lock (sentdata) { sentdata[year.ToString()] = sentimentData; }
                    }
                }));
            }

            await Task.WhenAll(tasks);

            var emotionDistributionAll = emotionAnalysis.PrepareEmotionDistribution(st);
            var coOccurrenceMatrixAll = emotionAnalysis.CalculateAndNormalizeCoOccurrence(st);
            var avgIntensityAll = emotionAnalysis.CalculateAverageIntensity(st);
            var emotionDistributionTwitterAll = emotionAnalysis.PrepareEmotionDistribution(st.Where(s => s.server == "Twitter").ToList());
            var emotionDistributionFacebookAll = emotionAnalysis.PrepareEmotionDistribution(st.Where(s => s.server == "Facebook").ToList());
            var sentimentDataAll = emotionAnalysis.GetSentimentDistribution(st);

            emdistrib["all"] = emotionDistributionAll;
            coocm["all"] = coOccurrenceMatrixAll;
            avgintens["all"] = avgIntensityAll;
            emdistribFB["all"] = emotionDistributionFacebookAll;
            emdistribTW["all"] = emotionDistributionTwitterAll;
            sentdata["all"] = sentimentDataAll;

            ViewBag.emotionAll = emdistrib;
            ViewBag.CoOccurrenceData = coocm;
            ViewBag.AverageIntensities = avgintens;
            ViewBag.emotionTwitter = emdistribTW;
            ViewBag.emotionFacebook = emdistribFB;
            ViewBag.SentimentData = sentdata;

            Dictionary<string, List<EntityFrequency>> emotionsNerMix = new Dictionary<string, List<EntityFrequency>>();
            Dictionary<string, List<EntityFrequency>> emotionsNerNames = new Dictionary<string, List<EntityFrequency>>();
            List<string> names = new List<string> { "ps" };
            List<string> mix = new List<string> { "io", "gc", "gu", "ic", "ms", "op", "oa", "if" };

            var uniqueEmotions = st
                .SelectMany(s => s.emotions)
                .Select(e => e.emotion)
                .Distinct()
                .ToList();

            Dictionary<string, Dictionary<string, List<EntityFrequency>>> emonermix = new Dictionary<string, Dictionary<string, List<EntityFrequency>>>();
            Dictionary<string, Dictionary<string, List<EntityFrequency>>> emonernames = new Dictionary<string, Dictionary<string, List<EntityFrequency>>>();
            Dictionary<string, List<PoliticianEmotionData>> emostats = new Dictionary<string, List<PoliticianEmotionData>>();

            tasks.Clear();
            for (int i = 2016; i <= 2022; i++)
            {
                var year = i;
                tasks.Add(Task.Run(() =>
                {
                    if (statementsByYear.TryGetValue(year, out var st_year))
                    {
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

                            var enm = nerAnalysis.PoliticEntityPieChartNormalized3(st_e, st_year, mix);
                            var enn = nerAnalysis.PoliticEntityPieChartNormalized3(st_e, st_year, names);

                            if (enm.Any()) { emotionsNerMixy[emotion] = enm; }
                            if (enn.Any()) { emotionsNerNamesy[emotion] = enn; }
                        }

                        lock (emonermix) { emonermix[year.ToString()] = emotionsNerMixy; }
                        lock (emonernames) { emonernames[year.ToString()] = emotionsNerNamesy; }

                        var resulty = emotionAnalysis.CalculateEmotionStats(st_year, uniqueEmotionsy);
                        lock (emostats) { emostats[year.ToString()] = resulty; }
                    }
                }));
            }

            await Task.WhenAll(tasks);

            Dictionary<string, List<EntityFrequency>> emotionsNerMixAll = new Dictionary<string, List<EntityFrequency>>();

            Dictionary<string, List<EntityFrequency>> emotionsNerNamesAll = new Dictionary<string, List<EntityFrequency>>();

            foreach (var emotion in uniqueEmotions)
            {
                var st_e = st.Where(s => s.emotions.Any(x => x.emotion == emotion)).ToList();

                var enm = nerAnalysis.PoliticEntityPieChartNormalized3(st_e, st, mix);
                var enn = nerAnalysis.PoliticEntityPieChartNormalized3(st_e, st, names);

                if (enm.Any()) { emotionsNerMixAll[emotion] = enm; }
                if (enn.Any()) { emotionsNerNamesAll[emotion] = enn; }
            }

            emonermix["all"] = emotionsNerMixAll;
            emonernames["all"] = emotionsNerNamesAll;

            ViewBag.emotionsNerMix = emonermix;
            ViewBag.emotionsNerNames = emonernames;

            var resultAll = emotionAnalysis.CalculateEmotionStats(st, uniqueEmotions);
            emostats["all"] = resultAll;
            ViewBag.politicStats = emostats;

            return View();
        }

       

        public async Task<IActionResult> NER([FromServices] StylometryAnalysis stylometryAnalysis,    [FromServices] StatementData statementData, [FromServices] TextAnalysis textAnalysis, [FromServices] SentimentAnalysis sentimentAnalysis, [FromServices] NERAnalysis nerAnalysis)
        {
            
            var st = _statementData.Statements;
            

            List<string> types = new List<string> { "io", "gc", "P", "ps", "gu", "ic", "ms", "op", "oa", "if" };
           
            Dictionary<string, Dictionary<string, List<KeyValuePair<string, int>>>> nercountypes = new Dictionary<string, Dictionary<string, List<KeyValuePair<string, int>>>>();
            Dictionary<string, List<string>> enttypes = new Dictionary<string, List<string>>();
            Dictionary<string, List<int>> typecounts = new Dictionary<string, List<int>>();
            Dictionary<string, List<EntitySentimentData>> entsentdata = new Dictionary<string, List<EntitySentimentData>>();
            Dictionary<string, List<string>> allt = new Dictionary<string, List<string>>();
            for (int i = 2016; i <= 2022; i++)
            {
                

                var yearlySt = st.Where(x => x.datum.Value.Year == i).ToList();
                var min = (i == 2019) ? 40 : 5;
                var groupedEntitiesy = nerAnalysis.GetTopEntitiesByType(yearlySt, types,min);
                nercountypes[i.ToString()] = groupedEntitiesy;
                var (entityTypesy, typeCountsy) = nerAnalysis.GetTopEntityTypes(yearlySt);
                enttypes[i.ToString()] = entityTypesy;
                typecounts[i.ToString()] = typeCountsy;

                var entitySentimentDatay = nerAnalysis.CalculateAverageSentimentType(yearlySt, types,min);
                entsentdata[i.ToString()] = entitySentimentDatay;
                allt[i.ToString()]= entitySentimentDatay
                .Select(x => x.EntityType)
                .Distinct()
                .ToList();
                
            }
           
          
       


            var groupedEntities = nerAnalysis.GetTopEntitiesByType(st, types,40);
            nercountypes["all"] = groupedEntities;
            ViewBag.nercounts_types = nercountypes;


            var (entityTypes, typeCounts) = nerAnalysis.GetTopEntityTypes(st);
            enttypes["all"] = entityTypes;
            typecounts["all"] = typeCounts;

            ViewBag.EntityTypes = enttypes;
            ViewBag.TypeCounts = typecounts;


            var entitySentimentData = nerAnalysis.CalculateAverageSentimentType(st, types,40);
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


        


    }
}