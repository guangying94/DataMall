#r "Microsoft.WindowsAzure.Storage"

using System.Net;
using Microsoft.WindowsAzure.Storage.Table;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ICollector<Identity> outTable, TraceWriter log)
{
    dynamic data = await req.Content.ReadAsAsync<object>();
    string name = data?.name;
    string channelid = data?.channelid;
    string conversationid = data?.conversationid;
    string fromid = data?.fromid;
    string fromname = data?.fromname;
    string serviceurl = data?.serviceurl;
    string toid = data?.toid;
    string toname = data?.toname;

    outTable.Add(new Identity()
    {
        PartitionKey = "Functions",
        RowKey = Guid.NewGuid().ToString(),
        Name = name,
        channelId = channelid,
        conversationId = conversationid,
        fromId = fromid,
        fromName = fromname,
        serviceUrl = serviceurl,
        toId = toid,
        toName = toname
    });
    return req.CreateResponse(HttpStatusCode.Created);
}

public class Identity : TableEntity
{
    public string Name { get; set; }
    public string channelId {get;set;}
    public string conversationId {get;set;}
    public string fromId {get;set;}
    public string fromName {get;set;}
    public string serviceUrl {get;set;}
    public string toId {get;set;}
    public string toName {get;set;}
}
