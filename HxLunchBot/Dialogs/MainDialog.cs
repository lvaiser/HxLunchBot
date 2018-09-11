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
            public const string RegisterVote = "registerVote";
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

            public string Description { get; set; }
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
                new ChoiceOption { Name = "CampoBravo", Description = "1) CampoBravo" },
                new ChoiceOption { Name = "El Estanciero", Description = "2) El Estanciero" },
                new ChoiceOption { Name = "Almacén & Co", Description = "3) Almacén & Co" }
            };

            public static List<string> YesNoList = YesNoOptions.Select(x => x.Value).ToList();

            public static List<string> VoteList = VoteOptions.Select(x => x.Description).ToList();

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

        public MainDialog()
        {
            Add(Inputs.Choice, new ChoicePrompt(Microsoft.Recognizers.Text.Culture.Spanish));
            Add(Name, new WaterfallStep[]
            {
                // Each step takes in a dialog context, arguments, and the next delegate.
                async (dc, args, next) =>
                {
                    await dc.Prompt(Inputs.Choice, $"Buen día {dc.Context.Activity.From.Name}, ¿Salimos a almorzar hoy?", new ChoicePromptOptions()
                    {
                        Choices = Lists.YesNoChoices,
                        RetryPromptActivity =
                            MessageFactory.SuggestedActions(Lists.YesNoList, "¿Salimos a almorzar hoy?") as Activity
                    });
                },
                async(dc, args, next) =>
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
                },
                async(dc, args, next) =>
                {
                    await dc.Begin(Dialogs.Ban, dc.ActiveDialog.State);
                },
                async(dc, args, next) =>
                {
                    await dc.Begin(Dialogs.RegisterVote, dc.ActiveDialog.State);
                }
            });
            Add(Dialogs.Vote, new WaterfallStep[]
            {
                // Each step takes in a dialog context, arguments, and the next delegate.
                async (dc, args, next) =>
                {
                    await dc.Prompt(Inputs.Choice, "¡Buenísimo! ¿A dónde querés ir?", new ChoicePromptOptions()
                    {
                        Choices = Lists.VoteChoices,
                        RetryPromptActivity = Lists.VoteReprompt
                    });
                },
                async(dc, args, next) =>
                {
                    var choice = (FoundChoice)args["Value"];
                    var vote = Lists.VoteOptions[choice.Index];

                    dc.ActiveDialog.State[Outputs.VotedOption] = vote;
                    await dc.Context.SendActivity($"¡{vote.Name}! ¡Gran elección!");
                    await dc.End(); // devolver voto
                }
            });
            Add(Dialogs.Ban, new WaterfallStep[]
            {
                async (dc, args, next) =>
                {
                    await dc.Prompt(Inputs.Choice, "Acá nos gusta la democracia, pero hasta ahí. ¿Vas a bannear algún lugar?", new ChoicePromptOptions()
                    {
                        Choices = Lists.YesNoChoices,
                        RetryPromptActivity =
                            MessageFactory.SuggestedActions(Lists.YesNoList, "¿Vas a bannear algún lugar?") as Activity
                    });
                },
                async(dc, args, next) =>
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
                },
                async(dc, args, next) =>
                {
                    var choice = (FoundChoice)args["Value"];
                    var vote = Lists.VoteOptions[choice.Index];

                    dc.ActiveDialog.State[Outputs.BannedOption] = vote;
                    await dc.Context.SendActivity($"¡{vote.Name} OUT!");
                    await dc.End(); // devolver ban
                }
            });
            Add(Dialogs.RegisterVote, new WaterfallStep[]
            {   
                async(dc, args, next) =>
                {                   
                    // guardar voto
                    args.TryGetValue(Outputs.VotedOption, out object votedOption);

                    await dc.Context.SendActivity($"Tu voto a {votedOption} ya fue registrado.\n" +
                            $"Hasta ahora somos <cant personas>. A las <hora limite> anunciamos al ganador. Suerte!");
                    await dc.End();
                }
            });
        }
    }
}
