using System.Net;
using System.Text;

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
    
}