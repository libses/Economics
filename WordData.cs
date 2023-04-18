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

    [Serializable]
    public class WordDataExtended
    {
        public string Word;
        public Dictionary<Ticker, Dictionary<DateWindow, double[]>> Data = new Dictionary<Ticker, Dictionary<DateWindow, double[]>>();
    }

    public static class StatisticsExtensions
    {
        public static double Median(this double[] Data)
        {
            return Data[(Data.Length - 1) / 2];
        }

        public static double Percentile(this double[] Data, double value)
        {
            return Data[(int)(value * Data.Length)];
        }

        public static double Dispersion(this double[] Data)
        {
            return Data.Sum(x => (Data.Median() - x) * (Data.Median() - x));
        }
    }

    [Serializable]
    public class Ticker
    {
        public string Name;
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            return ((Ticker)obj).Name.Equals(Name);
        }
    }

    [Serializable]
    public class DateWindow
    {
        public int Count;

        public override int GetHashCode()
        {
            return Count;
        }

        public override bool Equals(object? obj)
        {
            return ((DateWindow)obj).Count == Count;
        }
    }
}