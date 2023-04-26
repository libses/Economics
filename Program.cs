using System.Text;
//Сейчас все значения какие-то уж слишком негативные. Я не думаю, что мне нужно брать средний рост по дням, надо придумать что-то другое. Возможно возведение в степень здесь не является валидной операцией.
//Получается слишком много отрицательных значений. Это не очень реалистично.
//Нужны наверн ещё какие-то доверительные интервалы или что-то в этом духе. Иначе слишком уж получается странно, похожие слова как-то влияют по разному.
//И возможно стоит ре-парсануть новости нормально. Потому что сейчас слишком много всратых слов типа вмоскве и прочих. Наверн нужно ещё и в нормальную форму все привести, будет более чётко.
//2018/10/12
//03.01.2012

namespace Economics
{
    public class WordSV
    {
        public double MaxDispersion;
        public double GrowthTotal;
        public Dictionary<string, string> TickerDispersion;
        public string[,] Matrix;
    }

    public class CsvSV
    {
        public string Word;
        public string CSV;
        public double MaxDispersion;
        public double GrowthTotal;
    }

    public static class Settings
    {
        public static int FrequencyFilter = 700;
        public static int TakeCount = 100;
    }

    internal class Program
    {
        public static Dictionary<string, double> tickerToMedianDayGrowth= new Dictionary<string, double>();
        static async Task Main(string[] args)
        {
            var news = ParseFromCsvLentaEconomics();
            Console.WriteLine("News parsed");
            var newsStart = news.Min(x => x.Date);
            var newsEnd = news.Max(x => x.Date);
            var prices = File.ReadAllLines("full.csv").Skip(1).Select(x => new StockPrice(x)).Where(x => x.Price != 0).ToArray();
            var pricesStart = prices.Min(x => x.Date);
            var pricesEnd = prices.Max(x => x.Date);
            Console.WriteLine($"Mins are {newsStart} {pricesStart}");
            Console.WriteLine($"Maxes are {newsEnd} {pricesEnd}");
            Console.WriteLine("Prices parsed");
            var dateToTokens = ExtractDateToTokensDictionary(news);
            Console.WriteLine("DateToTokens Extracted");
            var dateToTickerToPrice = ExtractDateToTickerToPriceDict(prices);
            DetermineAverageGrowth(dateToTickerToPrice);
            Console.WriteLine("dateToTickerToPrice extracted");
            var daysCount = 7;


            var wordData = new Dictionary<string, WordDataExtended>();
            for (int i = 1; i <= daysCount; i++)
            {
                Console.WriteLine(i);
                var tokenToTickerToPriceDiffs = ExtractTokenToTickerToPriceDifferences(prices, dateToTokens, dateToTickerToPrice, i);
                Console.WriteLine("diffs calculated");
                wordData = GetWordDataExtended(wordData, i, tokenToTickerToPriceDiffs);
                Console.WriteLine("got word data");
            }

            ExportAll(wordData, daysCount);
        }

        public static void DetermineAverageGrowth(Dictionary<DateTime, Dictionary<string, StockPrice>> prices)
        {
            var tickers = "MOEXCH\tMOEXCN\tMOEXEU\tMOEXFN\tMOEXMM\tMOEXOG\tMOEXTL\tMOEXTN".Split("\t");
            var tickerToPriceDiffs = new Dictionary<string, List<double>>();
            foreach (var ticker in tickers)
            {
                tickerToPriceDiffs.Add(ticker, new());
            }

            var start = DateTime.Parse("2012/01/03");
            var end = DateTime.Parse("2020/11/01");
            for (DateTime i = start; i < end; i = i.AddDays(1))
            {
                if (!prices.ContainsKey(i))
                {
                    continue;
                }

                if (!prices.ContainsKey(i.AddDays(-1)))
                {
                    continue;
                }

                var todayPrices = prices[i];
                var yesterdayPrices = prices[i.AddDays(-1)];
                foreach (var ticker in tickers)
                {
                    if (!yesterdayPrices.ContainsKey(ticker))
                    {
                        continue;
                    }

                    var priceDiff = todayPrices[ticker].Price / yesterdayPrices[ticker].Price;
                    tickerToPriceDiffs[ticker].Add(priceDiff);
                }
            }

            tickerToMedianDayGrowth = tickerToPriceDiffs.ToDictionary(x => x.Key, y => y.Value.OrderBy(z => z).ToArray().Median());
        }

        public static void ExportAll(Dictionary<string, WordDataExtended> wordData, int daysCount)
        {
            var models = new List<CsvSV>();
            foreach (var word in wordData.Values)
            {
                var matrix = ToMatrix(word, daysCount);
                if (matrix == null)
                {
                    continue;
                }

                var csv = ToCsv(matrix.Matrix, matrix.TickerDispersion);
                var model = new CsvSV() { CSV = csv, MaxDispersion = matrix.MaxDispersion, Word = word.Word, GrowthTotal = matrix.GrowthTotal };
                models.Add(model);
            }

            models = models.OrderBy(x => x.MaxDispersion).Take(Settings.TakeCount).OrderByDescending(x => x.GrowthTotal).ToList();
            Console.WriteLine(models[0].MaxDispersion);
            var counter = 0;
            foreach (var model in models)
            {
                File.WriteAllText($"CSV\\{counter} growth {model.GrowthTotal} dispersion {model.MaxDispersion} {model.Word}.csv", model.CSV);
                counter++;
            }
        }

        public static WordSV ToMatrix(WordDataExtended wordData, int daysCount)
        {
            var sb = new string[wordData.Data.Count + 1, daysCount + 1];
            sb[0, 0] = wordData.Word;
            for (int i = 1; i < daysCount + 1; i++)
            {
                sb[0, i] = i.ToString(); 
            }

            var keys = wordData.Data.Keys.ToArray();
            for (int i = 1; i < wordData.Data.Count + 1; i++)
            {
                sb[i, 0] = keys[i - 1].Name;
            }

            var minDispersion = 1000000000d;
            var sum = 0d;
            var tickerMaxDispersion = new Dictionary<string, string>();
            for (int first = 1; first < wordData.Data.Count + 1; first++)
            {
                var maxDispersion = 0d;
                var ticker = keys[first - 1].Name;
                for (int second = 1; second < daysCount + 1; second++)
                {
                    if (!wordData.Data.ContainsKey(keys[first - 1]))
                    {
                        continue;
                    }

                    var dw = wordData.Data[keys[first - 1]];
                    if (!dw.ContainsKey(new DateWindow { Count = second }))
                    {
                        continue;
                    }

                    if (dw[new DateWindow { Count = second }].Length < Settings.FrequencyFilter)
                    {
                        return null;
                    }

                    var growthByDay = tickerToMedianDayGrowth[keys[first - 1].Name];
                    var growth = 1 + (growthByDay - 1) * second;
                    sb[first, second] = (dw[new DateWindow { Count = second }].Median() - growth).ToString("0.####");
                    var dispersion = dw[new DateWindow { Count = second }].Dispersion();
                    sum += dw[new DateWindow { Count = second }].Median() - growth;
                    if (dispersion > maxDispersion)
                    {
                        maxDispersion = dispersion;
                    }
                }

                tickerMaxDispersion.Add(ticker, maxDispersion.ToString("0.####"));
                if (maxDispersion < minDispersion)
                {
                    minDispersion = maxDispersion;
                }
            }

            return new WordSV() { Matrix = sb, MaxDispersion = minDispersion, GrowthTotal = sum, TickerDispersion = tickerMaxDispersion };
        }

        public static string ToCsv(string[,] matrix, Dictionary<string, string> dispersions)
        {
            var sb = new StringBuilder();
            for (int row = 0; row < matrix.GetLength(1); row++)
            {
                for (int column = 0; column < matrix.GetLength(0); column++)
                {
                    sb.Append(matrix[column, row]);
                    sb.Append(";");
                }

                sb.AppendLine();
            }

            foreach (var dispersion in dispersions.Keys)
            {
                sb.Append(dispersion);
                sb.Append(";");
                sb.AppendLine(dispersions[dispersion]);
            }

            return sb.ToString();
        }

        public static void WriteToBinaryFile<T>(string filePath, T objectToWrite, bool append = false)
        {
            using (Stream stream = File.Open(filePath, append ? FileMode.Append : FileMode.Create))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                binaryFormatter.Serialize(stream, objectToWrite);
            }
        }

        public static T ReadFromBinaryFile<T>(string filePath)
        {
            using (Stream stream = File.Open(filePath, FileMode.Open))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                return (T)binaryFormatter.Deserialize(stream);
            }
        }

        private static Dictionary<string, WordDataExtended> GetWordDataExtended(Dictionary<string, WordDataExtended> toExtend, int window, Dictionary<string, Dictionary<string, List<double>>> tokenToTickerToPriceDifferences)
        {
            foreach (var word in tokenToTickerToPriceDifferences.Keys)
            {
                if (!toExtend.ContainsKey(word))
                {
                    toExtend.Add(word, new WordDataExtended() { Word = word });
                }
                
                var currentWordData = toExtend[word];
                var tickerToPriceDiffs = tokenToTickerToPriceDifferences[word];
                foreach (var ticker in tickerToPriceDiffs.Keys)
                {
                    var nameTicker = new Ticker { Name = ticker };
                    var priceDiffs = tickerToPriceDiffs[ticker];
                    if (!currentWordData.Data.ContainsKey(nameTicker))
                    {
                        currentWordData.Data.Add(nameTicker, new Dictionary<DateWindow, double[]>());
                    }

                    var dateWindowToPriceDiffs = currentWordData.Data[nameTicker];
                    var oneDayWindow = new DateWindow() { Count = window };

                    if (!dateWindowToPriceDiffs.ContainsKey(oneDayWindow))
                    {
                        dateWindowToPriceDiffs.Add(oneDayWindow, priceDiffs.OrderBy(x => x).ToArray());
                    }
                }
            }

            return toExtend;
        }

        private static Dictionary<string, Dictionary<string, List<double>>> ExtractTokenToTickerToPriceDifferences(StockPrice[] prices, Dictionary<DateTime, HashSet<string>> dateToTokens, Dictionary<DateTime, Dictionary<string, StockPrice>> dateToTickerToPrice, int windowInDays)
        {
            var tokensToTickersToPriceDifferences = new Dictionary<string, Dictionary<string, List<double>>>();
            foreach (var price in prices)
            {
                if (!dateToTokens.ContainsKey(price.Date))
                {
                    continue;
                }

                var expirationDate = price.Date.AddDays(windowInDays);
                if (!dateToTickerToPrice.ContainsKey(expirationDate))
                {
                    continue;
                }

                var expirationPrice = dateToTickerToPrice[expirationDate];
                if (!expirationPrice.ContainsKey(price.Ticker))
                {
                    continue;
                }

                var expirationPriceByTicker = expirationPrice[price.Ticker];
                var priceDifference = expirationPriceByTicker.Price / price.Price;
                var tokens = dateToTokens[price.Date];
                foreach (var token in tokens)
                {
                    if (!tokensToTickersToPriceDifferences.ContainsKey(token))
                    {
                        tokensToTickersToPriceDifferences.Add(token, new Dictionary<string, List<double>>());
                    }

                    var tickerToPriceDifferences = tokensToTickersToPriceDifferences[token];
                    if (!tickerToPriceDifferences.ContainsKey(price.Ticker))
                    {
                        tickerToPriceDifferences.Add(price.Ticker, new List<double>());
                    }

                    var priceDifferences = tickerToPriceDifferences[price.Ticker];
                    priceDifferences.Add(priceDifference);
                }
            }

            return tokensToTickersToPriceDifferences;
        }

        private static Dictionary<DateTime, Dictionary<string, StockPrice>> ExtractDateToTickerToPriceDict(StockPrice[] prices)
        {
            var pricesDict = new Dictionary<DateTime, Dictionary<string, StockPrice>>();
            foreach (var price in prices)
            {
                if (!pricesDict.ContainsKey(price.Date))
                {
                    pricesDict.Add(price.Date, new Dictionary<string, StockPrice>());
                }

                pricesDict[price.Date].Add(price.Ticker, price);
            }

            return pricesDict;
        }

        private static Dictionary<DateTime, HashSet<string>> ExtractDateToTokensDictionary(SimpleNews[] news)
        {
            var dateToTokens = new Dictionary<DateTime, HashSet<string>>();
            foreach (var article in news)
            {
                if (!dateToTokens.ContainsKey(article.Date))
                {
                    dateToTokens.Add(article.Date, new HashSet<string>());
                }

                dateToTokens[article.Date].UnionWith(article.Tokens);
            }

            return dateToTokens;
        }

        public static SimpleNews[] ParseFromCsv()
        {
            var lines = File.ReadAllLines("normal.csv");
            var news = lines.Select(x => x.Split(",")).Where(x => DateTime.TryParse(x[1], out var n)).Select(x => new SimpleNews(x[0], x[1])).ToArray();
            return news;
        }

        public static SimpleNews[] ParseFromCsvLentaEconomics()
        {
            var lines = File.ReadAllLines("economical_lenta.csv");
            var news = lines.Skip(1)
                .Select(x => x.Split(";"))
                .Where(x => x.Length == 3)
                .Where(x => long.TryParse(x[2], out var _))
                .Select(x => new SimpleNews(x[1], long.Parse(x[2])))
                .ToArray();
            return news;
        }
    }
}