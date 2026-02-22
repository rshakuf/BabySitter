using ClApi;
using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BabySitter.Pages;
namespace BabySitter.Pages
{
    public partial class RegisterAsBabysitter : Page
    {
        List<City> cities;
        ApiService api = new ApiService();
        

        public RegisterAsBabysitter()
        {
            InitializeComponent();
            PutCityData();
        }

        public async void PutCityData()
        {
            cities = await api.GetAllCitiesAsync();
            List<string> clist = new List<string>();
            foreach (City c in cities)
            {
                clist.Add(c.CityName);
            }

            //clist = (List<string>)cities.Select(x => x.CityName); 
            cityname.ItemsSource = clist;
        }

      

        private void LogIn_Click(object sender, MouseButtonEventArgs e)
        {
            NavigationService?.Navigate(new Uri("Pages/LogInComputer.xaml", UriKind.Relative));
        }

        private async void CreateAccount(object sender, RoutedEventArgs e)
        {
            if (pass.Password != confirmpass.Password)
            {
                MessageBox.Show("הסיסמאות לא תואמות");
                return;
            }

            DateTime dob;
            DateTime.TryParse(dateofbirth.Text, out dob);

            int pricePerHour;
            int.TryParse(price.Text, out pricePerHour);

            BabySitterTeens teen = new BabySitterTeens()
            {
                FirstName = fname.Text,
                LastName = lname.Text,
                DateOfBirth = dob,
                PriceForAnHour = pricePerHour,
                MailOfRecommender = recommenderMail.Text,
                Password = pass.Password,
                Telephone = 0,          // אם אין שדה טלפון במסך
                ProfilePicture = " "    // אם השרת דורש ערך
            };

            await api.InsertBabySitterTeenAsync(teen);

            MessageBox.Show("הבייביסיטר נרשם בהצלחה");

            NavigationService?.Navigate(new Uri("Pages/Home.xaml", UriKind.Relative));
        }
    }
}