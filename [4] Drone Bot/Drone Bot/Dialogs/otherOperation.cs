using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Device;
using System.Device.Location;

namespace datamall_bot.Dialogs
{
    public class otherOperation
    {
        public static async Task<addressObj> GetAddress(string searchTerm)
        {
            var http = new HttpClient();
            http.DefaultRequestHeaders.Add("Accept", "application/json");
            string url = $"https://developers.onemap.sg/commonapi/search?searchVal={searchTerm}&returnGeom=Y&getAddrDetails=Y";
            var response = await http.GetAsync(url);
            var result = await response.Content.ReadAsStringAsync();
            addressObj addressJSON = JsonConvert.DeserializeObject<addressObj>(result);
            return addressJSON;
        }

        public static string generateMapURL(string lat, string lon)
        {
            string mapStyle = "";
            DateTime time = DateTime.UtcNow.AddHours(8);
            int hour = time.Hour;
            if (hour > 6 && hour < 18)
            {
                mapStyle = "default";
            }
            else
            {
                mapStyle = "night";
            }
            string point = $"[{lat},{lon},\"175,50,0\",\"A\"]";
            string mapURL = $"https://developers.onemap.sg/commonapi/staticmap/getStaticImage?layerchosen={mapStyle}&lat={lat}&lng={lon}&zoom=16&height=256&width=256&points=" + point;
            return mapURL;
        }

        //call NEA rest endpoint to retrieve all weather forecast
        public static async Task<weatherJSON> getWeatherForecast()
        {
            var http = new HttpClient();
            string url = "http://api.nea.gov.sg/api/WebAPI/?dataset=2hr_nowcast&keyref={NEA API Key}";
            var response = await http.GetAsync(url);
            var result = await response.Content.ReadAsStringAsync();

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(result);

            string jsonResponse = JsonConvert.SerializeXmlNode(doc);
            jsonResponse = jsonResponse.Replace("@", "");
            weatherJSON JSON = JsonConvert.DeserializeObject<weatherJSON>(jsonResponse);
            return JSON;
        }

        //there's multiple operation here
        //get user's coordinate as input
        //then it will calculate the minumum distance from each weather location to get the town name
        //based on that we can retrieve the respective weathers from NEA weather api call 
        public static async Task<string> getNearestWeatherForecast(string lat, string lon)
        {
            List<double> distanceMatrix = new List<double>();
            List<string> areaMatrix = new List<string>();
            List<string> forecastMatrix = new List<string>();

            double userLat = Convert.ToDouble(lat);
            double userLon = Convert.ToDouble(lon);
            var userCoord = new GeoCoordinate(userLat, userLon);

            weatherJSON JSON = await getWeatherForecast();
            int areaCount = JSON.channel.item.weatherForecast.area.Count();
            for(int i = 0; i < areaCount; i++)
            {
                double areaLat = Convert.ToDouble(JSON.channel.item.weatherForecast.area[i].lat);
                double areaLon = Convert.ToDouble(JSON.channel.item.weatherForecast.area[i].lon);
                var areaCoord = new GeoCoordinate(areaLat, areaLon);

                double distance = userCoord.GetDistanceTo(areaCoord);

                distanceMatrix.Add(distance);
                areaMatrix.Add(JSON.channel.item.weatherForecast.area[i].name);
                forecastMatrix.Add(JSON.channel.item.weatherForecast.area[i].forecast);
            }

            int minIndex = distanceMatrix.IndexOf(distanceMatrix.Min());
            string validTime = JSON.channel.item.validTime;
            string weatherForecast = weatherForecastAbbreviations(forecastMatrix[minIndex]);
            string response = $"FYI, The weather forecast for {areaMatrix[minIndex]} is {weatherForecast} @ {validTime}";
            return response;
        }

        public static string weatherForecastAbbreviations(string forecast)
        {
            Dictionary<string, string> weatherDictionary = new Dictionary<string, string>();
            weatherDictionary.Add("BR", "Mist");
            weatherDictionary.Add("CL", "Cloudy");
            weatherDictionary.Add("DR", "Drizzle");
            weatherDictionary.Add("FA", "Fair (Day");
            weatherDictionary.Add("FG", "Fog");
            weatherDictionary.Add("FN", "Fair(Night)");
            weatherDictionary.Add("FW", "Fair & Warm");
            weatherDictionary.Add("HG", "Heavy Thundery Showers with Gusty Winds");
            weatherDictionary.Add("HR", "Heavy Rain");
            weatherDictionary.Add("HS", "Heavy Showers");
            weatherDictionary.Add("HT", "Heavy Thundery Showers");
            weatherDictionary.Add("HZ", "Hazy");
            weatherDictionary.Add("LH", "Slightly Hazy");
            weatherDictionary.Add("LR", "Light Rain");
            weatherDictionary.Add("LS", "Light Showers");
            weatherDictionary.Add("OC", "Overcast");
            weatherDictionary.Add("PC", "Partly Cloudy (Day)");
            weatherDictionary.Add("PN", "Partly Cloudy (Night)");
            weatherDictionary.Add("PS", "Passing Showers");
            weatherDictionary.Add("RA", "Moderate Rain");
            weatherDictionary.Add("SH", "Showers");
            weatherDictionary.Add("SK", "Strong Winds, Showers");
            weatherDictionary.Add("SN", "Snow");
            weatherDictionary.Add("SR", "Strong Winds, Rain");
            weatherDictionary.Add("SS", "Snow Showers");
            weatherDictionary.Add("SU", "Sunny");
            weatherDictionary.Add("SW", "Strong Winds");
            weatherDictionary.Add("TL", "Thundery Showers");
            weatherDictionary.Add("WC", "Windy, Cloudy");
            weatherDictionary.Add("WD", "Windy");
            weatherDictionary.Add("WF", "Windy, Fair");
            weatherDictionary.Add("WR", "Windy, Rain");
            weatherDictionary.Add("WS", "Windy Showers");

            if(weatherDictionary.ContainsKey(forecast))
            {
                return weatherDictionary[forecast];
            }
            else
            {
                return "notFound";
            }
        }
    }

    public class addressObj
    {
        public int found { get; set; }
        public int totalNumPages { get; set; }
        public int pageNum { get; set; }
        public Result[] results { get; set; }
    }

    public class Result
    {
        public string SEARCHVAL { get; set; }
        public string BLK_NO { get; set; }
        public string ROAD_NAME { get; set; }
        public string BUILDING { get; set; }
        public string ADDRESS { get; set; }
        public string POSTAL { get; set; }
        public string X { get; set; }
        public string Y { get; set; }
        public string LATITUDE { get; set; }
        public string LONGITUDE { get; set; }
        public string LONGTITUDE { get; set; }
    }

    public class ForecastIssue
    {
        public string date { get; set; }
        public string time { get; set; }
    }

    public class Area
    {
        public string forecast { get; set; }
        public string lat { get; set; }
        public string lon { get; set; }
        public string name { get; set; }
    }

    public class WeatherForecast
    {
        public IList<Area> area { get; set; }
    }

    public class Item
    {
        public string title { get; set; }
        public string category { get; set; }
        public ForecastIssue forecastIssue { get; set; }
        public string validTime { get; set; }
        public WeatherForecast weatherForecast { get; set; }
    }

    public class Channel
    {
        public string title { get; set; }
        public string source { get; set; }
        public string description { get; set; }
        public Item item { get; set; }
    }

    public class weatherJSON
    {
        public Channel channel { get; set; }
    }

}