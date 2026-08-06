using Microsoft.Data.SqlClient;
using System.Data;
using WDC_STACKER.API.Models.Stacker;

namespace WDC_STACKER.API.Services;

public class KittingRequestService
{
    private readonly string _connectionString;

    public KittingRequestService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("WdcStackerDb")
            ?? throw new InvalidOperationException("Missing WdcStackerDb connection string.");
    }

    public async Task<List<KittingRequest>> GetKittingRequestsAsync()
    {
        const string sql = """
        SELECT
            [ID],
            [GRADE],
            [SLIDERPARTNUMBER],
            [TOTAL],
            [LEC],
            [PENNUM],
            [ACKNOWLEDGEBY],
            [ACTUALOUTPUT]
        FROM [HGA].[KITTING_REQUESTS]
        ORDER BY [ID];
        """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);

        await connection.OpenAsync();

        var results = new List<KittingRequest>();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            results.Add(new KittingRequest
            {
                ID = reader.GetInt32(0),
                GRADE = reader.GetString(1),
                SLIDERPARTNUMBER = reader.GetString(2),
                TOTAL = reader.GetInt32(3),
                LEC = reader.IsDBNull(4) ? null : reader.GetString(4),
                PENNUM = reader.IsDBNull(5) ? null : reader.GetString(5),
                ACKNOWLEDGEBY = reader.IsDBNull(6) ? null : reader.GetString(6),
                ACTUALOUTPUT = reader.IsDBNull(7) ? null : (int?)reader.GetInt32(7)
            });
        }

        return results;
    }

    public async Task<bool> AcknowledgeKittingRequestAsync(int id, string acknowledgedBy)
    {
        const string sql = """
        UPDATE [HGA].[KITTING_REQUESTS]
        SET [ACKNOWLEDGEBY] = @ACKNOWLEDGEBY
        WHERE [ID] = @ID;
        """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@ID", SqlDbType.Int).Value = id;
        command.Parameters.Add("@ACKNOWLEDGEBY", SqlDbType.VarChar, 50).Value = acknowledgedBy;

        await connection.OpenAsync();

        return await command.ExecuteNonQueryAsync() == 1;
    }
}
