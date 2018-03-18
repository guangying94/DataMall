 #load "proactiveMessage.csx"

using System;
using System.Threading.Tasks;

using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using System.Net;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Bot.Connector;
using System.IO;
using System.Globalization;
using System.Web;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.Bot.Builder.ConnectorEx;

// JSON object for camera images
public class imageObj
{
    public string odatametadata { get; set; }
    public Value[] value { get; set; }
}

public class Value
{
    public string CameraID { get; set; }
    public float Latitude { get; set; }
    public float Longitude { get; set; }
    public string ImageLink { get; set; }
}

// function to get traffic images from LTA data mall api
public class GetTrafficImages
{
    public async static Task<imageObj> GetImage()
    {
        var http = new HttpClient();
        //replace the "api key" with your api key
        http.DefaultRequestHeaders.Add("AccountKey", "<LTA Data Mall Key>");
        var response = await http.GetAsync("http://datamall2.mytransport.sg/ltaodataservice/Traffic-Images");
        var result = await response.Content.ReadAsStringAsync();
        imageObj TrafficImages = JsonConvert.DeserializeObject<imageObj>(result);
        return TrafficImages;
    }

    public static int getCameraID(string cameraID, imageObj trafficJSON)
    {
        int cameraNo = 0;
        int maxCameraNo = trafficJSON.value.Count() - 1;
        while(cameraID != trafficJSON.value[cameraNo].CameraID)
        {
            if (cameraNo == maxCameraNo)
            {
                cameraNo = 999;
                break;
            }
            else
            {
                cameraNo += 1;
            }
        }
        return cameraNo;
    }
}


public class customObj
{
    public string Id { get; set; }
    public string Project { get; set; }
    public string Iteration { get; set; }
    public DateTime Created { get; set; }
    public Prediction[] Predictions { get; set; }
}

public class Prediction
{
    public string TagId { get; set; }
    public string Tag { get; set; }
    public float Probability { get; set; }
}

public class customImage
{
    private const string uri = "<Custom Vision API URL>";
    private const string predictionKey = "<Custom Vision API Key>";

    public static async Task<string> getResult(string imageURL)
    {
        using (var client = new HttpClient())
        {
            client.BaseAddress = new Uri(uri);
            client.DefaultRequestHeaders.Add("Prediction-Key", predictionKey);
            byte[] byteData = Encoding.UTF8.GetBytes("{\"Url\":\"" + imageURL + "\"}");
            var uri2 = "";
            var response = await CallEndPoint(client, uri2, byteData);
            return response.Predictions[0].Tag;
        }
    }

    public static async Task<customObj> CallEndPoint(HttpClient client, string uri, byte[] byteData)
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
}

public class Message
{
    public ConversationReference RelatesTo { get; set; }
    public String Text { get; set; }
}

[Serializable]
public class BasicLuisDialog : LuisDialog<object>
{
    public BasicLuisDialog() : base(new LuisService(new LuisModelAttribute(Utils.GetAppSetting("LuisAppId"), Utils.GetAppSetting("LuisAPIKey"))))
    {
    }

    [LuisIntent("None")]
    public async Task NoneIntent(IDialogContext context, LuisResult result)
    {
        await context.PostAsync("Good day, HUMAN. Stop asking me nonsense, I've been watching the traffic all day long, chop chop!");
        context.Wait(MessageReceived);
    }

    [LuisIntent("registerAlert")]
    public async Task registerIntent(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
    {
        var activity2 = await activity;
        await insertDataAsync(context, activity2);
        await context.PostAsync("Alright!");
        context.Wait(MessageReceived);
    }

    [LuisIntent("checkCondition")]
    public async Task checkCondition(IDialogContext context, LuisResult result)
    {
        imageObj Traffic = await GetTrafficImages.GetImage();
        string cameraID = string.Empty;
        //cameraID = result.Entities[0].Entity;
        if(result.Entities.Count() < 1)
        {
            cameraID = "2701";
        }
        else
        {
            cameraID = result.Entities[0].Entity;
        }
        int cameraNo = GetTrafficImages.getCameraID(cameraID, Traffic);
        context.ConversationData.SetValue<string>("LastCamera", cameraID);
        string cameraImageURL = Traffic.value[cameraNo].ImageLink;

        string condition = await customImage.getResult(cameraImageURL);
        await context.PostAsync($"Camera {cameraID} is detecting {condition} traffic condition. Luckily I lives in internet.");
        context.Wait(MessageReceived);
    }

    [LuisIntent("informCondition")]
    public async Task informCondition(IDialogContext context, LuisResult result)
    {
        var queueMessage = new Message
        {
            RelatesTo = context.Activity.ToConversationReference(),
            Text = "trigger"
        };

        await AddMessageToQueue(JsonConvert.SerializeObject(queueMessage));
        await context.PostAsync("Walao, still need report... I rest 1 min and tell you...");
        context.Wait(MessageReceived);
    }

    [LuisIntent("showImage")]
    public async Task showImage(IDialogContext context, LuisResult result)
    {
        imageObj Traffic = await GetTrafficImages.GetImage();
        string cameraID = string.Empty;
        string lastCameraLocation = string.Empty;
        if(result.Entities.Count() < 1)
        {
            if (!context.ConversationData.TryGetValue("LastCamera", out lastCameraLocation))
            {
                //await context.PostAsync("Please tell me a cameraID!");
                cameraID = string.Empty;
            }
            else
            {
                cameraID = lastCameraLocation;
            }
        }
        else
        {
            cameraID = result.Entities[0].Entity;
        }

        if(cameraID != string.Empty)
        {
            context.ConversationData.SetValue<string>("LastCamera", cameraID);

            int cameraNo = GetTrafficImages.getCameraID(cameraID, Traffic);
            if (cameraNo > 500)
            {
                await context.PostAsync($"Camera {cameraID} is not found!");
                context.Wait(MessageReceived);
            }
            else
            {
                string cameraImageURL = Traffic.value[cameraNo].ImageLink;
                var replyTraffic = context.MakeMessage();
                replyTraffic.Attachments = new List<Attachment>()
            {
                new Attachment()
                {
                    ContentUrl = cameraImageURL,
                    ContentType = "image/png",
                    Name = "Camera.png"
                }
            };
                await context.PostAsync(replyTraffic);
                context.ConversationData.SetValue<int>("LastCameraNo", cameraNo);
                context.Wait(MessageReceived);
            }
        }
        else
        {
            await context.PostAsync("Please tell me a camera ID!");
            context.Wait(MessageReceived);
        }
    }

    [LuisIntent("cameraLocation")]
    public async Task cameraLocation(IDialogContext context, LuisResult result)
    {
        string response = "";
        string lastCameraLocation = string.Empty;
        // if no value
        if(!context.ConversationData.TryGetValue("LastCamera", out lastCameraLocation))
        {
            response = "I can't recall any camera ID you told me...";
        }
        else
        {
            imageObj Traffic = await GetTrafficImages.GetImage();
            int cameraNo = GetTrafficImages.getCameraID(lastCameraLocation, Traffic);
            string cameraLat = Traffic.value[cameraNo].Latitude.ToString();
            string cameraLong = Traffic.value[cameraNo].Longitude.ToString();
            string bingMapKey = "<Bing Map API key>";
            string mapURL = "http://dev.virtualearth.net/REST/v1/Imagery/Map/Road/" + cameraLat + "," + cameraLong + "/14?mapSize=280,140&pp=" + cameraLat + "," + cameraLong + "&key=" + bingMapKey;
            var replyTrafficMap = context.MakeMessage();
            replyTrafficMap.Attachments = new List<Attachment>()
            {
                new Attachment()
                {
                    ContentUrl = mapURL,
                    ContentType = "image/png",
                    Name = "map.png"
                }
            };
            await context.PostAsync(replyTrafficMap);

            response = $"Find the location of camera {lastCameraLocation} in map yourself.";
            await context.PostAsync(response);
        }
        context.Wait(MessageReceived);
    }

    public static async Task AddMessageToQueue(string message)
    {
        var storageAccount = CloudStorageAccount.Parse(Utils.GetAppSetting("AzureWebJobsStorage"));
        var queueClient = storageAccount.CreateCloudQueueClient();
        var queue = queueClient.GetQueueReference("bot-queue");

        await queue.CreateIfNotExistsAsync();

        var queueMessage = new CloudQueueMessage(message);
        await queue.AddMessageAsync(queueMessage);
    }

    private async Task insertDataAsync(IDialogContext context, IMessageActivity message)
        {

            string functionURL = "<Table Storage URL>";
            string toId = message.From.Id;
            string toName = message.From.Name;
            string fromId = message.Recipient.Id;
            string fromName = message.Recipient.Name;
            string serviceUrl = message.ServiceUrl;
            string channelId = message.ChannelId;
            string conversationId = message.Conversation.Id;

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(functionURL);
                byte[] byteData = Encoding.UTF8.GetBytes("{\"name\": \"Name\"," +
                    "\"channelid\": \"" + channelId + "\"," +
                    "\"conversationid\":\"" + conversationId + "\"," +
                    "\"fromid\":\"" + fromId + "\"," +
                    "\"fromname\": \"" + fromName + "\"," +
                    "\"serviceurl\":\"" + serviceUrl + "\"," +
                    "\"toid\":\"" + toId + "\"," +
                    "\"toname\":\"" + toName + "\"}");
                var itemContent = new ByteArrayContent(byteData);
                itemContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                var response = await client.PostAsync(functionURL, itemContent);
            }
        }
}