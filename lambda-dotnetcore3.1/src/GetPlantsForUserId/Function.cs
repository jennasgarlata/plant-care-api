using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Newtonsoft.Json.Linq;
using System.Linq;
using System;
using System.Globalization;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace GetPlantsForUserId
{

    public class Function
    {

        private static readonly HttpClient client = new HttpClient();

        private static async Task<string> GetCallingIP()
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("User-Agent", "AWS Lambda .Net Client");

            var msg = await client.GetStringAsync("http://checkip.amazonaws.com/").ConfigureAwait(continueOnCapturedContext:false);

            return msg.Replace("\n","");
        }

        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
        {
            var location = await GetCallingIP();
            var userid = "0";
            if (apigProxyEvent.QueryStringParameters.ContainsKey("userid"))
            {
                if (!apigProxyEvent.QueryStringParameters.TryGetValue("userid", out userid)){
                    return new APIGatewayProxyResponse
                    {
                        Body = JsonSerializer.Serialize("Could not find parameter."),
                        StatusCode = 400,
                        Headers = new Dictionary<string, string> { 
                            { "Content-Type", "application/json" },
                            { "Access-Control-Allow-Headers", "Content-Type,X-Amz-Date,Authorization,X-Api-Key,X-Amz-Security-Token" },
                            { "Access-Control-Allow-Methods", "GET,PUT,POST,DELETE,HEAD,OPTIONS" },
                            { "Access-Control-Allow-Credentials", "true" },
                            { "Access-Control-Allow-Origin", "*" },
                            { "X-Requested-With", "*" }
                        }
                    };
                }
            }
            var body = new List<Dictionary<string, string>>();
            var zipCode = "60614";

            var dbCon = DbConnection.Instance();
            dbCon.Server = "plant-care-app-db.ckxkonakdsgz.us-east-1.rds.amazonaws.com";
            dbCon.DatabaseName = "plant_care_app";
            dbCon.UserName = "admin";
            dbCon.Password = "AZOIdQaqpRhc4gIJNGML";
            dbCon.Port = "3306";

            try {
                if (dbCon.IsConnect())
                {
                    string query = "USE plant_care_app; SELECT location FROM tbl_user WHERE user_id = '" + userid + "';";
                    var cmd = new MySqlCommand(query, dbCon.Connection);
                    var reader = cmd.ExecuteReader();
                    
                    if (reader.Read())
                    {
                        zipCode = GetSafeDbString("location", reader);
                    }
                    dbCon.Close();
                }

                Weather currentWeather = GetCurrentWeatherForLocation(zipCode).Result;

                if (dbCon.IsConnect())
                {
                    string query = "USE plant_care_app;" +
                        "SELECT up.user_plant_id, " +
                            "up.plant_name, " +
                            "up.plant_location, " +
                            "up.window_facing, " +
                            "up.last_watered, " +
                            "pd.generic_name, " +
                            "pd.scientific_name, " +
                            "c1.text as sun, " +
                            "c2.text as soil, " +
                            "c3.text as type_c, " +
                            "c4.text as water, " +
                            "pd.water_code as water_code " +
                        "FROM tbl_user_plant up LEFT JOIN tbl_plant_data pd ON up.plant_data_ID = pd.plant_data_ID " +
                        "LEFT JOIN tbl_code c1 ON pd.sun_code = c1.code_id LEFT JOIN tbl_code c2 ON pd.soil_code = c2.code_id " +
                        "LEFT JOIN tbl_code c3 ON pd.type_code = c3.code_id LEFT JOIN tbl_code c4 ON pd.water_code = c4.code_id " +
                        "WHERE up.user_id = '" + userid + "';";
                    var cmd = new MySqlCommand(query, dbCon.Connection);
                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        var row = new Dictionary<string, string>();
                        var lastWatered = GetSafeDbDateTime("last_watered", reader);
                        var wateringCode = GetSafeDbString("water_code", reader);
                        row.Add("user-plant-id", reader.GetInt32("user_plant_id").ToString());
                        row.Add("plant-name", GetSafeDbString("plant_name", reader));
                        row.Add("plant-location", GetSafeDbString("plant_location", reader));
                        row.Add("window-facing", GetSafeDbString("window_facing", reader));
                        row.Add("last-watered", lastWatered);
                        row.Add("generic-name", GetSafeDbString("generic_name", reader));
                        row.Add("scientific-name", GetSafeDbString("scientific_name", reader));
                        row.Add("sun", GetSafeDbString("sun", reader));
                        row.Add("soil", GetSafeDbString("soil", reader));
                        row.Add("type", GetSafeDbString("type_c", reader));
                        row.Add("water", GetSafeDbString("water", reader));
                        row.Add("next-watering", EstimateNextWatering(lastWatered, wateringCode, currentWeather));
                        body.Add(row);
                    }
                    
                }
            }
            catch (Exception e){
                return new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(e.Message),
                StatusCode = 400,
                Headers = new Dictionary<string, string> { 
                    { "Content-Type", "application/json" },
                    { "Access-Control-Allow-Headers", "Content-Type,X-Amz-Date,Authorization,X-Api-Key,X-Amz-Security-Token" },
                    { "Access-Control-Allow-Methods", "GET,PUT,POST,DELETE,HEAD,OPTIONS" },
                    { "Access-Control-Allow-Credentials", "true" },
                    { "Access-Control-Allow-Origin", "*" },
                    { "X-Requested-With", "*" }
                }
            };
            }
            finally {
                dbCon.Close();
            }

            return new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(body),
                StatusCode = 200,
                Headers = new Dictionary<string, string> { 
                    { "Content-Type", "application/json" },
                    { "Access-Control-Allow-Headers", "Content-Type,X-Amz-Date,Authorization,X-Api-Key,X-Amz-Security-Token" },
                    { "Access-Control-Allow-Methods", "GET,PUT,POST,DELETE,HEAD,OPTIONS" },
                    { "Access-Control-Allow-Credentials", "true" },
                    { "Access-Control-Allow-Origin", "*" },
                    { "X-Requested-With", "*" }
                }
            };
        }
        private string GetSafeDbDateTime(string field, MySqlDataReader reader)
        {
            int ordinal = reader.GetOrdinal(field);
            try
            {
                if (reader.IsDBNull(ordinal))
                {
                    return "";
                }
                else
                {
                    return reader.GetDateTime(ordinal).ToShortDateString();
                }
            } catch (Exception e)
            {
                return "";
            }
            
        }
        private string GetSafeDbString(string field, MySqlDataReader reader)
        {
            int ordinal = reader.GetOrdinal(field);
    
            if (reader.IsDBNull(ordinal))
            {
                return "";
            } else
            {
                if (reader.GetDataTypeName(ordinal).Equals(MySqlDbType.DateTime))
                {
                    return reader.GetDateTime(ordinal).ToString();
                } else
                {
                    return reader.GetString(ordinal);
                }
                
            }
        }
        
        private async Task<Weather> GetCurrentWeatherForLocation(string zipCode)
        {
            var weather = new Weather();
            var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("https://weatherapi-com.p.rapidapi.com/forecast.json?q=" + zipCode + "&days=1"),

                Headers =
                {
                    { "X-RapidAPI-Key", "ff0bd133d1mshd2fafdbf4371986p15c311jsn52941a7ac5af" },
                    { "X-RapidAPI-Host", "weatherapi-com.p.rapidapi.com" },
                },
            };
            using (var response = await client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                JObject result = JObject.Parse(body);
                var forecastDays = result["forecast"]["forecastday"].Children().ToArray();
                ForecastDay currentForcastDay = forecastDays[0].ToObject<ForecastDay>();
                weather = currentForcastDay.day;
            }

            return weather;
        }

        private string EstimateNextWatering(string lastWaterDate, string wateringCode, Weather currentWeather)
        {
            if (lastWaterDate.Equals(""))
            {
                //if not watered in the past, water today!
                return DateTime.Today.ToShortDateString();
            }
            //get standard benchmarks for watering
            var daysBetweenWatering = 0;
            switch (wateringCode)
            {
                case "5MD":
                    daysBetweenWatering = 5;
                    break;
                case "5DRY":
                    daysBetweenWatering = 15;
                    break;
                case "0DRY":
                    daysBetweenWatering = 30;
                    break;
                default:
                    daysBetweenWatering = 7;
                    break;
            }


            //set average humidty and temperature from that endpoint
            //todo remove hardcoding
            var averageHumidity = currentWeather.avghumidity;
            var averageTemperature = currentWeather.avgtemp_f;

            //Create calendar to maniuplate dates
            DateTime date = DateTime.Parse(lastWaterDate);
            Calendar cal = CultureInfo.InvariantCulture.Calendar;

            //Set default next water date
            date = cal.AddDays(date, daysBetweenWatering);

            //HUMIDITY 
            if (averageHumidity >= 0 && averageHumidity < 20)
            {
                date = cal.AddDays(date, -2);
            }
            else if (averageHumidity >= 20 && averageHumidity < 40)
            {
                date = cal.AddDays(date, -1);
            }
            else if (averageHumidity >= 60 && averageHumidity < 80)
            {
                date = cal.AddDays(date, 1);
            }
            else if (averageHumidity >= 80 && averageHumidity < 100)
            {
                date = cal.AddDays(date, 2);
            }

            //TEMPERATURE
            if (averageTemperature < 0)
            {
                date = cal.AddDays(date, -3);
            }
            else if (averageTemperature >= 0 && averageTemperature < 20)
            {
                date = cal.AddDays(date, -2);
            }
            else if (averageTemperature >= 20 && averageTemperature < 40)
            {
                date = cal.AddDays(date, -1);
            }
            else if (averageTemperature >= 60 && averageTemperature < 80)
            {
                date = cal.AddDays(date, 1);
            }
            else if (averageTemperature >= 80 && averageTemperature < 100)
            {
                date = cal.AddDays(date, 2);
            }
            else if (averageTemperature >= 100)
            {
                date = cal.AddDays(date, 3);
            }

            return date.ToShortDateString();
        }
    }
}
