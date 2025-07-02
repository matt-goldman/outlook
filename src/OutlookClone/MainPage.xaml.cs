using System.Collections.ObjectModel;
using System.Net.Http.Json;
using OutlookClone.Models;

namespace OutlookClone
{
    public partial class MainPage : ContentPage
    {

        private string contentUri = "https://thesimpsonsquoteapi.glitch.me/quotes?count=20";

        public ObservableCollection<Simpson> Simpsons = new();

        private readonly List<Simpson> _sourceSimpsons = [];

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

            
            jsonResponse.ForEach(s => _sourceSimpsons.Add(s));
            
            foreach (var simpson in _sourceSimpsons)
            {
                Simpsons.Add(simpson);
            }

            LoadingIndicator.IsVisible = false;
        }

        private void FocusToggle_OnToggleChanged(object sender, bool e)
        {
            if (e)
            {
                Simpsons.Clear();
                
                _sourceSimpsons.Where(s => s.character.Contains("Homer", StringComparison.OrdinalIgnoreCase))
                               .ToList()
                               .ForEach(s => Simpsons.Add(s));
            }
            else
            {
                Simpsons.Clear();
                
                _sourceSimpsons.ForEach(s => Simpsons.Add(s));
            }
        }
    }
}