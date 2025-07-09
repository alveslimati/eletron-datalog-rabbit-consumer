using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Text;

public class RabbitConsumer
{
    private readonly string _hostName;
    private readonly string _userName;
    private readonly string _password;
    private readonly string _queueName;
    private readonly DatabaseService _databaseService;

    public RabbitConsumer(string hostName, string userName, string password, string queueName, DatabaseService databaseService)
    {
        _hostName = hostName;
        _userName = userName;
        _password = password;
        _queueName = queueName;
        _databaseService = databaseService;
    }

    public void ConsumeAndProcessMessages()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _hostName,
                UserName = _userName,
                Password = _password
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            // Garante que a fila existirá
            channel.QueueDeclare(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            Console.WriteLine($"Iniciando consumo da fila: {_queueName}");
            
            // Processar mensagens em lote (máximo desempenho)
            var consumer = new EventingBasicConsumer(channel);
            var messages = new ConcurrentBag<string>(); // Armazenar as mensagens temporariamente

            // Trata cada mensagem recebida
            consumer.Received += async (sender, @event) =>
            {
                var body = @event.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                messages.Add(message);

                // Reconhece a mensagem como processada
                channel.BasicAck(@event.DeliveryTag, false);

                // Processa e insere cada mensagem no banco
                await _databaseService.InsertDataAsync(message);
            };

            // Consumir mensagens
            channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);

            // Aguarda o esgotamento da fila
            Console.WriteLine("Pressione qualquer tecla para encerrar o consumidor...");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao consumir mensagens: {ex.Message}");
        }
    }
}