using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace FightLambda;

public class Function
{
    /// <summary>
    /// A simple function that takes a string and does a ToUpper
    /// </summary>
    /// <param name="input"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        
        context.Logger.Log("OnConnect called, body: " + request.Body);
        return new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Body = "You are connected",
        };
        //OutputData toReturn = new OutputData();
        //toReturn.statusCode = 200;
        //return toReturn;
    }
}
