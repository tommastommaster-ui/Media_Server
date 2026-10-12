using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MediaServer;

var listner = new HttpListener();
listner.Prefixes.Add("http://localhost:8080/");
listner.Start();
Console.WriteLine("Server startet");

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

    if (request.HttpMethod == "POST")
    {
        Console.WriteLine(request.ContentType);
        var data = await JsonSerializer.DeserializeAsync<RegisterNewUser>(request.InputStream);
        Console.WriteLine($"Username:{data?.Username}");
        Console.Write($"Password:{data?.Password}");
        context.Response.Close();
        
    }
    
}