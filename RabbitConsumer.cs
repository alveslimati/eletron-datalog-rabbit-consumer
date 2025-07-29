using RabbitMQ.Client;
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks; // Adicione esta diretiva

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
        _hostName = hostName;
        _userName = userName;
        _password = password;
        _queueName = queueName;
        _port = port;
        _virtualHost = virtualHost;
        _databaseService = databaseService;
    }

    // --- CORREÇÃO 1: Adicionado 'async Task' ---
    public async Task ConsumeAndProcessMessagesAsync()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _hostName,
                UserName = _userName,
                Password = _password,
                Port = _port,
                VirtualHost = _virtualHost,
                // --- CORREÇÃO 2: Propriedade de Timeout correta ---
                ContinuationTimeout = TimeSpan.FromSeconds(30)
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            Console.WriteLine($"Verificando a fila '{_queueName}' por mensagens...");
            int messagesProcessed = 0;

            while (true)
            {
                BasicGetResult? result = channel.BasicGet(_queueName, autoAck: false);
                if (result == null) break;

                string message = "";
                try
                {
                    var body = result.Body.ToArray();
                    message = Encoding.UTF8.GetString(body);
                    Console.WriteLine($"Mensagem recebida: {message}");

                    var maquinaData = JsonSerializer.Deserialize<MaquinaData>(message, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (maquinaData == null || string.IsNullOrWhiteSpace(maquinaData.NumeroSerial))
                    {
                        Console.WriteLine($"[AVISO] Mensagem inválida ou 'NumeroSerial' vazio. Descartando. Mensagem: {message}");
                        channel.BasicAck(result.DeliveryTag, false); 
                        continue;
                    }

                    // --- Agora o 'await' funciona porque o método é async ---
                    var (maquinaId, cnpjEmpresa) = await _databaseService.GetMachineInfoAsync(maquinaData.NumeroSerial!);
                    maquinaData.MaquinaId = maquinaId;
                    maquinaData.CnpjEmpresa = cnpjEmpresa;
                    
                    // --- E aqui também ---
                    await _databaseService.InsertProducaoDataAsync(maquinaData);

                    channel.BasicAck(result.DeliveryTag, false);
                    messagesProcessed++;
                    Console.WriteLine("Dados processados e inseridos com sucesso.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERRO] Falha ao processar a mensagem: {ex.Message}. Mensagem: {message}");
                    channel.BasicNack(result.DeliveryTag, false, requeue: false);
                }
            }

            Console.WriteLine(messagesProcessed > 0
                ? $"Processo finalizado. Total de {messagesProcessed} mensagens processadas."
                : "Nenhuma mensagem encontrada na fila neste ciclo.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERRO CRÍTICO] Falha ao conectar ou consumir mensagens: {ex.Message}");
            throw; 
        }
    }
}