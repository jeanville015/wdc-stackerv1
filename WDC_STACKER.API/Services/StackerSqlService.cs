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
    public async Task<List<BoxView>> GetBoxListCountAndPercentageAsync(int baselineCount, string process)
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
            AS DECIMAL(18, 2)) AS BoxListPercentage,
            MAX(
                CASE
                    WHEN UPPER(LTRIM(RTRIM(ISNULL(ha.[STATUS], '')))) = 'RELEASE'
                    THEN 1
                    ELSE 0
                END
            ) AS HasReleaseStatus
        FROM [BOXMANAGEMENT].[BOX].[BOXDETAILS] bd
        LEFT JOIN [BOXMANAGEMENT].[BOX].[HOLDER_ASSIGN] ha 
         ON bd.[BOXNO] = ha.[BOXNAME]
        AND UPPER(LTRIM(RTRIM(ISNULL(ha.[PROCESS], '')))) = @CLIENTCODE
        WHERE UPPER(LTRIM(RTRIM(ISNULL(bd.[CLIENTCODE], '')))) = @CLIENTCODE
        GROUP BY bd.[BOXNO], bd.[RACKNUM], bd.[LAYERROWNUM],bd.[LAYERCOLNUM]
        ORDER BY bd.[BOXNO];
        """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@BaselineCount", SqlDbType.Int).Value = baselineCount;
        command.Parameters.Add("@CLIENTCODE", SqlDbType.VarChar, 10).Value = process.Trim().ToUpperInvariant();

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
                BoxListPercentage = reader.GetDecimal(5),
                HasReleaseStatus = reader.GetInt32(6) == 1
            });
        }

        return results;
    }

    public async Task<List<BoxView>> GetFgiBoxListCountAndPercentageAsync(int baselineCount, string process)
    {
        const string sql = """
        SELECT
            bd.[BOXNO],
            bd.[RACKNUM],
            bd.[LAYERROWNUM],
            bd.[LAYERCOLNUM],
            COUNT(sbd.[BOXNO]) AS BoxListCount,
            CAST(
                (COUNT(sbd.[BOXNO]) * 100.0) / NULLIF(@BaselineCount, 0)
            AS DECIMAL(18, 2)) AS BoxListPercentage,
            MAX(
                CASE
                    WHEN UPPER(LTRIM(RTRIM(ISNULL(sbd.[SHIPBOXSTATUS], '')))) = 'RELEASE'
                    THEN 1
                    ELSE 0
                END
            ) AS HasReleaseStatus
        FROM [BOXMANAGEMENT].[BOX].[BOXDETAILS] bd
        LEFT JOIN [BOXMANAGEMENT].[BOX].[SHIPBOXDETAILS] sbd
            ON bd.[BOXNO] = sbd.[BOXNO]
        WHERE UPPER(LTRIM(RTRIM(ISNULL(bd.[CLIENTCODE], '')))) = @CLIENTCODE
        GROUP BY bd.[BOXNO], bd.[RACKNUM], bd.[LAYERROWNUM], bd.[LAYERCOLNUM]
        ORDER BY bd.[BOXNO];
        """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@BaselineCount", SqlDbType.Int).Value = baselineCount;
        command.Parameters.Add("@CLIENTCODE", SqlDbType.VarChar, 10).Value = process.Trim().ToUpperInvariant();

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
                BoxListPercentage = reader.GetDecimal(5),
                HasReleaseStatus = reader.GetInt32(6) == 1
            });
        }

        return results;
    }

    public async Task<List<ShipBoxView>> GetFgiShipBoxesByBoxNoAsync(string boxNo, int baselineCount, string process)
    {
        const string sql = """
        SELECT
            sbd.[SHIPBOXNAME],
            sbd.[SHIPBOXSTATUS],
            sbd.[SHIPBOXNUM],
            sbd.[LAYERROWNUM],
            sbd.[LAYERCOLNUM],
            COUNT(ha.[BOXNAME]) AS ShipBoxListCount,
            CAST(
                (COUNT(ha.[BOXNAME]) * 100.0) / NULLIF(@BaselineCount, 0)
            AS DECIMAL(18, 2)) AS ShipBoxListPercentage
        FROM [BOXMANAGEMENT].[BOX].[SHIPBOXDETAILS] sbd
        LEFT JOIN [BOXMANAGEMENT].[BOX].[HOLDER_ASSIGN] ha
            ON sbd.[SHIPBOXNAME] = ha.[SHIPBOXNAME]
            AND sbd.[BOXNO] = ha.[BOXNAME]
            AND UPPER(LTRIM(RTRIM(ISNULL(ha.[PROCESS], '')))) = @PROCESS
        WHERE sbd.[BOXNO] = @BoxNo
        GROUP BY
            sbd.[SHIPBOXNAME],
            sbd.[SHIPBOXSTATUS],
            sbd.[SHIPBOXNUM],
            sbd.[LAYERROWNUM],
            sbd.[LAYERCOLNUM]
        ORDER BY sbd.[SHIPBOXNAME];
        """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@BoxNo", SqlDbType.VarChar, 50).Value = boxNo;
        command.Parameters.Add("@BaselineCount", SqlDbType.Int).Value = baselineCount;
        command.Parameters.Add("@PROCESS", SqlDbType.VarChar, 10).Value = process.Trim().ToUpperInvariant();

        await connection.OpenAsync();

        var results = new List<ShipBoxView>();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var shipBoxStatus = Convert.ToString(reader["SHIPBOXSTATUS"])?.Trim() ?? string.Empty;

            results.Add(new ShipBoxView
            {
                BoxNo = boxNo,
                ShipBoxName = Convert.ToString(reader["SHIPBOXNAME"])?.Trim() ?? string.Empty,
                ShipBoxStatus = shipBoxStatus,
                ShipBoxNum = Convert.ToInt32(reader["SHIPBOXNUM"]),
                LayerRowNum = Convert.ToInt32(reader["LAYERROWNUM"]),
                LayerColNum = Convert.ToInt32(reader["LAYERCOLNUM"]),
                ShipBoxListCount = Convert.ToInt32(reader["ShipBoxListCount"]),
                ShipBoxListPercentage = Convert.ToDecimal(reader["ShipBoxListPercentage"]),
                HasReleaseStatus = string.Equals(shipBoxStatus, "RELEASE", StringComparison.OrdinalIgnoreCase)
            });
        }

        return results;
    }

    public async Task<bool> BoxNoExistsAsync(string boxNo, string clientCode)
    {
        const string sql = """
        SELECT COUNT(1)
        FROM [BOXMANAGEMENT].[BOX].[BOXDETAILS]
        WHERE [BOXNO] = @BOXNO
        AND UPPER(LTRIM(RTRIM(ISNULL([CLIENTCODE], '')))) = @CLIENTCODE;
        """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@BOXNO", SqlDbType.VarChar, 50).Value = boxNo;
        command.Parameters.Add("@CLIENTCODE", SqlDbType.VarChar, 10).Value = clientCode.Trim().ToUpperInvariant();

        await connection.OpenAsync();

        var count = (int)(await command.ExecuteScalarAsync() ?? 0);
        return count > 0;
    }

    public async Task InsertAssignmentAsync(BoxDetailsInsertData? boxDetails, HolderAssignInsertData holderAssign)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

        try
        {
            if (boxDetails is not null)
            {
                await InsertBoxDetailsAsync(boxDetails, connection, transaction);
            }

            await InsertHolderAssignAsync(holderAssign, connection, transaction);
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static async Task InsertBoxDetailsAsync(BoxDetailsInsertData data, SqlConnection connection, SqlTransaction transaction)
    {
        const string sql = """
        INSERT INTO [BOXMANAGEMENT].[BOX].[BOXDETAILS]
            ([BOXNO], [CLIENTCODE], [RACKNUM], [LAYERROWNUM], [LAYERCOLNUM], [UPDATEBY], [UPDATETS])
        VALUES
            (@BOXNO, @CLIENTCODE, @RACKNUM, @LAYERROWNUM, @LAYERCOLNUM, @UPDATEBY, @UPDATETS);
        """;

        await using var command = new SqlCommand(sql, connection, transaction);

        command.Parameters.Add("@BOXNO", SqlDbType.VarChar, 50).Value = data.BoxNo;
        command.Parameters.Add("@CLIENTCODE", SqlDbType.VarChar, 10).Value = data.ClientCode;
        command.Parameters.Add("@RACKNUM", SqlDbType.SmallInt).Value = data.RackNum;
        command.Parameters.Add("@LAYERROWNUM", SqlDbType.SmallInt).Value = data.LayerRowNum;
        command.Parameters.Add("@LAYERCOLNUM", SqlDbType.SmallInt).Value = data.LayerColNum;
        command.Parameters.Add("@UPDATEBY", SqlDbType.VarChar, 50).Value = data.UpdateBy;
        command.Parameters.Add("@UPDATETS", SqlDbType.DateTime).Value = data.UpdateTs;

        await command.ExecuteNonQueryAsync();
    }

    private static async Task InsertHolderAssignAsync(HolderAssignInsertData data, SqlConnection connection, SqlTransaction transaction)
    {

        const string sql = """
        INSERT INTO [BOXMANAGEMENT].[BOX].[HOLDER_ASSIGN]
            ([HOLDER], [BOXNAME], [PRODUCTNAME], [LEC], [Factory], [PROCESS], [UPDATEBY], [UPDATETS])
        VALUES
            (@HOLDER, @BOXNAME, @PRODUCTNAME, @LEC, @Factory, @PROCESS, @UPDATEBY, @UPDATETS);
        """;

        await using var command = new SqlCommand(sql, connection, transaction);

        command.Parameters.Add("@HOLDER", SqlDbType.VarChar, 50).Value = data.Holder;
        command.Parameters.Add("@BOXNAME", SqlDbType.VarChar, 50).Value = data.BoxName;
        command.Parameters.Add("@PRODUCTNAME", SqlDbType.NChar, 10).Value = data.ProductName;
        command.Parameters.Add("@LEC", SqlDbType.VarChar, 50).Value = data.Lec;
        command.Parameters.Add("@Factory", SqlDbType.VarChar, 50).Value = data.Factory;
        command.Parameters.Add("@PROCESS", SqlDbType.VarChar, 10).Value = data.Process;
        command.Parameters.Add("@UPDATEBY", SqlDbType.VarChar, 50).Value = data.UpdateBy;
        command.Parameters.Add("@UPDATETS", SqlDbType.DateTime).Value = data.UpdateTs;

        await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> HolderAssignExistsAsync(string holder, string process)
    {
        const string sql = """
        SELECT COUNT(1)
        FROM [BOXMANAGEMENT].[BOX].[HOLDER_ASSIGN]
        WHERE [HOLDER] = @HOLDER
          AND UPPER(LTRIM(RTRIM(ISNULL([PROCESS], '')))) = @PROCESS;
        """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@HOLDER", SqlDbType.VarChar, 50).Value = holder;
        command.Parameters.Add("@PROCESS", SqlDbType.VarChar, 10).Value = process.Trim().ToUpperInvariant();

        await connection.OpenAsync();

        var count = (int)(await command.ExecuteScalarAsync() ?? 0);
        return count > 0;
    }

    public async Task<List<BoxAssignment>> GetBoxAssignmentsAsync(string boxName, string process)
    {
        const string sql = """
        SELECT
            [HOLDER],
            [PRODUCTNAME],
            [FACTORY],
            [LEC],
            [STATUS]
        FROM [BOXMANAGEMENT].[BOX].[HOLDER_ASSIGN]
        WHERE [BOXNAME] = @BOXNAME
        AND UPPER(LTRIM(RTRIM(ISNULL([PROCESS], '')))) = @PROCESS
        ORDER BY [HOLDER];
        """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@BOXNAME", SqlDbType.VarChar, 50).Value = boxName;
        command.Parameters.Add("@PROCESS", SqlDbType.VarChar, 10).Value = process.Trim().ToUpperInvariant();

        await connection.OpenAsync();

        var results = new List<BoxAssignment>();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            results.Add(new BoxAssignment
            {
                Holder = Convert.ToString(reader["HOLDER"])?.Trim() ?? string.Empty,
                ProductName = Convert.ToString(reader["PRODUCTNAME"])?.Trim() ?? string.Empty,
                Factory = Convert.ToString(reader["FACTORY"])?.Trim() ?? string.Empty,
                Lec = Convert.ToString(reader["LEC"])?.Trim() ?? string.Empty,
                Status = Convert.ToString(reader["STATUS"])?.Trim() ?? string.Empty
            });
        }

        return results;
    }

    public async Task<List<BoxAssignment>> GetShipBoxAssignmentsAsync(string boxName, string shipBoxName, string process)
    {
        const string sql = """
    SELECT
        [HOLDER],
        [PRODUCTNAME],
        [FACTORY],
        [LEC],
        [PARTNUM],
        [PENNUM],
        [STATUS]
    FROM [BOXMANAGEMENT].[BOX].[HOLDER_ASSIGN]
    WHERE [SHIPBOXNAME] = @SHIPBOXNAME
        AND [BOXNAME] = @BOXNAME
        AND UPPER(LTRIM(RTRIM(ISNULL([PROCESS], '')))) = @PROCESS
    ORDER BY [HOLDER];
    """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@SHIPBOXNAME", SqlDbType.VarChar, 50).Value = shipBoxName;
        command.Parameters.Add("@BOXNAME", SqlDbType.VarChar, 50).Value = boxName;
        command.Parameters.Add("@PROCESS", SqlDbType.VarChar, 10).Value = process.Trim().ToUpperInvariant();

        await connection.OpenAsync();

        var results = new List<BoxAssignment>();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            results.Add(new BoxAssignment
            {
                Holder = Convert.ToString(reader["HOLDER"])?.Trim() ?? string.Empty,
                ProductName = Convert.ToString(reader["PRODUCTNAME"])?.Trim() ?? string.Empty,
                Factory = Convert.ToString(reader["FACTORY"])?.Trim() ?? string.Empty,
                Lec = Convert.ToString(reader["LEC"])?.Trim() ?? string.Empty,
                Partnum = Convert.ToString(reader["PARTNUM"])?.Trim() ?? string.Empty,
                Pennum = Convert.ToString(reader["PENNUM"])?.Trim() ?? string.Empty,
                Status = Convert.ToString(reader["STATUS"])?.Trim() ?? string.Empty
            });
        }

        return results;
    }

    public async Task<bool> ShipBoxNameExistsAsync(string shipBoxName)
    {
        const string sql = """
    SELECT COUNT(1)
    FROM [BOXMANAGEMENT].[BOX].[SHIPBOXDETAILS]
    WHERE [SHIPBOXNAME] = @SHIPBOXNAME;
    """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@SHIPBOXNAME", SqlDbType.VarChar, 50).Value = shipBoxName;

        await connection.OpenAsync();

        var count = (int)(await command.ExecuteScalarAsync() ?? 0);
        return count > 0;
    }

    public async Task InsertFgiAssignmentAsync(
        BoxDetailsInsertData? boxDetails,
        ShipBoxDetailsInsertData? shipBoxDetails,
        HolderAssignInsertData holderAssign)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

        try
        {
            if (boxDetails is not null)
            {
                await InsertBoxDetailsAsync(boxDetails, connection, transaction);
            }

            if (shipBoxDetails is not null)
            {
                await InsertShipBoxDetailsAsync(shipBoxDetails, connection, transaction);
            }

            await InsertFgiHolderAssignAsync(holderAssign, connection, transaction);
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static async Task InsertShipBoxDetailsAsync(
        ShipBoxDetailsInsertData data,
        SqlConnection connection,
        SqlTransaction transaction)
    {
        const string sql = """
    INSERT INTO [BOXMANAGEMENT].[BOX].[SHIPBOXDETAILS]
        ([BOXNO], [SHIPBOXNAME], [SHIPBOXSTATUS], [SHIPBOXNUM], [LAYERROWNUM], [LAYERCOLNUM], [UPDATEBY], [UPDATETS])
    VALUES
        (@BOXNO, @SHIPBOXNAME, @SHIPBOXSTATUS, @SHIPBOXNUM, @LAYERROWNUM, @LAYERCOLNUM, @UPDATEBY, @UPDATETS);
    """;

        await using var command = new SqlCommand(sql, connection, transaction);

        command.Parameters.Add("@BOXNO", SqlDbType.VarChar, 50).Value = data.BoxNo;
        command.Parameters.Add("@SHIPBOXNAME", SqlDbType.VarChar, 50).Value = data.ShipBoxName;
        command.Parameters.Add("@SHIPBOXSTATUS", SqlDbType.VarChar, 50).Value = data.ShipBoxStatus;
        command.Parameters.Add("@SHIPBOXNUM", SqlDbType.Int).Value = data.ShipBoxNum;
        command.Parameters.Add("@LAYERROWNUM", SqlDbType.SmallInt).Value = data.LayerRowNum;
        command.Parameters.Add("@LAYERCOLNUM", SqlDbType.SmallInt).Value = data.LayerColNum;
        command.Parameters.Add("@UPDATEBY", SqlDbType.VarChar, 50).Value = data.UpdateBy;
        command.Parameters.Add("@UPDATETS", SqlDbType.DateTime).Value = data.UpdateTs;

        await command.ExecuteNonQueryAsync();
    }

    private static async Task InsertFgiHolderAssignAsync(
        HolderAssignInsertData data,
        SqlConnection connection,
        SqlTransaction transaction)
    {
        const string sql = """
    INSERT INTO [BOXMANAGEMENT].[BOX].[HOLDER_ASSIGN]
        ([HOLDER], [BOXNAME], [SHIPBOXNAME], [PRODUCTNAME], [LEC], [Factory], [PROCESS], [UPDATEBY], [UPDATETS])
    VALUES
        (@HOLDER, @BOXNAME, @SHIPBOXNAME, @PRODUCTNAME, @LEC, @Factory, @PROCESS, @UPDATEBY, @UPDATETS);
    """;

        await using var command = new SqlCommand(sql, connection, transaction);

        command.Parameters.Add("@HOLDER", SqlDbType.VarChar, 50).Value = data.Holder;
        command.Parameters.Add("@BOXNAME", SqlDbType.VarChar, 50).Value = data.BoxName;
        command.Parameters.Add("@SHIPBOXNAME", SqlDbType.VarChar, 50).Value = data.ShipBoxName;
        command.Parameters.Add("@PRODUCTNAME", SqlDbType.NChar, 10).Value = data.ProductName;
        command.Parameters.Add("@LEC", SqlDbType.VarChar, 50).Value = data.Lec;
        command.Parameters.Add("@Factory", SqlDbType.VarChar, 50).Value = data.Factory;
        command.Parameters.Add("@PROCESS", SqlDbType.VarChar, 10).Value = data.Process;
        command.Parameters.Add("@UPDATEBY", SqlDbType.VarChar, 50).Value = data.UpdateBy;
        command.Parameters.Add("@UPDATETS", SqlDbType.DateTime).Value = data.UpdateTs;

        await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> DisassociateHolderAsync(string holder, string process)
    {
        const string sql = """
        DELETE
        FROM [BOXMANAGEMENT].[BOX].[HOLDER_ASSIGN]
        WHERE [HOLDER] = @HOLDER
          AND [STATUS] = 'RELEASE'
          AND UPPER(LTRIM(RTRIM(ISNULL([PROCESS], '')))) = @PROCESS;
        """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@HOLDER", SqlDbType.VarChar, 50).Value = holder;
        command.Parameters.Add("@PROCESS", SqlDbType.VarChar, 10).Value = process.Trim().ToUpperInvariant();

        await connection.OpenAsync();

        return await command.ExecuteNonQueryAsync() == 1;
    }

}
