using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using AdaptiveCards;
using Microsoft.Bot.Builder.Location;
using System.Web.Configuration;
using System.Threading;
using Newtonsoft.Json.Linq;
using Microsoft.Bot.Builder.FormFlow;

namespace datamall_bot.Dialogs
{
    [LuisModel("{LUIS ID}", "{LUIS Key}")]
    [Serializable]
    public class MainDialog : LuisDialog<object>
    {
        [LuisIntent("None")]
        public async Task NoneIntent(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            await context.SayAsync("Sorry, I don't understand");
            context.Wait(MessageReceived);
        }

        [LuisIntent("Greeting")]
        public async Task GreetingIntent(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            if(result.Query.ToLower() == "what can you do")
            {
                await context.PostAsync("Here's what I can help:");
                await context.PostAsync("I can help you determine if your area is drone flying zone by using GPS.");
                await context.PostAsync("Also, if needed, I can guide you on drone permit application.");
                await context.PostAsync("I'm also learning to determine if your drone need permit to fly.");
            }
            else
            {
                await context.SayAsync("Hi! I'm drone assistant bot. I can help you to decide if you can fly drone in your area. You can start with asking me \"What can you do?\"");
            }
            
            context.Wait(MessageReceived);
        }

        //check coordinate if that is a flying zone
        [LuisIntent("checkDroneFlyingZone")]
        public async Task checkDroneFlyingZoneIntent(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            var act = await activity;
            //this supports 2 channels, cortana and facebook
            //for cortana, it can access the user location directly
            //for facebook messenger, it will ask additional questions and get user to send location
            if (act.ChannelId == "cortana")
            {
                if (act.Entities != null)
                {
                    var userInfo = act.Entities.FirstOrDefault(e => e.Type.Equals("UserInfo"));
                    if (userInfo != null)
                    {

                        var currentLocation = userInfo.Properties["current_location"];
                        if (currentLocation != null)
                        {
                            var hub = currentLocation["Hub"];
                            var lat = hub.Value<double>("Latitude").ToString();
                            var lon = hub.Value<double>("Longitude").ToString();
                            bool flyDrone = await DroneOperation.validateDroneFlyingZone(lat, lon);
                            var botResponse = context.MakeMessage();
                            if(flyDrone)
                            {
                                botResponse.Speak = "Yes, you can fly drone here, but please be cautious!";
                                botResponse.Text = "It seems like you are in safe zone, enjoy your drone, but please be cautious. SAFETY FIRST.";
                            }
                            else
                            {
                                botResponse.Speak = "Sorry, you are in restricted zone. Please consult the authority if you need help.";
                                botResponse.Text = "Sorry, this is restricted area. If you need to fly drone here, please contact Civil Aviation Authority of Singapore for more details.";
                            }

                            await context.PostAsync(botResponse);
                            context.Wait(MessageReceived);
                        }
                        else
                        {
                            var botResponse = context.MakeMessage();
                            botResponse.Speak = "I can't find your location.";
                            botResponse.Text = "Location is not available.";
                            await context.PostAsync(botResponse);
                            context.Wait(MessageReceived);
                        }
                    }
                }
                else
                {
                    await context.SayAsync("Sorry, I can't retrieve your location now.");
                }
            }
            else
            {
                //route to the user location dialog
                if(act.ChannelId == "facebook")
                {
                    await context.Forward(new UserLocationDialog(), ResumeAfterDroneLocationDialogAsync, act, CancellationToken.None);
                }
                else
                {
                    await context.Forward(new UserLocationOneMapDialog(), ResumeAfterDroneLocationOneMapDialogAsync, act, CancellationToken.None);
                }
            }
        }

        //conversation for permit
        //this involved dialog flow, hence both will need to route to a separate dialog
        [LuisIntent("guidePermit")]
        public async Task guidePermineIntent(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            var act = await activity;
            if(act.ChannelId == "cortana")
            {
                //cortana
                await context.Forward(new UserFormDialog(), ResumeAfterFormDialogAsync, act, CancellationToken.None);
            }
            else if(act.ChannelId == "facebook")
            {
                //facebook
                context.Call(FlightStatusForm.BuildFormDialog(FormOptions.PromptInStart), SendFlightForm);
            }
            else
            {
                //webchat
                await context.Forward(new UserFormDialog(), ResumeAfterFormDialogAsync, act, CancellationToken.None);
            }
        }

        //get images from users and send to custom vision endpoint to detect drone type
        [LuisIntent("checkDroneType")]
        public async Task checkDroneTypeIntent(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            var act = await activity;
            if(act.ChannelId == "cortana")
            {
                await context.SayAsync("Make sure your drone is small in size, total mass including payload is less than 7 kg", "Make sure the total mass is less than 7 kilograms!");
            }
            else
            {
                try
                {
                    PromptDialog.Attachment(
                        context,
                        AfterDroneImageAsync,
                        "Show me your drone by taking a picture"
                        );
                }
                catch (Exception)
                {
                    await context.PostAsync("Sorry, it seems like something bad happened. Try again later.");
                    context.Wait(MessageReceived);
                }
            }
        }

        private async Task ResumeAfterDroneLocationDialogAsync(IDialogContext context, IAwaitable<Place> result)
        {
            var place = await result;
            if (place != default(Place))
            {
                var geo = (place.Geo as JObject)?.ToObject<GeoCoordinates>();
                if (geo != null)
                {
                    string lat = geo.Latitude.ToString();
                    string lon = geo.Longitude.ToString();
                    var reply = context.MakeMessage();
                    reply.Attachments.Add(new HeroCard
                    {
                        Title = "Open your location in bing maps!",
                        Buttons = new List<CardAction> {
                            new CardAction
                            {
                                Title = "Your location",
                                Type = ActionTypes.OpenUrl,
                                Value = $"https://www.bing.com/maps/?v=2&cp={geo.Latitude}~{geo.Longitude}&lvl=16&dir=0&sty=c&sp=point.{geo.Latitude}_{geo.Longitude}_You%20are%20here&ignoreoptin=1"
                            }
                        }
                    }.ToAttachment());
                    await context.PostAsync(reply);

                    bool flyDrone = await DroneOperation.validateDroneFlyingZone(lat, lon);                   
                    if(flyDrone)
                    {
                        await context.PostAsync("Yes, you can fly drone here.");
                    }
                    else
                    {
                        await context.PostAsync("Sorry, you are in restricted zone. However, we can guide you if a permit is required to fly drone at this location. Let me know if you need guidance here!");
                    }
                    string weatherTips = await otherOperation.getNearestWeatherForecast(lat, lon);
                    await context.PostAsync(weatherTips);
                    
                }
                else
                {
                    await context.PostAsync("No Coordinates");
                }
            }
            else
            {
                await context.PostAsync("No location");
            }
            context.Wait(MessageReceived);
        }

        private async Task ResumeAfterDroneLocationOneMapDialogAsync(IDialogContext context, IAwaitable<object> result)
        {
            context.Wait(MessageReceived);
        }

        private async Task ResumeAfterFormDialogAsync(IDialogContext context, IAwaitable<object> result)
        {
            context.Wait(MessageReceived);
        }

        //input for drone classification
        private async Task AfterDroneImageAsync(IDialogContext context, IAwaitable<IEnumerable<Attachment>> arguement)
        {
            var userImage = await arguement;
            if(userImage != null)
            {
                string imageUrl = userImage.Last().ContentUrl;
                string droneType = await DroneOperation.GetDroneType(imageUrl);

                List<string> recreationDrone = new List<string> { "DJI Mavic Pro", "DJI Phantom", "DJI Spark"};
                List<string> industrialDrone = new List<string> { "DJI Matrices" };

                if(droneType == "Unknown")
                {
                    string response = await DroneOperation.getImageCaption(imageUrl);
                    await context.PostAsync("Sorry, I didn't recognize this, but I think I saw " + response + ".");
                }
                else
                {
                    await context.PostAsync("I think I saw " + droneType);

                    if(recreationDrone.Contains(droneType))
                    {
                        await context.PostAsync("Looks like this drone is designed for recreation purpose, please fly it safe!");
                        await context.PostAsync("However, the drone is capable of flying more than 60m and in this case, permit is required. Ask me to guide you on the permit criteria.");
                    }
                    else
                    {
                        await context.PostAsync("Hey! This drone is designed for industrial usage, and you will need a permit to operate this.");
                        await context.PostAsync("You can ask me for the type of permit needed, just ask \"Guide Me\"");
                    }
                }
                context.Wait(MessageReceived);
            }
            else
            {
                await context.PostAsync("No image received.");
                context.Wait(MessageReceived);
            }
        }

        //route user flight information to Azure machine learning studio to get guidance
        private async Task SendFlightForm(IDialogContext context, IAwaitable<FlightStatusForm> result)
        {
            try
            {
                var response = await result;
                string indoorOutdoor = response.indoorOutdoorOptions.ToString();
                string operationPurpose = response.operationPurposeOptions.ToString();
                string totalMass = response.totalMassOptions.ToString();
                string flightHeight = response.flightHeightOptions.ToString();
                string restrictedZone = response.restrictedZoneOptions.ToString();

                string permitResponse = await DroneOperation.CheckPermitType(indoorOutdoor, operationPurpose, totalMass, flightHeight, restrictedZone);
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
            }
            catch(FormCanceledException)
            {
                await context.PostAsync("Was interupted?");
            }
            catch(Exception)
            {
                await context.PostAsync("Please try again later.");
            }
            finally
            {
                context.Wait(MessageReceived);
            }
        }

        //adaptive cards for forms
        public static AdaptiveCard generateFormAdaptiveCards()
        {
            List<AdaptiveChoice> indoorOutdoorChoices = new List<AdaptiveChoice>();
            indoorOutdoorChoices.Add(new AdaptiveChoice()
            {
                Title = "Indoor",
                Value = "Indoor"
            });
            indoorOutdoorChoices.Add(new AdaptiveChoice()
            {
                Title = "Outdoor",
                Value = "Outdoor"
            });

            List<AdaptiveChoice> operationPurposeChoices = new List<AdaptiveChoice>();
            operationPurposeChoices.Add(new AdaptiveChoice()
            {
                Title = "Recreational",
                Value = "Recreational"
            });
            operationPurposeChoices.Add(new AdaptiveChoice()
            {
                Title = "Research",
                Value = "Research"
            });
            operationPurposeChoices.Add(new AdaptiveChoice()
            {
                Title = "Non-recreational or non-research",
                Value = "NA"
            });

            List<AdaptiveChoice> binaryChoices = new List<AdaptiveChoice>();
            binaryChoices.Add(new AdaptiveChoice()
            {
                Title = "Yes",
                Value = "Yes"
            });
            binaryChoices.Add(new AdaptiveChoice()
            {
                Title = "No",
                Value = "No"
            });

            AdaptiveCard card = new AdaptiveCard();
            card.Body.Add(new AdaptiveTextBlock()
            {
                Text = "Pilot Flying Condition",
                Size = AdaptiveTextSize.ExtraLarge,
                Weight = AdaptiveTextWeight.Bolder
            });

            card.Body.Add(new AdaptiveTextBlock()
            {
                Text = "Are you flying indoor or outdoor?",
                Size = AdaptiveTextSize.Large,
                Weight = AdaptiveTextWeight.Default
            });

            card.Body.Add(new AdaptiveChoiceSetInput()
            {
                IsMultiSelect = false,
                Id = "indoorOutdoor",
                Style = AdaptiveChoiceInputStyle.Expanded,
                Choices = indoorOutdoorChoices               
            });

            card.Body.Add(new AdaptiveTextBlock()
            {
                Text = "What is your flight purpose?",
                Size = AdaptiveTextSize.Large,
                Weight = AdaptiveTextWeight.Default
            });

            card.Body.Add(new AdaptiveChoiceSetInput()
            {
                IsMultiSelect = false,
                Id = "operationPurpose",
                Style = AdaptiveChoiceInputStyle.Expanded,
                Choices = operationPurposeChoices
            });
            card.Body.Add(new AdaptiveTextBlock()
            {
                Text = "Is the total mass(drone + payload) more than 7kg?",
                Size = AdaptiveTextSize.Large,
                Weight = AdaptiveTextWeight.Default
            });
            card.Body.Add(new AdaptiveChoiceSetInput()
            {
                IsMultiSelect = false,
                Id = "totalMass",
                Style = AdaptiveChoiceInputStyle.Expanded,
                Choices = binaryChoices
            });
            card.Body.Add(new AdaptiveTextBlock()
            {
                Text = "Is your flight height more than 60m?",
                Size = AdaptiveTextSize.Large,
                Weight = AdaptiveTextWeight.Default
            });
            card.Body.Add(new AdaptiveChoiceSetInput()
            {
                IsMultiSelect = false,
                Id = "flightHeight",
                Style = AdaptiveChoiceInputStyle.Expanded,
                Choices = binaryChoices
            });
            card.Body.Add(new AdaptiveTextBlock()
            {
                Text = "Are you in restricted zone?",
                Size = AdaptiveTextSize.Large,
                Weight = AdaptiveTextWeight.Default
            });
            card.Body.Add(new AdaptiveChoiceSetInput()
            {
                IsMultiSelect = false,
                Id = "restrictedZone",
                Style = AdaptiveChoiceInputStyle.Expanded,
                Choices = binaryChoices
            });
            card.Actions = new List<AdaptiveAction>()
            {
                new AdaptiveSubmitAction
                {
                    Title = "Submit",
                    DataJson = "{ \"Type\": \"PilotFlyingCondition\" }"
                }
            };

            return card;
        }
    }
}