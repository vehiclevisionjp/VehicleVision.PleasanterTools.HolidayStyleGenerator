using CsvHelper;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NLog;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;

namespace VehicleVision.PleasanetrTools.HolidayStyleGenerator
{
    class Program
    {
        private static string currentPath => Directory.GetCurrentDirectory();

        private static readonly HttpClient httpClient = new HttpClient();

        private static Logger logger = LogManager.GetCurrentClassLogger();

        static async Task Main(string[] args)
        {
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                var argsDic = ArgsType(args);

                if (!argsDic.TryGetValue("p", out var basePath))
                {
                    logger.Fatal("Output path is not specified. Please specify with /p.");
                    return;
                }

                if (!Directory.Exists(basePath))
                {
                    logger.Fatal("Output path does not exist. Please check the path.");
                    return;
                }

                var outputPath = Path.Combine(basePath, "App_Data", "Parameters", "ExtendedStyles", "HolidayStyle");
                var paramCalendar = DeserializeFromFile<Parameters.Calendar>(Path.Combine(currentPath, "Parameters", "Calendar.json"));

                using (var request = new HttpRequestMessage(HttpMethod.Get, new Uri(paramCalendar.CalendarUrl)))
                using (var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        logger.Fatal("Unable to retrieve the CSV file for the calendar.");
                        return;
                    }

                    using (var content = response.Content)
                    using (var stream = await content.ReadAsStreamAsync())
                    using (var reader = new StreamReader(stream, Encoding.GetEncoding("Shift_JIS")))
                    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                    {
                        var records = csv.GetRecords<Calendar>();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }

        public static Dictionary<string, string> ArgsType(string[] args)
        {
            var argsDic = new Dictionary<string, string>();

            for (int i = 0; i < args.Length; i++)
            {
                //'/'始まりの場合はそれをパラメータと識別する
                if (Regex.IsMatch(args[i], "^/"))
                {
                    string key = Regex.Replace(args[i], "^/", "");
                    string value = string.Empty;

                    //今のサーチ場所が最末尾でない
                    //次の場所がパラメータでない
                    if (i != args.Length - 1 && !Regex.IsMatch(args[i + 1], "^/"))
                    {
                        value = args[i + 1];
                        i++;
                    }

                    argsDic.Add(key, value);
                }
            }

            return argsDic;
        }

        public static T DeserializeFromFile<T>(string path)
        {
            if (!File.Exists(path))
            {
                return default;
            }

            try
            {
                //JSONファイルを開く
                using (var stream = new FileStream(path, FileMode.Open))
                {
                    //JSONファイルを読み出す
                    using (var sr = new StreamReader(stream))
                    {
                        //デシリアライズオブジェクト関数に読み込んだデータを渡して、
                        //指定されたデータ用のクラス型で値を返す。
                        return JsonConvert.DeserializeObject<T>(sr.ReadToEnd());
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return default;
            }
        }
    }
}