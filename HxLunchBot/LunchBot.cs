using System;
using System.Linq;
using System.Net.Http;
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
        /// Every Conversation turn for our Bot will call this method. In here
        /// the bot checks the Activty type to verify it's a message, bumps the 
        /// turn conversation 'Turn' count, and then continues the conversation. 
        /// </summary>
        /// <param name="context">Turn scoped context containing all the data needed
        /// for processing this conversation turn. </param>        
        public async Task OnTurn(ITurnContext context)
        {
            // Get the conversation state from the turn context
            var state = context.GetConversationState<ConversationData>();

            var dc = LunchVote.CreateContext(context, state.DialogState);

            //if (context.Activity.Type == ActivityTypes.ConversationUpdate
            //    && context.Activity.MembersAdded != null && context.Activity.MembersAdded.Any())
            //{
            //    foreach (var newMember in context.Activity.MembersAdded)
            //    {
            //        if (newMember.Id != context.Activity.Recipient.Id)
            //        {
            //            await dc.Begin(MainDialog.Name);
            //        }
            //    }
            //}
            //// This bot is only handling Messages
            //else
            if (context.Activity.Type == ActivityTypes.Message)
            {
                // Bump the turn count. 
                state.TurnCount++;

                await dc.Continue();
                if (!context.Responded)
                {
                    await dc.Begin(MainDialog.Name);
                }
            }
        }
    }
}
