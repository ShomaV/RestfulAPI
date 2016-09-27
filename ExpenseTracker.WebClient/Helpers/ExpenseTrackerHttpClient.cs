namespace ExpenseTracker.WebClient.Helpers
{
    using Constants;
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;

    public class ExpenseTrackerHttpClient
    {
        public static HttpClient GetClient()
        {
            HttpClient client = new HttpClient
            {
                BaseAddress = new Uri(ExpenseTrackerConstants.ExpenseTrackerApi)
            };
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }
    }
}