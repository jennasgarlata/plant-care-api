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

namespace Login
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
            
            var body = new Dictionary<string, string>();

            var dbCon = DbConnection.Instance();
            dbCon.Server = "plant-care-app-db.ckxkonakdsgz.us-east-1.rds.amazonaws.com";
            dbCon.DatabaseName = "plant_care_app";
            dbCon.UserName = "admin";
            dbCon.Password = "AZOIdQaqpRhc4gIJNGML";
            dbCon.Port = "3306";

            try {
                var userName = ExtractQueryParam(apigProxyEvent, "userName", true);
                var password = ExtractQueryParam(apigProxyEvent, "password", true);
        
                if (dbCon.IsConnect())
                {
                    string query = "USE plant_care_app;" +
                        "SELECT user_id, name, location FROM tbl_user WHERE user_name ='" + userName + "' AND password ='" + password + "';";
                    var cmd = new MySqlCommand(query, dbCon.Connection);
                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        body.Add("userid", GetSafeDbString("user_id", reader));
                        body.Add("name", GetSafeDbString("name", reader));
                        body.Add("location", GetSafeDbString("location", reader));
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
