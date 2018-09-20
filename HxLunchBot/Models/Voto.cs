using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HxLunchBot.Models
{
    public class Voto
    {
        public string Votante { get; set; }

        public DateTime Fecha { get; set; }

        public int OpcionVotada { get; set; }

        public int OpcionBanneada { get; set; }

        public Voto(string votante)
        {
            this.Votante = votante;
        }
    }
}
