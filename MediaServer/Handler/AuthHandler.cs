using System.Net;
using Npgsql;
using System.Text.Json;

namespace MediaServer;

public class AuthHandler
{
    public async Task RegisterUserAsync(HttpListenerContext context, NpgsqlConnection connectionDB)
    {

        try
        {
            var data = await JsonSerializer.DeserializeAsync<CredUser>(context.Request.InputStream);

            await using var registerUser =
                new NpgsqlCommand("INSERT INTO users (username, hash_password) VALUES (@u, @p)", connectionDB);
            if (data == null || string.IsNullOrWhiteSpace(data.Username) || string.IsNullOrWhiteSpace(data.Password))
            {
                context.Response.StatusCode = 400;
                return;
            }

            string hash = BCrypt.Net.BCrypt.HashPassword(data.Password);
            registerUser.Parameters.AddWithValue("u", data.Username);
            registerUser.Parameters.AddWithValue("p", hash);
            await registerUser.ExecuteNonQueryAsync();
            context.Response.StatusCode = 201;
        }
        catch (PostgresException exception) when (exception.SqlState == "23505")
        {
            context.Response.StatusCode = 409;
        }
        catch (JsonException)
        {
            context.Response.StatusCode = 400;
        }
        catch(Exception)
        {
            context.Response.StatusCode = 500;
        }
        finally
        {
            context.Response.Close();
        }

    }

    public async Task LoginUserAsync(HttpListenerContext context, NpgsqlConnection connectionDB)
    {


        try
        {
            var data = await JsonSerializer.DeserializeAsync<CredUser>(context.Request.InputStream);
            if (string.IsNullOrWhiteSpace(data.Username) || string.IsNullOrWhiteSpace(data.Password))
            {
                context.Response.StatusCode = 400;
                return;
            }

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
                int userId = userExists.GetInt32(0);
                if (BCrypt.Net.BCrypt.Verify(data.Password, passwordFromUser) == false)
                {
                    context.Response.StatusCode = 401;
                }
                else
                {
                    await userExists.CloseAsync();
                    string authToken = Guid.NewGuid().ToString();

                    await using var giveToken =
                        new NpgsqlCommand("INSERT INTO tokens (token, user_id) VALUES (@t, @id)", connectionDB);
                    giveToken.Parameters.AddWithValue("t", authToken);
                    giveToken.Parameters.AddWithValue("id", userId);
                    await giveToken.ExecuteNonQueryAsync();

                    context.Response.StatusCode = 200;

                    byte[] buffer = JsonSerializer.SerializeToUtf8Bytes(new { token = authToken });
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "application/json";
                    context.Response.ContentLength64 = buffer.Length;
                    await context.Response.OutputStream.WriteAsync(buffer);
                }
            }
        }
        catch (JsonException)
        {
            context.Response.StatusCode = 400;
        }
        catch
        {
            context.Response.StatusCode = 404;
        }
        finally
        {
            context.Response.Close();
        }
    }


}