using HxLunchBot.Models;
using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Prompts.Choices;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace HxLunchBot.Dialogs
{
    public class MainDialog : DialogSet
    {
        public const string Name = "mainDialog";

        private DBClient _client;

        /// <summary>Contains the IDs for the other dialogs in the set.</summary>
        private static class Dialogs
        {
            public const string Vote = "vote";
            public const string VoteCount = "voteCount";
            public const string Ban = "ban";
        }

        private static class Inputs
        {
            public const string Choice = "choicePrompt";
        }

        /// <summary>Contains the keys used to manage dialog state.</summary>
        private static class Outputs
        {
            public const string VotedOption = "votedOption";
            public const string BannedOption = "bannedOption";
        }

        private List<Choice> ConvertToVoteChoices(IEnumerable<Restaurant> restaurants)
        {
            return ChoiceFactory.ToChoices(restaurants.Select(x => x.Nombre).ToList());
        }

        private Activity GetVoteReprompt(IList<string> voteList)
        {
                var reprompt = MessageFactory.SuggestedActions(voteList, "¿A dónde querés ir?");
                reprompt.AttachmentLayout = AttachmentLayoutTypes.List;
                return reprompt as Activity;
        }

        /// <summary>Contains the lists used to present options to the guest.</summary>
        private static class Lists
        {
            public static List<Choice> YesNoOptions { get; } = new List<Choice>
            {
                new Choice { Value = "Sí", Synonyms = new List<string> { "Si", "S" } },
                new Choice { Value = "No", Synonyms = new List<string> { "N" }  }
            };

            public static List<string> YesNoList = YesNoOptions.Select(x => x.Value).ToList();

            public static List<Choice> YesNoChoices { get; } = ChoiceFactory.ToChoices(YesNoList);

            public static List<Choice> MenuChoices { get; } = new List<Choice>
            {
                new Choice { Value = "Votar" },
                new Choice { Value = "Recuento" },
                new Choice { Value = "Salir" }
            };
        }

        public MainDialog()
        {
            _client = new DBClient();

            Add(Inputs.Choice, new ChoicePrompt(Microsoft.Recognizers.Text.Culture.Spanish));
            Add(Name, new WaterfallStep[]
            {
                MenuStep,
                MenuProcessStep
            });
            Add(Dialogs.Vote, new WaterfallStep[]
            {
                VotePromptStep,
                VoteProcessStep,
                RegisterVoteStep
            });
            Add(Dialogs.VoteCount, new WaterfallStep[]
            {
                VoteCountStep
            });
            Add(Dialogs.Ban, new WaterfallStep[]
            {
                BanConfirmStep,
                BanPromptStep,
                BanProcessStep
            });
        }

        private async Task MenuStep(DialogContext dc, IDictionary<string, object> args, SkipStepFunction next)
        {
            await dc.Prompt(Inputs.Choice, $"Buen día {dc.Context.Activity.From.Name}, por favor seleccioná una opción:", new ChoicePromptOptions()
            {
                Choices = Lists.MenuChoices,
                RetryPromptActivity =
                    MessageFactory.SuggestedActions(Lists.MenuChoices.Select(x => x.Value).ToList(), "Por favor seleccioná una opción:") as Activity
            });
        }

        private async Task MenuProcessStep(DialogContext dc, IDictionary<string, object> args, SkipStepFunction next)
        {
            var menuChoice = (FoundChoice)args["Value"];
            switch (menuChoice.Index)
            {
                case 0:
                    await dc.Begin(Dialogs.Vote, dc.ActiveDialog.State);
                    break;
                case 1:
                    await dc.Begin(Dialogs.VoteCount, dc.ActiveDialog.State);
                    break;
                default:
                    await dc.Context.SendActivity("¡Hasta la próxima!");
                    await dc.End();
                    break;
            }
        }

        private async Task VotePromptStep(DialogContext dc, IDictionary<string, object> args, SkipStepFunction next)
        {
            var votoDelDia = await _client.GetVotoDelDiaPorUsuario(dc.Context.Activity.From.Id);

            if (votoDelDia != null)
            {
                await dc.Context.SendActivity("Ya votaste hoy! ¬¬");
                await dc.End();
            }
            else
            {
                var state = dc.Context.GetConversationState<ConversationData>();
                state.Voto = new Voto(dc.Context.Activity.From.Id);
                state.Restaurants = await _client.GetRestaurants();

                var choices = this.ConvertToVoteChoices(state.Restaurants);

                await dc.Prompt(Inputs.Choice, "¡Buenísimo! ¿A dónde querés ir?", new ChoicePromptOptions()
                {
                    Choices = choices,
                    RetryPromptActivity = this.GetVoteReprompt(state.Restaurants.Select(x => x.Nombre).ToList())
                });
            }
        }

        private async Task VoteProcessStep(DialogContext dc, IDictionary<string, object> args, SkipStepFunction next)
        {
            var state = dc.Context.GetConversationState<ConversationData>();

            var choice = (FoundChoice)args["Value"];
            var voto = state.Restaurants.ToList()[choice.Index];

            state.Voto.OpcionVotada = choice.Index;

            await dc.Context.SendActivity($"¡{voto.Nombre}! ¡Gran elección!");
            await dc.Begin(Dialogs.Ban, dc.ActiveDialog.State);
        }

        private async Task VoteCountStep(DialogContext dc, IDictionary<string, object> args, SkipStepFunction next)
        {
            string recuento = string.Empty;
            var restaurants = await _client.GetRestaurants();

            var votosDict = await _client.GetVotosDelDia();
            var votos = votosDict.Select(x => x.Value).GroupBy(x => x.OpcionVotada);

            for (int i = 0; i < restaurants.Count(); i++)
            {
                var cantVotos = votos.SingleOrDefault(x => x.Key == i)?.Count() ?? 0;
                recuento += $"{restaurants[i].Nombre}: {cantVotos}\n";
            }

            await dc.Context.SendActivity(recuento);
            await dc.Begin(Name, dc.ActiveDialog.State);
        }

        private async Task BanConfirmStep(DialogContext dc, IDictionary<string, object> args, SkipStepFunction next)
        {
            await dc.Prompt(Inputs.Choice, "Acá nos gusta la democracia, pero hasta ahí. ¿Vas a bannear algún lugar?", new ChoicePromptOptions()
            {
                Choices = Lists.YesNoChoices,
                RetryPromptActivity =
                    MessageFactory.SuggestedActions(Lists.YesNoList, "¿Vas a bannear algún lugar?") as Activity
            });
        }

        private async Task BanPromptStep(DialogContext dc, IDictionary<string, object> args, SkipStepFunction next)
        {
            var state = dc.Context.GetConversationState<ConversationData>();

            var yesNo = (FoundChoice)args["Value"];
            if (yesNo.Index == 0)
            {
                await dc.Prompt(Inputs.Choice, "¡Maquiavélico! ¿Cuál querés bannear?", new ChoicePromptOptions()
                {
                    Choices = this.ConvertToVoteChoices(state.Restaurants),
                    RetryPromptActivity =
                        MessageFactory.SuggestedActions(
                            state.Restaurants.Select(x => x.Nombre).ToList(), 
                            "¿Cuál querés bannear?") as Activity
                });
            }
            else
            {
                state.Voto.OpcionBanneada = -1;
                await dc.End();
            }
        }

        private async Task BanProcessStep(DialogContext dc, IDictionary<string, object> args, SkipStepFunction next)
        {
            var state = dc.Context.GetConversationState<ConversationData>();

            var choice = (FoundChoice)args["Value"];
            var voto = state.Restaurants.ToList()[choice.Index];

            state.Voto.OpcionBanneada = choice.Index;

            await dc.Context.SendActivity($"¡{voto.Nombre} OUT!");
            await dc.End();
        }

        private async Task RegisterVoteStep(DialogContext dc, IDictionary<string, object> args, SkipStepFunction next)
        {
            var state = dc.Context.GetConversationState<ConversationData>();

            // guardar voto
            var voto = state.Voto;
            voto.Fecha = DateTime.Today;
            await _client.SaveVoto(voto);

            await dc.Context.SendActivity($"Tu voto a {state.Restaurants.ToList()[voto.OpcionVotada].Nombre} ya fue registrado.\n" +
                //$"Hasta ahora somos <cant personas>. A las <hora limite> anunciamos al ganador. Suerte!");
                "Suerte!");

            await dc.End();
        }    
    }
}
