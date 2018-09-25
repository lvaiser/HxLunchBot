using HxLunchBot.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace HxLunchBot
{
    public class DBClient
    {
        private HttpClient _client;

        public DBClient()
        {
            _client = new HttpClient()
            {
                BaseAddress = new Uri(Environment.GetEnvironmentVariable("DatabaseEndpoint"))
            };
        }

        public async Task<Restaurant[]> GetRestaurants()
        {
            var restaurants = await _client.GetStringAsync("/Restaurants/.json");
            return JsonConvert.DeserializeObject<Restaurant[]>(restaurants);
        }

        public async Task SaveVoto(Voto voto)
        {
            var json = JsonConvert.SerializeObject(voto);

            var buffer = System.Text.Encoding.UTF8.GetBytes(json);
            var byteContent = new ByteArrayContent(buffer);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await _client.PostAsync("/Votos/.json", byteContent);
        }

        public async Task<Voto> GetVotoDelDiaPorUsuario(string userId)
        {
            var votosDict = await GetVotosDelDia();
            var votos = votosDict.Select(x => x.Value);

            return votos.SingleOrDefault(x => x.Votante == userId);
        }

        public async Task<Dictionary<string, Voto>> GetVotosDelDia()
        {
            var votosJson = await _client.GetStringAsync($"/Votos/.json?" +
                $"orderBy=\"Fecha\"" +
                $"&startAt={JsonConvert.SerializeObject(DateTime.Today)}");

            return JsonConvert.DeserializeObject<Dictionary<string, Voto>>(votosJson);
        }
    }
}
