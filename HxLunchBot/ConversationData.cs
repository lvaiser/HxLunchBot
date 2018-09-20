using HxLunchBot.Models;
using System.Collections.Generic;

namespace HxLunchBot
{
    public class ConversationData
    {
        /// <summary>Property for storing dialog state.</summary>
        public Dictionary<string, object> DialogState { get; set; } = new Dictionary<string, object>();

        public IEnumerable<Restaurant> Restaurants;

        public Voto Voto { get; set; }

        public int TurnCount { get; set; } = 0;
    }
}