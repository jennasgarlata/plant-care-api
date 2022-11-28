using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace CreateUser
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
            try {
                var location = await GetCallingIP();
                var userName = ExtractQueryParam(apigProxyEvent, "userName", true);
                var name = ExtractQueryParam(apigProxyEvent, "name", true);
                var password = ExtractQueryParam(apigProxyEvent, "password", true);
                var locationParam = ExtractQueryParam(apigProxyEvent, "location", true);

                var dbCon = DbConnection.Instance();
                dbCon.Server = "plant-care-app-db.ckxkonakdsgz.us-east-1.rds.amazonaws.com";
                dbCon.DatabaseName = "plant_care_app";
                dbCon.UserName = "admin";
                dbCon.Password = "AZOIdQaqpRhc4gIJNGML";
                dbCon.Port = "3306";
                var userid = -1;
                try {
                    if (dbCon.IsConnect()) 
                    {
                        string query = "INSERT INTO plant_care_app.tbl_user " + 
                        "(user_name, name, password, location) " +
                        "VALUES (@user_name, @name, @password, @location);";
                        var cmd = new MySqlCommand(query, dbCon.Connection);
                        
                        cmd.Parameters.AddWithValue("@user_name", userName);
                        cmd.Parameters.AddWithValue("@name", name);
                        cmd.Parameters.AddWithValue("@password", password);
                        cmd.Parameters.AddWithValue("@location", locationParam);
                        cmd.Prepare();
                        var reader = cmd.ExecuteNonQuery();
                        
                    }
                } finally {
                    dbCon.Close();
                }

                return new APIGatewayProxyResponse
                {
                    Body = JsonSerializer.Serialize("User successfully added with user_id: " + userid),
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
            } catch (System.Exception e){
                
                return new APIGatewayProxyResponse
                {
                    Body = JsonSerializer.Serialize(e.Message),
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

        private string ExtractQueryParam(APIGatewayProxyRequest apigProxyEvent, string paramName, bool required){
            var paramVal = "";
            if (apigProxyEvent.QueryStringParameters.ContainsKey(paramName))
            {
                if (!apigProxyEvent.QueryStringParameters.TryGetValue(paramName, out paramVal)){
                    if (required){
                        throw new System.Exception("Missing param: " + paramName);
                    }
                }
            } else if (required){
                throw new System.Exception("Missing param: " + paramName);
            }
            return paramVal;
        }
    }
}
