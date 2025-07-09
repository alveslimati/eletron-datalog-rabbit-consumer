using MySqlConnector;
using System;
using System.Threading.Tasks;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task InsertDataAsync(string messagePayload)
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            // Exemplo simples: Insira os dados na tabela `dados`
            var query = "INSERT INTO dados (payload) VALUES (@payload)";
            using var command = new MySqlCommand(query, connection);
            
            // Adiciona o valor da mensagem como parâmetro
            command.Parameters.AddWithValue("@payload", messagePayload);

            // Executa o comando
            await command.ExecuteNonQueryAsync();
            Console.WriteLine("Dados inseridos no banco com sucesso.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao inserir os dados no banco: {ex.Message}");
        }
    }
}