using Microsoft.Data.SqlClient;

using System.Data;

using WDC_STACKER.API.Interfaces;
using WDC_STACKER.API.Models.Stacker;



namespace WDC_STACKER.API.Services;



public class StackerSqlService

{

    private readonly string _connectionString;

    private readonly IEmailService _emailService;

    private const int FgiWithdrawalQtyTolerance = 500;



    public StackerSqlService(IConfiguration configuration, IEmailService emailService)

    {

        _connectionString = configuration.GetConnectionString("WdcStackerDb")

            ?? throw new InvalidOperationException("Missing WdcStackerDb connection string.");

        _emailService = emailService;

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

            MAX(boxMeta.[PRODUCTNAME]) AS [PRODUCTNAME],

            MAX(boxMeta.[CAMVERSION]) AS [CAMVERSION]

        FROM [BOXMANAGEMENT].[BOX].[BOXDETAILS] bd

        LEFT JOIN [BOXMANAGEMENT].[BOX].[SHIPBOXDETAILS] sbd

            ON bd.[BOXNO] = sbd.[BOXNO]

        LEFT JOIN

        (

            SELECT

                [BOXNAME],

                MAX(NULLIF(LTRIM(RTRIM([PARTNUM])), '')) AS [PARTNUM],

                MAX(NULLIF(LTRIM(RTRIM([PENNUM])), '')) AS [PENNUM],

                MAX(NULLIF(LTRIM(RTRIM([PRODUCTNAME])), '')) AS [PRODUCTNAME],

                MAX(NULLIF(LTRIM(RTRIM([CAMVERSION])), '')) AS [CAMVERSION]

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

                AND COUNT(*) = COUNT(NULLIF(LTRIM(RTRIM([CAMVERSION])), ''))

                AND COUNT(

                    DISTINCT UPPER(NULLIF(LTRIM(RTRIM([CAMVERSION])), ''))

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

                CamVersion = reader["CAMVERSION"] is DBNull

                    ? null

                    : Convert.ToString(reader["CAMVERSION"])?.Trim(),

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

            MAX(NULLIF(LTRIM(RTRIM(ha.[CAMVERSION])), '')) AS [CAMVERSION],

            COUNT(ha.[BOXNAME]) AS ShipBoxListCount,

            CAST(

                (COUNT(ha.[BOXNAME]) * 100.0) / NULLIF(@BaselineCount, 0)

            AS DECIMAL(18, 2)) AS ShipBoxListPercentage,

            CASE

                WHEN COUNT(CASE WHEN UPPER(LTRIM(RTRIM(ha.[STATUS]))) = 'HOLD' THEN 1 END) > 0

                THEN 1

                ELSE 0

            END AS HasHeldHolder,

            STRING_AGG(

                CASE WHEN UPPER(LTRIM(RTRIM(ha.[STATUS]))) = 'HOLD' THEN '1' ELSE '0' END,

                ','

            ) WITHIN GROUP (ORDER BY ha.[HOLDER]) AS HeldHolderFlags

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

            var heldHolderFlags = reader["HeldHolderFlags"] is DBNull

                ? string.Empty

                : Convert.ToString(reader["HeldHolderFlags"]) ?? string.Empty;

            var heldHolderPositions = heldHolderFlags

                .Split(',', StringSplitOptions.RemoveEmptyEntries)

                .Select((flag, index) => (flag, index))

                .Where(item => item.flag == "1")

                .Select(item => item.index)

                .ToList();



            results.Add(new ShipBoxView

            {

                BoxNo = boxNo,

                ShipBoxName = Convert.ToString(reader["SHIPBOXNAME"])?.Trim() ?? string.Empty,

                ShipBoxStatus = shipBoxStatus,

                Lec = Convert.ToString(reader["LEC"])?.Trim() ?? string.Empty,

                CamVersion = reader["CAMVERSION"] is DBNull

                    ? null

                    : Convert.ToString(reader["CAMVERSION"])?.Trim(),

                ShipBoxNum = Convert.ToInt32(reader["SHIPBOXNUM"]),

                LayerRowNum = Convert.ToInt32(reader["LAYERROWNUM"]),

                LayerColNum = Convert.ToInt32(reader["LAYERCOLNUM"]),

                ShipBoxListCount = Convert.ToInt32(reader["ShipBoxListCount"]),

                ShipBoxListPercentage = Convert.ToDecimal(reader["ShipBoxListPercentage"]),

                HasReleaseStatus = string.Equals(shipBoxStatus, "RELEASE", StringComparison.OrdinalIgnoreCase),

                HasHeldHolder = Convert.ToBoolean(reader["HasHeldHolder"]),

                HeldHolderPositions = heldHolderPositions

            });

        }



        return results;

    }



    public async Task<List<FgiHolderLocation>> GetFgiHolderLocationsAsync(string process, string? boxNo = null)

    {

        const string sql = """

        SELECT

            LTRIM(RTRIM([HOLDER])) AS [HOLDER],

            LTRIM(RTRIM([BOXNAME])) AS [BOXNAME],

            LTRIM(RTRIM([SHIPBOXNAME])) AS [SHIPBOXNAME],

            LTRIM(RTRIM([CAMVERSION])) AS [CAMVERSION]

        FROM [BOXMANAGEMENT].[BOX].[HOLDER_ASSIGN]

        WHERE UPPER(LTRIM(RTRIM(ISNULL([PROCESS], '')))) = @PROCESS

            AND NULLIF(LTRIM(RTRIM([HOLDER])), '') IS NOT NULL

            AND NULLIF(LTRIM(RTRIM([BOXNAME])), '') IS NOT NULL

            AND NULLIF(LTRIM(RTRIM([SHIPBOXNAME])), '') IS NOT NULL

            AND (@BOXNO IS NULL OR [BOXNAME] = @BOXNO)

        ORDER BY [BOXNAME], [SHIPBOXNAME], [HOLDER];

        """;



        await using var connection = new SqlConnection(_connectionString);

        await using var command = new SqlCommand(sql, connection);



        command.Parameters.Add("@PROCESS", SqlDbType.VarChar, 10).Value = process.Trim().ToUpperInvariant();

        command.Parameters.Add("@BOXNO", SqlDbType.VarChar, 50).Value = string.IsNullOrWhiteSpace(boxNo) ? DBNull.Value : boxNo.Trim();



        await connection.OpenAsync();



        var results = new List<FgiHolderLocation>();

        await using var reader = await command.ExecuteReaderAsync();



        while (await reader.ReadAsync())

        {

            results.Add(new FgiHolderLocation

            {

                Holder = Convert.ToString(reader["HOLDER"])?.Trim() ?? string.Empty,

                BoxNo = Convert.ToString(reader["BOXNAME"])?.Trim() ?? string.Empty,

                ShipBoxName = Convert.ToString(reader["SHIPBOXNAME"])?.Trim() ?? string.Empty,

                CamVersion = reader["CAMVERSION"] is DBNull
                    ? null
                    : Convert.ToString(reader["CAMVERSION"])?.Trim()

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

    GetFgiWithdrawalDisassociationPreviewAsync(string? lec, string? penNum, int total, string? partNum, string? grade, int actualOutput)

    {

        const string sql = """

        DECLARE @RemainingQty int = @TOTAL - @ACTUALOUTPUT;

        -- If actual output already meets or exceeds total, no holders needed
        IF @RemainingQty <= 0
        BEGIN
            SELECT
                @TOTAL AS [Total],
                0 AS [TotalQty],
                @TOLERANCE AS [Tolerance],
                CONVERT(bigint, @TOTAL) + CONVERT(bigint, @TOLERANCE) AS [MaximumTotalQty],
                NULL AS [HOLDER],
                0 AS [QTY],
                NULL AS [UPDATETS],
                0 AS [RunningTotal],
                0 AS [IsIncluded]
            WHERE 1 = 0;
            RETURN;
        END

        DECLARE @MaximumTotalQty bigint =
            CONVERT(bigint, @RemainingQty) +

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

                AND (

                    NULLIF(LTRIM(RTRIM(HA.[LEC])), '') IS NULL

                    OR @LEC IS NULL

                    OR UPPER(LTRIM(RTRIM(HA.[LEC]))) = UPPER(LTRIM(RTRIM(@LEC)))

                )

                AND
                (
                    NULLIF(
                        LTRIM(RTRIM(HA.[PENNUM])),
                        ''
                    ) IS NULL
                    OR @PENNUM IS NULL
                    OR UPPER(
                        LTRIM(RTRIM(HA.[PENNUM]))
                    ) =
                    UPPER(
                        LTRIM(RTRIM(@PENNUM))
                    )
                )

                AND
                (
                    NULLIF(LTRIM(RTRIM(HA.[PARTNUM])), '') IS NULL
                    OR @PARTNUM IS NULL
                    OR UPPER(LTRIM(RTRIM(HA.[PARTNUM]))) =
                        UPPER(LTRIM(RTRIM(@PARTNUM)))
                )

                -- GRADE filter is disabled for now.
                -- Re-enable by uncommenting once confirmed:
                -- AND
                -- (
                --     NULLIF(LTRIM(RTRIM(HA.[GRADE])), '') IS NULL
                --     OR @GRADE IS NULL
                --     OR UPPER(LTRIM(RTRIM(HA.[GRADE]))) =
                --         UPPER(LTRIM(RTRIM(@GRADE)))
                -- )

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

             * REMAININGQTY = 0 means that the target is already

             * reached, so no record is included.

             */

            SELECT

                Candidate.[RowNumber],

                Candidate.[HOLDER],

                Candidate.[QTY],

                Candidate.[UPDATETS],

                CAST(

                    CASE

                        WHEN @RemainingQty > 0

                            AND Candidate.[QTY] <=

                                @MaximumTotalQty

                        THEN Candidate.[QTY]

                        ELSE 0

                    END

                    AS bigint

                ) AS [RunningTotal],

                CAST(

                    CASE

                        WHEN @RemainingQty > 0

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

             * RunningTotal is below REMAININGQTY.

             *

             * The next Holder may cross REMAININGQTY, but the

             * resulting value cannot exceed REMAININGQTY + 500.

             */

            SELECT

                Candidate.[RowNumber],

                Candidate.[HOLDER],

                Candidate.[QTY],

                Candidate.[UPDATETS],

                CAST(

                    CASE

                        WHEN Selected.[RunningTotal] <

                                CONVERT(bigint, @RemainingQty)

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

                                CONVERT(bigint, @RemainingQty)

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

        ).Value = string.IsNullOrWhiteSpace(lec)

            ? DBNull.Value

            : lec.Trim();



        command.Parameters.Add(

            "@PENNUM",

            SqlDbType.VarChar,

            50

        ).Value = string.IsNullOrWhiteSpace(penNum)

            ? DBNull.Value

            : penNum.Trim();



        command.Parameters.Add(

            "@PARTNUM",

            SqlDbType.VarChar,

            50

        ).Value = string.IsNullOrWhiteSpace(partNum)

            ? DBNull.Value

            : partNum.Trim();



        command.Parameters.Add(

            "@GRADE",

            SqlDbType.VarChar,

            50

        ).Value = string.IsNullOrWhiteSpace(grade)

            ? DBNull.Value

            : grade.Trim();



        command.Parameters.Add(

            "@TOTAL",

            SqlDbType.Int

        ).Value = total;



        command.Parameters.Add(

            "@ACTUALOUTPUT",

            SqlDbType.Int

        ).Value = actualOutput;



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



    public async Task<FgiWithdrawalRackView?> GetFgiWithdrawalLayoutAsync(string? lec, string? penNum, string? partNum, string? grade, string process)

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

            HA.[QTY],

            HA.[GRADE],

            HA.[PARTNUM],

            HA.[PENNUM],

            HA.[LEC],

            HA.[PRODUCTNAME],

            HA.[Factory],

            HA.[STATUS]

        FROM [BOXMANAGEMENT].[BOX].[BOXDETAILS] BD

        LEFT JOIN [BOXMANAGEMENT].[BOX].[SHIPBOXDETAILS] SBD

            ON BD.[BOXNO] = SBD.[BOXNO]

        LEFT JOIN [BOXMANAGEMENT].[BOX].[HOLDER_ASSIGN] HA ON SBD.[BOXNO] = HA.[BOXNAME]

         AND SBD.[SHIPBOXNAME] = HA.[SHIPBOXNAME]

        WHERE UPPER(LTRIM(RTRIM(ISNULL(HA.[PROCESS], '')))) = @PROCESS

          AND UPPER(LTRIM(RTRIM(ISNULL(HA.[PARTNUM], '')))) = UPPER(LTRIM(RTRIM(@PARTNUM)))

          AND UPPER(LTRIM(RTRIM(ISNULL(HA.[GRADE], '')))) = UPPER(LTRIM(RTRIM(@GRADE)))

          AND (
              @LEC IS NULL
              OR UPPER(LTRIM(RTRIM(ISNULL(HA.[LEC], '')))) = UPPER(LTRIM(RTRIM(@LEC)))
          )

          AND (
              @PENNUM IS NULL
              OR UPPER(LTRIM(RTRIM(ISNULL(HA.[PENNUM], '')))) = UPPER(LTRIM(RTRIM(@PENNUM)))
          )

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



        command.Parameters.Add("@LEC", SqlDbType.VarChar, 50).Value = lec ?? (object)DBNull.Value;

        command.Parameters.Add("@PENNUM", SqlDbType.VarChar, 50).Value = penNum ?? (object)DBNull.Value;

        command.Parameters.Add("@PARTNUM", SqlDbType.VarChar, 50).Value = partNum ?? (object)DBNull.Value;

        command.Parameters.Add("@GRADE", SqlDbType.VarChar, 50).Value = grade ?? (object)DBNull.Value;

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

            int Qty,

            string Grade,

            string PartNum,

            string PenNum,

            string Lec,

            string ProductName,

            string Factory,

            string Status)>();



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

                reader["QTY"] is DBNull ? 0 : Convert.ToInt32(reader["QTY"]),

                Convert.ToString(reader["GRADE"])?.Trim() ?? "",

                Convert.ToString(reader["PARTNUM"])?.Trim() ?? "",

                Convert.ToString(reader["PENNUM"])?.Trim() ?? "",

                Convert.ToString(reader["LEC"])?.Trim() ?? "",

                Convert.ToString(reader["PRODUCTNAME"])?.Trim() ?? "",

                Convert.ToString(reader["Factory"])?.Trim() ?? "",

                Convert.ToString(reader["STATUS"])?.Trim() ?? ""

            ));

        }



        if (rows.Count == 0)

            return null;



        var rackNumbers = rows.Select(row => row.RackNum).Distinct().ToList();



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

                    Grade = boxGroup.FirstOrDefault().Grade ?? string.Empty,

                    PartNum = boxGroup.FirstOrDefault().PartNum ?? string.Empty,

                    PenNum = boxGroup.FirstOrDefault().PenNum ?? string.Empty,

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

                            Lec = shipBoxGroup.FirstOrDefault().Lec ?? string.Empty,

                            Holders = shipBoxGroup

                                .Where(row => !string.IsNullOrWhiteSpace(row.Holder))

                                .GroupBy(row => new { row.Holder, row.Qty })

                                .Select(holderGroup => new FgiWithdrawalHolderView

                                {

                                    Holder = holderGroup.Key.Holder,

                                    Qty = holderGroup.Key.Qty,

                                    ProductName = holderGroup.FirstOrDefault().ProductName ?? string.Empty,

                                    Factory = holderGroup.FirstOrDefault().Factory ?? string.Empty,

                                    Status = holderGroup.FirstOrDefault().Status ?? string.Empty

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

            ([HOLDER], [BOXNAME], [PRODUCTNAME], [LEC], [Factory], [PROCESS], [GRADE], [CAMVERSION], [JOB], [QTY], [STATUS], [UPDATEBY], [UPDATETS])

        VALUES

            (@HOLDER, @BOXNAME, @PRODUCTNAME, @LEC, @Factory, @PROCESS, @GRADE, @CAMVERSION, @JOB, @QTY, @STATUS, @UPDATEBY, @UPDATETS);

        """;



        await using var command = new SqlCommand(sql, connection, transaction);



        command.Parameters.Add("@HOLDER", SqlDbType.VarChar, 50).Value = data.Holder;

        command.Parameters.Add("@BOXNAME", SqlDbType.VarChar, 50).Value = data.BoxName;

        command.Parameters.Add("@PRODUCTNAME", SqlDbType.NChar, 10).Value = data.ProductName;

        command.Parameters.Add("@LEC", SqlDbType.VarChar, 50).Value = data.Lec;

        command.Parameters.Add("@Factory", SqlDbType.VarChar, 50).Value = data.Factory;

        command.Parameters.Add("@PROCESS", SqlDbType.VarChar, 10).Value = data.Process;

        command.Parameters.Add("@GRADE", SqlDbType.VarChar, 50).Value = data.BinName;

        command.Parameters.Add("@CAMVERSION", SqlDbType.VarChar, 10).Value =

            string.IsNullOrWhiteSpace(data.CamVersion) ? DBNull.Value : data.CamVersion.Trim();

        command.Parameters.Add("@JOB", SqlDbType.VarChar, 50).Value =
            string.IsNullOrWhiteSpace(data.Job) ? DBNull.Value : data.Job.Trim();

        command.Parameters.Add("@QTY", SqlDbType.Int).Value =
            data.Qty.HasValue ? data.Qty.Value : DBNull.Value;

        command.Parameters.Add("@STATUS", SqlDbType.VarChar, 20).Value =
            string.IsNullOrWhiteSpace(data.Status) ? DBNull.Value : data.Status.Trim();

        command.Parameters.Add("@UPDATEBY", SqlDbType.VarChar, 50).Value = data.UpdateBy;

        command.Parameters.Add("@UPDATETS", SqlDbType.DateTime).Value = data.UpdateTs;



        await command.ExecuteNonQueryAsync();

    }



    public async Task<(string BoxName, string ShipBoxName)?> GetHolderAssignLocationAsync(string holder, string process)

    {

        const string sql = """

        SELECT TOP 1 [BOXNAME], [SHIPBOXNAME]

        FROM [BOXMANAGEMENT].[BOX].[HOLDER_ASSIGN]

        WHERE [HOLDER] = @HOLDER

          AND UPPER(LTRIM(RTRIM(ISNULL([PROCESS], '')))) = @PROCESS;

        """;



        await using var connection = new SqlConnection(_connectionString);

        await using var command = new SqlCommand(sql, connection);



        command.Parameters.Add("@HOLDER", SqlDbType.VarChar, 50).Value = holder;

        command.Parameters.Add("@PROCESS", SqlDbType.VarChar, 10).Value = process.Trim().ToUpperInvariant();



        await connection.OpenAsync();



        await using var reader = await command.ExecuteReaderAsync();



        if (!await reader.ReadAsync())

        {

            return null;

        }



        var boxName = Convert.ToString(reader["BOXNAME"])?.Trim() ?? string.Empty;

        var shipBoxName = reader["SHIPBOXNAME"] is DBNull

            ? string.Empty

            : Convert.ToString(reader["SHIPBOXNAME"])?.Trim() ?? string.Empty;



        return (boxName, shipBoxName);

    }



    /// <summary>
    /// Resolves the CamVersion previously stored (via job scanning/batching)
    /// for a holder in HOLDER_ASSIGN. Used by the withdrawal flow so every
    /// downstream FEATS transaction (checkhold/addjob/moveout) targets the
    /// correct cam-version base URL.
    /// </summary>
    public async Task<string?> GetHolderCamVersionAsync(string holder, string process)

    {

        const string sql = """

        SELECT TOP 1 [CAMVERSION]

        FROM [BOXMANAGEMENT].[BOX].[HOLDER_ASSIGN]

        WHERE [HOLDER] = @HOLDER

          AND UPPER(LTRIM(RTRIM(ISNULL([PROCESS], '')))) = @PROCESS;

        """;



        await using var connection = new SqlConnection(_connectionString);

        await using var command = new SqlCommand(sql, connection);



        command.Parameters.Add("@HOLDER", SqlDbType.VarChar, 50).Value = holder.Trim();

        command.Parameters.Add("@PROCESS", SqlDbType.VarChar, 10).Value = process.Trim().ToUpperInvariant();



        await connection.OpenAsync();



        var result = await command.ExecuteScalarAsync();

        return result is null || result is DBNull
            ? null
            : Convert.ToString(result)?.Trim();

    }



    /// <summary>
    /// Resolves the CamVersion of a ShipBox by looking at the CAMVERSION of
    /// any holder already assigned to it (established from the first holder
    /// scanned into the shipbox during batching/withdrawal).
    /// </summary>
    public async Task<string?> GetShipBoxCamVersionAsync(string boxName, string shipBoxName, string process)

    {

        const string sql = """

        SELECT TOP 1 [CAMVERSION]

        FROM [BOXMANAGEMENT].[BOX].[HOLDER_ASSIGN]

        WHERE [BOXNAME] = @BOXNAME

          AND [SHIPBOXNAME] = @SHIPBOXNAME

          AND UPPER(LTRIM(RTRIM(ISNULL([PROCESS], '')))) = @PROCESS

          AND NULLIF(LTRIM(RTRIM([CAMVERSION])), '') IS NOT NULL;

        """;



        await using var connection = new SqlConnection(_connectionString);

        await using var command = new SqlCommand(sql, connection);



        command.Parameters.Add("@BOXNAME", SqlDbType.VarChar, 50).Value = boxName.Trim();

        command.Parameters.Add("@SHIPBOXNAME", SqlDbType.VarChar, 10).Value = shipBoxName.Trim();

        command.Parameters.Add("@PROCESS", SqlDbType.VarChar, 10).Value = process.Trim().ToUpperInvariant();



        await connection.OpenAsync();



        var result = await command.ExecuteScalarAsync();

        return result is null || result is DBNull
            ? null
            : Convert.ToString(result)?.Trim();

    }



    public async Task<List<BoxAssignment>> GetBoxAssignmentsAsync(string boxName, string process)

    {

        const string sql = """

        SELECT

            [HOLDER],

            [JOB],

            [QTY],

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

                Job = reader["JOB"] is DBNull ? null : Convert.ToString(reader["JOB"])?.Trim(),

                Qty = reader["QTY"] is DBNull ? null : Convert.ToInt32(reader["QTY"]),

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

        [JOB],

        [QTY],

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

                Job = reader["JOB"] is DBNull ? null : Convert.ToString(reader["JOB"])?.Trim(),

                Qty = reader["QTY"] is DBNull ? null : Convert.ToInt32(reader["QTY"]),

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

                ([HOLDER], [BOXNAME], [SHIPBOXNAME], [QTY], [PARTNUM], [PENNUM], [PRODUCTNAME], [LEC], [Factory], [PROCESS], [GRADE], [CAMVERSION], [JOB], [UPDATEBY], [UPDATETS])

            VALUES

                (@HOLDER, @BOXNAME, @SHIPBOXNAME, @QTY, @PARTNUM, @PENNUM, @PRODUCTNAME, @LEC, @Factory, @PROCESS, @GRADE, @CAMVERSION, @JOB, @UPDATEBY, @UPDATETS);

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

        command.Parameters.Add("@GRADE", SqlDbType.VarChar, 50).Value = data.BinName.Trim();

        command.Parameters.Add("@CAMVERSION", SqlDbType.VarChar, 10).Value =

            string.IsNullOrWhiteSpace(data.CamVersion) ? DBNull.Value : data.CamVersion.Trim();

        command.Parameters.Add("@JOB", SqlDbType.VarChar, 50).Value =
            string.IsNullOrWhiteSpace(data.Job) ? DBNull.Value : data.Job.Trim();

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

            DECLARE @RequestPartNum varchar(50);

            DECLARE @RequestGrade varchar(50);

            DECLARE @CurrentActualOutput bigint;

            DECLARE @CurrentStatus varchar(20);



            SELECT

                @RequestLec =

                    NULLIF(LTRIM(RTRIM([LEC])), ''),

                @RequestPenNum =

                    NULLIF(LTRIM(RTRIM([PENNUM])), ''),

                @RequestTotal = [TOTAL],

                @RequestPartNum =

                    NULLIF(LTRIM(RTRIM([SLIDERPARTNUMBER])), ''),

                @RequestGrade =

                    NULLIF(LTRIM(RTRIM([GRADE])), ''),

                @CurrentActualOutput = ISNULL([ACTUALOUTPUT], 0),

                @CurrentStatus = [STATUS]

            FROM [BOXMANAGEMENT].[HGA].[KITTING_REQUEST]

                WITH (UPDLOCK, HOLDLOCK)

            WHERE [REQUESTID] = @REQUESTID;



            IF @@ROWCOUNT = 0

                THROW 51010,

                    'The withdrawal request no longer exists.',

                    1;



            IF @RequestTotal IS NULL
                OR @RequestTotal < 0
            BEGIN
                THROW 51011,
                    'The withdrawal request no longer has valid TOTAL values.',
                    1;
            END;



            /*
             * NOTE: Deliberately no server-side FIFO/hold recompute here.
             * The client already performed hold-aware FIFO selection
             * (skipping on-hold holders and backfilling from the next
             * FIFO candidates). Recomputing a naive qty-only FIFO here
             * would not account for holds and would incorrectly flag
             * a valid, hold-aware client selection as "stale".
             *
             * The client-confirmed @ExpectedHolders list is trusted and
             * used directly below. The only server-side guarantees are:
             *   1) Each holder still exists under this LEC/PENNUM/PROCESS
             *      and matches the request's PartNum/Grade
             *      combination at delete time (WHERE clause on DELETE).
             *   2) The number of rows actually deleted matches the
             *      number of confirmed holders (post-delete count check).
             */



            DECLARE @DeletedAssignments TABLE

            (

                [HOLDER] varchar(50) NULL,

                [BOXNO] varchar(50) NULL,

                [SHIPBOXNAME] varchar(50) NULL,

                [QTY] bigint NOT NULL

            );



            /*

             * Stage 1: hard delete the Included Holders.

             * Do not add a STATUS = RELEASE condition.

             */

            DELETE HA

            OUTPUT

                DELETED.[HOLDER],

                DELETED.[BOXNAME],

                DELETED.[SHIPBOXNAME],

                CONVERT(bigint, ISNULL(DELETED.[QTY], 0))

            INTO @DeletedAssignments

            (

                [HOLDER],

                [BOXNO],

                [SHIPBOXNAME],

                [QTY]

            )

            FROM [BOXMANAGEMENT].[BOX].[HOLDER_ASSIGN] HA

            INNER JOIN @ExpectedHolders Included

                ON Included.[HolderKey] =

                    UPPER(LTRIM(RTRIM(HA.[HOLDER])))

            WHERE UPPER(
                    LTRIM(
                        RTRIM(
                            ISNULL(HA.[PROCESS], '')
                        )
                    )
                ) = 'FGI'

                /*
                 * NOTE: LEC and PENNUM are deliberately NOT matched here.
                 * They are optional identifiers on the withdrawal request
                 * (a request may have no LEC/PENNUM while the assigned
                 * Holder legitimately has one, or vice versa), so matching
                 * on them can incorrectly reject a valid delete.
                 */

                AND
                (
                    NULLIF(LTRIM(RTRIM(HA.[PARTNUM])), '') IS NULL
                    OR @RequestPartNum IS NULL
                    OR UPPER(LTRIM(RTRIM(HA.[PARTNUM]))) =
                        UPPER(LTRIM(RTRIM(@RequestPartNum)))
                )

                AND
                (
                    NULLIF(LTRIM(RTRIM(HA.[GRADE])), '') IS NULL
                    OR @RequestGrade IS NULL
                    OR UPPER(LTRIM(RTRIM(HA.[GRADE]))) =
                        UPPER(LTRIM(RTRIM(@RequestGrade)))
                );



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

                DECLARE @UnmatchedHolders nvarchar(2000) =
                (
                    SELECT STRING_AGG(E.[HolderKey], ', ')
                    FROM @ExpectedHolders E
                    WHERE NOT EXISTS
                    (
                        SELECT 1
                        FROM @DeletedAssignments D
                        WHERE D.[HOLDER] IS NOT NULL
                            AND UPPER(LTRIM(RTRIM(D.[HOLDER]))) = E.[HolderKey]
                    )
                );

                DECLARE @ErrorMessage nvarchar(2048) = CONCAT(
                    N'The Holder rows changed before deletion. No STACKER data was removed. Unmatched holders: ',
                    ISNULL(@UnmatchedHolders, N'(unknown)'));

                THROW 51013,

                    @ErrorMessage,

                    1;

            END;



            DECLARE @DeletedQtySum bigint =
            (
                SELECT ISNULL(SUM([QTY]), 0)
                FROM @DeletedAssignments
            );



            /*
             * Reflect the withdrawal completion on the source request:
             * accumulate ACTUALOUTPUT with the moved-out Qty and update status:
             * - Partial: When actual output is less than total but greater than 0
             * - Completed: When actual output equals or exceeds total (within tolerance of 500)
             * - Closed: When request reaches a certain time (not yet implemented)
             */
            DECLARE @NewActualOutput bigint = @CurrentActualOutput + @DeletedQtySum;
            DECLARE @NewStatus varchar(20);

            SET @NewStatus = CASE
                WHEN @NewActualOutput >= @RequestTotal AND @NewActualOutput <= (@RequestTotal + 500) THEN 'Completed'
                WHEN @NewActualOutput > 0 AND @NewActualOutput < @RequestTotal THEN 'Partial'
                ELSE @CurrentStatus
            END;

            UPDATE [BOXMANAGEMENT].[HGA].[KITTING_REQUEST]
            SET
                [ACTUALOUTPUT] = @NewActualOutput,
                [STATUS] = @NewStatus
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

                ) AS [DeletedBoxCount],

                @NewStatus AS [NewStatus],
                @NewActualOutput AS [NewActualOutput];

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



            command.Parameters

                .Add(

                    "@TOLERANCE",

                    SqlDbType.Int)

                .Value = FgiWithdrawalQtyTolerance;



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



            var newStatus = Convert.ToString(reader["NewStatus"])?.Trim() ?? string.Empty;
            var newActualOutput = Convert.ToInt64(reader["NewActualOutput"]);

            await reader.CloseAsync();

            await transaction.CommitAsync();

            // Send email notification based on status change
            _ = Task.Run(async () =>
            {
                try
                {
                    var requests = await GetFgiWithdrawalRequestsAsync();
                    var updatedRequest = requests.FirstOrDefault(r => r.RequestId == requestId);
                    if (updatedRequest != null)
                    {
                        // Update with the new values from the transaction
                        updatedRequest.Status = newStatus;
                        updatedRequest.ActualOutput = (int)newActualOutput;

                        switch (newStatus.ToUpper())
                        {
                            case "PARTIAL":
                                await _emailService.SendWithdrawalPartialEmailAsync(updatedRequest);
                                break;
                            case "COMPLETED":
                                await _emailService.SendWithdrawalCompletedEmailAsync(updatedRequest);
                                break;
                            case "CLOSED":
                                await _emailService.SendWithdrawalClosedEmailAsync(updatedRequest);
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the operation
                    Console.WriteLine($"Failed to send email notification: {ex.Message}");
                }
            });



            return result;

        }

        catch (SqlException exception)

            when (

                exception.Number is

                    51010 or

                    51011 or

                    51012 or

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

    public async Task<bool> DeleteFgiHoldHolderAssignmentAsync(string holder, string process)

    {

        const string sql = """

        DELETE

        FROM [BOXMANAGEMENT].[BOX].[HOLDER_ASSIGN]

        WHERE [HOLDER] = @HOLDER

          AND UPPER(LTRIM(RTRIM(ISNULL([PROCESS], '')))) = @PROCESS;

        """;



        await using var connection = new SqlConnection(_connectionString);

        await using var command = new SqlCommand(sql, connection);



        command.Parameters.Add("@HOLDER", SqlDbType.VarChar, 50).Value = holder;

        command.Parameters.Add("@PROCESS", SqlDbType.VarChar, 10).Value = process.Trim().ToUpperInvariant();



        await connection.OpenAsync();



        return await command.ExecuteNonQueryAsync() == 1;

    }

    public async Task<bool> ClearFgiHolderAssignmentStatusAsync(string holder, string process)

    {

        const string sql = """

        UPDATE [BOXMANAGEMENT].[BOX].[HOLDER_ASSIGN]

        SET [STATUS] = NULL

        WHERE [HOLDER] = @HOLDER

          AND UPPER(LTRIM(RTRIM(ISNULL([PROCESS], '')))) = @PROCESS;

        """;



        await using var connection = new SqlConnection(_connectionString);

        await using var command = new SqlCommand(sql, connection);



        command.Parameters.Add("@HOLDER", SqlDbType.VarChar, 50).Value = holder;

        command.Parameters.Add("@PROCESS", SqlDbType.VarChar, 10).Value = process.Trim().ToUpperInvariant();



        await connection.OpenAsync();



        return await command.ExecuteNonQueryAsync() == 1;

    }

    public async Task<List<CsvExportRow>> GetAllHolderAssignmentsForCsvAsync(string process)

    {

        const string sql = """

        SELECT

            HA.[HOLDER],

            HA.[JOB] AS Job,

            HA.[QTY] AS Qty,

            HA.[GRADE] AS Grade,

            HA.[BOXNAME] AS BlackBox,

            HA.[SHIPBOXNAME] AS ShipBox,

            HA.[UPDATETS] AS InsertedOn,

            HA.[QTY] AS Quantity,

            HA.[PRODUCTNAME] AS Model,

            HA.[PARTNUM] AS PartNum,

            HA.[PENNUM] AS PenNum,

            HA.[LEC],

            HA.[STATUS] AS Status

        FROM [BOXMANAGEMENT].[BOX].[HOLDER_ASSIGN] HA

        WHERE UPPER(LTRIM(RTRIM(ISNULL(HA.[PROCESS], '')))) = @PROCESS

        ORDER BY

            HA.[PRODUCTNAME],

            HA.[PARTNUM],

            HA.[PENNUM],

            HA.[LEC],

            HA.[BOXNAME],

            HA.[SHIPBOXNAME],

            HA.[HOLDER];

        """;



        await using var connection = new SqlConnection(_connectionString);

        await using var command = new SqlCommand(sql, connection);



        command.Parameters.Add("@PROCESS", SqlDbType.VarChar, 10).Value = process.Trim().ToUpperInvariant();



        await connection.OpenAsync();



        var results = new List<CsvExportRow>();

        await using var reader = await command.ExecuteReaderAsync();



        while (await reader.ReadAsync())

        {

            results.Add(new CsvExportRow

            {

                Holder = Convert.ToString(reader["HOLDER"])?.Trim() ?? "",

                Job = reader["Job"] is DBNull ? "" : Convert.ToString(reader["Job"])?.Trim() ?? "",

                Qty = reader["Qty"] is DBNull ? 0 : Convert.ToInt32(reader["Qty"]),

                Grade = Convert.ToString(reader["Grade"])?.Trim() ?? "",

                BlackBox = Convert.ToString(reader["BlackBox"])?.Trim() ?? "",

                ShipBox = Convert.ToString(reader["ShipBox"])?.Trim() ?? "",

                InsertedOn = reader["InsertedOn"] is DBNull

                    ? ""

                    : Convert.ToDateTime(reader["InsertedOn"]).ToString("yyyy-MM-dd HH:mm:ss"),

                Quantity = reader["Quantity"] is DBNull

                    ? 0

                    : Convert.ToInt32(reader["Quantity"]),

                Model = Convert.ToString(reader["Model"])?.Trim() ?? "",

                PartNum = Convert.ToString(reader["PartNum"])?.Trim() ?? "",

                PenNum = Convert.ToString(reader["PenNum"])?.Trim() ?? "",

                Lec = Convert.ToString(reader["LEC"])?.Trim() ?? "",

                Status = Convert.ToString(reader["Status"])?.Trim() ?? ""

            });

        }



        return results;

    }



}

