using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

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
            var body = new List<Dictionary<string, string>>();
            body.Add(new Dictionary<string, string>{
                {"user-plant-id", "23"},
                {"plant-name", "Michael"},
                {"plant-location", "Kitchen"},
                {"window-facing", "NE"},
                {"last-watered", "10-15-2022"},
                {"generic-name", "Venus Fly Trap"},
                {"scientific-name", "trapus venus"},
                {"sun", "indirect"},
                {"soil", "acidic"},
                {"type", "tropical"},
                {"water", "moist"}
            });
            body.Add(new Dictionary<string, string>{
                {"user-plant-id", "24"},
                {"plant-name", "Buddy"},
                {"plant-location", "Kitchen"},
                {"window-facing", "E"},
                {"last-watered", "10-15-2022"},
                {"generic-name", "Christmas Tree"},
                {"scientific-name", "arbus christmas"},
                {"sun", "direct"},
                {"soil", "none"},
                {"type", "festive"},
                {"water", "light misting"}
            });
            body.Add(new Dictionary<string, string>{
                {"user-plant-id", "25"},
                {"plant-name", "Liesl"},
                {"plant-location", "Office"},
                {"window-facing", "S"},
                {"last-watered", "10-15-2022"},
                {"generic-name", "Edelweiss"},
                {"scientific-name", "soundus musicus"},
                {"sun", "direct"},
                {"soil", "basic"},
                {"type", "alpine"},
                {"water", "dry top"}
            });

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
    }
}
