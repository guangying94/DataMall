using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;

namespace datamall_bot.Dialogs
{
    [Serializable]
    public class UserFormDialog : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var activity = await result as IMessageActivity;
            if(activity.Value != null)
            {
                string responseText = activity.Value.ToString();
                formResponseObj JSON = JsonConvert.DeserializeObject<formResponseObj>(responseText);

                string userIndoorOutdoor = JSON.indoorOutdoor;
                string userOperationPurpose = JSON.operationPurpose;
                string userTotalMass = JSON.totalMass;
                string userFlightHeight = JSON.flightHeight;
                string userRestrictedZone = JSON.restrictedZone;
                
                if(checkUserMissingInput(userIndoorOutdoor, userOperationPurpose, userTotalMass,userFlightHeight,userRestrictedZone))
                {
                    await context.PostAsync("Sorry, you have missing value.");

                    AdaptiveCard card = MainDialog.generateFormAdaptiveCards();
                    Attachment attachment = new Attachment()
                    {
                        ContentType = AdaptiveCard.ContentType,
                        Content = card
                    };
                    var botResponse = context.MakeMessage();
                    botResponse.Attachments.Add(attachment);
                    await context.PostAsync(botResponse);
                }
                else
                {
                    //if users have provided all input
                    //then we run Azure Machine Learning to find out which permit to apply
                    string permitResponse = await DroneOperation.CheckPermitType(userIndoorOutdoor, userOperationPurpose, userTotalMass, userFlightHeight, userRestrictedZone);
                    await context.PostAsync("Here's what I suggested: " + permitResponse);
                    var reply = context.MakeMessage();
                    reply.Attachments.Add(new HeroCard
                    {
                        Title = permitResponse,
                        Buttons = new List<CardAction> {
                            new CardAction
                            {
                                Title = "Permit Application",
                                Type = ActionTypes.OpenUrl,
                                Value = "https://www.caas.gov.sg/public-passengers/unmanned-aircraft-systems/permit-application"
                            }
                        }
                    }.ToAttachment());
                    await context.PostAsync(reply);
                    context.Done(JSON);
                }
                
            }
            else
            {
                AdaptiveCard card = MainDialog.generateFormAdaptiveCards();
                Attachment attachment = new Attachment()
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = card
                };
                var botResponse = context.MakeMessage();
                botResponse.Attachments.Add(attachment);
                await context.PostAsync(botResponse);
            }
        }

        //check if there's any missing input
        private bool checkUserMissingInput(string indoorOutdoor, string operationPurpose, string totalMass, string flightHeight, string restrictedZone)
        {
            bool indoorOutdoorCheck = (indoorOutdoor.Length < 2);
            bool operationPurposeCheck = (operationPurpose.Length < 2);
            bool totalMassCheck = (totalMass.Length < 2);
            bool flightHeightCheck = (flightHeight.Length < 2);
            bool restrictedZoneCheck = (restrictedZone.Length < 2);

            return indoorOutdoorCheck && operationPurposeCheck && totalMassCheck && flightHeightCheck && restrictedZoneCheck;
        }
    }

    public class formResponseObj
    {
        public string Type { get; set; }
        public string indoorOutdoor { get; set; }
        public string operationPurpose { get; set; }
        public string totalMass { get; set; }
        public string flightHeight { get; set; }
        public string restrictedZone { get; set; }
    }
}


