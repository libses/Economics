namespace Economics
{
    public class CsvProcessor
    {
        public static void ConvertCsv()
        {
            var file = File.OpenText("lenta.csv");
            var outFile = File.CreateText("normal.csv");
            var line = file.ReadLine();
            for (int i = 0; i < 797831; i++)
            {
                var splitted = line.Split(",");
                if (splitted.Length < 3)
                {
                    line = file.ReadLine();
                    continue;
                }

                var title = splitted[1] + splitted[2];
                var date = splitted[splitted.Length - 1];
                var res = $"{title},{date}";
                outFile.WriteLine(res);
                line = file.ReadLine();
                if (i % 1000 == 0)
                {
                    Console.WriteLine(i);
                }
            }

            //outFile.Flush();
            file.Close();
            outFile.Close();
        }
    }
}