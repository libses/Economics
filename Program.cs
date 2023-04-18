using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace Economics
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var news = ParseFromCsv();
            Console.WriteLine("News parsed");
            var prices = File.ReadAllLines("full.csv").Skip(1).Select(x => new StockPrice(x)).Where(x => x.Price != 0).ToArray();
            Console.WriteLine("Prices parsed");
            var dateToTokens = ExtractDateToTokensDictionary(news);
            Console.WriteLine("DateToTokens Extracted");
            var dateToTickerToPrice = ExtractDateToTickerToPriceDict(prices);
            Console.WriteLine("dateToTickerToPrice extracted");

            var wordData = new Dictionary<string, WordDataExtended>();
            for (int i = 1; i <= 7; i++)
            {
                Console.WriteLine(i);
                var tokenToTickerToPriceDiffs = ExtractTokenToTickerToPriceDifferences(prices, dateToTokens, dateToTickerToPrice, i);
                Console.WriteLine("diffs calculated");
                wordData = GetWordDataExtended(wordData, i, tokenToTickerToPriceDiffs);
                Console.WriteLine("got word data");
            }

            
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
                    toExtend.Add(word, new WordDataExtended());
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

     
    }
}