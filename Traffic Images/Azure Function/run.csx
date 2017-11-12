#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"

using System;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

public class GetTrafficData
{
    public static async Task<imageObj> GetTrafficImage()
    {
        var http = new HttpClient();
        http.DefaultRequestHeaders.Add("AccountKey","<API Key>");
        string requestURL = "http://datamall2.mytransport.sg/ltaodataservice/Traffic-Images";
        var response = await http.GetAsync(requestURL);
        var result = await response.Content.ReadAsStringAsync();
        imageObj imageJSON = JsonConvert.DeserializeObject<imageObj>(result);
        return imageJSON;
    }
}

public static async Task<int> Run(TimerInfo myTimer, Binder binder, TraceWriter log)
{
    log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
    imageObj imageList = await GetTrafficData.GetTrafficImage();
    int count = imageList.value.Count;

    for(int i = 0; i < count; i++)
    {
        string cameraID = imageList.value[i].CameraID;
        string path = $"trafficimage/{cameraID}/{DateTime.Now.ToString("G")}.jpg";
        string imageURL = imageList.value[i].ImageLink;

        var attributes = new Attribute[]
        {    
            new BlobAttribute(path, FileAccess.Write),
            new StorageAccountAttribute("<storage account name>")
        };

        using (var writer = await binder.BindAsync<Stream>(attributes))
        {
            WebClient wc = new WebClient();
            MemoryStream stream = new MemoryStream(wc.DownloadData(imageURL));
            var byteArray = stream.ToArray();
            writer.Write(byteArray, 0, byteArray.Length);
        }
    }
    
    return 0;
}

public class Value
{
    public string CameraID { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string ImageLink { get; set; }
}

public class imageObj
{
    public string odatametadata { get; set; }
    public List<Value> value { get; set; }
}
