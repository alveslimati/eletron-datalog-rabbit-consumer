using System;
using Microsoft.Extensions.Configuration;

public class Program
{
    static void Main(string[] args)
    {
        // Carregar configurações do appsettings.json
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var rabbitMqConfig = configuration.GetSection("RabbitMQ");
        var mySqlConfig = configuration.GetSection("MySql");

        // Conexão ao banco
        var connectionString = $"Server={mySqlConfig["Host"]};Database={mySqlConfig["Database"]};User={mySqlConfig["UserName"]};Password={mySqlConfig["Password"]}";
        var databaseService = new DatabaseService(connectionString);

        // Configurar RabbitConsumer
        var rabbitConsumer = new RabbitConsumer(
            rabbitMqConfig["HostName"],
            rabbitMqConfig["UserName"],
            rabbitMqConfig["Password"],
            rabbitMqConfig["QueueName"],
            databaseService);

        // Iniciar o consumidor e processar mensagens
        rabbitConsumer.ConsumeAndProcessMessages();
    }
}