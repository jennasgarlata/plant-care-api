using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace WaterUserPlant
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
            var userPlantId = "0";
            if (apigProxyEvent.QueryStringParameters.ContainsKey("userPlantId"))
            {
                if (!apigProxyEvent.QueryStringParameters.TryGetValue("userPlantId", out userPlantId)){
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
            dbCon.Password = "";
            dbCon.Port = "3306";
            try {
                if (dbCon.IsConnect())
                {
                    string query = "USE plant_care_app;" +
                        "UPDATE tbl_user_plant SET last_watered = NOW() WHERE user_plant_id = '" + userPlantId + "';";
                    
                    // string query2 = "USE plant_care_app;" + 
                    //     "SELECT * from tbl_user_plant WHERE user_plant_id = '" + userPlantId + "';";
                    var cmd = new MySqlCommand(query, dbCon.Connection);
                    var exec = cmd.ExecuteNonQuery();
                    // var cmd2 = new MySqlCommand(query2, dbCon.Connection);
                    // var reader = cmd2.ExecuteReader();
                    // while (reader.Read()){
                    //     var row = new Dictionary<string, string>();
                    //     row.Add("user-plant-id", reader.GetInt32("user_plant_id").ToString());
                    //     row.Add("plant-name", GetSafeDbString("plant_name", reader));
                    //     row.Add("plant-location", GetSafeDbString("plant_location", reader));
                    //     row.Add("window-facing", GetSafeDbString("window_facing", reader));
                    //     row.Add("last-watered", GetSafeDbDateTime("last_watered", reader));
                    //     body.Add(row);
                    // }
                }
            } finally {
                dbCon.Close();
            }
            

            return new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize("Watered userplant: " + userPlantId),
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

        // private string GetSafeDbDateTime(string field, MySqlDataReader reader)
        // {
        //     int ordinal = reader.GetOrdinal(field);
        //     try
        //     {
        //         if (reader.IsDBNull(ordinal))
        //         {
        //             return "";
        //         }
        //         else
        //         {
        //             return reader.GetDateTime(ordinal).ToShortDateString();
        //         }
        //     } catch (Exception e)
        //     {
        //         return "";
        //     }
            
        // }
        // private string GetSafeDbString(string field, MySqlDataReader reader)
        // {
        //     int ordinal = reader.GetOrdinal(field);
    
        //     if (reader.IsDBNull(ordinal))
        //     {
        //         return "";
        //     } else
        //     {
        //         if (reader.GetDataTypeName(ordinal).Equals(MySqlDbType.DateTime))
        //         {
        //             return reader.GetDateTime(ordinal).ToString();
        //         } else
        //         {
        //             return reader.GetString(ordinal);
        //         }
                
        //     }
        // }
    }
}
