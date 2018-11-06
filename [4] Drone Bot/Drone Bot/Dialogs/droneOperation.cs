using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using Microsoft.Azure.Documents.Spatial;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace datamall_bot.Dialogs
{
    public class DroneOperation
    {
        // connection data for Azure Cosmos DB
        private const string EndpointUrl = "https://{cosmos db name}.documents.azure.com:443/";
        private const string PrimaryKey = "{Cosmos DB Key}";

        //connection data for Custom vision
        private const string customVisionKey = "{custom vision api key}";
        private const string customVisionUrl = "{custom vision url}";


        //this is to validate if the location is a flying zone
        //do check on 2 criterias
        //first one is whether the coordinate is in a restricted zone
        //second check is whether the coordinate is within 5km of restricted location
        public static async Task<bool> validateDroneFlyingZone(string lat, string lon)
        {
            bool zone = CheckFlyingZone(lat, lon);
            bool point = CheckFlyingPoints(lat, lon);
            await sendToPowerBIAsync(lat, lon, (zone&&point).ToString());
            return zone && point;
        }

        //this is to check if the coordinate is within a zone
        public static bool CheckFlyingZone(string lat, string lon)
        {
            string SQLQuery = "SELECT c.Name,c.RestrictionType FROM c WHERE ST_WITHIN({'type': 'Point', 'coordinates':[" + lon + ", " + lat + "]}, c.location) AND c.RestrictionType = 'Protected Area'";
            DocumentClient client = new DocumentClient(new Uri(EndpointUrl), PrimaryKey);
            IQueryable<RestrictionZone> restrictionQueryInSql = client.CreateDocumentQuery<RestrictionZone>(
                UriFactory.CreateDocumentCollectionUri("GeospatialDB", "GeospatialCollection"),
                SQLQuery, new FeedOptions { EnableScanInQuery = true });

            int count = 0;

            foreach (RestrictionZone zone in restrictionQueryInSql)
            {
                count++;
            }

            if (count == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //this is to check if the coordinate is within 5km of a restricted location
        public static bool CheckFlyingPoints(string lat, string lon)
        {
            string SQLQuery = "SELECT c.Name FROM c WHERE ST_DISTANCE(c.location, {'type': 'Point', 'coordinates':[" + lon + ", " + lat + "]}) < 5000 AND c.RestrictionType = 'Airbase'";
            DocumentClient client = new DocumentClient(new Uri(EndpointUrl), PrimaryKey);
            IQueryable<RestrictionPoint> restrictionQueryInSql = client.CreateDocumentQuery<RestrictionPoint>(
                UriFactory.CreateDocumentCollectionUri("GeospatialDB", "GeospatialCollection"),
                SQLQuery, new FeedOptions { EnableScanInQuery = true });

            int count = 0;

            foreach (RestrictionPoint zone in restrictionQueryInSql)
            {
                count = count++;
            }

            if (count == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //call custom vision api to detect the type
        public static async Task<string> GetDroneType(string imageURL)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(customVisionUrl);
                client.DefaultRequestHeaders.Add("Prediction-Key", customVisionKey);
                byte[] byteData = Encoding.UTF8.GetBytes("{\"Url\":\"" + imageURL + "\"}");
                string queryString = "";
                var response = await CallCustomVisionEndPoint(client, queryString, byteData);
                string predictedTag = response.predictions[0].tagName;
                if (response.predictions[0].probability < 0.5)
                {
                    return "Unknown";
                }
                else
                {
                    return predictedTag;
                }
            }
        }

        //response the caption if drone is not detected
        public static async Task<string> getImageCaption(string imageURL)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "<Computer vision API Key>");
            var uri = "<computer vision api url>";
            HttpResponseMessage response;
            byte[] byteData = Encoding.UTF8.GetBytes("{\"url\":\"" + imageURL + "\"}");

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                response = await client.PostAsync(uri, content);
                var result = await response.Content.ReadAsStringAsync();
                computerVisionObj ImageJSON = JsonConvert.DeserializeObject<computerVisionObj>(result);
                return ImageJSON.description.captions[0].text;
            }
        }

        //call custom vision endpoint
        public static async Task<customObj> CallCustomVisionEndPoint(HttpClient client, string uri, byte[] byteData)
        {
            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                var response = await client.PostAsync(uri, content);
                var result = await response.Content.ReadAsStringAsync();
                customObj customJSON = JsonConvert.DeserializeObject<customObj>(result);
                return customJSON;
            }
        }

        //send the user location to Power BI
        //this is to find out the high demand area
        public static async Task sendToPowerBIAsync(string lat, string lon, string type)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("{Power BI streaming dataset endpoint}");
                byte[] byteData = Encoding.UTF8.GetBytes("[{\"Lat\": \"" + lat + "\",\"Lon\": \"" + lon + "\",\"Type\": \"" + type + "\"}]");
                string queryString = "";
                var content = new ByteArrayContent(byteData);
                var response = await client.PostAsync(queryString, content);
            }
        }

        //check the permit needed base on users input
        //this is using Azure Machine Learning studio to create the endpoint
        public static async Task<string> CheckPermitType(string indoorOutdoor, string operationPurpose, string totalMass, string flightHeight, string restrictedZone)
        {
            using (var client = new HttpClient())
            {
                var scoreRequest = new
                {

                    Inputs = new Dictionary<string, StringTable>() {
                        {
                            "input1",
                            new StringTable()
                            {
                                ColumnNames = new string[] {"Indoor Or Outdoor", "Operation Purpose (Recretional/Research/NA)", "Total Mass More Than 7kg", "Flight Height More Than 60m", "In Restricted Zone", "Permit"},
                                Values = new string[,] {  {indoorOutdoor, operationPurpose, totalMass, flightHeight, restrictedZone, "" },}
                            }
                        },
                    },
                    GlobalParameters = new Dictionary<string, string>()
                    {
                    }
                };
                const string apiKey = "{Azure Machine Learning Studio API Key}";
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                client.BaseAddress = new Uri("{Azure Machine Learning Studio Endpoint}");

                HttpResponseMessage response = await client.PostAsJsonAsync("", scoreRequest);

                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    AMLSObject JSON = JsonConvert.DeserializeObject<AMLSObject>(result);
                    string predictedResponse = JSON.Results.output1.value.Values[0][3];
                    List<double> scoringList = new List<double>();
                    for (int i = 0; i < 3; i++)
                    {
                        scoringList.Add(Convert.ToDouble(JSON.Results.output1.value.Values[0][i]));
                    }

                    double maxScoring = scoringList.Max();
                    if (maxScoring > 0.6)
                    {
                        return predictedResponse;
                    }
                    else
                    {
                        return "Low confidence on " + predictedResponse;
                    }
                }
                else
                {
                    return "Sorry, I can't contact the web service now.";
                }
            }
        }
    }
}

public class RestrictionZone
{
    [JsonProperty("id")]
    public string Id { get; set; }
    public string Name { get; set; }
    public string RestrictionType { get; set; }

    [JsonProperty("location")]
    public Polygon Location { get; set; }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }
}

public class RestrictionPoint
{
    [JsonProperty("id")]
    public string Id { get; set; }
    public string Name { get; set; }
    public string RestrictionType { get; set; }

    [JsonProperty("location")]
    public Point Location { get; set; }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }
}

public class customObj
{
    public string id { get; set; }
    public string project { get; set; }
    public string iteration { get; set; }
    public DateTime created { get; set; }
    public Prediction[] predictions { get; set; }
}

public class Prediction
{
    public float probability { get; set; }
    public string tagId { get; set; }
    public string tagName { get; set; }
}

public class StringTable
{
    public string[] ColumnNames { get; set; }
    public string[,] Values { get; set; }
}

public class AMLSObject
{
    public Results Results { get; set; }
}

public class Results
{
    public Output1 output1 { get; set; }
}

public class Output1
{
    public string type { get; set; }
    public Value value { get; set; }
}

public class Value
{
    public string[] ColumnNames { get; set; }
    public string[] ColumnTypes { get; set; }
    public string[][] Values { get; set; }
}

public class Tag
{
    public string name { get; set; }
    public double confidence { get; set; }

}

public class Caption
{
    public string text { get; set; }
    public double confidence { get; set; }
}

public class Description
{
    public List<string> tags { get; set; }
    public List<Caption> captions { get; set; }
}

public class Metadata
{
    public int width { get; set; }
    public int height { get; set; }
    public string format { get; set; }
}

public class computerVisionObj
{
    public List<Tag> tags { get; set; }
    public Description description { get; set; }
    public string requestId { get; set; }
    public Metadata metadata { get; set; }
}