using System;
using System.Threading.Tasks;
using HxLunchBot.Models;
using Microsoft.Bot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Schema;

namespace HxLunchBot
{
    public class LunchBot : IBot
    {
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
            // This bot is only handling Messages
            if (context.Activity.Type == ActivityTypes.Message)
            {
                // Get the conversation state from the turn context
                var state = context.GetConversationState<EchoState>();

                if (state.TurnCount == 0)
                {
                    //await context.SendActivity($"Buen día {context.Activity.From.Name}, ¿Salimos a almorzar hoy?");

                    await PedirConfirmacion(context, $"Buen día {context.Activity.From.Name}, ¿Salimos a almorzar hoy? (S/N)");
                }
                else if (state.TurnCount == 1)
                {
                    if (context.Activity.Text == "S")
                    {
                        state.Voto = new Voto(context.Activity.From.Id);

                        await this.SendMenu(context, "¡Buenísimo! ¿A dónde querés ir?");
                    }
                    else
                    {
                        await Cancelar(context, state);
                    }
                }
                else if (state.TurnCount == 2)
                {
                    if (int.TryParse(context.Activity.Text, out int opcion))
                    {
                        if (opcion == 0)
                        {
                            await Cancelar(context, state);
                        }
                        else
                        {
                            // guardar voto
                            state.Voto.OpcionVotada = opcion;

                            // preguntar por ban
                            await this.PedirConfirmacion(context, "Gran elección. Acá nos gusta la democracia, pero hasta ahí. ¿Vas a bannear algún lugar? (S/N)");
                        }
                    }
                    else
                    {
                        await context.SendActivity("La opción no es válida, por favor volvé a votar.");
                        state.TurnCount = 1;
                        await this.SendMenu(context, "¿A dónde querés ir?");
                    }
                }
                else if (state.TurnCount == 3)
                {
                    if (context.Activity.Text == "S")
                    {
                        // menu de ban
                        await this.SendMenu(context, "¡Maquiavélico! ¿A cuál?");
                    }
                    else
                    {
                        // registrar voto
                        state.Voto.IsRegistrado = true;
                        await context.SendActivity($"Tu voto a {state.Voto.OpcionVotada} ya fue registrado.\n" +
                            $"Hasta ahora somos <cant personas>. A las <hora limite> anunciamos al ganador. Suerte!");
                    }
                }
                else if (state.Voto.IsRegistrado)
                {
                    // informar estado
                    await context.SendActivity($"Tu voto a {state.Voto.OpcionVotada} ya fue registrado.\n" +
                        $"Hasta ahora somos <cant personas>. A las <hora limite> anunciamos al ganador. Suerte!");
                }
                else if (state.Voto.OpcionBanneada == 0)
                {
                    // estaba banneando
                    if (int.TryParse(context.Activity.Text, out int opcion))
                    {
                        if (opcion == 0)
                        {
                            await this.PedirConfirmacion(context, "¿Entonces? ¿Vas a bannear algún lugar? (S/N)");
                            state.TurnCount = 2;
                        }
                        else
                        {
                            // guardar voto
                            state.Voto.OpcionBanneada = opcion;
                            // registrar voto
                            state.Voto.IsRegistrado = true;
                            await context.SendActivity($"Tu voto a {state.Voto.OpcionVotada} ya fue registrado.\n" +
                                $"Hasta ahora somos <cant personas>. A las <hora limite> anunciamos al ganador. Suerte!");
                        }
                    }
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

        private async Task Cancelar(ITurnContext context, EchoState state)
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
