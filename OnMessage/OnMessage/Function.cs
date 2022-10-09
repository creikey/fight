using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.ApiGatewayManagementApi;
using Amazon.ApiGatewayManagementApi.Model;
using Amazon.DynamoDBv2;
using System.Text;
using Amazon.Runtime.Internal;
using System.Diagnostics;
using MessageTypes;
using Newtonsoft.Json;



// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace OnMessage;



public class Function
{
    const string TableName = "fight";
    const string LobbiesTableName = "fight-lobbies";
    const int MaxLobbies = 1000; // start at 1000 so always 3 digits
    const int InitialLobby = 100;

    private async Task sendTo(APIGatewayProxyRequest request, string connectionId, dynamic toSend)
    {
        var client = new AmazonApiGatewayManagementApiClient(new AmazonApiGatewayManagementApiConfig()
        {
            ServiceURL = "https://" + request.RequestContext.DomainName + "/" + request.RequestContext.Stage
        });
        MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(toSend, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
        })));
        PostToConnectionRequest postRequest = new PostToConnectionRequest()
        {
            ConnectionId = connectionId,
            Data = stream
        };

        await client.PostToConnectionAsync(postRequest);
    }

    private async Task sendSelf(APIGatewayProxyRequest request, dynamic toSend)
    {
        await sendTo(request, request.RequestContext.ConnectionId, toSend);
    }

    /// <summary>
    /// A simple function that takes a string and does a ToUpper
    /// </summary>
    /// <param name="input"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            if (request.Body.Length > 1000)
            {
                throw new Exception("Body too big");
            }

            ClientToServer msg = Newtonsoft.Json.JsonConvert.DeserializeObject<ClientToServer>(request.Body);
            if (msg.uuid == "server") throw new Exception("Not allowed to access or modify server uuid");

            Amazon.DynamoDBv2.AmazonDynamoDBClient client = new Amazon.DynamoDBv2.AmazonDynamoDBClient();
            switch (msg.type)
            {
                case ClientToServer.Type.JoinLobby:
                    if (msg.lobbyToJoin == null || msg.lobbyToJoin == "") throw new Exception("lobbyToJoin must be not null and have length greater than 0");
                    Amazon.DynamoDBv2.Model.GetItemResponse lobbyToHost = await client.GetItemAsync(LobbiesTableName, new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>()
                    {
                        {"lobby", new Amazon.DynamoDBv2.Model.AttributeValue(msg.lobbyToJoin)},
                    });
                    string otheruuid = lobbyToHost.Item["host"].S;

                    await client.PutItemAsync(TableName, new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>()
                    {
                        {"uuid", new Amazon.DynamoDBv2.Model.AttributeValue(msg.uuid)},
                        {"connectionId", new Amazon.DynamoDBv2.Model.AttributeValue(request.RequestContext.ConnectionId)},
                        {"otherPlayer", new Amazon.DynamoDBv2.Model.AttributeValue(otheruuid)},
                    });
                    await client.UpdateItemAsync(TableName, new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>()
                    {
                        {"uuid", new Amazon.DynamoDBv2.Model.AttributeValue(otheruuid)},
                    }, new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValueUpdate>()
                    {
                        {"otherPlayer", new Amazon.DynamoDBv2.Model.AttributeValueUpdate(new Amazon.DynamoDBv2.Model.AttributeValue(msg.uuid), Amazon.DynamoDBv2.AttributeAction.PUT)},
                    });

                    Amazon.DynamoDBv2.Model.GetItemResponse hostData = await client.GetItemAsync(TableName, new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>()
                    {
                        {"uuid", new Amazon.DynamoDBv2.Model.AttributeValue(otheruuid)},
                    });
                    var msgToSend = new ServerToClient
                    {
                        type = ServerToClient.Type.LobbyReadyToPlay
                    };
                    await sendTo(request, hostData.Item["connectionId"].S, msgToSend);
                    await sendSelf(request, msgToSend);
                    break;

                case ClientToServer.Type.CreateLobby:
                    const string lastLobbyKey = "lastLobby";
                    Amazon.DynamoDBv2.Model.GetItemResponse existingData = await client.GetItemAsync(TableName, new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>()
                    {
                        {"uuid", new Amazon.DynamoDBv2.Model.AttributeValue("server")},
                    });
                    int lastLobby = InitialLobby;
                    if(existingData.Item.ContainsKey(lastLobbyKey))
                    {
                        lastLobby = Int32.Parse(existingData.Item[lastLobbyKey].S);
                    }
                    lastLobby += 1;
                    lastLobby %= MaxLobbies;
                    string newLobbyCode = lastLobby.ToString();

                    // @Perf could batch these together
                    await client.PutItemAsync(TableName, new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>()
                    {
                        {"uuid", new Amazon.DynamoDBv2.Model.AttributeValue("server")},
                        {lastLobbyKey, new Amazon.DynamoDBv2.Model.AttributeValue(newLobbyCode)},
                    });
                    if (msg.uuid == null || msg.uuid.Length < 3) throw new Exception("Invalid uuid, must exist and be greater than 3 characters");
                    await client.PutItemAsync(TableName, new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>()
                    {
                        {"uuid", new Amazon.DynamoDBv2.Model.AttributeValue(msg.uuid)},
                        {"connectionId", new Amazon.DynamoDBv2.Model.AttributeValue(request.RequestContext.ConnectionId)},
                        {"otherPlayer", new Amazon.DynamoDBv2.Model.AttributeValue("")},
                    });
                    await client.PutItemAsync(LobbiesTableName, new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>()
                    {
                        {"lobby", new Amazon.DynamoDBv2.Model.AttributeValue(newLobbyCode)},
                        {"host", new Amazon.DynamoDBv2.Model.AttributeValue(msg.uuid)},
                    });
                    await sendSelf(request, new ServerToClient
                    {
                        type = ServerToClient.Type.CreatedLobby,
                        lobbyCode = newLobbyCode,
                    });
                    break;

                case ClientToServer.Type.MyRoundInput:
                    Amazon.DynamoDBv2.Model.GetItemResponse myData = await client.GetItemAsync(TableName, new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>()
                    {
                        {"uuid", new Amazon.DynamoDBv2.Model.AttributeValue(msg.uuid)},
                    });
                    Amazon.DynamoDBv2.Model.GetItemResponse otherData = await client.GetItemAsync(TableName, new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>()
                    {
                        {"uuid", new Amazon.DynamoDBv2.Model.AttributeValue(myData.Item["otherPlayer"].S)},
                    });

                    await sendTo(request, otherData.Item["connectionId"].S, new ServerToClient
                    {
                        type = ServerToClient.Type.RoundInput,
                        otherPlayerRoundInput = msg.roundInput,
                    });
                    
                    break;
            }
        }
        catch (AggregateException exceptions)
        {
            foreach (var ex in exceptions.InnerExceptions)
            {
                // Get stack trace for the exception with source file information
                var st = new StackTrace(ex, true);
                // Get the top stack frame
                var frame = st.GetFrame(st.FrameCount - 1);
                // Get the line number from the stack frame
                int lineNumber = frame.GetFileLineNumber();

                await sendSelf(request, new ServerToClient
                {
                    type = ServerToClient.Type.Error,
                    errorMessage = "Async error on line " + lineNumber + ": " + ex.Message,
                });
            }
        }
        catch (Exception ex)
        {
            // Get stack trace for the exception with source file information
            var st = new StackTrace(ex, true);
            // Get the top stack frame
            var frame = st.GetFrame(st.FrameCount - 1);
            // Get the line number from the stack frame
            int lineNumber = frame.GetFileLineNumber();

            await sendSelf(request, new ServerToClient
            {
                type = ServerToClient.Type.Error,
                errorMessage = "Error on line " + lineNumber + ": " + ex.Message,
            });
        }

        // keep connected always
        return new APIGatewayProxyResponse
        {
            StatusCode = 200,
        };
    }
}
