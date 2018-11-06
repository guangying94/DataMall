using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

namespace datamall_bot.Dialogs
{
    [Serializable]
    public class UserLocationOneMapDialog : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        //this is the dialog that resposne to users whether they can fly drone
        //one important thing here is the capability to retrieve the town name using OneMap API
        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as IMessageActivity;
            if(activity.Text.Length == 6)
            {
                addressObj locationJSON = await otherOperation.GetAddress(activity.Text);
                string lat = locationJSON.results[0].LATITUDE;
                string lon = locationJSON.results[0].LONGTITUDE;
                bool flyDrone = await DroneOperation.validateDroneFlyingZone(lat, lon);
                if (flyDrone)
                {
                    await context.PostAsync("Yes, you can fly drone here.");
                }
                else
                {
                    await context.PostAsync("Sorry, you are in restricted zone. However, we can guide you if a permit is required to fly drone at this location. Let me know if you need guidance here!");
                }
                string weatherTips = await otherOperation.getNearestWeatherForecast(lat, lon);
                await context.PostAsync(weatherTips);
                context.Done(locationJSON);
            }
            else
            {
                var askText = new PromptOptions<string>("Where are you? You can search by address or postal code", retry: "Where are you?");
                var prompt = new PromptDialog.PromptString(askText);
                context.Call<string>(prompt, afterLocationAsync);
            }          
        }

        //this is to check if the location is available / exist in Singapore
        private async Task afterLocationAsync(IDialogContext context, IAwaitable<string> arguement)
        {
            string searchTerm = await arguement;
            addressObj JSON = await otherOperation.GetAddress(searchTerm);

            if(JSON.found < 1)
            {
                await context.PostAsync("Sorry, this place is not found.");
            }
            else
            {
                await context.PostAsync("Which one?");
                var replyToConversation = context.MakeMessage();
                replyToConversation.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                replyToConversation.Attachments = new List<Attachment>();

                string imageUrl = "";

                int resultCount = JSON.found;

                for(int i = 0; i < resultCount; i++)
                {
                    imageUrl = otherOperation.generateMapURL(JSON.results[i].LATITUDE, JSON.results[i].LONGTITUDE);
                    List<CardImage> cardImages = new List<CardImage>();
                    cardImages.Add(new CardImage(url: imageUrl));

                    List<CardAction> cardButtons = new List<CardAction>();

                    CardAction plButton = new CardAction()
                    {
                        Value = $"{JSON.results[i].POSTAL}",
                        Type = "postBack",
                        Title = "Select"
                    };

                    cardButtons.Add(plButton);

                    HeroCard plCard = new HeroCard()
                    {
                        Title = JSON.results[i].BUILDING,
                        Subtitle = JSON.results[i].ADDRESS,
                        Images = cardImages,
                        Buttons = cardButtons
                    };

                    Attachment plAttachment = plCard.ToAttachment();
                    replyToConversation.Attachments.Add(plAttachment);
                }

                await context.PostAsync(replyToConversation);
            }
        }
    }
}