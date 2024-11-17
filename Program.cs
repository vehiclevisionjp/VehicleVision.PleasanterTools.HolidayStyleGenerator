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

                //パラメータを取得する
                var argsDic = ArgsType(args);

                //ペースパスの有無を取得する
                if (!argsDic.TryGetValue("p", out var basePath))
                {
                    logger.Fatal("Output path is not specified. Please specify with /p.");
                    return;
                }

                //ベースのパスが存在しない時は落とす
                if (!Directory.Exists(basePath))
                {
                    logger.Fatal("Output path does not exist. Please check the path.");
                    return;
                }

                //全更新するかどうか
                var allRefresh = argsDic.ContainsKey("a");

                //ベースパスから出力先のパスを取得する
                var outputPath = Path.Combine(basePath, "App_Data", "Parameters", "ExtendedStyles", "CalendarStyle");

                //出力先が存在しない時は作る
                if (!Path.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }

                //パラメータ読み取り
                //ジェネレータのカレンダー設定
                var paramCalendar = DeserializeFromFile<Parameters.Calendar>(Path.Combine(currentPath, "Parameters", "Calendar.json"));

                //内閣府のサイトより公示された祝日データを取得する
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

                        foreach (var recordsYear in records.GroupBy(record => record.Date.Year))
                        {
                            var outputFile = Path.Combine(outputPath, $"CalendarHoliday-{recordsYear.Key}.css");

                            //既に出力済みのファイルがある場合は削除する
                            if (File.Exists(outputFile))
                            {
                                //存在するファイルが過去のものである場合は更新
                                if (recordsYear.Key < DateTime.Today.Year && !allRefresh)
                                {
                                    continue;
                                }

                                File.Delete(outputFile);
                                logger.Info($"{Path.GetFileName(outputFile)} Deleted.");
                            }

                            if (recordsYear.Any())
                            {
                                foreach (var record in recordsYear)
                                {
                                    File.AppendAllText(
                                        outputFile,
                                        @$"#CalendarBody #Grid tbody tr td[data-id=""{record.Date:yyyy/M/d}""]:not(.other-month){{background-color:{paramCalendar.HolidayColor} !important;}}"
                                        + Environment.NewLine
                                        + $@"#CalendarBody #Grid tbody tr td[data-id=""{record.Date:yyyy/M/d}""] div .day:after{{content:""{record.Title}"";margin-left:5px;}}"
                                        + Environment.NewLine
                                    );
                                }

                                logger.Info($"{Path.GetFileName(outputFile)} Created.");
                            }
                        }
                    }
                }

                //土日のデータについてはパラメータから基準日を読み出して使う
                //FirstDayOfWeek/日曜位置/土曜位置
                //0=日 7/1
                //1=月 6/7
                //2=火 5/6
                //3=水 4/5
                //4=木 3/4
                //5=金 2/3
                //6=土 1/2
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