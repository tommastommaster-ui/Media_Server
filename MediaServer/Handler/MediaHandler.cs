using System.Net;
using System.Text.Json;
using Npgsql;

namespace MediaServer.Handler;

public class MediaHandler
{
    public async Task CreateMediaAsync(HttpListenerContext context, NpgsqlConnection connectionDB)
    {
        try
        {
            string? header = context.Request.Headers["Authorization"];
            if (header == null || !header.StartsWith("Bearer "))
            {
                context.Response.StatusCode = 401;
                return;
            }

            string tokenFromUser = header.Substring(7);

            await using var tokenInDB = new NpgsqlCommand(
                "SELECT user_id FROM tokens WHERE token = @t", connectionDB);
            tokenInDB.Parameters.AddWithValue("t", tokenFromUser);
            object? result = await tokenInDB.ExecuteScalarAsync();

            if (result == null)
            {
                context.Response.StatusCode = 401;
                return;
            }

            int userId = (int)result;


            var data = await JsonSerializer.DeserializeAsync<MediaEntry>(context.Request.InputStream,
                new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            if (data == null || string.IsNullOrWhiteSpace(data.Title))
            {
                context.Response.StatusCode = 400;
                return;
            }

            await using var insert =
                new NpgsqlCommand(
                    "INSERT INTO media_entries (title, description, media_type, release_year, age_restriction, creator_id, genres) " +
                    "VALUES (@title, @desc, @type, @year, @age, @creator, @geners)", connectionDB);
            insert.Parameters.AddWithValue("title", data.Title);
            insert.Parameters.AddWithValue("desc", data.Description);
            insert.Parameters.AddWithValue("type", data.MediaType);
            insert.Parameters.AddWithValue("year", data.ReleaseYear);
            insert.Parameters.AddWithValue("age", data.AgeRestriction);
            insert.Parameters.AddWithValue("creator", userId);
            insert.Parameters.AddWithValue("geners", (data.Genres ?? new()).ToArray());
            await insert.ExecuteNonQueryAsync();

            context.Response.StatusCode = 201;
        }
        catch (JsonException)
        {
            context.Response.StatusCode = 400;
        }
        catch (PostgresException e) when (e.SqlState == "23514")
        {
            context.Response.StatusCode = 400;
        }
        catch (Exception e)
        {
            context.Response.StatusCode = 404;
            Console.WriteLine(e);
        }
        finally
        {
            context.Response.Close();
        }
    }



    public async Task GetMediaAsync(HttpListenerContext context, NpgsqlConnection connectionDB)
    {
        try
        {
            string path = context.Request.Url!.AbsolutePath;
            string? header = context.Request.Headers["Authorization"];
            if (header == null || !header.StartsWith("Bearer "))
            {
                context.Response.StatusCode = 401;
                return;
            }
            
            string tokenFromUser = header.Substring(7);

            await using var tokenInDB = new NpgsqlCommand(
                "SELECT user_id FROM tokens WHERE token = @t", connectionDB);
            tokenInDB.Parameters.AddWithValue("t", tokenFromUser);
            object? result = await tokenInDB.ExecuteScalarAsync();

            if (result == null)
            {
                context.Response.StatusCode = 401;
                return;
            }

            string idMediaEntry = path.Substring("/api/media/".Length);
            if (!int.TryParse(idMediaEntry, out int mediaId))
            {
                context.Response.StatusCode = 400;
                return;
            }


            await using var getMediaInfo = new NpgsqlCommand(
                "SELECT id, title, description, media_type, release_year, age_restriction, creator_id, genres FROM media_entries WHERE id = @id",
                connectionDB);
            getMediaInfo.Parameters.AddWithValue("id", mediaId);

            await using var reader = await getMediaInfo.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                context.Response.StatusCode = 404;
                return;
            }

            var media = new MediaEntry
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                MediaType = reader.GetString(3),
                ReleaseYear = reader.GetInt32(4),
                AgeRestriction = reader.GetInt32(5),
                CreatorId = reader.GetInt32(6),
                Genres = reader.GetFieldValue<string[]>(7).ToList()
            };

            byte[] buffer = JsonSerializer.SerializeToUtf8Bytes(media);
            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer);



        }
        catch (JsonException)
        {
            context.Response.StatusCode = 400;
        }
        catch (Exception e)
        {
            context.Response.StatusCode = 500;
            Console.WriteLine(e);
        }
        finally
        {
            context.Response.Close();
        }
    }

    public async Task UpdateMedia(HttpListenerContext context, NpgsqlConnection connectionDB)
    {
        try
        {
            string path = context.Request.Url!.AbsolutePath;
            string? header = context.Request.Headers["Authorization"];
            if (header == null || !header.StartsWith("Bearer "))
            {
                context.Response.StatusCode = 401;
                return;
            }

            string tokenFromUser = header.Substring(7);

            string idMediaEntry = path.Substring("/api/media/".Length);
            if (!int.TryParse(idMediaEntry, out int mediaId))
            {
                context.Response.StatusCode = 400;
                return;
            }

            await using var tokenInDB = new NpgsqlCommand(
                "SELECT user_id FROM tokens WHERE token = @t", connectionDB);
            tokenInDB.Parameters.AddWithValue("t", tokenFromUser);
            object? result = await tokenInDB.ExecuteScalarAsync();

            if (result == null)
            {
                context.Response.StatusCode = 401;
                return;
            }

            int userId = (int)result;
            //userId überprüffen mit eintrag id

            await using var ownerCheck = new NpgsqlCommand(
                "SELECT creator_id FROM  media_entries WHERE id = @id", connectionDB);
            ownerCheck.Parameters.AddWithValue("id", mediaId);
            object? owner = await ownerCheck.ExecuteScalarAsync();

            if (owner == null)
            {
                context.Response.StatusCode = 404;
            }
            else if ((int)owner != userId)
            {
                context.Response.StatusCode = 403;
            }
            else
            {
                var data = await JsonSerializer.DeserializeAsync<MediaEntry>(context.Request.InputStream,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (data == null || string.IsNullOrWhiteSpace(data.Title))
                {
                    context.Response.StatusCode = 400;
                    return;
                }

                await using var update =
                    new NpgsqlCommand(
                        "UPDATE media_entries SET title = @title, description = @desc, media_type = @type, release_year = @year, age_restriction = @age WHERE id = @id",
                        connectionDB);
                update.Parameters.AddWithValue("title", data.Title);
                update.Parameters.AddWithValue("desc", data.Description);
                update.Parameters.AddWithValue("type", data.MediaType);
                update.Parameters.AddWithValue("year", data.ReleaseYear);
                update.Parameters.AddWithValue("age", data.AgeRestriction);
                update.Parameters.AddWithValue("id", mediaId);
                await update.ExecuteNonQueryAsync();

                context.Response.StatusCode = 200;
            }

            

        }
        catch (JsonException)
        {
            context.Response.StatusCode = 400;
        }
        catch (PostgresException e) when (e.SqlState == "23514")
        {
            context.Response.StatusCode = 400;
        }
        catch (Exception e)
        {
            context.Response.StatusCode = 500;
            Console.WriteLine(e);
        }
        finally
        {
            context.Response.Close();
        }
        
    }

    public async Task DelteMedia(HttpListenerContext context, NpgsqlConnection connectionDB)
    {
        try
        {
            string path = context.Request.Url!.AbsolutePath;
            string idMediaEntry = path.Substring("/api/media/".Length);

            string? header = context.Request.Headers["Authorization"];
            if (header == null || !header.StartsWith("Bearer "))
            {
                context.Response.StatusCode = 401;
                return;

            }

            if (!int.TryParse(idMediaEntry, out int mediaId))
            {
                context.Response.StatusCode = 400;
                return;
            }

            string tokenFromUser = header.Substring(7);
            await using var tokenInDB = new NpgsqlCommand(
                "SELECT user_id FROM tokens WHERE token = @t", connectionDB);
            tokenInDB.Parameters.AddWithValue("t", tokenFromUser);
            object? result = await tokenInDB.ExecuteScalarAsync();

            if (result == null)
            {
                context.Response.StatusCode = 401;
                return;
            }

            int userId = (int)result;
            //userId überprüffen mit eintrag id

            await using var ownerCheck = new NpgsqlCommand(
                "SELECT creator_id FROM  media_entries WHERE id = @id", connectionDB);
            ownerCheck.Parameters.AddWithValue("id", mediaId);
            object? owner = await ownerCheck.ExecuteScalarAsync();

            if (owner == null)
            {
                context.Response.StatusCode = 404;
            }
            else if ((int)owner != userId)
            {
                context.Response.StatusCode = 403;
            }
            else
            {
                await using var deleteMediaEntry = new NpgsqlCommand(
                    "DELETE FROM media_entries WHERE id = @id", connectionDB);
                deleteMediaEntry.Parameters.AddWithValue("id", mediaId);
                await deleteMediaEntry.ExecuteNonQueryAsync();
                context.Response.StatusCode = 204;
            }

        }
        catch (JsonException)
        {
            context.Response.StatusCode = 400;
        }
        catch (Exception e)
        {
            context.Response.StatusCode = 500;
            Console.WriteLine(e);
        }
        finally
        {
            context.Response.Close();
        }
    }

}