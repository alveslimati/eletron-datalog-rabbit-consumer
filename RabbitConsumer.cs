using RabbitMQ.Client;
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks; // Adicione esta diretiva
using System.Collections.Generic;

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
            ContinuationTimeout = TimeSpan.FromSeconds(30)
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        // Declaração da fila com Dead Letter Exchange configurado
        var deadLetterExchange = "dead_letter_exchange";
        var deadLetterQueue = _queueName + ".dlq";

        channel.ExchangeDeclare(deadLetterExchange, "direct", true);
        channel.QueueDeclare(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", deadLetterExchange },
                { "x-dead-letter-routing-key", deadLetterQueue }
            });
        channel.QueueDeclare(
            queue: deadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        Console.WriteLine($"Fila configurada: '{_queueName}', com DLQ: '{deadLetterQueue}'.");
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

                // Processamento e inserção no banco de dados
                var (maquinaId, cnpjEmpresa) = await _databaseService.GetMachineInfoAsync(maquinaData.NumeroSerial!);
                maquinaData.MaquinaId = maquinaId;
                maquinaData.CnpjEmpresa = cnpjEmpresa;

                await _databaseService.InsertProducaoDataAsync(maquinaData);

                channel.BasicAck(result.DeliveryTag, false);
                messagesProcessed++;
                Console.WriteLine("Dados processados e inseridos com sucesso.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO] Falha ao processar a mensagem: {ex.Message}. Mensagem: {message}");

                // Implementação do Dead Letter em caso de falhas graves ou múltiplos reprocessamentos
                int retryCount = GetRetryCount(result.BasicProperties); // Obtenha o número de tentativas
                if (retryCount >= 5)
                {
                    Console.WriteLine($"[DLQ] Mensagem movida para Dead Letter Queue após {retryCount} tentativas. Mensagem: {message}");
                    await _databaseService.InsertProducaoLogAsync(message, ex.Message);
                    channel.BasicNack(result.DeliveryTag, false, requeue: false); // Move para DLQ
                }
                else
                {
                    SetRetryHeader(channel, result.BasicProperties, retryCount + 1); // Incrementa a contagem de tentativas
                    channel.BasicNack(result.DeliveryTag, false, requeue: true); // Reinsere na fila principal
                }
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

    /// <summary>
    /// Obtém o número de tentativas de uma mensagem RabbitMQ usando o cabeçalho.
    /// </summary>
    private int GetRetryCount(IBasicProperties properties)
    {
        if (properties.Headers != null && properties.Headers.TryGetValue("x-retry-count", out var retryHeader))
        {
            return Convert.ToInt32(Encoding.UTF8.GetString((byte[])retryHeader));
        }
        return 0;
    }

    /// <summary>
    /// Define o cabeçalho para o reprocessamento da mensagem.
    /// </summary>
    private void SetRetryHeader(IModel channel, IBasicProperties properties, int retryCount)
    {
        properties.Headers ??= new Dictionary<string, object>();
        properties.Headers["x-retry-count"] = Encoding.UTF8.GetBytes(retryCount.ToString());
    }
}