using HxLunchBot.Models;
using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Prompts.Choices;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace HxLunchBot.Dialogs
{
    public class MainDialog : DialogSet
    {
        public const string Name = "mainDialog";

        /// <summary>Contains the IDs for the other dialogs in the set.</summary>
        private static class Dialogs
        {
            public const string Vote = "vote";
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

        private class ChoiceOption
        {
            public string Name { get; set; }

            public string DisplayName { get; set; }
        }

        /// <summary>Contains the lists used to present options to the guest.</summary>
        private static class Lists
        {
            public static List<Choice> YesNoOptions { get; } = new List<Choice>
            {
                new Choice { Value = "Sí", Synonyms = new List<string> { "Si", "S" } },
                new Choice { Value = "No", Synonyms = new List<string> { "N" }  }
            };

            /// <summary>The options for the top-level dialog.</summary>
            public static List<ChoiceOption> VoteOptions { get; } = new List<ChoiceOption>
            {
                new ChoiceOption { Name = "CampoBravo", DisplayName = "1) CampoBravo" },
                new ChoiceOption { Name = "El Estanciero", DisplayName = "2) El Estanciero" },
                new ChoiceOption { Name = "Almacén & Co", DisplayName = "3) Almacén & Co" }
            };

            public static List<string> YesNoList = YesNoOptions.Select(x => x.Value).ToList();

            public static List<string> VoteList = VoteOptions.Select(x => x.DisplayName).ToList();

            public static List<Choice> YesNoChoices { get; } = ChoiceFactory.ToChoices(YesNoList);

            public static List<Choice> VoteChoices { get; } = ChoiceFactory.ToChoices(VoteList);

            public static Activity VoteReprompt
            {
                get
                {
                    var reprompt = MessageFactory.SuggestedActions(VoteList, "¿A dónde querés ir?");
                    reprompt.AttachmentLayout = AttachmentLayoutTypes.List;
                    return reprompt as Activity;
                }
            }
        }

        private async Task WelcomeStep(DialogContext dc, IDictionary<string, object> args, SkipStepFunction next)
        {
            await dc.Prompt(Inputs.Choice, $"Buen día {dc.Context.Activity.From.Name}, ¿Salimos a almorzar hoy?", new ChoicePromptOptions()
            {
                Choices = Lists.YesNoChoices,
                RetryPromptActivity =
                    MessageFactory.SuggestedActions(Lists.YesNoList, "¿Salimos a almorzar hoy?") as Activity
            });
        }

        private async Task ConfirmLunchStep(DialogContext dc, IDictionary<string, object> args, SkipStepFunction next)
        {
            var yesNo = (FoundChoice)args["Value"];
            if (yesNo.Index == 0)
            {
                await dc.Begin(Dialogs.Vote, dc.ActiveDialog.State);
            }
            else
            {
                await dc.Context.SendActivity("¡Qué lástima! Si te arrepentís avisame, tenés tiempo hasta las 12:30 PM.");
                await dc.End();
            }
        }

        private async Task VotePromptStep(DialogContext dc, IDictionary<string, object> args, SkipStepFunction next)
        {
            var state = dc.Context.GetConversationState<ConversationData>();
            state.Voto = new Voto(dc.Context.Activity.From.Id);

            await dc.Prompt(Inputs.Choice, "¡Buenísimo! ¿A dónde querés ir?", new ChoicePromptOptions()
            {
                Choices = Lists.VoteChoices,
                RetryPromptActivity = Lists.VoteReprompt
            });
        }

        private async Task VoteProcessStep(DialogContext dc, IDictionary<string, object> args, SkipStepFunction next)
        {
            var choice = (FoundChoice)args["Value"];
            var vote = Lists.VoteOptions[choice.Index];

            var state = dc.Context.GetConversationState<ConversationData>();
            state.Voto.OpcionVotada = choice.Index;

            await dc.Context.SendActivity($"¡{vote.Name}! ¡Gran elección!");
            await dc.Begin(Dialogs.Ban, dc.ActiveDialog.State);
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
            var yesNo = (FoundChoice)args["Value"];
            if (yesNo.Index == 0)
            {
                await dc.Prompt(Inputs.Choice, "¡Maquiavélico! ¿Cuál querés bannear?", new ChoicePromptOptions()
                {
                    Choices = Lists.VoteChoices,
                    RetryPromptActivity =
                        MessageFactory.SuggestedActions(Lists.VoteList, "¿Cuál querés bannear?") as Activity
                });
            }
            else
            {
                await dc.End();
            }
        }

        private async Task BanProcessStep(DialogContext dc, IDictionary<string, object> args, SkipStepFunction next)
        {
            var choice = (FoundChoice)args["Value"];
            var vote = Lists.VoteOptions[choice.Index];

            var state = dc.Context.GetConversationState<ConversationData>();
            state.Voto.OpcionBanneada = choice.Index;

            await dc.Context.SendActivity($"¡{vote.Name} OUT!");
            await dc.End();
        }

        private async Task RegisterVoteStep(DialogContext dc, IDictionary<string, object> args, SkipStepFunction next)
        {
            // guardar voto
            var voto = dc.Context.GetConversationState<ConversationData>().Voto;

            await dc.Context.SendActivity($"Tu voto a {Lists.VoteOptions[voto.OpcionVotada].Name} ya fue registrado.\n" +
                    $"Hasta ahora somos <cant personas>. A las <hora limite> anunciamos al ganador. Suerte!");
            await dc.End();
        }

        public MainDialog()
        {
            Add(Inputs.Choice, new ChoicePrompt(Microsoft.Recognizers.Text.Culture.Spanish));
            Add(Name, new WaterfallStep[]
            {
                WelcomeStep,
                ConfirmLunchStep
            });
            Add(Dialogs.Vote, new WaterfallStep[]
            {
                VotePromptStep,
                VoteProcessStep,
                RegisterVoteStep
            });
            Add(Dialogs.Ban, new WaterfallStep[]
            {
                BanConfirmStep,
                BanPromptStep,
                BanProcessStep
            });
        }
    }
}
