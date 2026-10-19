using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MediaServer;
using Npgsql;

//datenbank kurz
const string dbConnect = "Host=localhost;Port=5432;Username=postgres;Password=mysecretpassword;Database=postgres";

string path = "/Users/simon/RiderProjects/MediaServer/MediaServer/usersDatenbank.json";

var users = new List<CredUser>();
if (File.Exists(path))
{
    users = JsonSerializer.Deserialize<List<CredUser>>(File.ReadAllText("usersDatenbank.json"));
    
    if (users == null)
    {
        users = new List<CredUser>();
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
        var data = await JsonSerializer.DeserializeAsync<CredUser>(request.InputStream);
        Console.WriteLine($"Username:{data?.Username}");
        Console.Write($"Password:{data?.Password}");
        users.Add(data);
        var json = JsonSerializer.Serialize(users);
        File.WriteAllText(path, json);

        try
        {
            await using var dataDb = new NpgsqlCommand("INSERT INTO users (username, hash_password) VALUES (@u, @p)", connectionDB);
            string hash = BCrypt.Net.BCrypt.HashPassword(data.Password);
            dataDb.Parameters.AddWithValue("u", data.Username);
            dataDb.Parameters.AddWithValue("p", hash);
            await dataDb.ExecuteNonQueryAsync();

            context.Response.StatusCode = 201;
        }
        catch (PostgresException exception) when (exception.SqlState == "23505")
        {
            context.Response.StatusCode = 409;
            Console.WriteLine("Error username is already in db");
        }
        context.Response.Close();
    }
    
    if (request.HttpMethod == "POST" && request.Url!.AbsolutePath == "/api/users/login")
    {
        var data = await JsonSerializer.DeserializeAsync<CredUser>(request.InputStream);
        if (data == null)
        {
            context.Response.StatusCode = 400;
            context.Response.Close();
            continue;
        }
        Console.WriteLine($"Username:{data?.Username}");
        Console.Write($"Password:{data?.Password}");
        try
        {
            await using var dataDb =
                new NpgsqlCommand("SELECT id, hash_password FROM users WHERE username = @u", connectionDB);
            dataDb.Parameters.AddWithValue("u", data.Username);
            await using var userExists = await dataDb.ExecuteReaderAsync();

            if (await userExists.ReadAsync() == false)
            {
                //username nicht in db
                context.Response.StatusCode = 401;
            }
            else
            {
                string passwordFromUser = userExists.GetString(1);
                if (BCrypt.Net.BCrypt.Verify(data.Password, passwordFromUser) == false)
                {
                    context.Response.StatusCode = 401;
                }
                else
                {
                    context.Response.StatusCode = 200;
                }
            }

            //passwort von der zeile wo user ist
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        context.Response.Close();
        
    }
 
    
}