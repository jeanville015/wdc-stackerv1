using Microsoft.Data.SqlClient;
using System.Data;
using WDC_STACKER.API.Models.Stacker;

namespace WDC_STACKER.API.Services;

public class StackerSqlService
{
    private readonly string _connectionString;
    private const int FgiWithdrawalQtyTolerance = 500;

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
            ) AS HasReleaseStatus,
            MAX(boxMeta.[PARTNUM]) AS [PARTNUM],
            MAX(boxMeta.[PENNUM]) AS [PENNUM],
            MAX(boxMeta.[PRODUCTNAME]) AS [PRODUCTNAME]
        FROM [BOXMANAGEMENT].[BOX].[BOXDETAILS] bd
        LEFT JOIN [BOXMANAGEMENT].[BOX].[SHIPBOXDETAILS] sbd
            ON bd.[BOXNO] = sbd.[BOXNO]
        LEFT JOIN
        (
            SELECT
                [BOXNAME],
                MAX(NULLIF(LTRIM(RTRIM([PARTNUM])), '')) AS [PARTNUM],
                MAX(NULLIF(LTRIM(RTRIM([PENNUM])), '')) AS [PENNUM],
                MAX(NULLIF(LTRIM(RTRIM([PRODUCTNAME])), '')) AS [PRODUCTNAME]
            FROM [BOXMANAGEMENT].[BOX].[HOLDER_ASSIGN]
            WHERE UPPER(LTRIM(RTRIM(ISNULL([PROCESS], '')))) = @CLIENTCODE
            GROUP BY [BOXNAME]
            HAVING
                COUNT(*) = COUNT(NULLIF(LTRIM(RTRIM([PARTNUM])), ''))
                AND COUNT(
                    DISTINCT UPPER(NULLIF(LTRIM(RTRIM([PARTNUM])), ''))
                ) = 1
                AND COUNT(*) = COUNT(NULLIF(LTRIM(RTRIM([PRODUCTNAME])), ''))
                AND COUNT(
                    DISTINCT UPPER(NULLIF(LTRIM(RTRIM([PRODUCTNAME])), ''))
                ) = 1
                AND
                (
                    COUNT(NULLIF(LTRIM(RTRIM([PENNUM])), '')) = 0
                    OR
                    (
                        COUNT(*) = COUNT(NULLIF(LTRIM(RTRIM([PENNUM])), ''))
                        AND COUNT(
                            DISTINCT UPPER(NULLIF(LTRIM(RTRIM([PENNUM])), ''))
                        ) = 1
                    )
                )
        ) boxMeta
            ON bd.[BOXNO] = boxMeta.[BOXNAME]
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
                HasReleaseStatus = reader.GetInt32(6) == 1,
                PartNum = reader["PARTNUM"] is DBNull
                    ? null
                    : Convert.ToString(reader["PARTNUM"])?.Trim(),
                PenNum = reader["PENNUM"] is DBNull
                    ? null
                    : Convert.ToString(reader["PENNUM"])?.Trim(),
                ProductName = reader["PRODUCTNAME"] is DBNull
                    ? null
                    : Convert.ToString(reader["PRODUCTNAME"])?.Trim(),
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
            CASE
                WHEN COUNT(ha.[HOLDER]) > 0
                 AND COUNT(NULLIF(LTRIM(RTRIM(ha.[LEC])), '')) = COUNT(ha.[HOLDER])
                 AND COUNT(
                        DISTINCT UPPER(NULLIF(LTRIM(RTRIM(ha.[LEC])), ''))
                     ) = 1
                THEN MAX(NULLIF(LTRIM(RTRIM(ha.[LEC])), ''))
                ELSE NULL
            END AS [LEC],
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
                Lec = Convert.ToString(reader["LEC"])?.Trim() ?? string.Empty,
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

    public async Task<List<FgiWithdrawalRequestView>> GetFgiWithdrawalRequestsAsync()
    {
        const string sql = """
        SELECT
            [REQUESTID],
            [DATE],
            [REQUESTOR],
            [SHIFT],
            [MODEL],
            [CATEGORY],
            [GRADE],
            [SLIDERPARTNUMBER],
            [HEADTYPE],
            [TOTAL],
            [REMARKS],
            [ACKNOWLEDGEBY],
            [ACTUALOUTPUT],
            [STATUS],
            [LEC],
            [PENNUM]
        FROM [BOXMANAGEMENT].[HGA].[KITTING_REQUEST]
        ORDER BY [DATE] DESC;
        """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);

        await connection.OpenAsync();

        var results = new List<FgiWithdrawalRequestView>();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            results.Add(new FgiWithdrawalRequestView
            {
                RequestId = Convert.ToInt64(reader["REQUESTID"]),
                Date = reader["DATE"] is DBNull
                    ? null
                    : Convert.ToDateTime(reader["DATE"]),
                Requestor = Convert.ToString(reader["REQUESTOR"])?.Trim() ?? "",
                Shift = Convert.ToString(reader["SHIFT"])?.Trim() ?? "",
                Model = Convert.ToString(reader["MODEL"])?.Trim() ?? "",
                Category = Convert.ToString(reader["CATEGORY"])?.Trim() ?? "",
                Grade = Convert.ToString(reader["GRADE"])?.Trim() ?? "",
                SliderPartNumber = Convert.ToString(reader["SLIDERPARTNUMBER"])?.Trim() ?? "",
                HeadType = Convert.ToString(reader["HEADTYPE"])?.Trim() ?? "",
                Total = reader["TOTAL"] is DBNull
                    ? null
                    : Convert.ToInt32(reader["TOTAL"]),
                Remarks = Convert.ToString(reader["REMARKS"])?.Trim() ?? "",
                AcknowledgeBy = Convert.ToString(reader["ACKNOWLEDGEBY"])?.Trim() ?? "",
                ActualOutput = reader["ACTUALOUTPUT"] is DBNull
                    ? null
                    : Convert.ToInt32(reader["ACTUALOUTPUT"]),
                Status = Convert.ToString(reader["STATUS"])?.Trim() ?? "",
                Lec = Convert.ToString(reader["LEC"])?.Trim() ?? "",
                PenNum = Convert.ToString(reader["PENNUM"])?.Trim() ?? ""
            });
        }

        return results;
    }

    public async Task<FgiWithdrawalDisassociationPreviewView>
    GetFgiWithdrawalDisassociationPreviewAsync(string lec, string? penNum, int total)
    {
        const string sql = """
        DECLARE @MaximumTotalQty bigint =
            CONVERT(bigint, @TOTAL) +
            CONVERT(bigint, @TOLERANCE);

        WITH OrderedCandidates AS
        (
            SELECT
                ROW_NUMBER() OVER
                (
                    ORDER BY
                        CASE
                            WHEN HA.[UPDATETS] IS NULL
                            THEN 1
                            ELSE 0
                        END,
                        HA.[UPDATETS] ASC,
                        HA.[HOLDER] ASC
                ) AS [RowNumber],
                LTRIM(RTRIM(HA.[HOLDER])) AS [HOLDER],
                CONVERT(
                    bigint,
                    ISNULL(HA.[QTY], 0)
                ) AS [QTY],
                HA.[UPDATETS]
            FROM [BOXMANAGEMENT].[BOX].[HOLDER_ASSIGN] HA
            WHERE UPPER(
                    LTRIM(
                        RTRIM(
                            ISNULL(HA.[PROCESS], '')
                        )
                    )
                ) = 'FGI'
                AND UPPER(
                    LTRIM(
                        RTRIM(
                            ISNULL(HA.[LEC], '')
                        )
                    )
                ) =
                UPPER(LTRIM(RTRIM(@LEC)))
                AND
                (
                    NULLIF(
                        LTRIM(RTRIM(HA.[PENNUM])),
                        ''
                    ) IS NULL
                    OR
                    (
                        @PENNUM IS NOT NULL
                        AND UPPER(
                            LTRIM(RTRIM(HA.[PENNUM]))
                        ) =
                        UPPER(
                            LTRIM(RTRIM(@PENNUM))
                        )
                    )
                )
                AND NULLIF(
                    LTRIM(RTRIM(HA.[HOLDER])),
                    ''
                ) IS NOT NULL
                AND ISNULL(HA.[QTY], 0) > 0
        ),
        FifoSelection AS
        (
            /*
             * First FIFO record.
             *
             * TOTAL = 0 means that the target is already
             * reached, so no record is included.
             */
            SELECT
                Candidate.[RowNumber],
                Candidate.[HOLDER],
                Candidate.[QTY],
                Candidate.[UPDATETS],
                CAST(
                    CASE
                        WHEN @TOTAL > 0
                            AND Candidate.[QTY] <=
                                @MaximumTotalQty
                        THEN Candidate.[QTY]
                        ELSE 0
                    END
                    AS bigint
                ) AS [RunningTotal],
                CAST(
                    CASE
                        WHEN @TOTAL > 0
                            AND Candidate.[QTY] <=
                                @MaximumTotalQty
                        THEN 1
                        ELSE 0
                    END
                    AS bit
                ) AS [IsIncluded]
            FROM OrderedCandidates Candidate
            WHERE Candidate.[RowNumber] = 1

            UNION ALL

            /*
             * Continue evaluating FIFO records only while
             * RunningTotal is below TOTAL.
             *
             * The next Holder may cross TOTAL, but the
             * resulting value cannot exceed TOTAL + 500.
             */
            SELECT
                Candidate.[RowNumber],
                Candidate.[HOLDER],
                Candidate.[QTY],
                Candidate.[UPDATETS],
                CAST(
                    CASE
                        WHEN Selected.[RunningTotal] <
                                CONVERT(bigint, @TOTAL)
                            AND Candidate.[QTY] <=
                                @MaximumTotalQty -
                                Selected.[RunningTotal]
                        THEN
                            Selected.[RunningTotal] +
                            Candidate.[QTY]
                        ELSE
                            Selected.[RunningTotal]
                    END
                    AS bigint
                ) AS [RunningTotal],
                CAST(
                    CASE
                        WHEN Selected.[RunningTotal] <
                                CONVERT(bigint, @TOTAL)
                            AND Candidate.[QTY] <=
                                @MaximumTotalQty -
                                Selected.[RunningTotal]
                        THEN 1
                        ELSE 0
                    END
                    AS bit
                ) AS [IsIncluded]
            FROM FifoSelection Selected
            INNER JOIN OrderedCandidates Candidate
                ON Candidate.[RowNumber] =
                    Selected.[RowNumber] + 1
        )
        SELECT
            [HOLDER],
            [QTY],
            [UPDATETS],
            [RunningTotal],
            [IsIncluded]
        FROM FifoSelection
        ORDER BY [RowNumber]
        OPTION (MAXRECURSION 0);
        """;

        await using var connection =
            new SqlConnection(_connectionString);
        await using var command =
            new SqlCommand(sql, connection);

        command.Parameters.Add(
            "@LEC",
            SqlDbType.VarChar,
            50
        ).Value = lec.Trim();

        command.Parameters.Add(
            "@PENNUM",
            SqlDbType.VarChar,
            50
        ).Value = string.IsNullOrWhiteSpace(penNum)
            ? DBNull.Value
            : penNum.Trim();

        command.Parameters.Add(
            "@TOTAL",
            SqlDbType.Int
        ).Value = total;

        command.Parameters.Add(
            "@TOLERANCE",
            SqlDbType.Int
        ).Value = FgiWithdrawalQtyTolerance;

        await connection.OpenAsync();

        var records =
            new List<FgiWithdrawalSourceRecordView>();
        var totalQty = 0L;

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var runningTotal =
                Convert.ToInt64(reader["RunningTotal"]);

            records.Add(new FgiWithdrawalSourceRecordView
            {
                Holder =
                    Convert.ToString(reader["HOLDER"])?.Trim() ?? "",
                Qty = Convert.ToInt64(reader["QTY"]),
                UpdateTs = reader["UPDATETS"] is DBNull
                    ? null
                    : Convert.ToDateTime(reader["UPDATETS"]),
                RunningTotal = runningTotal,
                IsIncluded =
                    Convert.ToBoolean(reader["IsIncluded"])
            });

            totalQty = runningTotal;
        }

        return new FgiWithdrawalDisassociationPreviewView
        {
            Total = total,
            TotalQty = totalQty,
            Tolerance = FgiWithdrawalQtyTolerance,
            MaximumTotalQty =
                (long)total + FgiWithdrawalQtyTolerance,
            SourceRecords = records
        };
    }

    public async Task<bool> AcknowledgeFgiWithdrawalRequestAsync(long requestId, string userId)
    {
        const string sql = """
            UPDATE [BOXMANAGEMENT].[HGA].[KITTING_REQUEST]
            SET [ACKNOWLEDGEBY] = @USERID
            WHERE [REQUESTID] = @REQUESTID
              AND NULLIF(LTRIM(RTRIM([ACKNOWLEDGEBY])), '') IS NULL;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@REQUESTID", SqlDbType.BigInt).Value = requestId;
        command.Parameters.Add("@USERID", SqlDbType.VarChar, 50).Value = userId;

        await connection.OpenAsync();

        return await command.ExecuteNonQueryAsync() == 1;
    }

    public async Task<FgiWithdrawalRackView?> GetFgiWithdrawalLayoutAsync(string lec, string process)
    {
        const string sql = """
        SELECT
            BD.[BOXNO],
            BD.[RACKNUM],
            BD.[LAYERROWNUM] AS BoxLayerRowNum,
            BD.[LAYERCOLNUM] AS BoxLayerColNum,
            SBD.[SHIPBOXNAME],
            SBD.[SHIPBOXNUM],
            SBD.[LAYERROWNUM] AS ShipBoxLayerRowNum,
            SBD.[LAYERCOLNUM] AS ShipBoxLayerColNum,
            HA.[HOLDER],
            HA.[QTY]
        FROM [BOXMANAGEMENT].[BOX].[BOXDETAILS] BD
        LEFT JOIN [BOXMANAGEMENT].[BOX].[SHIPBOXDETAILS] SBD
            ON BD.[BOXNO] = SBD.[BOXNO]
        LEFT JOIN [BOXMANAGEMENT].[BOX].[HOLDER_ASSIGN] HA ON SBD.[BOXNO] = HA.[BOXNAME]
         AND SBD.[SHIPBOXNAME] = HA.[SHIPBOXNAME]
        WHERE UPPER(LTRIM(RTRIM(ISNULL(HA.[PROCESS], '')))) = @PROCESS
          AND HA.[LEC] = @LEC
        ORDER BY
            BD.[RACKNUM],
            BD.[LAYERROWNUM],
            BD.[LAYERCOLNUM],
            SBD.[LAYERROWNUM],
            SBD.[LAYERCOLNUM],
            HA.[HOLDER];
        """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@LEC", SqlDbType.VarChar, 50).Value = lec;
        command.Parameters.Add("@PROCESS", SqlDbType.VarChar, 10).Value =
            process.Trim().ToUpperInvariant();

        await connection.OpenAsync();

        var rows = new List<(
            string BoxNo,
            int RackNum,
            int BoxRow,
            int BoxColumn,
            string ShipBoxName,
            int ShipBoxNum,
            int ShipBoxRow,
            int ShipBoxColumn,
            string Holder,
            int Qty)>();

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            rows.Add((
                Convert.ToString(reader["BOXNO"])?.Trim() ?? "",
                Convert.ToInt32(reader["RACKNUM"]),
                Convert.ToInt32(reader["BoxLayerRowNum"]),
                Convert.ToInt32(reader["BoxLayerColNum"]),
                Convert.ToString(reader["SHIPBOXNAME"])?.Trim() ?? "",
                Convert.ToInt32(reader["SHIPBOXNUM"]),
                Convert.ToInt32(reader["ShipBoxLayerRowNum"]),
                Convert.ToInt32(reader["ShipBoxLayerColNum"]),
                Convert.ToString(reader["HOLDER"])?.Trim() ?? "",
                reader["QTY"] is DBNull ? 0 : Convert.ToInt32(reader["QTY"])
            ));
        }

        if (rows.Count == 0)
            return null;

        var rackNumbers = rows.Select(row => row.RackNum).Distinct().ToList();

        if (rackNumbers.Count != 1)
        {
            throw new InvalidOperationException(
                $"LEC '{lec}' maps to more than one rack.");
        }

        return new FgiWithdrawalRackView
        {
            RackNum = rackNumbers[0],
            Boxes = rows
                .GroupBy(row => new
                {
                    row.BoxNo,
                    row.BoxRow,
                    row.BoxColumn
                })
                .Select(boxGroup => new FgiWithdrawalBoxView
                {
                    BoxNo = boxGroup.Key.BoxNo,
                    LayerRowNum = boxGroup.Key.BoxRow,
                    LayerColNum = boxGroup.Key.BoxColumn,
                    ShipBoxes = boxGroup
                        .GroupBy(row => new
                        {
                            row.ShipBoxName,
                            row.ShipBoxNum,
                            row.ShipBoxRow,
                            row.ShipBoxColumn
                        })
                        .Select(shipBoxGroup => new FgiWithdrawalShipBoxView
                        {
                            ShipBoxName = shipBoxGroup.Key.ShipBoxName,
                            ShipBoxNum = shipBoxGroup.Key.ShipBoxNum,
                            LayerRowNum = shipBoxGroup.Key.ShipBoxRow,
                            LayerColNum = shipBoxGroup.Key.ShipBoxColumn,
                            Holders = shipBoxGroup
                                .Where(row => !string.IsNullOrWhiteSpace(row.Holder))
                                .GroupBy(row => new { row.Holder, row.Qty })
                                .Select(holderGroup => new FgiWithdrawalHolderView
                                {
                                    Holder = holderGroup.Key.Holder,
                                    Qty = holderGroup.Key.Qty
                                })
                                .OrderBy(holder => holder.Holder)
                                .ToList()
                        })
                        .OrderBy(shipBox => shipBox.LayerRowNum)
                        .ThenBy(shipBox => shipBox.LayerColNum)
                        .ToList()
                })
                .OrderBy(box => box.LayerRowNum)
                .ThenBy(box => box.LayerColNum)
                .ToList()
        };
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

    public async Task<bool> ShipBoxNameExistsAsync(
        string boxNo,
        string shipBoxName)
    {
        const string sql = """
        SELECT COUNT(1)
        FROM [BOXMANAGEMENT].[BOX].[SHIPBOXDETAILS]
        WHERE [BOXNO] = @BOXNO
          AND [SHIPBOXNAME] = @SHIPBOXNAME;
        """;

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);

        command.Parameters.Add("@BOXNO", SqlDbType.VarChar, 50).Value =
            boxNo.Trim();
        command.Parameters.Add("@SHIPBOXNAME", SqlDbType.VarChar, 50).Value =
            shipBoxName.Trim();

        await connection.OpenAsync();

        var count = (int)(await command.ExecuteScalarAsync() ?? 0);
        return count > 0;
    }

    public async Task InsertFgiAssignmentAsync(
        BoxDetailsInsertData? boxDetails,
        ShipBoxDetailsInsertData? shipBoxDetails,
        HolderAssignInsertData holderAssign)
    {
        ValidateFgiAssignmentInput(boxDetails, shipBoxDetails, holderAssign);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var transaction =
            (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            await AcquireFgiAssignmentLocksAsync(
                holderAssign,
                connection,
                transaction);

            await EnsureFgiBoxAsync(
                boxDetails,
                holderAssign,
                connection,
                transaction);

            var shipBoxCreated = await EnsureFgiShipBoxAsync(
                shipBoxDetails,
                holderAssign,
                connection,
                transaction);

            await ValidateFgiAssignmentAsync(
                holderAssign,
                shipBoxCreated,
                connection,
                transaction);

            await InsertFgiHolderAssignAsync(holderAssign, connection, transaction);
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static void ValidateFgiAssignmentInput(
        BoxDetailsInsertData? boxDetails,
        ShipBoxDetailsInsertData? shipBoxDetails,
        HolderAssignInsertData holderAssign)
    {
        if (!string.Equals(
                holderAssign.Process?.Trim(),
                "FGI",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "InsertFgiAssignmentAsync only accepts FGI assignments.");
        }

        ValidateRequiredFgiIdentifier(holderAssign.Holder, nameof(holderAssign.Holder));
        ValidateRequiredFgiIdentifier(holderAssign.BoxName, nameof(holderAssign.BoxName));
        ValidateRequiredFgiIdentifier(
            holderAssign.ShipBoxName,
            nameof(holderAssign.ShipBoxName),
            10);

        if (string.IsNullOrWhiteSpace(holderAssign.PartNum))
        {
            throw new InvalidOperationException(
                "PartNum is required for an FGI assignment.");
        }

        if (string.IsNullOrWhiteSpace(holderAssign.ProductName))
        {
            throw new InvalidOperationException(
                "ProductName is required for an FGI assignment.");
        }

        if (!holderAssign.Qty.HasValue)
        {
            throw new InvalidOperationException(
                "Qty is required for an FGI assignment.");
        }

        if (boxDetails is not null)
        {
            if (!SameFgiIdentifier(boxDetails.BoxNo, holderAssign.BoxName))
            {
                throw new InvalidOperationException(
                    "The BoxDetails BoxNo does not match the assignment BoxName.");
            }

            if (!string.Equals(
                    boxDetails.ClientCode?.Trim(),
                    "FGI",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "The new Box must use the FGI client code.");
            }
        }

        if (shipBoxDetails is not null &&
            (!SameFgiIdentifier(shipBoxDetails.BoxNo, holderAssign.BoxName) ||
             !SameFgiIdentifier(shipBoxDetails.ShipBoxName, holderAssign.ShipBoxName)))
        {
            throw new InvalidOperationException(
                "The ShipBoxDetails target does not match the assignment target.");
        }
    }

    private static void ValidateRequiredFgiIdentifier(
        string? value,
        string fieldName,
        int maxLength = 50)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"{fieldName} is required for an FGI assignment.");
        }

        if (value.Trim().Length > maxLength)
        {
            throw new InvalidOperationException(
                $"{fieldName} cannot exceed {maxLength} characters.");
        }
    }

    private static bool SameFgiIdentifier(string? left, string? right)
    {
        return string.Equals(
            left?.Trim(),
            right?.Trim(),
            StringComparison.OrdinalIgnoreCase);
    }

    private static async Task AcquireFgiAssignmentLocksAsync(
        HolderAssignInsertData data,
        SqlConnection connection,
        SqlTransaction transaction)
    {
        const string sql = """
        DECLARE @LockResult int;

        EXEC @LockResult = sys.sp_getapplock
            @Resource = @HOLDER_LOCK,
            @LockMode = 'Exclusive',
            @LockOwner = 'Transaction',
            @LockTimeout = 15000;

        IF @LockResult < 0
            THROW 51000, 'Unable to lock the FGI holder.', 1;

        EXEC @LockResult = sys.sp_getapplock
            @Resource = @BOX_LOCK,
            @LockMode = 'Exclusive',
            @LockOwner = 'Transaction',
            @LockTimeout = 15000;

        IF @LockResult < 0
            THROW 51000, 'Unable to lock the FGI Box.', 1;

        EXEC @LockResult = sys.sp_getapplock
            @Resource = @SHIPBOX_LOCK,
            @LockMode = 'Exclusive',
            @LockOwner = 'Transaction',
            @LockTimeout = 15000;

        IF @LockResult < 0
            THROW 51000, 'Unable to lock the FGI ShipBox.', 1;
        """;

        await using var command = new SqlCommand(sql, connection, transaction);

        command.Parameters.Add("@HOLDER_LOCK", SqlDbType.NVarChar, 255).Value =
            BuildFgiLockName("HOLDER", data.Holder);
        command.Parameters.Add("@BOX_LOCK", SqlDbType.NVarChar, 255).Value =
            BuildFgiLockName("BOX", data.BoxName);
        command.Parameters.Add("@SHIPBOX_LOCK", SqlDbType.NVarChar, 255).Value =
            BuildFgiLockName(
                "SHIPBOX",
                $"{data.BoxName}|{data.ShipBoxName}");

        await command.ExecuteNonQueryAsync();
    }

    private static string BuildFgiLockName(string targetType, string value)
    {
        return $"WDC_STACKER:FGI:{targetType}:{value.Trim().ToUpperInvariant()}";
    }

    private static async Task EnsureFgiBoxAsync(
        BoxDetailsInsertData? boxDetails,
        HolderAssignInsertData holderAssign,
        SqlConnection connection,
        SqlTransaction transaction)
    {
        const string sql = """
        SELECT COUNT(1)
        FROM [BOXMANAGEMENT].[BOX].[BOXDETAILS] WITH (UPDLOCK, HOLDLOCK)
        WHERE [BOXNO] = @BOXNO
          AND UPPER(LTRIM(RTRIM(ISNULL([CLIENTCODE], '')))) = @CLIENTCODE;
        """;

        int boxCount;

        await using (var command = new SqlCommand(sql, connection, transaction))
        {
            command.Parameters.Add("@BOXNO", SqlDbType.VarChar, 50).Value =
                holderAssign.BoxName.Trim();

            command.Parameters.Add("@CLIENTCODE", SqlDbType.VarChar, 10).Value =
                "FGI";

            boxCount = Convert.ToInt32(
                await command.ExecuteScalarAsync() ?? 0);
        }

        if (boxCount > 1)
        {
            throw new InvalidOperationException(
                $"Box '{holderAssign.BoxName}' has duplicate FGI BOXDETAILS rows.");
        }

        if (boxCount == 1)
        {
            return;
        }

        if (boxDetails is null)
        {
            throw new InvalidOperationException(
                $"FGI Box '{holderAssign.BoxName}' does not exist and no BoxDetails were supplied.");
        }

        await InsertBoxDetailsAsync(
            new BoxDetailsInsertData
            {
                BoxNo = holderAssign.BoxName.Trim(),
                RackNum = boxDetails.RackNum,
                LayerRowNum = boxDetails.LayerRowNum,
                LayerColNum = boxDetails.LayerColNum,
                UpdateBy = boxDetails.UpdateBy,
                UpdateTs = boxDetails.UpdateTs,
                ClientCode = "FGI"
            },
            connection,
            transaction);
    }

    private static async Task<bool> EnsureFgiShipBoxAsync(
        ShipBoxDetailsInsertData? shipBoxDetails,
        HolderAssignInsertData holderAssign,
        SqlConnection connection,
        SqlTransaction transaction)
    {
        const string sql = """
        SELECT COUNT(1)
        FROM [BOXMANAGEMENT].[BOX].[SHIPBOXDETAILS] WITH (UPDLOCK, HOLDLOCK)
        WHERE [BOXNO] = @BOXNO
          AND [SHIPBOXNAME] = @SHIPBOXNAME;
        """;

        int shipBoxCount;

        await using (var command = new SqlCommand(sql, connection, transaction))
        {
            command.Parameters.Add("@BOXNO", SqlDbType.VarChar, 50).Value =
                holderAssign.BoxName.Trim();
            command.Parameters.Add("@SHIPBOXNAME", SqlDbType.VarChar, 50).Value =
                holderAssign.ShipBoxName.Trim();

            shipBoxCount = Convert.ToInt32(
                await command.ExecuteScalarAsync() ?? 0);
        }

        if (shipBoxCount > 1)
        {
            throw new InvalidOperationException(
                $"ShipBox '{holderAssign.ShipBoxName}' has duplicate rows inside Box '{holderAssign.BoxName}'.");
        }

        if (shipBoxCount == 1)
        {
            return false;
        }

        if (shipBoxDetails is null)
        {
            throw new InvalidOperationException(
                $"ShipBox '{holderAssign.ShipBoxName}' does not exist and no ShipBoxDetails were supplied.");
        }

        await InsertShipBoxDetailsAsync(
            new ShipBoxDetailsInsertData
            {
                BoxNo = holderAssign.BoxName.Trim(),
                ShipBoxName = holderAssign.ShipBoxName.Trim(),
                ShipBoxStatus = shipBoxDetails.ShipBoxStatus,
                ShipBoxNum = shipBoxDetails.ShipBoxNum,
                LayerRowNum = shipBoxDetails.LayerRowNum,
                LayerColNum = shipBoxDetails.LayerColNum,
                UpdateBy = shipBoxDetails.UpdateBy,
                UpdateTs = shipBoxDetails.UpdateTs
            },
            connection,
            transaction);
        return true;
    }

    private static async Task ValidateFgiAssignmentAsync(
        HolderAssignInsertData data,
        bool shipBoxCreated,
        SqlConnection connection,
        SqlTransaction transaction)
    {
        const string sql = """
        DECLARE @NormalizedPartNum varchar(50) =
            NULLIF(UPPER(LTRIM(RTRIM(@PARTNUM))), '');
        DECLARE @NormalizedPenNum varchar(50) =
            NULLIF(UPPER(LTRIM(RTRIM(@PENNUM))), '');
        DECLARE @NormalizedProductName nvarchar(50) =
            NULLIF(UPPER(LTRIM(RTRIM(@PRODUCTNAME))), N'');
        DECLARE @NormalizedLec varchar(50) =
            NULLIF(UPPER(LTRIM(RTRIM(@LEC))), '');

        IF @NormalizedPartNum IS NULL OR @NormalizedProductName IS NULL
            THROW 51001, 'PartNum and ProductName are required for FGI.', 1;

        IF EXISTS
        (
            SELECT 1
            FROM [BOXMANAGEMENT].[BOX].[HOLDER_ASSIGN] WITH (UPDLOCK, HOLDLOCK)
            WHERE UPPER(LTRIM(RTRIM(ISNULL([HOLDER], ''))))
                    = UPPER(LTRIM(RTRIM(@HOLDER)))
              AND UPPER(LTRIM(RTRIM(ISNULL([PROCESS], '')))) = 'FGI'
        )
            THROW 51002, 'Holder is already assigned to FGI.', 1;

        IF EXISTS
        (
            SELECT 1
            FROM [BOXMANAGEMENT].[BOX].[HOLDER_ASSIGN] ha WITH (UPDLOCK, HOLDLOCK)
            WHERE UPPER(LTRIM(RTRIM(ISNULL(ha.[BOXNAME], ''))))
                    = UPPER(LTRIM(RTRIM(@BOXNAME)))
              AND UPPER(LTRIM(RTRIM(ISNULL(ha.[PROCESS], '')))) = 'FGI'
              AND
              (
                  ISNULL(
                      NULLIF(UPPER(LTRIM(RTRIM(ha.[PARTNUM]))), ''),
                      ''
                  ) <> ISNULL(@NormalizedPartNum, '')
                  OR ISNULL(
                      NULLIF(UPPER(LTRIM(RTRIM(ha.[PENNUM]))), ''),
                      ''
                  ) <> ISNULL(@NormalizedPenNum, '')
                  OR ISNULL(
                      NULLIF(UPPER(LTRIM(RTRIM(ha.[PRODUCTNAME]))), ''),
                      ''
                  ) <> ISNULL(@NormalizedProductName, '')
              )
        )
            THROW 51003,
                'Target Box contains a different PartNum, PenNum, or ProductName.',
                1;

        IF @NormalizedLec IS NULL
        BEGIN
            IF @SHIPBOXCREATED = 0
                THROW 51004,
                    'A holder with null LEC requires a newly created ShipBox.',
                    1;

            IF EXISTS
            (
                SELECT 1
                FROM [BOXMANAGEMENT].[BOX].[HOLDER_ASSIGN] WITH (UPDLOCK, HOLDLOCK)
                WHERE UPPER(LTRIM(RTRIM(ISNULL([SHIPBOXNAME], ''))))
                        = UPPER(LTRIM(RTRIM(@SHIPBOXNAME)))
                  AND UPPER(LTRIM(RTRIM(ISNULL([BOXNAME], ''))))
                        = UPPER(LTRIM(RTRIM(@BOXNAME)))
                  AND UPPER(LTRIM(RTRIM(ISNULL([PROCESS], '')))) = 'FGI'
            )
                THROW 51004,
                    'A null-LEC holder cannot use an occupied ShipBox.',
                    1;
        END
        ELSE IF EXISTS
        (
            SELECT 1
            FROM [BOXMANAGEMENT].[BOX].[HOLDER_ASSIGN] ha WITH (UPDLOCK, HOLDLOCK)
            WHERE UPPER(LTRIM(RTRIM(ISNULL(ha.[SHIPBOXNAME], ''))))
                    = UPPER(LTRIM(RTRIM(@SHIPBOXNAME)))
              AND UPPER(LTRIM(RTRIM(ISNULL(ha.[BOXNAME], ''))))
                    = UPPER(LTRIM(RTRIM(@BOXNAME)))
              AND UPPER(LTRIM(RTRIM(ISNULL(ha.[PROCESS], '')))) = 'FGI'
              AND
              (
                  ISNULL(
                      NULLIF(UPPER(LTRIM(RTRIM(ha.[PARTNUM]))), ''),
                      ''
                  ) <> ISNULL(@NormalizedPartNum, '')
                  OR ISNULL(
                      NULLIF(UPPER(LTRIM(RTRIM(ha.[PENNUM]))), ''),
                      ''
                  ) <> ISNULL(@NormalizedPenNum, '')
                  OR ISNULL(
                      NULLIF(UPPER(LTRIM(RTRIM(ha.[PRODUCTNAME]))), ''),
                      ''
                  ) <> ISNULL(@NormalizedProductName, '')
                  OR ISNULL(
                      NULLIF(UPPER(LTRIM(RTRIM(ha.[LEC]))), ''),
                      ''
                  ) <> ISNULL(@NormalizedLec, '')
              )
        )
            THROW 51005,
                'Target ShipBox contains different holder metadata or LEC.',
                1;
        """;

        await using var command = new SqlCommand(sql, connection, transaction);

        command.Parameters.Add("@HOLDER", SqlDbType.VarChar, 50).Value =
            data.Holder.Trim();
        command.Parameters.Add("@BOXNAME", SqlDbType.VarChar, 50).Value =
            data.BoxName.Trim();
        command.Parameters.Add("@SHIPBOXNAME", SqlDbType.VarChar, 10).Value =
            data.ShipBoxName.Trim();
        command.Parameters.Add("@PARTNUM", SqlDbType.VarChar, 50).Value =
            data.PartNum.Trim();
        command.Parameters.Add("@PENNUM", SqlDbType.VarChar, 50).Value =
            string.IsNullOrWhiteSpace(data.PenNum)
                ? DBNull.Value
                : data.PenNum.Trim();
        command.Parameters.Add("@PRODUCTNAME", SqlDbType.NVarChar, 50).Value =
            data.ProductName.Trim();
        command.Parameters.Add("@LEC", SqlDbType.VarChar, 50).Value =
            string.IsNullOrWhiteSpace(data.Lec)
                ? DBNull.Value
                : data.Lec.Trim();
        command.Parameters.Add("@SHIPBOXCREATED", SqlDbType.Bit).Value =
            shipBoxCreated;

        await command.ExecuteNonQueryAsync();
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
                ([HOLDER], [BOXNAME], [SHIPBOXNAME], [QTY], [PARTNUM], [PENNUM], [PRODUCTNAME], [LEC], [Factory], [PROCESS], [UPDATEBY], [UPDATETS])
            VALUES
                (@HOLDER, @BOXNAME, @SHIPBOXNAME, @QTY, @PARTNUM, @PENNUM, @PRODUCTNAME, @LEC, @Factory, @PROCESS, @UPDATEBY, @UPDATETS);
            """;

        await using var command = new SqlCommand(sql, connection, transaction);

        command.Parameters.Add("@HOLDER", SqlDbType.VarChar, 50).Value = data.Holder.Trim();
        command.Parameters.Add("@BOXNAME", SqlDbType.VarChar, 50).Value = data.BoxName.Trim();
        command.Parameters.Add("@SHIPBOXNAME", SqlDbType.VarChar, 10).Value = data.ShipBoxName.Trim();
        command.Parameters.Add("@QTY", SqlDbType.Int).Value = data.Qty!.Value;
        command.Parameters.Add("@PARTNUM", SqlDbType.VarChar, 50).Value = data.PartNum.Trim();
        command.Parameters.Add("@PENNUM", SqlDbType.VarChar, 50).Value =
            string.IsNullOrWhiteSpace(data.PenNum)
                ? DBNull.Value
                : data.PenNum.Trim();
        command.Parameters.Add("@PRODUCTNAME", SqlDbType.NChar, 10).Value = data.ProductName.Trim();
        command.Parameters.Add("@LEC", SqlDbType.VarChar, 50).Value =
            string.IsNullOrWhiteSpace(data.Lec)
                ? DBNull.Value
                : data.Lec.Trim();
        command.Parameters.Add("@Factory", SqlDbType.VarChar, 50).Value = data.Factory;
        command.Parameters.Add("@PROCESS", SqlDbType.VarChar, 10).Value = data.Process.Trim().ToUpperInvariant();
        command.Parameters.Add("@UPDATEBY", SqlDbType.VarChar, 50).Value = data.UpdateBy;
        command.Parameters.Add("@UPDATETS", SqlDbType.DateTime).Value = data.UpdateTs;

        await command.ExecuteNonQueryAsync();
    }

    public async Task<FgiWithdrawalDisassociationResult>
    DisassociateFgiWithdrawalAsync(
        long requestId,
        IReadOnlyCollection<string> includedHolders)
    {
        static FgiWithdrawalDisassociationResult Failure(
            string message) => new()
            {
                Success = false,
                Message = message
            };

        var holderKeys = includedHolders
            .Select(holder =>
                holder?.Trim().ToUpperInvariant() ??
                string.Empty)
            .ToArray();

        if (holderKeys.Length == 0 ||
            holderKeys.Any(string.IsNullOrWhiteSpace))
        {
            return Failure(
                "At least one included Holder is required.");
        }

        /*
         * Two additional SQL parameters are used below.
         * Keep this below SQL Server's 2100-parameter limit.
         */
        if (holderKeys.Length > 2000)
        {
            return Failure("Too many included Holders.");
        }

        if (holderKeys.Any(holder => holder.Length > 50))
        {
            return Failure(
                "A Holder cannot exceed 50 characters.");
        }

        if (holderKeys
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() != holderKeys.Length)
        {
            return Failure(
                "The included Holder list contains duplicates.");
        }

        var holderValuesSql = string.Join(
            ", ",
            holderKeys.Select(
                (_, index) => $"(@HOLDER_{index})"));

        var sql = $"""
            SET NOCOUNT ON;
            SET XACT_ABORT ON;

            DECLARE @ExpectedHolders TABLE
            (
                [HolderKey] varchar(50) NOT NULL PRIMARY KEY
            );

            INSERT INTO @ExpectedHolders ([HolderKey])
            VALUES {holderValuesSql};

            DECLARE @RequestLec varchar(50);
            DECLARE @RequestPenNum varchar(50);
            DECLARE @RequestTotal int;

            SELECT
                @RequestLec =
                    NULLIF(LTRIM(RTRIM([LEC])), ''),
                @RequestPenNum =
                    NULLIF(LTRIM(RTRIM([PENNUM])), ''),
                @RequestTotal = [TOTAL]
            FROM [BOXMANAGEMENT].[HGA].[KITTING_REQUEST]
                WITH (UPDLOCK, HOLDLOCK)
            WHERE [REQUESTID] = @REQUESTID;

            IF @@ROWCOUNT = 0
                THROW 51010,
                    'The withdrawal request no longer exists.',
                    1;

            IF @RequestLec IS NULL
                OR @RequestTotal IS NULL
                OR @RequestTotal < 0
            BEGIN
                THROW 51011,
                    'The withdrawal request no longer has valid LEC and TOTAL values.',
                    1;
            END;

            DECLARE @DeletedAssignments TABLE
            (
                [BOXNO] varchar(50) NULL,
                [SHIPBOXNAME] varchar(50) NULL,
                [QTY] bigint NULL
            );

            /*
             * Stage 1: hard delete the server-confirmed Included Holders
             * (the FIFO + Check Hold decision already happened when the
             * disassociation preview was loaded; this step trusts that
             * confirmed list rather than re-deriving it here, since holds
             * cannot be re-checked from SQL). Do not add a
             * STATUS = RELEASE condition.
             */
            DELETE HA
            OUTPUT
                DELETED.[BOXNAME],
                DELETED.[SHIPBOXNAME],
                DELETED.[QTY]
            INTO @DeletedAssignments
            (
                [BOXNO],
                [SHIPBOXNAME],
                [QTY]
            )
            FROM [BOXMANAGEMENT].[BOX].[HOLDER_ASSIGN] HA
            INNER JOIN @ExpectedHolders Expected
                ON Expected.[HolderKey] =
                    UPPER(LTRIM(RTRIM(HA.[HOLDER])))
            WHERE UPPER(
                    LTRIM(
                        RTRIM(
                            ISNULL(HA.[PROCESS], '')
                        )
                    )
                ) = 'FGI'
                AND UPPER(
                    LTRIM(
                        RTRIM(
                            ISNULL(HA.[LEC], '')
                        )
                    )
                ) =
                UPPER(LTRIM(RTRIM(@RequestLec)))
                AND
                (
                    NULLIF(
                        LTRIM(RTRIM(HA.[PENNUM])),
                        ''
                    ) IS NULL
                    OR
                    (
                        @RequestPenNum IS NOT NULL
                        AND UPPER(
                            LTRIM(RTRIM(HA.[PENNUM]))
                        ) =
                        UPPER(
                            LTRIM(
                                RTRIM(@RequestPenNum)
                            )
                        )
                    )
                )
                AND NULLIF(
                    LTRIM(RTRIM(HA.[HOLDER])),
                    ''
                ) IS NOT NULL
                AND ISNULL(HA.[QTY], 0) > 0;

            /*
             * Integrity check only: every confirmed Holder must still
             * exist and qualify (FGI / LEC / PENNUM / QTY > 0). This does
             * NOT re-derive which holders should be included (that
             * decision, including hold checks, already happened
             * server-side when the preview was confirmed).
             */
            IF
            (
                SELECT COUNT(*)
                FROM @DeletedAssignments
            ) <>
            (
                SELECT COUNT(*)
                FROM @ExpectedHolders
            )
            BEGIN
                THROW 51013,
                    'The Holder rows changed before deletion. No STACKER data was removed.',
                    1;
            END;

            /*
             * "Update Table (Actual Output)": set (not increment)
             * KITTING_REQUEST.ACTUALOUTPUT to the sum of the Qty of the
             * Holders withdrawn in this transaction.
             */
            DECLARE @ActualOutputSum bigint =
            (
                SELECT ISNULL(SUM(ISNULL([QTY], 0)), 0)
                FROM @DeletedAssignments
            );

            UPDATE [BOXMANAGEMENT].[HGA].[KITTING_REQUEST]
            SET [ACTUALOUTPUT] = @ActualOutputSum
            WHERE [REQUESTID] = @REQUESTID;

            DECLARE @DeletedShipBoxes TABLE
            (
                [BOXNO] varchar(50),
                [SHIPBOXNAME] varchar(50)
            );

            /*
             * Stage 2: remove only affected ShipBoxes that now
             * have zero Holders.
             *
             * The NOT EXISTS deliberately checks every process.
             */
            DELETE ShipBox
            OUTPUT
                DELETED.[BOXNO],
                DELETED.[SHIPBOXNAME]
            INTO @DeletedShipBoxes
            (
                [BOXNO],
                [SHIPBOXNAME]
            )
            FROM [BOXMANAGEMENT].[BOX].[SHIPBOXDETAILS]
                ShipBox
            INNER JOIN
            (
                SELECT DISTINCT
                    [BOXNO],
                    [SHIPBOXNAME]
                FROM @DeletedAssignments
                WHERE [BOXNO] IS NOT NULL
                    AND [SHIPBOXNAME] IS NOT NULL
            ) Affected
                ON Affected.[BOXNO] =
                    ShipBox.[BOXNO]
                AND Affected.[SHIPBOXNAME] =
                    ShipBox.[SHIPBOXNAME]
            WHERE NOT EXISTS
            (
                SELECT 1
                FROM [BOXMANAGEMENT].[BOX].[HOLDER_ASSIGN]
                    RemainingHolder
                    WITH (UPDLOCK, HOLDLOCK)
                WHERE RemainingHolder.[BOXNAME] =
                        ShipBox.[BOXNO]
                    AND RemainingHolder.[SHIPBOXNAME] =
                        ShipBox.[SHIPBOXNAME]
            );

            DECLARE @DeletedBoxes TABLE
            (
                [BOXNO] varchar(50)
            );

            /*
             * Stage 3: remove only Boxes affected by Stage 2
             * that now have zero ShipBoxes.
             */
            DELETE Box
            OUTPUT DELETED.[BOXNO]
                INTO @DeletedBoxes ([BOXNO])
            FROM [BOXMANAGEMENT].[BOX].[BOXDETAILS] Box
            INNER JOIN
            (
                SELECT DISTINCT [BOXNO]
                FROM @DeletedShipBoxes
            ) Affected
                ON Affected.[BOXNO] = Box.[BOXNO]
            WHERE UPPER(
                    LTRIM(
                        RTRIM(
                            ISNULL(Box.[CLIENTCODE], '')
                        )
                    )
                ) = 'FGI'
                AND NOT EXISTS
                (
                    SELECT 1
                    FROM [BOXMANAGEMENT].[BOX].[SHIPBOXDETAILS]
                        RemainingShipBox
                        WITH (UPDLOCK, HOLDLOCK)
                    WHERE RemainingShipBox.[BOXNO] =
                        Box.[BOXNO]
                );

            SELECT
                (
                    SELECT COUNT(*)
                    FROM @DeletedAssignments
                ) AS [DeletedHolderCount],
                (
                    SELECT COUNT(*)
                    FROM @DeletedShipBoxes
                ) AS [DeletedShipBoxCount],
                (
                    SELECT COUNT(*)
                    FROM @DeletedBoxes
                ) AS [DeletedBoxCount];
            """;

        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var transaction =
            (SqlTransaction)await connection
                .BeginTransactionAsync(
                    IsolationLevel.Serializable);

        try
        {
            await using var command =
                new SqlCommand(
                    sql,
                    connection,
                    transaction);

            command.Parameters
                .Add(
                    "@REQUESTID",
                    SqlDbType.BigInt)
                .Value = requestId;

            for (
                var index = 0;
                index < holderKeys.Length;
                index++)
            {
                command.Parameters
                    .Add(
                        $"@HOLDER_{index}",
                        SqlDbType.VarChar,
                        50)
                    .Value = holderKeys[index];
            }

            await using var reader =
                await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                throw new InvalidOperationException(
                    "The disassociation delete returned no result.");
            }

            var result =
                new FgiWithdrawalDisassociationResult
                {
                    Success = true,
                    Message =
                        "Holders were removed from STACKER data successfully.",
                    DeletedHolderCount =
                        Convert.ToInt32(
                            reader["DeletedHolderCount"]),
                    DeletedShipBoxCount =
                        Convert.ToInt32(
                            reader["DeletedShipBoxCount"]),
                    DeletedBoxCount =
                        Convert.ToInt32(
                            reader["DeletedBoxCount"])
                };

            await reader.CloseAsync();
            await transaction.CommitAsync();

            return result;
        }
        catch (SqlException exception)
            when (
                exception.Number is
                    51010 or
                    51011 or
                    51013)
        {
            await transaction.RollbackAsync();
            return Failure(exception.Message);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
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
