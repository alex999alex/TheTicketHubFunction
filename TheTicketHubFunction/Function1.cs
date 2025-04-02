using System;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using System.Diagnostics.Metrics;
using System.Net;
using System.Numerics;

namespace TheTicketHubFunction
{
    public class Function1
    {
        private readonly ILogger<Function1> _logger;

        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
        }

        [Function(nameof(Function1))]
        public async Task Run([QueueTrigger("thetickethub", Connection = "AzureWebJobsStorage")] QueueMessage message)
        {
            _logger.LogInformation($"C# Queue trigger function processed: {message.MessageText}");

            string messageJson = message.MessageText;


            // Deserialize the message
            var options = new JsonSerializerOptions

            {

                PropertyNameCaseInsensitive = true

            };

            TheTicketHub? theTicketHub = JsonSerializer.Deserialize<TheTicketHub>(messageJson, options);

            if (theTicketHub == null)
            {
                _logger.LogError("Failed to deserialize the message");
                return;
            }

            _logger.LogInformation($"Name: {theTicketHub.Name}");

            // Add the concertId to database
            // get connection string from app settings
            string? connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("SQL connection string is not set in the environment variables.");
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync(); // Note the ASYNC

                var query = "INSERT INTO Tickets (concertId, email, name, phone, quantity, creditCard, expiration, securityCode, address, city, province, postalCode, country) VALUES (    @concertId,    @email,    @name,    @phone,    @quantity,    @creditCard,    @expiration,    @securityCode,    @address,    @city,    @province,    @postalCode,    @country);";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@concertId", theTicketHub.ConcertId);
                    cmd.Parameters.AddWithValue("@email", theTicketHub.Email);
                    cmd.Parameters.AddWithValue("@name", theTicketHub.Name);
                    cmd.Parameters.AddWithValue("@phone", theTicketHub.Phone);
                    cmd.Parameters.AddWithValue("@quantity", theTicketHub.Quantity);
                    cmd.Parameters.AddWithValue("@creditCard", theTicketHub.CreditCard);
                    cmd.Parameters.AddWithValue("@expiration", theTicketHub.Expiration);
                    cmd.Parameters.AddWithValue("@securityCode", theTicketHub.SecurityCode);
                    cmd.Parameters.AddWithValue("@address", theTicketHub.Address);
                    cmd.Parameters.AddWithValue("@city", theTicketHub.City);
                    cmd.Parameters.AddWithValue("@province", theTicketHub.Province);
                    cmd.Parameters.AddWithValue("@postalCode", theTicketHub.PostalCode);
                    cmd.Parameters.AddWithValue("@country", theTicketHub.Country);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
