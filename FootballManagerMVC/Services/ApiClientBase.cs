using System.Net.Http;
using System.Net.Http.Json;

namespace FootballManagerMVC.Services
{
    public class ApiClient
    {
        private readonly HttpClient _http;

        public ApiClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<T>> GetListAsync<T>(string endpoint)
            => await _http.GetFromJsonAsync<List<T>>(endpoint);

        public async Task<T> GetAsync<T>(string endpoint)
            => await _http.GetFromJsonAsync<T>(endpoint);

        public async Task<HttpResponseMessage> PostAsync<T>(string endpoint, T data)
            => await _http.PostAsJsonAsync(endpoint, data);

        public async Task<HttpResponseMessage> PutAsync<T>(string endpoint, T data)
            => await _http.PutAsJsonAsync(endpoint, data);

        public async Task<HttpResponseMessage> DeleteAsync(string endpoint)
            => await _http.DeleteAsync(endpoint);

        public async Task<HttpResponseMessage> PostAsync(string endpoint)
    => await _http.PostAsync(endpoint, null);

    }
}
