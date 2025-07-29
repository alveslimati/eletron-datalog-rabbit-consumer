using System;
using Microsoft.Extensions.Configuration;

public class Program
{
    static void Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var rabbitMqConfig = configuration.GetSection("RabbitMQ");
        var mySqlConfig = configuration.GetSection("MySql");

        var hostName = rabbitMqConfig["HostName"] ?? throw new ArgumentNullException("RabbitMQ:HostName não pode ser nulo.");
        var userName = rabbitMqConfig["UserName"] ?? throw new ArgumentNullException("RabbitMQ:UserName não pode ser nulo.");
        var password = rabbitMqConfig["Password"] ?? throw new ArgumentNullException("RabbitMQ:Password não pode ser nulo.");
        var queueName = rabbitMqConfig["QueueName"] ?? throw new ArgumentNullException("RabbitMQ:QueueName não pode ser nulo.");
        
        // --- NOVAS LINHAS ---
        var portStr = rabbitMqConfig["Port"] ?? throw new ArgumentNullException("RabbitMQ:Port não pode ser nulo.");
        if (!int.TryParse(portStr, out int port))
        {
            throw new ArgumentException("RabbitMQ:Port deve ser um número inteiro válido.");
        }
        var virtualHost = rabbitMqConfig["VirtualHost"] ?? throw new ArgumentNullException("RabbitMQ:VirtualHost não pode ser nulo.");
        // --- FIM DAS NOVAS LINHAS ---

        var connectionString = $"Server={mySqlConfig["Host"]};Database={mySqlConfig["Database"]};User={mySqlConfig["UserName"]};Password={mySqlConfig["Password"]}";
        var databaseService = new DatabaseService(connectionString);

        // Atualize a chamada do construtor para incluir os novos parâmetros
        var rabbitConsumer = new RabbitConsumer(
            hostName,
            userName,
            password,
            queueName,
            port,        // Passe a porta
            virtualHost, // Passe o virtual host
            databaseService);

        rabbitConsumer.ConsumeAndProcessMessages();
    }
}