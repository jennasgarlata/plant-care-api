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
            dbCon.Password = "AZOIdQaqpRhc4gIJNGML";
            dbCon.Port = "3306";
            try {
                if (dbCon.IsConnect())
                {
                    string query = "USE plant_care_app;" +
                        "UPDATE tbl_user_plant SET last_watered = NOW() WHERE user_plant_id = '" + userPlantId + "';";
                    var cmd = new MySqlCommand(query, dbCon.Connection);
                    var exec = cmd.ExecuteNonQuery();
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
    }
}
