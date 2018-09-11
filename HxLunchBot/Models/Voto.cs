using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HxLunchBot.Models
{
    public class Voto
    {
        public Guid Id { get; set; }

        public string Votante { get; set; }

        public DateTime Fecha { get; set; }

        public int OpcionVotada { get; set; }

        public int OpcionBanneada { get; set; }

        public bool IsRegistrado { get; set; }

        public Voto(string votante)
        {
            this.Id = new Guid();
            this.Votante = votante;
        }
    }
}
