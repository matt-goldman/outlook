using System.Collections.ObjectModel;
using System.Net.Http.Json;
using OutlookClone.Models;

namespace OutlookClone
{
    public partial class MainPage : ContentPage
    {

        private string contentUri = "https://thesimpsonsquoteapi.glitch.me/quotes?count=20";

        public ObservableCollection<Simpson> Simpsons = new();

        public MainPage()
        {
            InitializeComponent();
            MessageCollection.ItemsSource = Simpsons;
        }

        protected override async void OnAppearing()
        {
            LoadingIndicator.IsVisible = true;

            base.OnAppearing();

            var httpClient = new HttpClient();

            var jsonResponse = await httpClient.GetFromJsonAsync<List<Simpson>>(contentUri);

            jsonResponse.ForEach(s => Simpsons.Add(s));

            LoadingIndicator.IsVisible = false;
        }
    }
}