using Newtonsoft.Json;
using System.Text;

namespace Economics
{
    public class WordData
    {
        public WordData(string word, double[] data)
        {
            Word = word;
            Data = data.OrderBy(x => x).ToArray();
        }

        public string Word;
        public double[] Data;
        public double Median => Data[(Data.Length - 1) / 2];
        public double Percentile(double value) => Data[(int)(value * Data.Length)];
        public double Percentile90 => Percentile(0.9);
        public double Percentile10 => Percentile(0.1);
        public double Dispersion => Data.Sum(x => (Median - x) * (Median - x));
    }

    public class News
    {
        public string Link { get; set; }

        public string Headline { get; set; }

        public string Category { get; set; }

        [JsonProperty("short_description")]
        public string ShortDescription { get; set; }

        public string Authors { get; set; }

        public DateTime Date { get; set; }
    }

    public class SimpleNews
    {
        public string Text { get; set; }
        public DateTime Date { get; set; }
        
        public string[] Tokens { get; set; }

        public SimpleNews(News news)
        {
            Text = $"{news.Headline} {news.ShortDescription}";
            Date = news.Date;
            Tokens = Text.ToLower().Split(" ").Select(x => string.Join("", x.Where(y => char.IsLetter(y)))).Distinct().ToArray();
        }

        public SimpleNews(string text, string date)
        {
            Text = text;
            Date = DateTime.Parse(date);
            Tokens = Text.ToLower().Split(" ").Select(x => string.Join("", x.Where(y => char.IsLetter(y)))).Distinct().ToArray();
        }
    }

    public class StockPrice
    {
        public string Ticker { get; set; }
        public DateTime Date { get; set; }
        public double Price { get; set; }

        public StockPrice(string csv)
        {
            var spltitted = csv.Split(",");
            Ticker = spltitted[0];
            Date = Parse(spltitted[1]);
            if (Ticker == "MOEXREPO")
            {
                Price = 0;
                return;
            }

            Price = double.Parse(spltitted[2].Replace('.', ','));
        }

        public DateTime Parse(string date)
        {
            if (DateTime.TryParse(date, out var res))
            {
                return res;
            }

            var lexems = date.Split('/');
            if (lexems[2].Length == 2)
            {
                lexems[2] = $"20{lexems[2]}";
            }

            return new DateTime(int.Parse(lexems[2]), int.Parse(lexems[0]), int.Parse(lexems[1]));
        }
    }
    
    internal class Program
    {
        static void Main(string[] args)
        {
            var result = new Dictionary<string, Dictionary<string, double>>();
            var news = ParseFromCsv();

            var prices = File.ReadAllLines("full.csv").Skip(1).Select(x => new StockPrice(x)).Where(x => x.Price != 0).ToArray();
            Console.WriteLine(prices);
            var priceGroups = new Dictionary<string, List<StockPrice>>();
            foreach (var price in prices)
            {
                if (!priceGroups.ContainsKey(price.Ticker))
                {
                    priceGroups.Add(price.Ticker, new List<StockPrice>());
                }

                priceGroups[price.Ticker].Add(price);
            }

            foreach (var key in priceGroups.Keys)
            {
                var dateToPriceDict = priceGroups[key].ToDictionary(x => x.Date);

                Console.WriteLine(key);
                var res = CountAndWrite(news, dateToPriceDict, 1, 20, 100);
                result.Add(key, res);
            }

            var allTickers = result.Keys.Select(x => x).ToArray();
            var allWords = result.SelectMany(x => x.Value.Keys.ToArray()).Distinct().ToArray();
            var tickersDict = allTickers.Select((x, i) => (x, i)).ToDictionary(x => x.x, y => y.i);
            var wordsDict = allWords.Select((x, i) => (x, i)).ToDictionary(x => x.x, y => y.i);
            var matrix = new double[allTickers.Length, allWords.Length];
            foreach (var ticker in result.Keys)
            {
                foreach (var word in result[ticker].Keys)
                {
                    matrix[tickersDict[ticker], wordsDict[word]] = result[ticker][word];
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Word;{string.Join(";", allTickers)}");
            for (int i = 0; i < allWords.Length; i++)
            {
                sb.Append($"{allWords[i]};");
                for (int j = 0; j < allTickers.Length; j++)
                {
                    if (matrix[j, i] != 0)
                    {
                        sb.Append($"{matrix[j, i]};");
                    }
                    else
                    {
                        sb.Append(";");
                    }
                }

                sb.AppendLine();
            }

            File.WriteAllText("outtput.txt", sb.ToString());
        }

        public static SimpleNews[] ParseFromJson()
        {
            var lines = File.ReadAllLines("news.json");
            var news = lines.Select(x => new SimpleNews(JsonConvert.DeserializeObject<News>(x))).ToArray();
            return news;
        }

        public static SimpleNews[] ParseFromCsv()
        {
            var lines = File.ReadAllLines("normal.csv");
            var news = lines.Select(x => x.Split(",")).Where(x => DateTime.TryParse(x[1], out var n)).Select(x => new SimpleNews(x[0], x[1])).ToArray();
            return news;
        }

        public static Dictionary<string, double> CountAndWrite(SimpleNews[] news, Dictionary<DateTime, StockPrice> dateToPriceDict, int daysCount, int FrequencyFilter, double DispersionFilter)
        {
            var year = GetWordData(news, dateToPriceDict, TimeSpan.FromDays(daysCount), FrequencyFilter, DispersionFilter);
            Console.WriteLine($"{daysCount} days");
            Console.WriteLine(year.Length);
            PrintWordData(year);
            Console.WriteLine();
            Console.WriteLine("---------------------------------");
            return year.ToDictionary(x => x.Word, x => x.Median);
        }

        public static void PrintWordData(WordData[] wordData)
        {
            Console.WriteLine($"{"Word", -16}\tMedian\tD\t0.1\t0.9\tCount");
            foreach (var data in wordData)
            {
                Console.WriteLine($"{data.Word, -16}\t{data.Median.ToString("0.###")}\t{data.Dispersion.ToString("0.###")}\t{data.Percentile10.ToString("0.###")}\t{data.Percentile90.ToString("0.###")}\t{data.Data.Length}");
            }
        }

        public static WordData[] GetWordData(SimpleNews[] news, Dictionary<DateTime, StockPrice> dateToPriceDict, TimeSpan Timespan, int FrequencyFilter, double DispersionFilter)
        {
            var diffDict = new Dictionary<string, List<double>>();
            var dateTokenSet = new Dictionary<string, HashSet<DateTime>>();
            foreach (var e in news)
            {
                foreach (var token in e.Tokens)
                {
                    var currentDate = e.Date;
                    if (dateTokenSet.ContainsKey(token))
                    {
                        if (dateTokenSet[token].Contains(currentDate))
                        {
                            continue;
                        }
                        else
                        {
                            dateTokenSet[token].Add(currentDate);
                        }
                    }
                    else
                    {
                        dateTokenSet.Add(token, new HashSet<DateTime> { currentDate });
                    }

                    var timespanAfter = e.Date.Add(Timespan);
                    if (diffDict.ContainsKey(token))
                    {
                        if (dateToPriceDict.ContainsKey(currentDate) && dateToPriceDict.ContainsKey(timespanAfter))
                        {
                            diffDict[token].Add(dateToPriceDict[timespanAfter].Price / dateToPriceDict[currentDate].Price);
                        }
                    }
                    else
                    {
                        diffDict.Add(token, new List<double>());
                        if (dateToPriceDict.ContainsKey(currentDate) && dateToPriceDict.ContainsKey(timespanAfter))
                        {
                            diffDict[token].Add(dateToPriceDict[timespanAfter].Price / dateToPriceDict[currentDate].Price);
                        }
                    }
                }
            }

            return diffDict.Where(x => x.Value.Count > FrequencyFilter).Select(x => new WordData(x.Key, x.Value.ToArray())).Where(x => x.Dispersion < DispersionFilter).OrderBy(x => x.Dispersion).Take(200).ToArray();
        }
    }
}