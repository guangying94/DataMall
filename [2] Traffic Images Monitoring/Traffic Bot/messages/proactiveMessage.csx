#load "identityObj.csx"

using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

public class ProactiveMessage
    {
        public static async Task Resume(string name, string adhocMessage)
        {
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

        public static async Task<IdentityObject> retrieveID(string name)
        {
            var http = new HttpClient();
            http.DefaultRequestHeaders.Add("Accept", "application/json;odata=nometadata");
            string requestURL = "<Table Storage URL>";
            var response = await http.GetAsync(requestURL);
            var result = await response.Content.ReadAsStringAsync();
            IdentityObject identityJSON = JsonConvert.DeserializeObject<IdentityObject>(result);
            return identityJSON;
        }

    }