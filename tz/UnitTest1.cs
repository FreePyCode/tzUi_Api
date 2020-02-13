using NUnit.Framework;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace Tests
{
    public class GeoPoint
    {
        public string title;
        public string location_type;
        public int woeid;
        public string latt_long;
    }
    
    public class WeatherState
    {
        public string weather_state_name;
        public float min_temp;
        public float max_temp;
        public float the_temp;
        public string applicable_date;
    }
    public class ApiTests
    {
        public string baseUri = "https://www.metaweather.com/api";
        static HttpClient client = new HttpClient();
        public async Task<JToken> GetJToken(string Uri)
        {
            string json = await client.GetStringAsync(Uri);
            var result = JToken.Parse(json);
            return result;
        }
        public bool weatherValid(WeatherState weather)
        {
            int x = int.Parse(weather.applicable_date.Split("-")[1]);
            if ((x < 3) || (x == 12))
            {
                return weather.the_temp < 0;
            }
            else if ((9 < x) && (x > 5))
            {
                return weather.the_temp > 0;
            }
            else 
            {
                return (weather.the_temp > 1) && (weather.the_temp < 20);
            }
        }
        public List<T> jToList<T>(JToken JList)
        {
            var resultList = new List<T>();
            foreach (var item in JList)
            {
                resultList.Add(item.ToObject<T>());
            }
            return resultList;
        }
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        [TestCase("min", "53.90255,27.563101")]
        public void PaternQueruNoNull(string pattern, string real_lat_long)
        {
            string mainTestErr = "Coordinates don't match";
            string LocationNull = "Location is null";

            string Uri = $"{baseUri}/location/search/?query={pattern}";
            var task = GetJToken(Uri);
            task.Wait();
            var jGeoPoints = task.Result;
            var geoPoints = jToList<GeoPoint>(jGeoPoints);
            var Point = geoPoints.Find((x) => x.title == "Minsk");
            Assert.IsNotNull(Point, LocationNull);
            Assert.IsTrue(Point?.latt_long == real_lat_long, mainTestErr);
        }
        
        [Test]

        [TestCase("834463")]
        public void TemperatureRange(string pattern)
        {
            string ErrorMessage = "The temperature is not within the specified range";
            string Uri = $"{baseUri}/location/{pattern}";
            var task = GetJToken(Uri);
            task.Wait();
            var jWeatherList = task.Result["consolidated_weather"];
            var weatherList = jToList<WeatherState>(jWeatherList);
            Assert.IsTrue(weatherList.TrueForAll((x) => weatherValid(x)), ErrorMessage);
        }

        [Test]
        [TestCase("834463")]//Сайт не дает информацию по минску 5 лет назад
        [TestCase("44418")]//А это Лондон информация о нем есть на сайте и погода совпадает (13:02:2020)
        public void FoundMatches(string woeid)
        {
            string mainTestErr = "no matches found";
            var today = (DateTime.Today);
            string gg = today.ToString("yyyy-MM-dd");
            var fiveYearsAgo = today.AddYears(-5);

            string fiveYearsAgoUri = $"{baseUri}/location/{woeid}/{fiveYearsAgo.ToString("yyyy'/'MM'/'dd")}";
            string todayUri = $"{baseUri}/location/{woeid}";

            var task2 = GetJToken(todayUri);
            var task = GetJToken(fiveYearsAgoUri);
            task.Wait();
            task2.Wait();

            var jWeatherList = task2.Result["consolidated_weather"];
            var weatherList = jToList<WeatherState>(jWeatherList);
            var todayWheather = weatherList.Find((x) => x.applicable_date == gg)?.weather_state_name;
            var jGeoPoints = task.Result;
            var geoPoints = jToList<WeatherState>(jGeoPoints);
            Assert.NotNull(geoPoints.Find((x) => x.weather_state_name == todayWheather), mainTestErr);
        }
    }
    public class UiTests
    {
        [Test]
        public void LangCurrencySwitch()
        {
            using (IWebDriver driver = new ChromeDriver())
            {
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
                driver.Navigate().GoToUrl("https://www.booking.com/");
                driver.FindElement(By.XPath(".//*[@data-id='language_selector']")).Click();
                driver.FindElement(By.XPath(".//*[@class='lang_en-us']")).Click();
                driver.FindElement(By.XPath(".//*[@data-id='currency_selector']")).Click();
                driver.FindElement(By.XPath(".//*[@class='currency_USD']")).Click();
            }
        }

        [Test]
        public void GoToPurchaseTicketPageTest()
        {
            using (IWebDriver driver = new ChromeDriver())
            {
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
                driver.Navigate().GoToUrl("https://www.booking.com/");
                driver.FindElement(By.XPath(".//*[@data-decider-header='flights']")).Click();
                driver.FindElement(By.XPath(".//*[contains(@class,'FlightSearchForm')]"));//поиск элемента на странице выбора билетов (если не нашло соответственно там страница с ошибкой или иная страница)
                var result = driver.Url;
                Assert.IsTrue(result.Contains("https://booking.kayak.com/"));
            }
        }

        [Test]
        public void dashboardTest()
        {
            using (IWebDriver driver = new ChromeDriver())
            {
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
                driver.Navigate().GoToUrl("https://www.booking.com/");
                driver.FindElement(By.XPath(".//*[contains(@class,'account_register_option')]")).Click();
                driver.FindElement(By.XPath(".//*[@id='username']")).SendKeys("stet2021@list.ru" + Keys.Enter);
                driver.FindElement(By.XPath(".//*[@id='password']")).SendKeys("simulacres" + Keys.Enter);
                driver.FindElement(By.XPath(".//*[@data-command='show-profile-menu']")).Click();
                driver.FindElement(By.XPath(".//*[contains(@class,'--mydashboard')]")).Click();
                driver.FindElement(By.XPath(".//*[contains(@class,'profile-content-card__title')]"));//поиск элемента на странице личного кабинета (если не нашло соответственно там страница с ошибкой или иная страница)
                var result = driver.Url;
                Assert.IsTrue(result.Contains("https://secure.booking.com/mydashboard"));
            }
        }

        [Test]
        //[TestCase(5, 5, 5)]
        //[TestCase(1, 0, 1)]
        [TestCase(2, 1, 1)]
        //[TestCase(1, 1, 1)]
        public void FilterTest(int Adults, int Children, int rooms)
        {
            void modFieldset(IWebDriver webDriver, IWebElement element, int count)
            {
                var text = element.FindElement(By.XPath(".//*[contains(@data-bui-ref,'input-stepper-value')]")).Text;
                var subButton = element.FindElement(By.XPath(".//*[contains(@class,'bui-stepper__subtract-button')]"));
                var addButton = element.FindElement(By.XPath(".//*[contains(@class,'bui-stepper__add-button')]"));
                int realCount = int.Parse(text);
                if (realCount > count)
                {
                    for (int i = realCount; i > count; i--)
                    {
                        subButton.Click();
                    }
                }
                else if (realCount < count)
                {
                    for (int i = realCount; i < count; i++)
                    { 
                        addButton.Click();
                    }
                }

            }
            using (IWebDriver driver = new ChromeDriver())
            {
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
                driver.Navigate().GoToUrl("https://www.booking.com/");
                driver.FindElement(By.XPath(".//input[@name='ss']")).SendKeys("Прага");
                driver.FindElement(By.XPath(".//*[@class='xp__guests__count']")).Click();
                var childrenElement = driver.FindElement(By.XPath(".//div[contains(@class,'sb-group-children')]"));
                modFieldset(driver, childrenElement, Children);
                var adultsElement = driver.FindElement(By.XPath(".//div[contains(@class,'sb-group__field-adults')]"));
                modFieldset(driver, adultsElement, Adults);
                var roomElement = driver.FindElement(By.XPath(".//div[contains(@class,'sb-group__field-room')]")); 
                modFieldset(driver, roomElement, rooms);
                var begin = (DateTime.Today).AddDays(7).ToString("yyyy-MM-dd");
                var end = (DateTime.Today).AddDays(9).ToString("yyyy-MM-dd");
                driver.FindElement(By.XPath(".//*[contains(@class,'xp__dates-inner xp__dates__checkin')]")).Click();
                driver.FindElement(By.XPath($".//*[contains(@data-date,'{begin}')]")).Click(); 
                driver.FindElement(By.XPath($".//*[contains(@data-date,'{end}')]")).Click();
                driver.FindElement(By.XPath(".//*[contains(@class,'searchbox__button')]")).Click();
                driver.FindElement(By.XPath(".//*[contains(@class,'sort_category')]"));
            }
        }
    }
}