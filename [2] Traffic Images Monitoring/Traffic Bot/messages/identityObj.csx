using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;

public class IdentityObject
{
    public Value[] value { get; set; }
}

public class Value
{
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTime Timestamp { get; set; }
    public string Name { get; set; }
    public string channelId { get; set; }
    public string conversationId { get; set; }
    public string fromId { get; set; }
    public string fromName { get; set; }
    public string serviceUrl { get; set; }
    public string toId { get; set; }
    public string toName { get; set; }
}