namespace Economics
{
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
}