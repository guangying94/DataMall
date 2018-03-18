# Road Traffic Images

There are 2 section in this project, which is 
1. Traffic Images visualization on Power BI
1. Custom Vision on Traffic Images

## [1] Traffic Images Visualization on Power BI
Please refer to this [**link**](https://www.youtube.com/watch?v=ZzZ4Q9QEFaA) for the full tutorial.

## [2.1] Custom Vision on Traffic Images
I'm leveraging Microsoft Cognitive Services - [**Custom Vision**](https://www.customvision.ai/) for this section. Essentially, the main idea is to train a model that can recognize the traffic condition, such as light traffic or heavy traffic. Then we can leverage on apps or bots to monitor the situation. In this case, we will be using bots.

Before we upload and tag the images, I have written an Azure Function to store all traffic images in Blob Storage. Refer to the _Azure Function_ folder above.

Once you have collected the images, you can start the training. Login to [**Custom Vision**](https://www.customvision.ai/), create new a new project, and upload the images. For each image, place the tag, in this case, I put "light" or "heavy".

![Custom Vision](https://6wegtq.dm.files.1drv.com/y4mukjwvJt0jsu-l8YiHz9iuDbl8I9qj5MA9ATFtNPA-dBXYXkDW_HWdS6bbhozp9I3hnVD8QnUpt1P-FQlyDrTfH47cM2V1Q00hQcYvzkBACAaYYHn2CnXYvRILRFdae4LnZgXitar7GVdA4Gs6OukuiGRJQhHzsOD01ueLNpnBMqaTWxvFr-GrXRQVrG7G2i3WyPq90sSf1N6zq35F6V64A?width=1024&height=555&cropmode=none)

Once you have trained the service, you can obtain the URL under *PERFORMANCE* tab, _Prediction URL_. Then, navigate to project settings (gear icon), and copy the *Prediction Key*, this will be your API key.

## [2.2] Integrate Custom Vision into Bots

First, define the JSON object group. Then, create a function to call the custom vision services that you have trained previously.

```json
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
```
Replace the URL and API key from the portal that you have obtained previously. The input of this function is imageURL, which you can obtain from the traffic images URL. Simply just parse it into this function and you will get the predicted result.

```csharp
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
            // You can add in confidence score if needed. For simplicity, I just return the tag
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
```

For this project, the bot is able to send out proactive messages, i.e. the bot will send message to user when it detects a condition. You can refer to the documentation over here: [**Send Proactive Messages**](https://docs.microsoft.com/en-us/bot-framework/dotnet/bot-builder-dotnet-proactive-messages). In order to achieve that, first you need to collect metadata of this conversation, here, I created a table storage to store the metadata, and I invoke Azure function to trigger the storing process, you can refer the code in _identityStore_ folder.

In your main dialog, create a function to send necessary metadata to this Azure function to process.

```csharp
private async Task insertDataAsync(IDialogContext context, IMessageActivity message)
{

    string functionURL = "<Azure Function URL>";
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
        byte[] byteData = Encoding.UTF8.GetBytes("{\"name\": \"<Name>\"," +
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
```
To send proactive messages, simply retrieve the information from Table Storage, and create a message.

```csharp
public static async Task Resume(string name, string adhocMessage)
{
    // retrieveID is a function to retrieve information from Table Storage
    IdentityObject identityJSON = await retrieveID(name);

    string fromId = identityJSON.value[0].fromId;
    string fromName = identityJSON.value[0].fromName;
    string toId = identityJSON.value[0].toId;
    string toName = identityJSON.value[0].toName;
    string serviceUrl = identityJSON.value[0].serviceUrl;
    string channelId = identityJSON.value[0].channelId;
    string conversationId = identityJSON.value[0].conversationId;

    var userAccount = new ChannelAccount(toId, toName);
    var botAccount = new ChannelAccount(fromId, fromName);
    var connector = new ConnectorClient(new Uri(serviceUrl));

    //create a message activity here
    IMessageActivity message = Activity.CreateMessageActivity();
    if (!string.IsNullOrEmpty(conversationId) && !string.IsNullOrEmpty(channelId))
    {
        message.ChannelId = channelId;
    }
    else
    {
        conversationId = (await connector.Conversations.CreateDirectConversationAsync(botAccount, userAccount)).Id;
    }
    message.From = botAccount;
    message.Recipient = userAccount;
    message.Conversation = new ConversationAccount(id: conversationId);
    message.Text = adhocMessage;
    message.Locale = "en-Us";
    await connector.Conversations.SendToConversationAsync((Activity)message);
}
```

Here's a sample conversation:
![Conversation](https://tp1qeg.dm.files.1drv.com/y4mAMPx7E5Yws4Wb-itaPTHVIvgWzSY0MwUhE5di7Oxod2vj6xBU1Axgey49tiI8AELRrraTtXwr_n4FcrZBfLM7bzKTZG0MWlLfDjbJljs8L8qFTfz3WZR_8N9BVcCXwzPEW8OvymsjxaIe1Bq0NWRl_ZLWdZCNsgVuIYAndVXix_AyQXWX4RBq_41ELlprmxbx2tCpgIkOGiI8pUVI8P3KA?width=810&height=4188&cropmode=none)