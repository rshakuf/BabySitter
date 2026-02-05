using Microsoft.Extensions.Hosting;
using Model;
using BabysitterApi;
using ClApi;

namespace Test2
{
    internal class Program
    {
        //static async Task Main(string[] args)
        //{
        //    var host = Host.CreateDefaultBuilder(args)
        //        .ConfigureServices((context, services) =>
        //        {
        //            services.AddHttpClient();
        //            services.AddScoped<ApiService>(sp =>
        //            {
        //                var client = sp.GetRequiredService<HttpClient>();
        //                var baseUri = "https://z9vchnvr-5266.euw.devtunnels.ms"; 
        //                return new ApiService(client, baseUri);
        //            });
        //        })
        //     .Build();

        // Get the service and use it
        //using (var scope = host.Services.CreateScope())
        //{
        //  var babysitterService = scope.ServiceProvider.GetRequiredService<ApiService>();
        //  var allCities = await babysitterService.GetAllCitiesAsync();
        //  int x = allCities.Count;
        //  // כאן תכתוב את התוכנית שלך 
        static async Task Main(string[] args)
        {
            //var host = Host.CreateDefaultBuilder(args)
            //    .ConfigureServices((context, services) =>
            //    {
            //        services.AddHttpClient();
            //        services.AddScoped<ApiService>(sp =>
            //        {
            //            var client = sp.GetRequiredService<HttpClient>();
            //            var baseUri = "https://z9vchnvr-5266.euw.devtunnels.ms";
            //            return new ApiService(client, baseUri);
            //        });
            //    })
            //    .Build();

            //using (var scope = host.Services.CreateScope())
            //{
            //    var api = scope.ServiceProvider.GetRequiredService<ApiService>();

            //    using (var scope = host.Services.CreateScope())
            //{
            //    var buyerService = scope.ServiceProvider.GetRequiredService<ApiService>();
            //    var buyers = await buyerService.GetBuyers();
            //    int x = buyers.Count;
            //    
            
            ApiService api = new ApiService();
            //===== Cities =====

            var cities = await api.GetAllCitiesAsync();
            Console.WriteLine($"Cities count: {cities.Count}");
            Console.WriteLine("before city");
            foreach (var c in cities)
            {
                Console.WriteLine($"City ID: {c.Id}, City Name: {c.CityName}");
            }
            await api.InsertCityAsync(new City { CityName = "בדיקה עיר" });
            var city = cities.First();
            city.CityName = "new city11";
            await api.UpdateCityAsync(city);
            await api.DeleteCityAsync(cities.Last().Id);

            Console.WriteLine("after city");

            cities = await api.GetAllCitiesAsync();
            foreach (var c in cities)
            {
                Console.WriteLine($"City ID: {c.Id}, City Name: {c.CityName}");
            }

             //===== BabySitterRate =====
            var rates = await api.GetAllBabySitterRatesAsync();
            Console.WriteLine($"Rates count: {rates.Count}");

            Console.WriteLine("before rates");
            foreach (var c in rates)
            {
                Console.WriteLine($"rate ID: {c.Id}, rate Name: {c.Stars}");
            }

            await api.InsertBabySitterRateAsync(new BabySitterRate { Stars = 50 });
            var rate = rates.First();
            rate.Stars = 60;
            await api.UpdateBabySitterRateAsync(rate);
            await api.DeleteBabySitterRateAsync(rates.Last().Id);

            Console.WriteLine("after rates");

            rates = await api.GetAllBabySitterRatesAsync();
            foreach (var c in rates)
            {
                Console.WriteLine($"rate ID: {c.Id}, rate Name: {c.Stars}");
            }

             //===== BabySitterTeens =====
            var teens = await api.GetAllBabySitterTeensAsync();
            Console.WriteLine($"Teens count: {teens.Count}");

            Console.WriteLine("before teen");
            foreach (var c in teens)
            {
                Console.WriteLine($"rate ID:  {c.Id} , teen Name:  {c.FirstName}");
            }

            await api.InsertBabySitterTeenAsync(new BabySitterTeens { FirstName = "נער בדיקה" });
            var teen = teens.First();
            teen.FirstName = "נער מעודכן";
            await api.UpdateBabySitterTeenAsync(teen);
            await api.DeleteBabySitterTeenAsync(teens.Last().Id);

            Console.WriteLine("after teen");

            teens = await api.GetAllBabySitterTeensAsync();
            foreach (var c in teens)
            {
                Console.WriteLine($"teen ID: {c.Id}, teen Name: {c.FirstName}");
            }

            // ===== Parents =====
            var parents = await api.GetAllParentsAsync();
            Console.WriteLine($"Parents count: {parents.Count}");

            Console.WriteLine("before parent");
            foreach (var c in parents)
            {
                Console.WriteLine($"parent ID:  {c.Id} , parent Name:  {c.FirstName}");
            }

            await api.InsertParentAsync(new Parents { FirstName = "הורה בדיקה" });
            var parent = parents.First();
            parent.FirstName = "הורה מעודכן";
            await api.UpdateParentAsync(parent);
            await api.DeleteParentAsync(parents.Last().Id);

            parents = await api.GetAllParentsAsync();
            foreach (var c in parents)
            {
                Console.WriteLine($"parent ID: {c.Id}, parent Name: {c.FirstName}");
            }

            // ===== Users =====
            var users = await api.GetAllUsersAsync();
            Console.WriteLine($"Users count: {users.Count}");

            Console.WriteLine("before user");
            foreach (var c in users)
            {
                Console.WriteLine($"user ID:  {c.Id} , user Name:  {c.FirstName}");
            }

            await api.InsertUserAsync(new User { FirstName = "user_test" });
            var user = users.First();
            user.FirstName = "user_updated";
            await api.UpdateUserAsync(user);
            await api.DeleteUserAsync(users.Last().Id);

            Console.WriteLine("after user");
            users = await api.GetAllUsersAsync();
            foreach (var c in users)
            {
                Console.WriteLine($"user ID: {c.Id}, user Name: {c.FirstName}");
            }

            // ===== UserProfile =====
            var profiles = await api.GetAllUserProfilesAsync();
            Console.WriteLine($"Profiles count: {profiles.Count}");

            await api.InsertUserProfileAsync(new UserProfile { Email = "בדיקה" });
            var profile = profiles.First();
            profile.Email = "עודכן";
            await api.UpdateUserProfileAsync(profile);
            await api.DeleteUserProfileAsync(profiles.Last().Id);

            // ===== Messages =====
            var messages = await api.GetAllMessagesAsync();
            Console.WriteLine($"Messages count: {messages.Count}");

            await api.InsertMessageAsync(new Messages { MessageText = "שלום" });
            var message = messages.First();
            message.MessageText = "עודכן";
            await api.UpdateMessageAsync(message);
            await api.DeleteMessageAsync(messages.Last().Id);

            // ===== Requests =====
            var requests = await api.GetAllRequestsAsync();
            Console.WriteLine($"Requests count: {requests.Count}");

            await api.InsertRequestAsync(new Requests { Status = "בקשה" });
            var request = requests.First();
            request.Status = "בקשה מעודכנת";
            await api.UpdateRequestAsync(request);
            await api.DeleteRequestAsync(requests.Last().Id);

            // ===== Reviews =====
            var reviews = await api.GetAllReviewsAsync();
            Console.WriteLine($"Reviews count: {reviews.Count}");

            await api.InsertReviewAsync(new Reviews { Rating = 5 });
            var review = reviews.First();
            review.Rating = 5;
            await api.UpdateReviewAsync(review);
            await api.DeleteReviewAsync(reviews.Last().Id);

            // ===== Schedule =====
            var schedules = await api.GetAllSchedulesAsync();
            Console.WriteLine($"Schedules count: {schedules.Count}");

            await api.InsertScheduleAsync(new Schedule { DayOfWeek = "Sunday" });
            var schedule = schedules.First();
            schedule.DayOfWeek = "Monday";
            await api.UpdateScheduleAsync(schedule);
            await api.DeleteScheduleAsync(schedules.Last().Id);

            // ===== JobHistory =====
            var jobs = await api.GetAllJobHistoryAsync();
            Console.WriteLine($"Jobs count: {jobs.Count}");

            Console.WriteLine(await api.InsertJobHistoryAsync(new JobHistory { TotalPayment = 10000 }));
            jobs = await api.GetAllJobHistoryAsync();
            var job = jobs.Last();
            job.TotalPayment = 10000;
            Console.WriteLine(await api.UpdateJobHistoryAsync(job));
            Console.WriteLine(await api.DeleteJobHistoryAsync(jobs.Last().Id));

            // ===== ChildOfParent =====
            var children = await api.GetAllChildrenOfParentsAsync();
            Console.WriteLine($"Children count: {children.Count}");

            Console.WriteLine(await api.InsertChildOfParentAsync(new ChildOfParent { FirstName = "נחום" }));
            children = await api.GetAllChildrenOfParentsAsync();
            var child = children.Last();
            child.FirstName = "מעודכן";
            Console.WriteLine(await api.UpdateChildOfParentAsync(child));
            Console.WriteLine(await api.DeleteChildOfParentAsync(children.Last().Id));

            Console.WriteLine("✔ All API tests finished");
            //    }

            //}


        }
    }
}
   
