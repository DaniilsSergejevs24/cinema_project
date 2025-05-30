using System;
using System.Security.Cryptography;
using System.Text;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;
using System.Data;

namespace cinema_project
{
    public class DatabaseHelper
    {
        private readonly string connectionString;

        public DatabaseHelper()
        {
            connectionString = "datasource=127.0.0.1;port=3307;username=root;password=;database=database_cinema;";
        }

        public static string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            string hashOfInput = HashPassword(password);
            return hashOfInput.Equals(hashedPassword, StringComparison.Ordinal);
        }

        public async Task InitializeDatabaseAsync()
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string createUsersTable = @"
                        CREATE TABLE IF NOT EXISTS users (
                            user_id INT AUTO_INCREMENT PRIMARY KEY,
                            username VARCHAR(50) NOT NULL UNIQUE,
                            email VARCHAR(100) NOT NULL UNIQUE,
                            password VARCHAR(255) NOT NULL,
                            first_name VARCHAR(50) NOT NULL,
                            last_name VARCHAR(50) NOT NULL,
                            phone VARCHAR(20),
                            date_of_birth DATE,
                            profile_picture VARCHAR(500),
                            join_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                            last_login TIMESTAMP NULL,
                            is_active BOOLEAN DEFAULT TRUE,
                            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                            updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                        )";
                    using (var command = new MySqlCommand(createUsersTable, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Database initialization failed: {ex.Message}");
            }
        }

        public async Task<bool> RegisterUserAsync(string username, string email, string password, string firstName, string lastName, string phone = null, DateTime? dateOfBirth = null)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string checkUserQuery = "SELECT COUNT(*) FROM users WHERE email = @email OR username = @username";
                    using (var checkCommand = new MySqlCommand(checkUserQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@email", email);
                        checkCommand.Parameters.AddWithValue("@username", username);
                        int userCount = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                        if (userCount > 0) return false;
                    }

                    string insertQuery = @"
                        INSERT INTO users (username, email, password, first_name, last_name, phone, date_of_birth) 
                        VALUES (@username, @email, @password, @firstName, @lastName, @phone, @dateOfBirth)";
                    using (var command = new MySqlCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@username", username);
                        command.Parameters.AddWithValue("@email", email);
                        command.Parameters.AddWithValue("@password", HashPassword(password));
                        command.Parameters.AddWithValue("@firstName", firstName);
                        command.Parameters.AddWithValue("@lastName", lastName);
                        command.Parameters.AddWithValue("@phone", (object)phone ?? DBNull.Value);
                        command.Parameters.AddWithValue("@dateOfBirth", (object)dateOfBirth ?? DBNull.Value);
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (MySqlException ex) when (ex.Number == 1062) { return false; }
            catch (Exception ex) { throw new Exception($"Registration failed: {ex.Message}"); }
        }

        public async Task<User> AuthenticateUserAsync(string loginIdentifier, string password)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = "SELECT user_id, password FROM users WHERE email = @loginIdentifier OR username = @loginIdentifier";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@loginIdentifier", loginIdentifier);
                        using (var reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                string storedHashedPassword = reader.GetString("password");
                                int userId = reader.GetInt32("user_id");
                                if (VerifyPassword(password, storedHashedPassword))
                                {
                                    reader.Close();
                                    return await GetUserByIdAsync(userId, connection);
                                }
                            }
                        }
                        return null;
                    }
                }
            }
            catch (Exception ex) { throw new Exception($"Authentication failed: {ex.Message}"); }
        }

        public async Task<User> GetUserAsync(string email)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    return await GetUserByEmailAsync(email, connection);
                }
            }
            catch (Exception ex) { throw new Exception($"Failed to get user by email: {ex.Message}"); }
        }

        private async Task<User> GetUserByIdAsync(int userId, MySqlConnection connection = null)
        {
            bool closeConnection = false;
            if (connection == null)
            {
                connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();
                closeConnection = true;
            }
            try
            {
                string query = @"
                    SELECT user_id, username, email, first_name, last_name, phone, 
                           date_of_birth, profile_picture, join_date, last_login, is_active 
                    FROM users WHERE user_id = @userId";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    return await ReadUserData(command);
                }
            }
            finally { if (closeConnection && connection.State == ConnectionState.Open) await connection.CloseAsync(); }
        }

        private async Task<User> GetUserByEmailAsync(string email, MySqlConnection connection = null)
        {
            bool closeConnection = false;
            if (connection == null)
            {
                connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();
                closeConnection = true;
            }
            try
            {
                string query = @"
                    SELECT user_id, username, email, first_name, last_name, phone, 
                           date_of_birth, profile_picture, join_date, last_login, is_active 
                    FROM users WHERE email = @email";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@email", email);
                    return await ReadUserData(command);
                }
            }
            finally { if (closeConnection && connection.State == ConnectionState.Open) await connection.CloseAsync(); }
        }

        private async Task<User> ReadUserData(MySqlCommand command)
        {
            using (var reader = (MySqlDataReader)await command.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    return new User
                    {
                        Id = reader.GetInt32("user_id"),
                        Username = reader.GetString("username"),
                        Email = reader.GetString("email"),
                        FirstName = reader.IsDBNull(reader.GetOrdinal("first_name")) ? null : reader.GetString("first_name"),
                        LastName = reader.IsDBNull(reader.GetOrdinal("last_name")) ? null : reader.GetString("last_name"),
                        Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString("phone"),
                        DateOfBirth = reader.IsDBNull(reader.GetOrdinal("date_of_birth")) ? (DateTime?)null : reader.GetDateTime("date_of_birth"),
                        ProfilePicture = reader.IsDBNull(reader.GetOrdinal("profile_picture")) ? null : reader.GetString("profile_picture"),
                        JoinDate = reader.GetDateTime("join_date"),
                        LastLogin = reader.IsDBNull(reader.GetOrdinal("last_login")) ? (DateTime?)null : reader.GetDateTime("last_login"),
                        IsActive = reader.GetBoolean("is_active")
                    };
                }
            }
            return null;
        }

        public async Task<bool> UpdatePasswordAsync(string email, string newPassword)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = "UPDATE users SET password = @password WHERE email = @email";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@email", email);
                        command.Parameters.AddWithValue("@password", HashPassword(newPassword));
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex) { throw new Exception($"Password update failed: {ex.Message}"); }
        }

        public async Task<bool> UserExistsByEmailAsync(string email)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = "SELECT COUNT(*) FROM users WHERE email = @email";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@email", email);
                        int count = Convert.ToInt32(await command.ExecuteScalarAsync());
                        return count > 0;
                    }
                }
            }
            catch (Exception ex) { throw new Exception($"Failed to check if user exists: {ex.Message}"); }
        }

        public async Task CreatePasswordResetTokenAsync(int userId, string token, DateTime expiresAt)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string query = "INSERT INTO password_reset_tokens (user_id, token, expires_at) VALUES (@userId, @token, @expiresAt)";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@userId", userId);
                    command.Parameters.AddWithValue("@token", token);
                    command.Parameters.AddWithValue("@expiresAt", expiresAt);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }


        public async Task<List<User>> GetAllUsersAsync()
        {
            var users = new List<User>();
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"
                SELECT user_id, username, email, first_name, last_name, phone, 
                       date_of_birth, profile_picture, join_date, last_login, is_active 
                FROM users 
                ORDER BY join_date DESC";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        using (var reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                users.Add(new User
                                {
                                    Id = reader.GetInt32("user_id"),
                                    Username = reader.GetString("username"),
                                    Email = reader.GetString("email"),
                                    FirstName = reader.IsDBNull(reader.GetOrdinal("first_name")) ? null : reader.GetString("first_name"),
                                    LastName = reader.IsDBNull(reader.GetOrdinal("last_name")) ? null : reader.GetString("last_name"),
                                    Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString("phone"),
                                    DateOfBirth = reader.IsDBNull(reader.GetOrdinal("date_of_birth")) ? (DateTime?)null : reader.GetDateTime("date_of_birth"),
                                    ProfilePicture = reader.IsDBNull(reader.GetOrdinal("profile_picture")) ? null : reader.GetString("profile_picture"),
                                    JoinDate = reader.GetDateTime("join_date"),
                                    LastLogin = reader.IsDBNull(reader.GetOrdinal("last_login")) ? (DateTime?)null : reader.GetDateTime("last_login"),
                                    IsActive = reader.GetBoolean("is_active")
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get all users: {ex.Message}");
            }
            return users;
        }

        public async Task<User> GetUserByIdAsync(int userId)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"
                SELECT user_id, username, email, first_name, last_name, phone, 
                       date_of_birth, profile_picture, join_date, last_login, is_active 
                FROM users 
                WHERE user_id = @userId";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userId", userId);
                        using (var reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new User
                                {
                                    Id = reader.GetInt32("user_id"),
                                    Username = reader.GetString("username"),
                                    Email = reader.GetString("email"),
                                    FirstName = reader.IsDBNull(reader.GetOrdinal("first_name")) ? null : reader.GetString("first_name"),
                                    LastName = reader.IsDBNull(reader.GetOrdinal("last_name")) ? null : reader.GetString("last_name"),
                                    Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString("phone"),
                                    DateOfBirth = reader.IsDBNull(reader.GetOrdinal("date_of_birth")) ? (DateTime?)null : reader.GetDateTime("date_of_birth"),
                                    ProfilePicture = reader.IsDBNull(reader.GetOrdinal("profile_picture")) ? null : reader.GetString("profile_picture"),
                                    JoinDate = reader.GetDateTime("join_date"),
                                    LastLogin = reader.IsDBNull(reader.GetOrdinal("last_login")) ? (DateTime?)null : reader.GetDateTime("last_login"),
                                    IsActive = reader.GetBoolean("is_active")
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get user by ID: {ex.Message}");
            }
            return null;
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string checkQuery = "SELECT COUNT(*) FROM users WHERE (email = @email OR username = @username) AND user_id != @userId";
                    using (var checkCommand = new MySqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@email", user.Email);
                        checkCommand.Parameters.AddWithValue("@username", user.Username);
                        checkCommand.Parameters.AddWithValue("@userId", user.Id);
                        int count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                        if (count > 0) return false;
                    }

                    string updateQuery = @"
                UPDATE users 
                SET username = @username, 
                    email = @email, 
                    first_name = @firstName, 
                    last_name = @lastName, 
                    phone = @phone, 
                    date_of_birth = @dateOfBirth, 
                    is_active = @isActive,
                    updated_at = CURRENT_TIMESTAMP
                WHERE user_id = @userId";

                    using (var command = new MySqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@username", user.Username);
                        command.Parameters.AddWithValue("@email", user.Email);
                        command.Parameters.AddWithValue("@firstName", user.FirstName);
                        command.Parameters.AddWithValue("@lastName", user.LastName);
                        command.Parameters.AddWithValue("@phone", (object)user.Phone ?? DBNull.Value);
                        command.Parameters.AddWithValue("@dateOfBirth", (object)user.DateOfBirth ?? DBNull.Value);
                        command.Parameters.AddWithValue("@isActive", user.IsActive);
                        command.Parameters.AddWithValue("@userId", user.Id);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to update user: {ex.Message}");
            }
        }

        public async Task<bool> ToggleUserStatusAsync(int userId)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"
                UPDATE users 
                SET is_active = NOT is_active, 
                    updated_at = CURRENT_TIMESTAMP 
                WHERE user_id = @userId";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userId", userId);
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to toggle user status: {ex.Message}");
            }
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        try
                        {
                            var relatedTables = await GetTablesWithUserIdAsync(connection, transaction);

                            foreach (var tableName in relatedTables)
                            {
                                string deleteQuery = $"DELETE FROM {tableName} WHERE user_id = @userId";
                                using (var command = new MySqlCommand(deleteQuery, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@userId", userId);
                                    await command.ExecuteNonQueryAsync();
                                }
                            }

                            string deleteUserQuery = "DELETE FROM users WHERE user_id = @userId";
                            using (var command = new MySqlCommand(deleteUserQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@userId", userId);
                                int rowsAffected = await command.ExecuteNonQueryAsync();

                                if (rowsAffected == 0)
                                {
                                    await transaction.RollbackAsync();
                                    return false;
                                }
                            }

                            await transaction.CommitAsync();
                            return true;
                        }
                        catch (Exception)
                        {
                            await transaction.RollbackAsync();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete user: {ex.Message}");
            }
        }

        private async Task<List<string>> GetTablesWithUserIdAsync(MySqlConnection connection, MySqlTransaction transaction)
        {
            var tables = new List<string>();
            try
            {
                string query = @"
            SELECT DISTINCT TABLE_NAME 
            FROM INFORMATION_SCHEMA.COLUMNS 
            WHERE TABLE_SCHEMA = DATABASE() 
            AND COLUMN_NAME = 'user_id' 
            AND TABLE_NAME != 'users'";

                using (var command = new MySqlCommand(query, connection, transaction))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            tables.Add(reader.GetString("TABLE_NAME"));
                        }
                    }
                }
            }
            catch (Exception)
            {
                
            }
            return tables;
        }

        public async Task<bool> DeleteUserSimpleAsync(int userId)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string deleteUserQuery = "DELETE FROM users WHERE user_id = @userId";

                    using (var command = new MySqlCommand(deleteUserQuery, connection))
                    {
                        command.Parameters.AddWithValue("@userId", userId);
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete user: {ex.Message}");
            }
        }

        public async Task<bool> SoftDeleteUserAsync(int userId)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = @"
                UPDATE users 
                SET is_active = 0, 
                    deleted_at = CURRENT_TIMESTAMP,
                    updated_at = CURRENT_TIMESTAMP
                WHERE user_id = @userId AND is_active = 1";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userId", userId);
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to soft delete user: {ex.Message}");
            }
        }

        public async Task<bool> UserExistsAsync(int userId)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = "SELECT COUNT(*) FROM users WHERE user_id = @userId";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userId", userId);
                        int count = Convert.ToInt32(await command.ExecuteScalarAsync());
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to check if user exists: {ex.Message}");
            }
        }

    }

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string ProfilePicture { get; set; }
        public DateTime JoinDate { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool IsActive { get; set; }
    }
}