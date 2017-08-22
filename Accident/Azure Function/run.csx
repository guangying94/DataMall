#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"

using System;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

public class Value
{
    public string Type { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Message { get; set; }
}

public class Example
{
    public string odatametadata { get; set; }
    public IList<Value> value { get; set; }
}

public class Record : TableEntity
{
    public string Type {get; set;}
    public double Latitude {get; set;}
    public double Longitude {get; set;}
    public string Message {get; set;}
}


public class GetAccidentData
{
    public static async Task<Example> GetAccidentMessage()

    {
        var http = new HttpClient();
        http.DefaultRequestHeaders.Add("AccountKey","<Your api key>");
        string requestURL = "http://datamall2.mytransport.sg/ltaodataservice/TrafficIncidents";
        var response = await http.GetAsync(requestURL);
        var result = await response.Content.ReadAsStringAsync();
        Example accidentJSON = JsonConvert.DeserializeObject<Example>(result);
        return accidentJSON;
    }
}

public static async Task<int> Run(TimerInfo myTimer, ICollector<Record> outTable, TraceWriter log)
{
    log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
    Example accJSON = await GetAccidentData.GetAccidentMessage();

    int count = accJSON.value.Count();

    for(int i = 0; i < count; i++)
    {
        outTable.Add(new Record()
        {
            PartitionKey = "Functions",
            RowKey = Guid.NewGuid().ToString(),
            Type = accJSON.value[i].Type,
            Latitude = accJSON.value[i].Latitude,
            Longitude = accJSON.value[i].Longitude,
            Message = accJSON.value[i].Message
        });
    }

    return 0;
}
