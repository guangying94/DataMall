using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.ConnectorEx;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

namespace datamall_bot.Dialogs
{
    [Serializable]
    public class UserLocationDialog : IDialog<Place>
    {
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        //get location from users
        //this is from messenger
        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as IMessageActivity;
            var reply = context.MakeMessage();
            reply.ChannelData = new FacebookMessage
                (
                    text: "Where are you?",
                    quickReplies: new List<FacebookQuickReply>
                    {
                        new FacebookQuickReply(
                            contentType: FacebookQuickReply.ContentTypes.Location,
                            title: default(string),
                            payload: default(string)
                        )
                    }
                );
            await context.PostAsync(reply);
            context.Wait(LocationReceivedAsync);
        }

        public virtual async Task LocationReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> arguement)
        {
            var msg = await arguement;
            var location = msg.Entities?.Where(t => t.Type == "Place").Select(t => t.GetAs<Place>()).FirstOrDefault();
            context.Done(location);
        }
    }
}