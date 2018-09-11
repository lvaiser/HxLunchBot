using System;
using System.Threading.Tasks;
using HxLunchBot.Dialogs;
using HxLunchBot.Models;
using Microsoft.Bot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Schema;

namespace HxLunchBot
{
    public class LunchBot : IBot
    {
        private static MainDialog LunchVote { get; } = new MainDialog();

        /// <summary>
        /// Every Conversation turn for our EchoBot will call this method. In here
        /// the bot checks the Activty type to verify it's a message, bumps the 
        /// turn conversation 'Turn' count, and then echoes the users typing
        /// back to them. 
        /// </summary>
        /// <param name="context">Turn scoped context containing all the data needed
        /// for processing this conversation turn. </param>        
        public async Task OnTurn(ITurnContext context)
        {

            // Get the conversation state from the turn context
            var state = context.GetConversationState<ConversationData>();

            var dc = LunchVote.CreateContext(context, state.DialogState);

            // This bot is only handling Messages
            if (context.Activity.Type == ActivityTypes.Message)
            {
                await dc.Continue();
                if (!context.Responded)
                {
                    await dc.Begin(MainDialog.Name);
                }

                // Bump the turn count. 
                state.TurnCount++;

                // Echo back to the user whatever they typed.
                //await context.SendActivity($"Turn {state.TurnCount}: You sent '{context.Activity.Text}'");
            }
        }

        private async Task PedirConfirmacion(ITurnContext context, string text)
        {
            // Create the activity and add suggested actions.
            var activity = MessageFactory.SuggestedActions(
                new CardAction[]
                {
                            new CardAction(title: "Si", type: ActionTypes.ImBack, value: "S"),
                            new CardAction( title: "No", type: ActionTypes.ImBack, value: "N")
                }, text: text);

            // Send the activity as a reply to the user.
            await context.SendActivity(activity);
        }

        private async Task Cancelar(ITurnContext context, ConversationData state)
        {
            await context.SendActivity($"¡Qué lástima! Si te arrepentís avisame, tenés tiempo hasta las 12:30 PM.");
            state.TurnCount = -1;
        }

        private async Task SendMenu(ITurnContext context, string prompt)
        {
            // Create the activity and add suggested actions.
            var activity = MessageFactory.SuggestedActions(
                new CardAction[]
                {
                            new CardAction(title: "CampoBravo", type: ActionTypes.ImBack, value: "1"),
                            new CardAction(title: "El Estanciero", type: ActionTypes.ImBack, value: "2"),
                            new CardAction(title: "Almacén & Co", type: ActionTypes.ImBack, value: "3"),
                }, text: $"{prompt}\n" +
                    $"1 - CampoBravo\n" +
                    $"2 - El Estanciero\n" +
                    $"3 - Almacén & Co\n" +
                    $"0 - Cancelar");

            await context.SendActivity(activity);
        }
    }
}
