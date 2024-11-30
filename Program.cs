using CsvHelper;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NLog;
using System.Drawing;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;

namespace VehicleVision.PleasanterTools.HolidayStyleGenerator
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

                //全更新するかどうか
                var allRefresh = argsDic.ContainsKey("a");

                //ペースパスの有無を取得する
                if (!argsDic.TryGetValue("p", out var rootPath))
                {
                    rootPath = Path.Combine(currentPath, "..", "Implem.Pleasanter");
                }

                //ベースのパスが存在しない時は落とす
                if (!Directory.Exists(rootPath))
                {
                    logger.Fatal("Root path does not exist. Please check the path.");
                    return;
                }

                var exStylePath = Path.Combine(rootPath, "App_Data", "Parameters", "ExtendedStyles");

                //拡張スタイルのパスが存在しない時は落とす
                if (!Directory.Exists(exStylePath))
                {
                    logger.Fatal("ExtendedStyles path does not exist. Please check the path.");
                    return;
                }

                //ベースパスから出力先のパスを取得する
                var outputPath = Path.Combine(exStylePath, "CalendarStyle");

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

                        //祝日
                        foreach (var recordsYear in records.GroupBy(record => record.Date.Year))
                        {
                            var outputFile = Path.Combine(outputPath, $"CalendarStyle-Holiday{recordsYear.Key}.css");

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
                                        @$"#CalendarBody #Grid tbody tr td[data-id=""{record.Date:yyyy/M/d}""]:not(.other-month){{background-color:{paramCalendar.HolidayBackgroundColor} !important;}}"
                                        + Environment.NewLine
                                        + $@"#CalendarBody #Grid tbody tr td[data-id=""{record.Date:yyyy/M/d}""] div .day:after{{content:""{record.Title}"";margin-left:5px;}}"
                                        + Environment.NewLine
                                    );
                                }

                                logger.Info($"{Path.GetFileName(outputFile)} Created.");
                            }
                        }

                        //週末
                        {
                            var outputFile = Path.Combine(outputPath, $"CalendarStyle-Weekend.css");

                            //既に出力済みのファイルがある場合は削除する
                            if (File.Exists(outputFile))
                            {
                                File.Delete(outputFile);
                                logger.Info($"{Path.GetFileName(outputFile)} Deleted.");
                            }

                            File.AppendAllText(
                            outputFile,
                                @$"#CalendarBody #Grid tbody tr td:nth-child({paramCalendar.SaturdayIndex}):not(.other-month){{background-color:{paramCalendar.SaturdayBackgroundColor};}}"
                                + Environment.NewLine
                                + @$"#CalendarBody #Grid tbody tr td:nth-child({paramCalendar.SundayIndex}):not(.other-month){{background-color:{paramCalendar.SundayBackgroundColor};}}"
                                + Environment.NewLine
                            );

                            logger.Info($"{Path.GetFileName(outputFile)} Created.");
                        }
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