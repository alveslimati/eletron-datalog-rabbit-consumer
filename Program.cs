using System;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
public class Program
{
    // --- CORREÇÃO 3: Main precisa ser async Task para usar await ---
    static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var rabbitMqConfig = configuration.GetSection("RabbitMQ");
        var mySqlConfig = configuration.GetSection("MySql");

        var hostName = rabbitMqConfig["HostName"]!;
        var userName = rabbitMqConfig["UserName"]!;
        var password = rabbitMqConfig["Password"]!;
        var queueName = rabbitMqConfig["QueueName"]!;
        var port = int.Parse(rabbitMqConfig["Port"]!);
        var virtualHost = rabbitMqConfig["VirtualHost"]!;

        var connectionString = $"Server={mySqlConfig["Host"]};Database={mySqlConfig["Database"]};User={mySqlConfig["UserName"]};Password={mySqlConfig["Password"]}";
        var databaseService = new DatabaseService(connectionString);

        var rabbitConsumer = new RabbitConsumer(
            hostName,
            userName,
            password,
            queueName,
            port,
            virtualHost,
            databaseService);

        // Renomeei o método para indicar que é async
        // e o chamamos com await.
        await rabbitConsumer.ConsumeAndProcessMessagesAsync();
    }
}