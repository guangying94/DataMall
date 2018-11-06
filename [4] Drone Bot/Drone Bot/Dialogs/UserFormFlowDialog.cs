using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;



namespace datamall_bot.Dialogs
{
    public enum IndoorOutdoorOptions { Indoor = 1, Outdoor };
    public enum OperationPurposeOptions { Recreational = 1, Research, NotAbove };
    public enum TotalMassOptions { Yes = 1, No };
    public enum FlightHeightOptions { Yes = 1, No };
    public enum RestrictedZoneOptions { Yes = 1, No };

    [Serializable]
    //to build the form for users to input their flight
    public class FlightStatusForm
    {
        [Prompt("Are you flying indoor or outdoor? {||}")]
        public IndoorOutdoorOptions indoorOutdoorOptions;

        [Prompt("What is your flight purpose?{||}")]
        public OperationPurposeOptions operationPurposeOptions;

        [Prompt("Is the total mass(drone + payload) more than 7kg? {||}")]
        public TotalMassOptions totalMassOptions;

        [Prompt("Is your flight height more than 60m? {||}")]
        public FlightHeightOptions flightHeightOptions;

        [Prompt("Are you in restricted zone? {||}")]
        public RestrictedZoneOptions restrictedZoneOptions;

        public static IForm<FlightStatusForm> BuildForm()
        {
            return new FormBuilder<FlightStatusForm>().Message("Please provide the information about your flight")
                .OnCompletion(async (context, profileForm) =>
                {
                })
                .Build();
        }

        
        public static IFormDialog<FlightStatusForm> BuildFormDialog(FormOptions options = FormOptions.PromptInStart)
        {
            return FormDialog.FromForm(BuildForm, options);
        }
        
    }
}