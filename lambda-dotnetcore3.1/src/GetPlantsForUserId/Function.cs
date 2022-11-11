using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

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

            var dbCon = DbConnection.Instance();
            dbCon.Server = "plant-care-app-db.ckxkonakdsgz.us-east-1.rds.amazonaws.com";
            dbCon.DatabaseName = "plant_care_app";
            dbCon.UserName = "admin";
            dbCon.Password = "ADD ME";
            dbCon.Port = "3306";

            if (dbCon.IsConnect())
            {
                string query = "USE plant_care_app;" +
                    "SELECT up.user_plant_id, " +
                        "up.plant_name," +
                        "up.plant_location, " +
                        "up.window_facing, " +
                        "up.last_watered, " +
                        "pd.generic_name, " +
                        "pd.scientific_name, " +
                        "c1.text as sun, " +
                        "c2.text as soil, " +
                        "c3.text as type_c, " +
                        "c4.text as water " +
                    "FROM tbl_user_plant up LEFT JOIN tbl_plant_data pd ON up.plant_data_ID = pd.plant_data_ID " +
                    "LEFT JOIN tbl_code c1 ON pd.sun_code = c1.code_id LEFT JOIN tbl_code c2 ON pd.soil_code = c2.code_id " +
                    "LEFT JOIN tbl_code c3 ON pd.type_code = c3.code_id LEFT JOIN tbl_code c4 ON pd.water_code = c4.code_id " +
                    "WHERE up.user_id = '" + userid + "';";
                var cmd = new MySqlCommand(query, dbCon.Connection);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var row = new Dictionary<string, string>();
                    row.Add("user-plant-id", reader.GetInt32("user_plant_id").ToString());
                    row.Add("plant-name", GetSafeDbString("plant_name", reader));
                    row.Add("plant-location", GetSafeDbString("plant_location", reader));
                    row.Add("window-facing", GetSafeDbString("window_facing", reader));
                    row.Add("last-watered", GetSafeDbString("last_watered", reader));
                    row.Add("generic-name", GetSafeDbString("generic_name", reader));
                    row.Add("scientific-name", GetSafeDbString("scientific_name", reader));
                    row.Add("sun", GetSafeDbString("sun", reader));
                    row.Add("soil", GetSafeDbString("soil", reader));
                    row.Add("type", GetSafeDbString("type_c", reader));
                    row.Add("water", GetSafeDbString("water", reader));

                    body.Add(row);
                }
                dbCon.Close();
            }

            // // body.Add(new Dictionary<string, string>{
            // //     {"user-plant-id", "23"},
            // //     {"plant-name", "Michael"},
            // //     {"plant-location", "Kitchen"},
            // //     {"window-facing", "NE"},
            // //     {"last-watered", "10-15-2022"},
            // //     {"generic-name", "Venus Fly Trap"},
            // //     {"scientific-name", "trapus venus"},
            // //     {"sun", "indirect"},
            // //     {"soil", "acidic"},
            // //     {"type", "tropical"},
            // //     {"water", "moist"}
            // // });
            // // body.Add(new Dictionary<string, string>{
            // //     {"user-plant-id", "24"},
            // //     {"plant-name", "Buddy"},
            // //     {"plant-location", "Kitchen"},
            // //     {"window-facing", "E"},
            // //     {"last-watered", "10-15-2022"},
            // //     {"generic-name", "Christmas Tree"},
            // //     {"scientific-name", "arbus christmas"},
            // //     {"sun", "direct"},
            // //     {"soil", "none"},
            // //     {"type", "festive"},
            // //     {"water", "light misting"}
            // // });
            // // body.Add(new Dictionary<string, string>{
            // //     {"user-plant-id", "25"},
            // //     {"plant-name", "Liesl"},
            // //     {"plant-location", "Office"},
            // //     {"window-facing", "S"},
            // //     {"last-watered", "10-15-2022"},
            // //     {"generic-name", "Edelweiss"},
            // //     {"scientific-name", "soundus musicus"},
            // //     {"sun", "direct"},
            // //     {"soil", "basic"},
            // //     {"type", "alpine"},
            // //     {"water", "dry top"}
            // // });

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
                    return reader.GetDateTime(ordinal).ToLongDateString();
                } else
                {
                    return reader.GetString(ordinal);
                }
                
            }
        }
    }
}
