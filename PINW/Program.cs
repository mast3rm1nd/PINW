using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;
using System.IO;
using System.Text.RegularExpressions;

namespace PINW
{
    class Program
    {
        static void Main(string[] args)
        {
            var requestedCity = "";
            var daysToPrint = 0;

            #region InputAndChecks
            if (args.Length == 0)
            {
                var help = "";

                help += "Использование: pinw название_города [количество_дней]" + Environment.NewLine + Environment.NewLine;
                help += "Например: pinw Йошкар-Ола   // запрос текущей погоды" + Environment.NewLine;
                help += "Например: pinw Йошкар-Ола 1 // запрос погоды на сегодня" + Environment.NewLine;
                help += "Например: pinw Йошкар-Ола 7 // запрос погоды на неделю вперёд" + Environment.NewLine + Environment.NewLine;
                help += "Если название города содержит пробел, замените его нижним подчёркиванием" + Environment.NewLine;
                help += "Например: pinw Великий_Новгород" + Environment.NewLine;

                Console.Write(help);

                return;
            }
            else if (args.Length == 1)
                requestedCity = args[0].Replace('_', ' ');
            else if(args.Length == 2)
            {
                requestedCity = args[0].Replace('_', ' ');

                if(!int.TryParse(args[1], out daysToPrint))
                {
                    Console.WriteLine("Неверные параметры");
                    return;
                }

                if (daysToPrint < 0)
                    daysToPrint = 2;
                else if (daysToPrint > 10)
                    daysToPrint = 10;
            }
            else
            {
                var help = "";

                help += "Использование: pinw название_города [количество_дней]" + Environment.NewLine + Environment.NewLine;
                help += "Например: pinw Йошкар-Ола   // запрос текущей погоды" + Environment.NewLine;
                help += "Например: pinw Йошкар-Ола 1 // запрос погоды на сегодня" + Environment.NewLine;
                help += "Например: pinw Йошкар-Ола 7 // запрос погоды на неделю вперёд" + Environment.NewLine + Environment.NewLine;
                help += "Если название города содержит пробел, замените его нижним подчёркиванием" + Environment.NewLine;
                help += "Например: pinw Великий_Новгород" + Environment.NewLine;

                Console.Write(help);

                return;
            }
            #endregion

            //requestedCity = "Лондон";

            switch(requestedCity.ToLower())
            {
                case "питер":
                case "спб": requestedCity = "Санкт-Петербург"; break;
                case "нск": requestedCity = "Новосибирск"; break;
                case "мск": requestedCity = "Москва"; break;
            }

            var html = GetHtmlByURL("https://pogoda.yandex.ru/search?request=" + requestedCity);


            #region Checks
            if (html.Contains("По вашему запросу ничего не нашлось"))
            {
                Console.WriteLine("Нет информации по городу \"" + requestedCity + "\"");

                return;
            }

            var fewLinksRegex = "\"link place-list__item-name\" href=\"/(?<FirstLink>.+?)\">(?<CityName>.+?)<";

            if (Regex.IsMatch(html, fewLinksRegex))
            {
                var linksMatch = Regex.Match(html, fewLinksRegex);

                requestedCity = linksMatch.Groups["CityName"].Value;

                var cityLink = linksMatch.Groups["FirstLink"].Value;

                html = GetHtmlByURL("https://pogoda.yandex.ru/" + cityLink);
            }
            #endregion


            #region WeatherRegexes
            var currentDayRegex = "\"current-weather__local-time\">(?<LocalTimeNow>.+?)<" +
                                ".+?" +
                                "\"current-weather__comment\">(?<CurrentWeatherType>.+?)<" +
                                ".+?" +
                                "_thermometer_type_now\">(?<CurrentTemperature>.+?) #хз что за символ тут\n" +
                                ".+?" +
                                "\"wind-speed\">(?<CurrentWind>.+?)\\s";


            var weatherRegex = "\"forecast-detailed__weekday\">(?<DayOfWeek>[а-я]+)< #день недели\n" +
                                ".+?" +
                                "\"forecast-detailed__day-number\">(?<DayNumber>\\d+) #число\n" +
                                ".+?" +
                                "\"forecast-detailed__day-month\">(?<MonthName>[а-я]+) #название месяца\n" +
                                "(</span><spanclass=\"forecast-detailed__day-name\">(?<DetailedDayName>[а-я]+))* #сегодня или завтра - опционально\n" +
                                ".+?" +
                                "утром############\n" +
                                ".+?" +
                                "\"weather-table__temp\">(?<TempUtro>.+?)</div>" +
                                ".+?" +
                                "cell_type_condition" +
                                ".+?" +
                                "\"weather-table__value\">(?<WeatherTypeUtro>.+?)<" +
                                ".+?" +
                                "\"wind-speed\">(?<WindUtro>.+?)<" +
                                ".+?" +
                                "днём############\n" +
                                ".+?" +
                                "\"weather-table__temp\">(?<TempDen>.+?)</div>" +
                                ".+?" +
                                "cell_type_condition" +
                                ".+?" +
                                "\"weather-table__value\">(?<WeatherTypeDen>.+?)<" +
                                ".+?" +
                                "\"wind-speed\">(?<WindDen>.+?)<" +
                                ".+?" +
                                "вечером############\n" +
                                ".+?" +
                                "\"weather-table__temp\">(?<TempVecher>.+?)</div>" +
                                ".+?" +
                                "cell_type_condition" +
                                ".+?" +
                                "\"weather-table__value\">(?<WeatherTypeVecher>.+?)<" +
                                ".+?" +
                                "\"wind-speed\">(?<WindVecher>.+?)<" +
                                ".+?" +
                                "ночью############\n" +
                                ".+?" +
                                "\"weather-table__temp\">(?<TempNoch>.+?)</div>" +
                                ".+?" +
                                "cell_type_condition" +
                                ".+?" +
                                "\"weather-table__value\">(?<WeatherTypeNoch>.+?)<" +
                                ".+?" +
                                "\"wind-speed\">(?<WindNoch>.+?)<";
            #endregion


            #region CurrentDayForecast
            if (daysToPrint == 0)
            {
                var currentDayMatch = Regex.Match(html, currentDayRegex, RegexOptions.IgnorePatternWhitespace);

                var currTime = currentDayMatch.Groups["LocalTimeNow"].Value;
                var currTemperature = currentDayMatch.Groups["CurrentTemperature"].Value.Replace('−', '-');
                var currWeatherType = currentDayMatch.Groups["CurrentWeatherType"].Value;
                var currWind = float.Parse(currentDayMatch.Groups["CurrentWind"].Value);


                var currWeather =
                string.Format("{0}:", requestedCity) + Environment.NewLine +
                string.Format("Местное время = {0}", currTime) + Environment.NewLine +
                string.Format("Температура = {0} °C", currTemperature) + Environment.NewLine +
                currWeatherType + Environment.NewLine +
                string.Format("Скорость ветра = {0} м/c", currWind) + Environment.NewLine;

                Console.WriteLine(currWeather);

                return;
            }
            #endregion

            var weatherMatches = Regex.Matches(html, weatherRegex, RegexOptions.IgnorePatternWhitespace);

            for (int matchIndex = 0; matchIndex < daysToPrint && matchIndex < weatherMatches.Count; matchIndex++)
            {
                var date = weatherMatches[matchIndex].Groups["DayNumber"].Value;
                var month = weatherMatches[matchIndex].Groups["MonthName"].Value;
                var dayOfWeek = weatherMatches[matchIndex].Groups["DayOfWeek"].Value;
                var detailedDay = weatherMatches[matchIndex].Groups["DetailedDayName"].Value; // optional

                // utr
                var tUtr = weatherMatches[matchIndex].Groups["TempUtro"].Value.Replace("&hellip;", "...").Replace('−', '-');
                var weatherTypeUtr = weatherMatches[matchIndex].Groups["WeatherTypeUtro"].Value;
                var windUtr = weatherMatches[matchIndex].Groups["WindUtro"].Value;
                // den
                var tDen = weatherMatches[matchIndex].Groups["TempDen"].Value.Replace("&hellip;", "...").Replace('−', '-');
                var weatherTypeDen = weatherMatches[matchIndex].Groups["WeatherTypeDen"].Value;
                var windDen = weatherMatches[matchIndex].Groups["WindDen"].Value;
                // vech
                var tVecher = weatherMatches[matchIndex].Groups["TempVecher"].Value.Replace("&hellip;", "...").Replace('−', '-');
                var weatherTypeVecher = weatherMatches[matchIndex].Groups["WeatherTypeVecher"].Value;
                var windVecher = weatherMatches[matchIndex].Groups["WindVecher"].Value;
                //noch
                var tNoch = weatherMatches[matchIndex].Groups["TempNoch"].Value.Replace("&hellip;", "...").Replace('−', '-');
                var weatherTypeNoch = weatherMatches[matchIndex].Groups["WeatherTypeNoch"].Value;
                var windNoch = weatherMatches[matchIndex].Groups["WindNoch"].Value;


                var format = "{0,10} °C, {1}, {2} м/с";
                var currDayWeather =
                string.Format("{0} {1}, {2}{3}:", date, month, dayOfWeek, detailedDay == "" ? "" : " (" + detailedDay + ")") + Environment.NewLine +
                string.Format("Утром:   " + format, tUtr, weatherTypeUtr, windUtr) + Environment.NewLine +
                string.Format("Днём:    " + format, tDen, weatherTypeDen, windDen) + Environment.NewLine +
                string.Format("Вечером: " + format, tVecher, weatherTypeVecher, windVecher) + Environment.NewLine +
                string.Format("Ночью:   " + format, tNoch, weatherTypeNoch, windNoch) + Environment.NewLine;

                Console.WriteLine(currDayWeather);
            }


            //Console.Read();
        }


        static string GetHtmlByURL(string url)
        {
            using (var browser = new WebClient())
            {
                Stream data = browser.OpenRead(url);
                StreamReader reader = new StreamReader(data);
                var html = reader.ReadToEnd();

                return html;
            }
        }
    }
}
