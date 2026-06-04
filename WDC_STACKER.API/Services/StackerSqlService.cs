using Microsoft.Data.SqlClient;
using System.Data;
using WDC_STACKER.API.Models.Stacker;

namespace WDC_STACKER.API.Services;

public class StackerSqlService
{
    private readonly string _connectionString;

    public StackerSqlService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("WdcStackerDb")
            ?? throw new InvalidOperationException("Missing WdcStackerDb connection string.");
    }

    public async Task<bool> HolderExistsAsync(string holder)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.YourTable
            WHERE Holder = @Holder;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@Holder", SqlDbType.NVarChar, 50).Value = holder;

        await connection.OpenAsync();

        var count = (int)await command.ExecuteScalarAsync();
        return count > 0;
    }
    public async Task<List<BoxView>> GetBoxListCountAndPercentageAsync(int baselineCount)
    {
        const string sql = """
        SELECT 
            bd.[BOXNO],
            bd.[RACKNUM],
            bd.[LAYERROWNUM],
            bd.[LAYERCOLNUM],
            COUNT(ha.[BOXNAME]) AS BoxListCount,
            CAST(
                (COUNT(ha.[BOXNAME]) * 100.0) / NULLIF(@BaselineCount, 0) 
            AS DECIMAL(18, 2)) AS BoxListPercentage
        FROM [BOXMANAGEMENT].[BOX].[BOXDETAILS] bd
        LEFT JOIN [BOXMANAGEMENT].[BOX].[HOLDER_ASSIGN] ha 
            ON bd.[BOXNO] = ha.[BOXNAME]
        GROUP BY bd.[BOXNO], bd.[RACKNUM], bd.[LAYERROWNUM],bd.[LAYERCOLNUM]
        ORDER BY bd.[BOXNO];
        """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@BaselineCount", SqlDbType.Int).Value = baselineCount;

        await connection.OpenAsync();

        var results = new List<BoxView>();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            results.Add(new BoxView
            {
                BoxNo = reader.GetString(0),
                RackNum = reader.GetInt16(1),
                LayerRowNum = reader.GetInt16(2),
                LayerColNum = reader.GetInt16(3),
                BoxListCount = reader.GetInt32(4),
                BoxListPercentage = reader.GetDecimal(5)
            });
        }

        return results; 
    }
}