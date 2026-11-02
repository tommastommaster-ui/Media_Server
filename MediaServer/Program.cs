using System.Net;
using MediaServer;
using MediaServer.Handler;
using Npgsql;

//datenbank kurz
const string dbConnect = "Host=localhost;Port=5432;Username=postgres;Password=mysecretpassword;Database=postgres";


var listner = new HttpListener();
listner.Prefixes.Add("http://localhost:8080/");
listner.Start();
Console.WriteLine("Server startet");

await using var connectionDB = new NpgsqlConnection(dbConnect);
await connectionDB.OpenAsync();
Console.WriteLine("Verbunden mit DB!:)");
var auth = new AuthHandler();
var media = new MediaHandler();

while (true)
{
    //hier den server am laufen halten
    var context = await listner.GetContextAsync();
    var request = context.Request;

    
    if (request.HttpMethod == "POST" && request.Url!.AbsolutePath == "/api/users/register")
    {
        await auth.RegisterUserAsync(context, connectionDB);
        continue;
    }
    
    if (request.HttpMethod == "POST" && request.Url!.AbsolutePath == "/api/users/login")
    {
        await auth.LoginUserAsync(context, connectionDB);
        continue;
    }

    if (request.HttpMethod == "POST" && request.Url!.AbsolutePath == "/api/media")
    {
        await media.CreateMediaAsync(context, connectionDB);
        continue;
    }
    
    //genere problem weil über noch ander db machen muss

    string path = request.Url!.AbsolutePath;

    if (request.HttpMethod == "DELETE" && path.StartsWith("/api/media"))
    {
        await media.DelteMedia(context, connectionDB);
        continue;
    }
    
    if (request.HttpMethod == "PUT" && path.StartsWith("/api/media"))
    {
        await media.UpdateMedia(context, connectionDB);
        continue;
    }

    if (request.HttpMethod == "GET" && path.StartsWith("/api/media"))
    {
        await media.GetMediaAsync(context, connectionDB);
        continue;
    }
}