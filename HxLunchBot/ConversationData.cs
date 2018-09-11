using HxLunchBot.Models;
using System.Collections.Generic;

namespace HxLunchBot
{
    /// <summary>
    /// Class for storing conversation state. 
    /// </summary>
    //public class EchoState
    //{
    //    public int TurnCount { get; set; } = 0;

    //    public Voto Voto { get; set; }
    //}

    public class ConversationData
    {
        /// <summary>Property for storing dialog state.</summary>
        public Dictionary<string, object> DialogState { get; set; } = new Dictionary<string, object>();

        public Voto Voto { get; set; }

        public int TurnCount { get; set; } = 0;
    }
}