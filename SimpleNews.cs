namespace Economics
{
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
}