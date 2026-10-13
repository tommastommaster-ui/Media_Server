using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MediaServer;
using Npgsql;

//datenbank kurz
const string dbConnect = "Host=localhost;Port=5432;Username=mrp;Password=1234!;Database=mrpdb";

string path = "/Users/simon/RiderProjects/MediaServer/MediaServer/usersDatenbank.json";

var users = new List<RegisterNewUser>();
if (File.Exists(path))
{
    users = JsonSerializer.Deserialize<List<RegisterNewUser>>(File.ReadAllText("usersDatenbank.json"));
    
    if (users == null)
    {
        users = new List<RegisterNewUser>();
    }
}



var listner = new HttpListener();
listner.Prefixes.Add("http://localhost:8080/");
listner.Start();
Console.WriteLine("Server startet");

await using var connectionDB = new NpgsqlConnection(dbConnect);
await connectionDB.OpenAsync();
Console.WriteLine("Verbunden mit DB!:)");

while (true)
{
    //hier den server am laufen halten
    var context = await listner.GetContextAsync();
    var request = context.Request;
    var response = context.Response;

    
    if (request.HttpMethod == "GET")
    {
        var buffer = Encoding.UTF8.GetBytes("Hello Server");
        response.StatusCode = 200;
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer);
    }

    if (request.HttpMethod == "POST" && request.Url!.AbsolutePath == "/api/users/register")
    {
        Console.WriteLine(request.ContentType);
        var data = await JsonSerializer.DeserializeAsync<RegisterNewUser>(request.InputStream);
        
        Console.WriteLine($"Username:{data?.Username}");
        Console.Write($"Password:{data?.Password}");
        users.Add(data);
        var json = JsonSerializer.Serialize(users);
        File.WriteAllText(path, json);

        await using var dataDb =
            new NpgsqlCommand("INSERT INTO users (username, password_hash) VALUES (@u, @p)", connectionDB);
        dataDb.Parameters.AddWithValue("u", data.Username);
        dataDb.Parameters.AddWithValue("p", data.Password);
        await dataDb.ExecuteNonQueryAsync();
        
        context.Response.Close();
        
    }
    
    if (request.HttpMethod == "POST" && request.Url!.AbsolutePath == "/api/users/login")
    {
        Console.WriteLine(request.ContentType);
        var data = await JsonSerializer.DeserializeAsync<RegisterNewUser>(request.InputStream);
        Console.WriteLine($"Username:{data?.Username}");
        Console.Write($"Password:{data?.Password}");
        context.Response.Close();
        
    }
    
    //auf postgres setzen und fertig
    
}