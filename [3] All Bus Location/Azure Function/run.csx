#r "Newtonsoft.Json"

using System.Net;
using Newtonsoft.Json;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info("C# HTTP trigger function processed a request.");

        string busNo = "";
        int direction = 0;
        dynamic data = await req.Content.ReadAsAsync<object>();
        busNo = data?.ServiceNo;
        direction = data?.Direction;

        List<Coordinate> response = await busObject.returnAllBusCoordinate(busNo,direction);

        CoordinateObject responseJSON = new CoordinateObject();
        responseJSON.ServiceNo = busNo;
        responseJSON.Direction = direction;
        responseJSON.BusCoordinate = response;

    return busNo == null
        ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body")
        : req.CreateResponse(HttpStatusCode.OK, responseJSON,"application/json");
}

//Class to process bus
    class busObject
    {
        public static async Task<List<string>> returnBusRoute(string busNo, int direction)
        {
            List<string> busStopCode = new List<string>();
            string baseURL = "<Azure Table Storage of all bus service>";
            string filter = $"&$filter=ServiceNo%20eq%20'{busNo}'%20and%20Direction%20eq%20{direction.ToString()}";
            string requestURL = baseURL + filter;
            var http = new HttpClient();
            http.DefaultRequestHeaders.Add("Accept", "application/json;odata=nometadata");
            var response = await http.GetAsync(requestURL);
            var result = await response.Content.ReadAsStringAsync();
            BusRoute routeJSON = JsonConvert.DeserializeObject<BusRoute>(result);
            int count = routeJSON.value.Count();
            for(int i = 0; i < count; i++)
            {
                busStopCode.Add(routeJSON.value[i].BusStopCode);
            }
            return busStopCode;
        }

        public static async Task<string> returnBusLocation(string busStopCode, string serviceNo)
        {
            var http = new HttpClient();
            http.DefaultRequestHeaders.Add("AccountKey", "<LTA Account Key>");
            string requestURL = "http://datamall2.mytransport.sg/ltaodataservice/BusArrivalv2?BusStopCode=" + busStopCode + "&ServiceNo=" + serviceNo;
            var response = await http.GetAsync(requestURL);
            var result = await response.Content.ReadAsStringAsync();
            BusArrival nextbusdata = JsonConvert.DeserializeObject<BusArrival>(result);
            string location = $"{nextbusdata.Services[0].NextBus.Latitude},{nextbusdata.Services[0].NextBus.Longitude}";
            return location;            
        }

        public static async Task<List<Coordinate>> returnAllBusCoordinate(string busNo, int direction)
        {
            List<string> busStopCode = await returnBusRoute(busNo, direction);
            List<Coordinate> coordinateList = new List<Coordinate>();
            List<string> busCoordinateList = new List<string>();
            string coordinatePH = "";

            foreach(string busStop in busStopCode)
            {
                coordinatePH = await returnBusLocation(busStop, busNo);
                if (!busCoordinateList.Exists(item => item == coordinatePH) & coordinatePH != "0,0")
                {
                    busCoordinateList.Add(coordinatePH);
                }
            }
            
            foreach(string location in busCoordinateList)
            {
                int charCount = location.IndexOf(",");
                string lat = location.Substring(0, charCount);
                string lon = location.Substring(charCount + 1);
                Coordinate insert = new Coordinate() { Latitude = lat, Longitude = lon };
                coordinateList.Add(insert);
            }
            return coordinateList;         
        }
    }

//JSON Object
    public class BusRoute
    {
        public Value[] value { get; set; }
    }

    public class Value
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTime Timestamp { get; set; }
        public string ServiceNo { get; set; }
        public string Operator { get; set; }
        public int Direction { get; set; }
        public int StopSequence { get; set; }
        public string BusStopCode { get; set; }
        public float Distance { get; set; }
        public string WD_FirstBus { get; set; }
        public string WD_LastBus { get; set; }
        public string SAT_FirstBus { get; set; }
        public string SAT_LastBus { get; set; }
        public string SUN_FirstBus { get; set; }
        public string SUN_LastBus { get; set; }
    }

    public class BusArrival
    {
        public string odatametadata { get; set; }
        public string BusStopCode { get; set; }
        public Service[] Services { get; set; }
    }

    public class Service
    {
        public string ServiceNo { get; set; }
        public string Operator { get; set; }
        public Nextbus NextBus { get; set; }
        public Nextbus2 NextBus2 { get; set; }
        public Nextbus3 NextBus3 { get; set; }
    }

    public class Nextbus
    {
        public string OriginCode { get; set; }
        public string DestinationCode { get; set; }
        public DateTime? EstimatedArrival { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string VisitNumber { get; set; }
        public string Load { get; set; }
        public string Feature { get; set; }
        public string Type { get; set; }
    }

    public class Nextbus2
    {
        public string OriginCode { get; set; }
        public string DestinationCode { get; set; }
        public DateTime? EstimatedArrival { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string VisitNumber { get; set; }
        public string Load { get; set; }
        public string Feature { get; set; }
        public string Type { get; set; }
    }

    public class Nextbus3
    {
        public string OriginCode { get; set; }
        public string DestinationCode { get; set; }
        public DateTime? EstimatedArrival { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string VisitNumber { get; set; }
        public string Load { get; set; }
        public string Feature { get; set; }
        public string Type { get; set; }
    }

    public class CoordinateObject
    {
        public string ServiceNo {get; set;}
        public int Direction {get; set; }
        public List<Coordinate> BusCoordinate {get; set;}
    }

    public class Coordinate
    {
        public string Latitude { get; set; }
        public string Longitude { get; set; }
    }
