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
    /// <summary>
    /// Interaction logic for Register.xaml
    /// </summary>
    public partial class Register : Page
    {
        List<City> cities;
        ApiService api = new ApiService();
        public Register()
        {
            InitializeComponent();
            PutCityData();
        }

        public async void PutCityData()
        {
             cities = await api.GetAllCitiesAsync(); 
            List<string> clist=new List<string>();
            foreach(City c in cities)
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

        //private void CreateAccount(object sender, RoutedEventArgs e)
        //{
        //    string fn = fname.Text;
        //    string ln = lname.Text;
        //    string num = numofkids.Text;
        //    string pas = pass.Password;
        //    City currcity = cities[cityname.SelectedIndex];
        //    string confirmpasss = confirmpass.Password;
        //    if(confirmpasss!=pas)
        //    { passerror.Visibility = Visibility.Visible;
        //    }
        //    else
        //    {
        //        Parents p = new Parents() { FirstName = fn, CityNameId = currcity, LastName=ln, NumOfKids= int.Parse(num), Password=pas };
        //        api.InsertParentAsync(p);
        //        //NavigationService?.Navigate(new Uri("Pages/LogInComputer.xaml", UriKind.Relative));
        //    }




        //}
        //private async void CreateAccount(object sender, RoutedEventArgs e)
        //{
        //    string fn = fname.Text;
        //    string ln = lname.Text;
        //    string num = numofkids.Text;
        //    string pas = pass.Password;
        //    City currcity = cities[cityname.SelectedIndex];
        //    string confirmpasss = confirmpass.Password;

        //    if (confirmpasss != pas)
        //    {
        //        passerror.Visibility = Visibility.Visible;
        //        return;
        //    }

        //    Parents p = new Parents()
        //    {
        //        FirstName = fn,
        //        LastName = ln,
        //        CityNameId = currcity,
        //        NumOfKids = int.Parse(num),
        //        Password = pas
        //    };


        //    await api.InsertParentAsync(p);

        //    NavigationService.Navigate(new ChildOfParents(p));
        //}

        private async void CreateAccount(object sender, RoutedEventArgs e)
        {
            string fn = fname.Text;
            string ln = lname.Text;
            string num = numofkids.Text;
            string pas = pass.Password;
            City currcity = cities[cityname.SelectedIndex];
            string confirmpasss = confirmpass.Password;

            // משיכת הטקסט משדה הטלפון
            string phoneText = phone.Text;

            if (confirmpasss != pas)
            {
                passerror.Visibility = Visibility.Visible;
                return;
            }

            DateTime dob;
            DateTime.TryParse(dateofbirth.Text, out dob);

            // המרת מספר הטלפון מטקסט למספר שלם
            // (אם הוא יכניס אותיות בטעות, זה יחזיר 0 כדי לא לקרוס)
            int phoneNumber = 0;
            int.TryParse(phoneText, out phoneNumber);

            Parents p = new Parents()
            {
                FirstName = fn,
                LastName = ln,
                CityNameId = currcity,
                NumOfKids = int.Parse(num),
                Password = pas,
                DateOfBirth = dob,
                Telephone = phoneNumber // הצבת מספר הטלפון כאן
            };

            await api.InsertParentAsync(p);
            NavigationService.Navigate(new ChildOfParents(p));
        }
    }

}

