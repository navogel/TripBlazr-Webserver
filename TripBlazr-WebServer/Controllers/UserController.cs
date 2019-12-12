using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data;
using Microsoft.Data.SqlClient;
using TripBlazr_WebServer.Models;
using Microsoft.AspNetCore.Http;

namespace TripBlazr_WebServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IConfiguration _config;

        public UserController(IConfiguration config)
        {
            _config = config;
        }

        public SqlConnection Connection
        {
            get
            {
                return new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Id, UserName, Password, Email FROM [User]";
                    SqlDataReader reader = cmd.ExecuteReader();
                    List<User> users = new List<User>();

                    while (reader.Read())
                    {
                        User user = new User
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            UserName = reader.GetString(reader.GetOrdinal("UserName")),
                            Password = reader.GetString(reader.GetOrdinal("Password")),
                            Email = reader.GetString(reader.GetOrdinal("Email")),
                        };

                        users.Add(user);
                    }
                    reader.Close();
                    //from controllerbase interface - returns official json result with 200 status code
                    return Ok(users);
                }
            }
        }

        [HttpGet("{id}", Name = "GetUser")]
        public async Task<IActionResult> Get([FromRoute] int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT
                            Id, UserName, Password, Email
                        FROM [User]
                        WHERE Id = @id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));
                    SqlDataReader reader = cmd.ExecuteReader();

                    User user = null;

                    if (reader.Read())
                    {
                        user = new User
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            UserName = reader.GetString(reader.GetOrdinal("UserName")),
                            Password = reader.GetString(reader.GetOrdinal("Password")),
                            Email = reader.GetString(reader.GetOrdinal("Email")),
                        };
                    }
                    reader.Close();

                    if (user == null)
                    {
                        return NotFound($"No User found wit the ID of {id}");
                    }
                    return Ok(user);
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] User user)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO [User] (UserName, Password, Email)
                                        OUTPUT INSERTED.Id
                                        VALUES (@UserName, @Password, @Email)";
                    cmd.Parameters.Add(new SqlParameter("@UserName", user.UserName));
                    cmd.Parameters.Add(new SqlParameter("@Password", user.Password));
                    cmd.Parameters.Add(new SqlParameter("@Email", user.Email));

                    int newId = (int)cmd.ExecuteScalar();
                    user.Id = newId;
                    return CreatedAtRoute("GetUser", new { id = newId }, user);
                }
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put([FromRoute] int id, [FromBody] User user)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"UPDATE [User]
                                            SET UserName = @UserName,
                                                Password = @Password,
                                                Email = @Email
                                            WHERE Id = @id";
                        cmd.Parameters.Add(new SqlParameter("@UserName", user.UserName));
                        cmd.Parameters.Add(new SqlParameter("@Password", user.Password));
                        cmd.Parameters.Add(new SqlParameter("@Email", user.Email));
                        cmd.Parameters.Add(new SqlParameter("@id", id));

                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            return new StatusCodeResult(StatusCodes.Status204NoContent);
                        }
                        throw new Exception("No rows affected");
                    }
                }
            }
            catch (Exception)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"DELETE FROM [User] WHERE Id = @id";
                        cmd.Parameters.Add(new SqlParameter("@id", id));

                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            return new StatusCodeResult(StatusCodes.Status204NoContent);
                        }
                        throw new Exception("No rows affected");
                    }
                }
            }
            catch (Exception)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        private bool UserExists(int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT Id, UserName, Password, Email
                        FROM [User]
                        WHERE Id = @id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));

                    SqlDataReader reader = cmd.ExecuteReader();
                    return reader.Read();
                }
            }
        }
    }
}
