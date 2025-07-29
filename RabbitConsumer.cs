using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class RabbitConsumer
{
    private readonly string _hostName;
    private readonly string _userName;
    private readonly string _password;
    private readonly string _queueName;
    private readonly int _port;
    private readonly string _virtualHost;
    private readonly DatabaseService _databaseService;

    public RabbitConsumer(string hostName, string userName, string password, string queueName, int port, string virtualHost, DatabaseService databaseService)
    {
        _hostName = hostName ?? throw new ArgumentNullException(nameof(hostName));
        _userName = userName ?? throw new ArgumentNullException(nameof(userName));
        _password = password ?? throw new ArgumentNullException(nameof(password));
        _queueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
        _port = port;
        _virtualHost = virtualHost ?? throw new ArgumentNullException(nameof(virtualHost));
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
    }

    public void ConsumeAndProcessMessages()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _hostName,
                UserName = _userName,
                Password = _password,
                Port = _port,
                VirtualHost = _virtualHost
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            Console.WriteLine($"Iniciando consumo da fila: {_queueName}");

            var consumer = new EventingBasicConsumer(channel);

            consumer.Received += async (sender, @event) =>
            {
                var body = @event.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                // --- ALTERAÇÃO PRINCIPAL AQUI ---
                // A mensagem recebida JÁ É o JSON que precisamos.
                Console.WriteLine($"Mensagem recebida e sendo processada: {message}");

                try
                {
                    // Desserializa a mensagem DIRETAMENTE para um objeto do tipo MaquinaData
                    var maquinaData = JsonSerializer.Deserialize<MaquinaData>(message, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true // Ignorar diferenças de maiúsculas e minúsculas
                    });

                    // Verifica se a mensagem foi desserializada com sucesso
                    if (maquinaData == null || string.IsNullOrWhiteSpace(maquinaData.NumeroSerial))
                    {
                        Console.WriteLine($"Erro: Mensagem inválida ou `NumeroSerial` vazio. Mensagem: {message}");
                        channel.BasicAck(@event.DeliveryTag, multiple: false);
                        return;
                    }

                    // Busca o maquina_id e cnpj da empresa com base no numero_serial
                    var (maquinaId, cnpjEmpresa) = await _databaseService.GetMachineInfoAsync(maquinaData.NumeroSerial!);
                    maquinaData.MaquinaId = maquinaId;
                    maquinaData.CnpjEmpresa = cnpjEmpresa;
                    
                    // Insere os dados no banco de dados
                    await _databaseService.InsertProducaoDataAsync(maquinaData);

                    // Confirma que a mensagem foi processada com sucesso
                    channel.BasicAck(@event.DeliveryTag, multiple: false);
                    Console.WriteLine("Dados processados e inseridos com sucesso.");
                }
                catch (JsonException jsonEx)
                {
                    Console.WriteLine($"Erro ao desserializar mensagem JSON: {jsonEx.Message}");
                    channel.BasicAck(@event.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao processar a mensagem: {ex.Message}");
                    channel.BasicAck(@event.DeliveryTag, multiple: false);
                }
            };

            channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);

            Console.WriteLine("Pressione qualquer tecla para encerrar o consumidor...");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao consumir mensagens: {ex.Message}");
        }
    }
}

// A classe OuterMessage foi removida pois não é mais necessária.