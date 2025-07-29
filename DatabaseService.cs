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

    /// <summary>
    /// Busca o id_maquina e o cnpj da empresa na tabela dispositivo_esp32 pelo numero_serial (codigo_hex)
    /// </summary>
    public async Task<(int maquinaId, string cnpj)> GetMachineInfoAsync(string numeroSerial)
    {
        if (string.IsNullOrWhiteSpace(numeroSerial))
        {
            throw new ArgumentException("O número serial não pode ser nulo ou vazio.", nameof(numeroSerial));
        }

        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            // Query para buscar id_maquina e cnpj
            var query = "SELECT id_maquina, cnpj FROM dispositivo_esp32 WHERE codigo_hex = @numero_serial";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@numero_serial", numeroSerial);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                // Lê os valores do banco
                int maquinaId = reader.GetInt32(0);

                // Converte o CNPJ para string
                string cnpj = reader.GetFieldValue<long>(1).ToString(); // Lê como Int64 e converte para string

                return (maquinaId, cnpj);
            }

            throw new Exception($"Número serial '{numeroSerial}' não encontrado na base de dados.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao buscar informações da máquina: {ex.Message}");
            throw; // Relança a exceção para ser tratada em nível superior
        }
    }
    /// <summary>
    /// Insere os dados na tabela `producao`
    /// </summary>
    public async Task InsertProducaoDataAsync(MaquinaData maquinaData)
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
            INSERT INTO producao (maquina_id, operador_id, status, velocidade, producao, cnpj_empresa, timestamp) 
            VALUES (@maquinaId, @operadorId, @status, @velocidade, @producao, @cnpjEmpresa, @timestamp)";

            using var command = new MySqlCommand(query, connection);

            // Adiciona os valores da classe `MaquinaData` como parâmetros
            command.Parameters.AddWithValue("@maquinaId", maquinaData.MaquinaId.HasValue
                ? (object)maquinaData.MaquinaId.Value
                : DBNull.Value); // Trata como DBNull se for null

            command.Parameters.AddWithValue("@operadorId", maquinaData.OperadorId.HasValue
                ? (object)maquinaData.OperadorId.Value
                : DBNull.Value); // Trata como DBNull se for null

            command.Parameters.AddWithValue("@status", !string.IsNullOrEmpty(maquinaData.Status)
                ? (object)maquinaData.Status
                : DBNull.Value); // Trata como DBNull se for null ou string vazia

            command.Parameters.AddWithValue("@velocidade", maquinaData.Velocidade.HasValue
                ? (object)maquinaData.Velocidade.Value
                : DBNull.Value); // Trata como DBNull se for null

            command.Parameters.AddWithValue("@producao", maquinaData.Producao.HasValue
                ? (object)maquinaData.Producao.Value
                : DBNull.Value); // Trata como DBNull se for null

            command.Parameters.AddWithValue("@cnpjEmpresa", !string.IsNullOrEmpty(maquinaData.CnpjEmpresa)
                ? (object)maquinaData.CnpjEmpresa
                : DBNull.Value); // Trata como DBNull se for null ou string vazia

            command.Parameters.AddWithValue("@timestamp", maquinaData.Timestamp.HasValue
                ? (object)maquinaData.Timestamp.Value
                : DBNull.Value); // Trata como DBNull se for null

            // Executa o comando
            await command.ExecuteNonQueryAsync();
            Console.WriteLine("Dados da máquina inseridos na tabela `producao` com sucesso.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao inserir dados na tabela `producao`: {ex.Message}");
        }
    }
}