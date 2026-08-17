
using FeatsServiceReference;
using WDC_STACKER.API.Models;
using WDC_STACKER.API.Models.Feats;
using WDC_STACKER.API.Models.Stacker;
using WDC_STACKER.API.Services;
using System.Collections.Concurrent;

namespace WDC_STACKER.API.Aggregate
{
    public class StackerAggregate
    {
        private readonly FeatsService _featsService;
        private readonly AhsService _ahsService;
        private readonly FeatsCredentialStore _credentialStore;
        private readonly CapacityConfigService _capacityConfigService;
        private readonly StackerSqlService _stackerSqlService;
        private readonly IConfiguration _config;
        private readonly ILogger<StackerAggregate> _logger;
        private static readonly ConcurrentDictionary<string, (bool IsOnHold, string HoldSource)> _holdCheckCache = new();
        private static readonly ConcurrentDictionary<string, (FgiWithdrawalDisassociationPreviewView Preview, DateTime CachedAt)> _previewCache = new();
        private static readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(10);

        // ── In-site hold badge cache (grid/rack display only) ───────────────────
        // Separate from _holdCheckCache above (which gates scan/assign eligibility)
        // so a slow-changing display badge never invalidates the assign-time cache.
        private static readonly TimeSpan InSiteHoldCacheDuration = TimeSpan.FromMinutes(2);
        private static readonly ConcurrentDictionary<string, InSiteHoldCacheEntry>
            InSiteHoldCache = new(StringComparer.OrdinalIgnoreCase);

        private sealed record InSiteHoldCacheEntry(bool IsOnInSiteHold, DateTime ExpiresAt);
        private const string DefaultFgiRbf2Operation = "735617 RBF 2";

        public StackerAggregate(FeatsService featsService, AhsService ahsService, FeatsCredentialStore credentialStore, CapacityConfigService capacityConfigService, StackerSqlService stackerSqlService, IConfiguration config, ILogger<StackerAggregate> logger)
        {
            _featsService = featsService;
            _ahsService = ahsService;
            _credentialStore = credentialStore;
            _capacityConfigService = capacityConfigService;
            _stackerSqlService = stackerSqlService;
            _config = config;
            _logger = logger;

        }

        public bool IsSessionTokenValid(string token)
        {
            return _credentialStore.TryGet(token, out _);
        }

        public async Task<bool> CanAccessConfigurationAsync(string username, string password)
        {
            // Privilege lookups are not holder/cam-specific; cam3.4 is used
            // as the default endpoint for this administrative check.
            var result = await ExecuteFeatsQueryAsync(
                queryType: "UsersByPrivilege",
                fieldNames: new[] { "EmployeeName", "FullName" },
                filterName: "Privilege",
                filterValue: "TAP_FAB3ADMIN",
                recordLimit: 250,
                username: username,
                password: password,
                camVersion: WDC_STACKER.API.Models.CamVersion.Cam3_4);

            if (!result.Success)
                return false;

            return result.ParsedResult.Rows.Any(row =>
                row.TryGetValue("EmployeeName", out var employeeName) &&
                !string.IsNullOrWhiteSpace(employeeName) &&
                employeeName.Contains(username, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<GridViewBoxMapResult> MapGridViewBoxData(
            string? clientKey,
            HolderAssignInsertData? holderData = null,
            string? token = null)
        {
            var config = await _capacityConfigService.GetAsync(clientKey);
            var process = ResolveProcess(clientKey);
            var isFgi = string.Equals(process, "FGI", StringComparison.OrdinalIgnoreCase);

            var boxes = isFgi
                ? await _stackerSqlService.GetFgiBoxListCountAndPercentageAsync(config.MAX_ITEM_PER_BOX, process)
                : await _stackerSqlService.GetBoxListCountAndPercentageAsync(config.MAX_ITEM_PER_BOX, process);

            if (isFgi)
            {
                foreach (var box in boxes)
                {
                    box.ShipBoxes = await _stackerSqlService.GetFgiShipBoxesByBoxNoAsync(
                        box.BoxNo,
                        config.MAX_ITEM_PER_BOX_SHIPBOX,
                        process);
                }

                // Populate in-site hold badges for the rack overview grid (mini
                // ShipBox cells) whenever a session token is available. No-op if
                // token is null/expired so all existing callers are unaffected.
                if (!string.IsNullOrWhiteSpace(token))
                {
                    await PopulateFgiInSiteHoldStatusAsync(boxes, process, token);
                }
            }

            foreach (var box in boxes)
            {
                box.IsSuggestedTarget = false;

                foreach (var shipBox in box.ShipBoxes)
                {
                    shipBox.IsSuggestedTarget = false;
                }
            }

            if (isFgi)
            {
                if (holderData is null)
                {
                    return new GridViewBoxMapResult
                    {
                        Boxes = boxes,
                        HasSuggestedTarget = false,
                        Message = "FGI holder metadata is required to calculate a target."
                    };
                }

                if (string.IsNullOrWhiteSpace(holderData.PartNum) ||
                    string.IsNullOrWhiteSpace(holderData.ProductName))
                {
                    return new GridViewBoxMapResult
                    {
                        Boxes = boxes,
                        HasSuggestedTarget = false,
                        Message = "PartNum and ProductName are required for FGI targeting."
                    };
                }

                var existingTarget = TrySelectSuggestedFgiTarget(
                    boxes,
                    config,
                    holderData);

                if (existingTarget is not null)
                {
                    return new GridViewBoxMapResult
                    {
                        Boxes = boxes,
                        HasSuggestedTarget = true,
                        Message = "Grid view box data mapped successfully."
                    };
                }

                var newBox = TryCreateNextFgiBox(
                    boxes,
                    config,
                    holderData);

                if (newBox is null)
                {
                    return new GridViewBoxMapResult
                    {
                        Boxes = boxes,
                        HasSuggestedTarget = false,
                        Message = "No compatible FGI target is available and all settings are maxed out."
                    };
                }

                boxes.Add(newBox);

                return new GridViewBoxMapResult
                {
                    Boxes = boxes,
                    HasSuggestedTarget = true,
                    Message = "Grid view box data mapped successfully."
                };
            }

            var suggested = SelectSuggestedBox(boxes);

            if (suggested is not null)
            {
                suggested.IsSuggestedTarget = true;

                return new GridViewBoxMapResult
                {
                    Boxes = boxes,
                    HasSuggestedTarget = true,
                    Message = "Grid view box data mapped successfully."
                };
            }

            var allBoxesAreFullOrEmpty = boxes.Count == 0 || boxes.All(x => x.BoxListPercentage >= 100);

            if (allBoxesAreFullOrEmpty)
            {
                var lastBox = boxes
                    .OrderByDescending(x => x.RackNum)
                    .ThenByDescending(x => x.LayerRowNum)
                    .ThenByDescending(x => x.LayerColNum)
                    .FirstOrDefault();

                var newRackNum = 1;
                var newLayerRowNum = 1;
                var newLayerColNum = 1;

                if (lastBox is not null)
                {
                    newRackNum = lastBox.RackNum;
                    newLayerRowNum = lastBox.LayerRowNum;
                    newLayerColNum = lastBox.LayerColNum;

                    if (lastBox.LayerColNum < config.BOX_COUNT)
                    {
                        newLayerColNum = lastBox.LayerColNum + 1;
                    }
                    else if (lastBox.LayerRowNum < config.LAYER_COUNT)
                    {
                        newLayerColNum = 1;
                        newLayerRowNum = lastBox.LayerRowNum + 1;
                    }
                    else if (lastBox.RackNum < config.RACK_COUNT)
                    {
                        newLayerColNum = 1;
                        newLayerRowNum = 1;
                        newRackNum = lastBox.RackNum + 1;
                    }
                    else
                    {
                        return new GridViewBoxMapResult
                        {
                            Boxes = boxes,
                            HasSuggestedTarget = false,
                            Message = "All Settings are maxed out!"
                        };
                    }
                }

                var newBox = new BoxView
                {
                    BoxNo = $"R{newRackNum:00}L{newLayerRowNum:00}C{newLayerColNum:00}",
                    RackNum = newRackNum,
                    LayerRowNum = newLayerRowNum,
                    LayerColNum = newLayerColNum,
                    BoxListCount = 0,
                    BoxListPercentage = 0,
                    IsSuggestedTarget = true,
                    HasReleaseStatus = false
                };

                boxes.Add(newBox);

                return new GridViewBoxMapResult
                {
                    Boxes = boxes,
                    HasSuggestedTarget = true,
                    Message = "Grid view box data mapped successfully."
                };
            }

            return new GridViewBoxMapResult
            {
                Boxes = boxes,
                HasSuggestedTarget = false,
                Message = "No suggested target box was found."
            };
        }

        private static BoxView? SelectSuggestedBox(List<BoxView> boxes)
        {
            if (boxes.Count == 0)
                return null;

            if (boxes.All(x => x.BoxListPercentage >= 100))
                return null;

            return boxes
                .Where(x => x.BoxListPercentage < 100)
                .OrderBy(x => x.RackNum)
                .ThenBy(x => x.LayerRowNum)
                .ThenBy(x => x.LayerColNum)
                .FirstOrDefault();
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private static bool SameRequiredIdentity(
            string? left,
            string? right)
        {
            var normalizedLeft = NormalizeOptional(left);
            var normalizedRight = NormalizeOptional(right);

            return normalizedLeft is not null &&
                   normalizedRight is not null &&
                   string.Equals(
                       normalizedLeft,
                       normalizedRight,
                       StringComparison.OrdinalIgnoreCase);
        }

        private static bool SameNullableIdentity(
            string? left,
            string? right)
        {
            var normalizedLeft = NormalizeOptional(left);
            var normalizedRight = NormalizeOptional(right);

            if (normalizedLeft is null ||
                normalizedRight is null)
            {
                return normalizedLeft is null &&
                       normalizedRight is null;
            }

            return string.Equals(
                normalizedLeft,
                normalizedRight,
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsCompatibleFgiBox(
            BoxView box,
            HolderAssignInsertData holderData)
        {
            return box.BoxListPercentage < 100 &&
                   SameRequiredIdentity(
                       box.PartNum,
                       holderData.PartNum) &&
                   SameNullableIdentity(
                       box.PenNum,
                       holderData.PenNum) &&
                   SameRequiredIdentity(
                       box.ProductName,
                       holderData.ProductName) &&
                   SameRequiredIdentity(
                       box.CamVersion,
                       holderData.CamVersion);
        }

        private static BoxView? TrySelectSuggestedFgiTarget(
            List<BoxView> boxes,
            CapacityConfig config,
            HolderAssignInsertData holderData)
        {
            var compatibleBoxes = boxes
                .Where(box => IsCompatibleFgiBox(box, holderData))
                .OrderBy(box => box.BoxListPercentage)
                .ThenBy(box => box.RackNum)
                .ThenBy(box => box.LayerRowNum)
                .ThenBy(box => box.LayerColNum);

            foreach (var box in compatibleBoxes)
            {
                if (!TryEnsureSuggestedFgiShipBox(
                        box,
                        config,
                        holderData.Lec))
                {
                    continue;
                }

                box.IsSuggestedTarget = true;
                return box;
            }

            return null;
        }

        private static bool TryEnsureSuggestedFgiShipBox(
            BoxView box,
            CapacityConfig config,
            string? targetLec)
        {
            foreach (var shipBox in box.ShipBoxes)
            {
                shipBox.IsSuggestedTarget = false;
            }

            var normalizedLec = NormalizeOptional(targetLec);

            if (normalizedLec is not null)
            {
                var suggested = box.ShipBoxes
                    .Where(x =>
                        x.ShipBoxListPercentage < 100 &&
                        SameRequiredIdentity(x.Lec, normalizedLec))
                    .OrderBy(x => x.ShipBoxListPercentage)
                    .ThenBy(x => x.ShipBoxNum)
                    .ThenBy(x => x.LayerRowNum)
                    .ThenBy(x => x.LayerColNum)
                    .FirstOrDefault();

                if (suggested is not null)
                {
                    suggested.IsSuggestedTarget = true;
                    return true;
                }
            }

            // A missing LEC always requires a new ShipBox. A non-null LEC
            // reaches this point only when no compatible ShipBox exists.
            return TryAddSuggestedFgiShipBox(
                box,
                config,
                normalizedLec);
        }

        private static bool TryAddSuggestedFgiShipBox(
            BoxView box,
            CapacityConfig config,
            string? normalizedLec)
        {
            if (config.MAX_ITEM_PER_BOX <= 0 ||
                box.ShipBoxes.Count >= config.MAX_ITEM_PER_BOX ||
                config.MAX_ITEM_PER_BOX_SHIPBOX <= 0 ||
                config.LAYER_COUNT_SHIPBOX <= 0 ||
                config.BOX_COUNT_SHIPBOX <= 0)
            {
                return false;
            }

            var lastShipBox = box.ShipBoxes
                .OrderByDescending(x => x.ShipBoxNum)
                .ThenByDescending(x => x.LayerRowNum)
                .ThenByDescending(x => x.LayerColNum)
                .FirstOrDefault();

            var newShipBoxNum = 1;
            var newLayerRowNum = 1;
            var newLayerColNum = 1;

            if (lastShipBox is not null)
            {
                newShipBoxNum = lastShipBox.ShipBoxNum;
                newLayerRowNum = lastShipBox.LayerRowNum;
                newLayerColNum = lastShipBox.LayerColNum;

                if (lastShipBox.LayerColNum < config.BOX_COUNT_SHIPBOX)
                {
                    newLayerColNum = lastShipBox.LayerColNum + 1;
                }
                else if (lastShipBox.LayerRowNum < config.LAYER_COUNT_SHIPBOX)
                {
                    newLayerColNum = 1;
                    newLayerRowNum = lastShipBox.LayerRowNum + 1;
                }
                else
                {
                    return false;
                }
            }

            box.ShipBoxes.Add(new ShipBoxView
            {
                BoxNo = box.BoxNo,
                ShipBoxName =
                    $"S{newShipBoxNum:00}L{newLayerRowNum:00}C{newLayerColNum:00}",
                ShipBoxStatus = string.Empty,
                ShipBoxNum = newShipBoxNum,
                LayerRowNum = newLayerRowNum,
                LayerColNum = newLayerColNum,
                ShipBoxListCount = 0,
                ShipBoxListPercentage = 0,
                IsSuggestedTarget = true,
                HasReleaseStatus = true,
                Lec = normalizedLec ?? string.Empty,
                CamVersion = box.CamVersion
            });

            return true;
        }

        private static BoxView? TryCreateNextFgiBox(
            List<BoxView> boxes,
            CapacityConfig config,
            HolderAssignInsertData holderData)
        {
            if (config.RACK_COUNT <= 0 ||
                config.LAYER_COUNT <= 0 ||
                config.BOX_COUNT <= 0)
            {
                return null;
            }

            var lastBox = boxes
                .OrderByDescending(x => x.RackNum)
                .ThenByDescending(x => x.LayerRowNum)
                .ThenByDescending(x => x.LayerColNum)
                .FirstOrDefault();

            var newRackNum = 1;
            var newLayerRowNum = 1;
            var newLayerColNum = 1;

            if (lastBox is not null)
            {
                newRackNum = lastBox.RackNum;
                newLayerRowNum = lastBox.LayerRowNum;
                newLayerColNum = lastBox.LayerColNum;

                if (lastBox.LayerColNum < config.BOX_COUNT)
                {
                    newLayerColNum = lastBox.LayerColNum + 1;
                }
                else if (lastBox.LayerRowNum < config.LAYER_COUNT)
                {
                    newLayerColNum = 1;
                    newLayerRowNum = lastBox.LayerRowNum + 1;
                }
                else if (lastBox.RackNum < config.RACK_COUNT)
                {
                    newLayerColNum = 1;
                    newLayerRowNum = 1;
                    newRackNum = lastBox.RackNum + 1;
                }
                else
                {
                    return null;
                }
            }

            var newBox = new BoxView
            {
                BoxNo = $"R{newRackNum:00}L{newLayerRowNum:00}C{newLayerColNum:00}",
                PartNum = NormalizeOptional(holderData.PartNum),
                PenNum = NormalizeOptional(holderData.PenNum),
                ProductName = NormalizeOptional(holderData.ProductName),
                CamVersion = NormalizeOptional(holderData.CamVersion),
                RackNum = newRackNum,
                LayerRowNum = newLayerRowNum,
                LayerColNum = newLayerColNum,
                BoxListCount = 0,
                BoxListPercentage = 0,
                IsSuggestedTarget = false,
                HasReleaseStatus = false
            };

            if (!TryEnsureSuggestedFgiShipBox(
                    newBox,
                    config,
                    holderData.Lec))
            {
                return null;
            }

            newBox.IsSuggestedTarget = true;
            return newBox;
        }

        public async Task<ScanHolderJobResponse> ScanHolderJobAsync(string holder, string token, string? clientKey)
        {
            if (!_credentialStore.TryGet(token, out var credentials))
            {
                return new ScanHolderJobResponse
                {
                    Success = false,
                    CanAssign = false,
                    Holder = holder,
                    Message = "Invalid or expired token."
                };
            }

            var isFgi = string.Equals(
                clientKey?.Trim(),
                "WDC_STACKER.CLIENT.FGI",
                StringComparison.OrdinalIgnoreCase);

            var process = ResolveProcess(clientKey);
            var existingLocation = await _stackerSqlService.GetHolderAssignLocationAsync(holder, process);

            if (existingLocation is not null)
            {
                var existingGridViewBoxMap = await MapGridViewBoxData(clientKey, token: token);
                var existingBoxes = existingGridViewBoxMap.Boxes;
                var existingBox = existingBoxes.FirstOrDefault(b =>
                    string.Equals(b.BoxNo, existingLocation.Value.BoxName, StringComparison.OrdinalIgnoreCase));

                var locationMessage = existingBox is not null
                    ? $"Holder is already assigned to Box {existingBox.BoxNo} (Rack {existingBox.RackNum}, Layer {existingBox.LayerRowNum}, Column {existingBox.LayerColNum})"
                    : $"Holder is already assigned to Box {existingLocation.Value.BoxName}";

                if (existingBox is not null)
                {
                    existingBox.IsSuggestedTarget = true;
                }

                if (!string.IsNullOrWhiteSpace(existingLocation.Value.ShipBoxName))
                {
                    locationMessage += $", ShipBox {existingLocation.Value.ShipBoxName}";
                }

                return new ScanHolderJobResponse
                {
                    Success = false,
                    CanAssign = false,
                    Holder = holder,
                    Message = locationMessage,
                    GridViewBoxes = existingBoxes
                };
            }

            var fieldNames = new List<string>
            {
                "Holder",
                "BuildCode",
                "Job",
                "JobLevel",
                "Operation",
                "ProductName",
                "BinName",
                "ClassName",
                "ShipTicket",
                "ParentHolder",
                "Routing",
                "Workflow",
                "WaferNum",
                "SliderCount",
                "HoldReason",
                "HoldComment",
                "InProcess"
            };

            if (isFgi)
            {
                fieldNames.AddRange(new[]
                {
                    "PartNumber",
                    "Experiment",
                    "QuantityGood",
                    "QuantityLoaded",
                    "SliderCount"
                });
            }

            var (holderJobResult, resolvedCamVersion) = await ExecuteFeatsQueryAcrossCamVersionsAsync(
                queryType: "HolderJob",
                fieldNames: fieldNames.ToArray(),
                filterName: "Holder",
                filterValue: holder,
                recordLimit: 250,
                username: credentials.Username,
                password: credentials.Password);

            if (!holderJobResult.Success)
            {
                return new ScanHolderJobResponse
                {
                    Success = false,
                    CanAssign = false,
                    Holder = holder,
                    Message = holderJobResult.Message,
                    RawQueryResult = holderJobResult
                };
            }

            var row = holderJobResult.ParsedResult.Rows.FirstOrDefault();

            if (row is null || resolvedCamVersion is null)
            {
                return new ScanHolderJobResponse
                {
                    Success = false,
                    CanAssign = false,
                    Holder = holder,
                    Message = "HolderJob record was not found.",
                    RawQueryResult = holderJobResult
                };
            }

            var camVersion = resolvedCamVersion;
            var config = await _capacityConfigService.GetAsync(clientKey);

            var operation = GetField(row, "Operation");
            var parentHolder = GetField(row, "ParentHolder");
            var shipTicket = GetField(row, "ShipTicket");
            var partNumber = GetField(row, "PartNumber");
            var experimentId = isFgi
                ? GetField(row, "Experiment")
                : string.Empty;
            var productName = GetField(row, "ProductName");
            var buildCode = GetField(row, "BuildCode");
            var binName = GetField(row, "BinName");
            var job = GetField(row, "Job");
            var sliderCount = GetField(row, "SliderCount");
            var holdReason = GetField(row, "HoldReason");
            var holdComment = GetField(row, "HoldComment");
            var inProcess = GetField(row, "InProcess");

            _logger.LogInformation(
                "[SCAN VALIDATION] Holder={Holder}, ClientKey={ClientKey}, IsFGI={IsFGI}",
                holder,
                clientKey,
                isFgi);
            _logger.LogInformation(
                "[SCAN VALIDATION] Values - Operation={Operation}, PartNumber={PartNumber}, ProductName={ProductName}, BinName={BinName}, HoldReason={HoldReason}, HoldComment={HoldComment}, InProcess={InProcess}",
                operation,
                partNumber ?? "NULL",
                productName ?? "NULL",
                binName ?? "NULL",
                holdReason ?? "NULL",
                holdComment ?? "NULL",
                inProcess ?? "NULL");
        
        #region Validation Process
            // 1. Operation must match config.ValidOperation
            _logger.LogInformation(
                "[SCAN VALIDATION] Checking Operation - Expected={ExpectedOperation}, Actual={ActualOperation}",
                config.ValidOperation,
                operation);
            if (!string.Equals(operation, config.ValidOperation, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "[SCAN VALIDATION] FAILED - Operation mismatch. Expected={Expected}, Actual={Actual}",
                    config.ValidOperation,
                    operation);
                return new ScanHolderJobResponse
                {
                    Success = false,
                    CanAssign = false,
                    Holder = holder,
                    Message = "Operation is not valid",
                    HolderJob = row,
                    RawQueryResult = holderJobResult
                };
            }
            _logger.LogInformation("[SCAN VALIDATION] PASSED - Operation check");

            //Note: disable for now while on development/QA, enable once on UAT/PROD
            //2. ParentHolder must have No value 
            if (!string.IsNullOrWhiteSpace(parentHolder))
            {
               return new ScanHolderJobResponse
               {
                   Success = false,
                   CanAssign = false,
                   Holder = holder,
                   Message = "Holder has a Parent!",
                   HolderJob = row,
                   RawQueryResult = holderJobResult
               };
            }

            // Note: disable for now while on development/QA, enable once on UAT/PROD
            // 3. ShipTicket must be empty
            //if (!string.IsNullOrWhiteSpace(shipTicket))
            //{
            //    return new ScanHolderJobResponse
            //    {
            //        Success = false,
            //        CanAssign = false,
            //        Holder = holder,
            //        Message = "ShipTicket has value!",
            //        HolderJob = row,
            //        RawQueryResult = holderJobResult
            //    };
            //}

            // 4. FGI Box identity requires PartNum and ProductName.
            // If PartNumber is missing, MoveOut to RBF2 and apply Hold.
            _logger.LogInformation(
                "[SCAN VALIDATION] Checking PartNumber/ProductName - PartNumber={PartNumber}, ProductName={ProductName}",
                partNumber ?? "NULL",
                productName ?? "NULL");
            if (isFgi &&
                (string.IsNullOrWhiteSpace(partNumber) ||
                 string.IsNullOrWhiteSpace(productName)))
            {
                _logger.LogWarning(
                    "[SCAN VALIDATION] FAILED - Missing PartNumber or ProductName. PartNumber={PartNumber}, ProductName={ProductName}",
                    partNumber ?? "NULL",
                    productName ?? "NULL");
                
                // Determine which field(s) are missing for dynamic messaging
                var missingPartNumber = string.IsNullOrWhiteSpace(partNumber);
                var missingProductName = string.IsNullOrWhiteSpace(productName);
                var validationHoldComment = missingPartNumber && missingProductName
                    ? "NO PART NUMBER AND NO PRODUCT NAME"
                    : missingPartNumber
                        ? "NO PART NUMBER"
                        : "NO PRODUCT NAME";

                // Release any existing hold first, so MoveOut is not blocked by a holder already on hold.
                var releaseResult = await ReleaseHolderWithRetryAsync(
                    holder: holder,
                    holderType: null,
                    comment: string.Empty,
                    username: credentials.Username,
                    password: credentials.Password,
                    camVersion: camVersion);

                _logger.LogInformation(
                    "[SCAN VALIDATION] Release - Success={Success}, Message={Message}",
                    releaseResult.Success,
                    releaseResult.Message);

                if (!releaseResult.Success)
                {
                    _logger.LogWarning(
                        "[SCAN VALIDATION] Release failed, proceeding to MoveOut anyway: {Message}",
                        releaseResult.Message);
                }

                // MoveOut to RBF2
                var moveOutResult = await _featsService.MoveOutAsync(
                    holder: holder,
                    holderType: null,
                    resource: null,
                    nextOp: "735617 RBF 2",
                    username: credentials.Username,
                    password: credentials.Password,
                    camVersion: camVersion);

                _logger.LogInformation(
                    "[SCAN VALIDATION] MoveOut to RBF2 - Success={Success}, Message={Message}",
                    moveOutResult.Success,
                    moveOutResult.Message);
                if (!moveOutResult.Success)
                {
                    _logger.LogError(
                        "[SCAN VALIDATION] FAILED - MoveOut to RBF2 failed: {Message}",
                        moveOutResult.Message);
                    return new ScanHolderJobResponse
                    {
                        Success = false,
                        CanAssign = false,
                        Holder = holder,
                        Message = $"MoveOut to 735617 RBF 2 failed: {moveOutResult.Message}",
                        HolderJob = row,
                        RawQueryResult = holderJobResult
                    };
                }

                // Apply Hold with reason TAP and dynamic comment based on missing field(s)
                var holdResult = await _featsService.HoldHolderAsync(
                    holder: holder,
                    holderType: null,
                    holdReasonCode: "TAP",
                    comment: validationHoldComment,
                    username: credentials.Username,
                    password: credentials.Password,
                    camVersion: camVersion);

                _logger.LogInformation(
                    "[SCAN VALIDATION] Hold application - Success={Success}, Message={Message}",
                    holdResult.Success,
                    holdResult.Message);
                if (!holdResult.Success)
                {
                    // Compensating logic: Hold failed after MoveOut succeeded
                    // Log the partial failure state and return error
                    _logger.LogWarning(
                        "[SCAN VALIDATION] PARTIAL FAILURE - MoveOut succeeded but Hold failed. Message: {HoldMessage}",
                        holdResult.Message);

                    return new ScanHolderJobResponse
                    {
                        Success = false,
                        CanAssign = false,
                        Holder = holder,
                        Message = $"MoveOut to RBF2 succeeded, but Hold failed: {holdResult.Message}. Holder is now moved out but not held.",
                        HolderJob = row,
                        RawQueryResult = holderJobResult
                    };
                }

                // Dynamic return message based on missing field(s)
                var returnMessage = missingPartNumber && missingProductName
                    ? "PartNumber and ProductName are missing. Holder has been moved out to RBF2 and held with reason TAP: NO PART NUMBER AND NO PRODUCT NAME."
                    : missingPartNumber
                        ? "PartNumber is missing. Holder has been moved out to RBF2 and held with reason TAP: NO PART NUMBER."
                        : "ProductName is missing. Holder has been moved out to RBF2 and held with reason TAP: NO PRODUCT NAME.";

                return new ScanHolderJobResponse
                {
                    Success = false,
                    CanAssign = false,
                    Holder = holder,
                    Message = returnMessage,
                    HolderJob = row,
                    RawQueryResult = holderJobResult
                };
            }
            _logger.LogInformation("[SCAN VALIDATION] PASSED - PartNumber/ProductName check");

            // 5. BinName must be exactly 5 characters for FGI.
            // If not, MoveOut to RBF2.
            _logger.LogInformation(
                "[SCAN VALIDATION] Checking BinName - BinName={BinName}, Length={Length}",
                binName ?? "NULL",
                binName?.Length ?? 0);
            if (isFgi && binName?.Length != 5)
            {
                _logger.LogWarning(
                    "[SCAN VALIDATION] FAILED - BinName must be exactly 5 characters. Current={BinName}, Length={Length}",
                    binName ?? "NULL",
                    binName?.Length ?? 0);
                var moveOutResult = await _featsService.MoveOutAsync(
                    holder: holder,
                    holderType: null,
                    resource: null,
                    nextOp: "735617 RBF 2",
                    username: credentials.Username,
                    password: credentials.Password,
                    camVersion: camVersion);

                _logger.LogInformation(
                    "[SCAN VALIDATION] MoveOut to RBF2 - Success={Success}, Message={Message}",
                    moveOutResult.Success,
                    moveOutResult.Message);
                if (!moveOutResult.Success)
                {
                    _logger.LogError(
                        "[SCAN VALIDATION] FAILED - MoveOut to RBF2 failed: {Message}",
                        moveOutResult.Message);
                    return new ScanHolderJobResponse
                    {
                        Success = false,
                        CanAssign = false,
                        Holder = holder,
                        Message = $"MoveOut to 735617 RBF 2 failed: {moveOutResult.Message}",
                        HolderJob = row,
                        RawQueryResult = holderJobResult
                    };
                }

                return new ScanHolderJobResponse
                {
                    Success = false,
                    CanAssign = false,
                    Holder = holder,
                    Message = $"BinName must be exactly 5 characters. Current BinName: '{binName}'. Holder has been moved out to RBF2.",
                    HolderJob = row,
                    RawQueryResult = holderJobResult
                };
            }
            _logger.LogInformation("[SCAN VALIDATION] PASSED - BinName check");

            // 6. Two-step Hold Validation
            // Step 1: Check FEATS HoldReason and HoldComment
            // If both are null, holder is free of holds
            _logger.LogInformation(
                "[SCAN VALIDATION] Checking FEATS Hold - HoldReason={HoldReason}, HoldComment={HoldComment}",
                holdReason ?? "NULL",
                holdComment ?? "NULL");
            if (!string.IsNullOrWhiteSpace(holdReason) || !string.IsNullOrWhiteSpace(holdComment))
            {
                if (isFgi)
                {
                    _logger.LogWarning(
                        "[SCAN VALIDATION] FAILED - Holder has FEATS hold. HoldReason={HoldReason}, HoldComment={HoldComment}",
                        holdReason ?? "NULL",
                        holdComment ?? "NULL");

                    // Save hold reason/comment before releasing, so it can be re-applied after MoveOut.
                    var savedHoldReasonCode = holdReason;
                    var savedHoldComment = holdComment;

                    var releaseResult = await ReleaseHolderWithRetryAsync(
                        holder: holder,
                        holderType: null,
                        comment: string.Empty,
                        username: credentials.Username,
                        password: credentials.Password,
                        camVersion: camVersion);

                    _logger.LogInformation(
                        "[SCAN VALIDATION] Release - Success={Success}, Message={Message}",
                        releaseResult.Success,
                        releaseResult.Message);

                    if (!releaseResult.Success)
                    {
                        _logger.LogWarning(
                            "[SCAN VALIDATION] Release failed, proceeding to MoveOut anyway: {Message}",
                            releaseResult.Message);
                    }

                    var moveOutResult = await _featsService.MoveOutAsync(
                        holder: holder,
                        holderType: null,
                        resource: null,
                        nextOp: "735617 RBF 2",
                        username: credentials.Username,
                        password: credentials.Password,
                        camVersion: camVersion);

                    _logger.LogInformation(
                        "[SCAN VALIDATION] MoveOut to RBF2 - Success={Success}, Message={Message}",
                        moveOutResult.Success,
                        moveOutResult.Message);

                    var reapplyHoldResult = await _featsService.HoldHolderAsync(
                        holder: holder,
                        holderType: null,
                        holdReasonCode: savedHoldReasonCode,
                        comment: savedHoldComment,
                        username: credentials.Username,
                        password: credentials.Password,
                        camVersion: camVersion);

                    _logger.LogInformation(
                        "[SCAN VALIDATION] Re-apply Hold - Success={Success}, Message={Message}",
                        reapplyHoldResult.Success,
                        reapplyHoldResult.Message);

                    if (!reapplyHoldResult.Success)
                    {
                        var partialMessage = moveOutResult.Success
                            ? $"Holder was moved to 735617 RBF 2 but NOT re-held: {reapplyHoldResult.Message}"
                            : $"MoveOut failed ({moveOutResult.Message}) and the holder was NOT re-held: {reapplyHoldResult.Message}";

                        return new ScanHolderJobResponse
                        {
                            Success = false,
                            CanAssign = false,
                            Holder = holder,
                            Message = partialMessage,
                            HolderJob = row,
                            RawQueryResult = holderJobResult
                        };
                    }

                    if (!moveOutResult.Success)
                    {
                        return new ScanHolderJobResponse
                        {
                            Success = false,
                            CanAssign = false,
                            Holder = holder,
                            Message = $"MoveOut to 735617 RBF 2 failed: {moveOutResult.Message}. Hold was re-applied at the current location.",
                            HolderJob = row,
                            RawQueryResult = holderJobResult
                        };
                    }

                    return new ScanHolderJobResponse
                    {
                        Success = false,
                        CanAssign = false,
                        Holder = holder,
                        Message = $"Holder has FEATS hold. HoldReason: '{savedHoldReasonCode}', HoldComment: '{savedHoldComment}'. Holder was released, moved to 735617 RBF 2, and re-held.",
                        HolderJob = row,
                        RawQueryResult = holderJobResult
                    };
                }

                _logger.LogInformation(
                    "[SCAN VALIDATION] PWD Holder has FEATS hold, will be assigned with STATUS=HOLD. HoldReason={HoldReason}, HoldComment={HoldComment}",
                    holdReason ?? "NULL",
                    holdComment ?? "NULL");
            }
            _logger.LogInformation("[SCAN VALIDATION] PASSED - FEATS Hold check");

            // Step 2: Check AHS SliderCheck2 for all operations in config
            // Loop through each operation and check if holder has hold/slider issue
            // If any operation returns "EXISTS" or "ONHOLD", fail validation
            // Only if all operations return "PASSED" does it pass
            var holdValidationOperations = config.HoldValidationOperations;
            if (holdValidationOperations == null || holdValidationOperations.Count == 0)
            {
                // If no operations configured, use the current operation as fallback
                holdValidationOperations = new List<string> { operation };
            }
            _logger.LogInformation(
                "[SCAN VALIDATION] Checking AHS SliderCheck2 for {Count} operations: {Operations}",
                holdValidationOperations.Count,
                string.Join(", ", holdValidationOperations));

            foreach (var validationOperation in holdValidationOperations)
            {
                _logger.LogInformation(
                    "[SCAN VALIDATION] AHS SliderCheck2 - Operation={Operation}",
                    validationOperation);
                var sliderCheckResult = await _ahsService.SliderCheck2Async(
                    holder: holder,
                    operation: validationOperation,
                    checkExist: false);

                _logger.LogInformation(
                    "[SCAN VALIDATION] AHS SliderCheck2 Result - Success={Success}, RawResponse={RawResponse}",
                    sliderCheckResult.Success,
                    sliderCheckResult.RawResponse ?? "NULL");
                if (!sliderCheckResult.Success)
                {
                    _logger.LogError(
                        "[SCAN VALIDATION] FAILED - AHS SliderCheck2 failed for operation '{Operation}': {Message}",
                        validationOperation,
                        sliderCheckResult.Message);
                    return new ScanHolderJobResponse
                    {
                        Success = false,
                        CanAssign = false,
                        Holder = holder,
                        Message = $"AHS SliderCheck2 failed for operation '{validationOperation}': {sliderCheckResult.Message}",
                        HolderJob = row,
                        RawQueryResult = holderJobResult
                    };
                }

                // Check if result contains EXISTS, ONHOLD, ERROR, or PASSED
                var responseUpper = sliderCheckResult.RawResponse?.ToUpperInvariant() ?? string.Empty;
                var isHoldIssue = responseUpper.Contains("EXISTS") || responseUpper.Contains("ONHOLD");
                var isError = responseUpper.Contains("ERROR");
                var isPassed = responseUpper.Contains("PASSED");

                _logger.LogInformation(
                    "[SCAN VALIDATION] AHS Response Check - Response={Response}, IsHoldIssue={IsHoldIssue}, IsError={IsError}, IsPassed={IsPassed}",
                    responseUpper,
                    isHoldIssue,
                    isError,
                    isPassed);

                if (isHoldIssue) //RELEASE - MOVEOUT - HOLD
                {
                    _logger.LogWarning(
                        "[SCAN VALIDATION] FAILED - Holder has hold/slider issue for operation '{Operation}'. Response: {Response}",
                        validationOperation,
                        responseUpper);

                    // Save hold reason/comment before releasing, so it can be re-applied after MoveOut.
                    var savedHoldReasonCode = holdReason;
                    var savedHoldComment = holdComment;

                    var releaseResult = await ReleaseHolderWithRetryAsync(
                        holder: holder,
                        holderType: null,
                        comment: string.Empty,
                        username: credentials.Username,
                        password: credentials.Password,
                        camVersion: camVersion);

                    _logger.LogInformation(
                        "[SCAN VALIDATION] Release - Success={Success}, Message={Message}",
                        releaseResult.Success,
                        releaseResult.Message);

                    if (!releaseResult.Success)
                    {
                        _logger.LogWarning(
                            "[SCAN VALIDATION] Release failed, proceeding to MoveOut anyway: {Message}",
                            releaseResult.Message);
                    }

                    // MoveOut to RBF2
                    var moveOutResult = await _featsService.MoveOutAsync(
                        holder: holder,
                        holderType: null,
                        resource: null,
                        nextOp: "735617 RBF 2",
                        username: credentials.Username,
                        password: credentials.Password,
                        camVersion: camVersion);

                    _logger.LogInformation(
                        "[SCAN VALIDATION] MoveOut to RBF2 - Success={Success}, Message={Message}",
                        moveOutResult.Success,
                        moveOutResult.Message);

                    var reapplyHoldResult = await _featsService.HoldHolderAsync(
                        holder: holder,
                        holderType: null,
                        holdReasonCode: savedHoldReasonCode,
                        comment: savedHoldComment,
                        username: credentials.Username,
                        password: credentials.Password,
                        camVersion: camVersion);

                    _logger.LogInformation(
                        "[SCAN VALIDATION] Re-apply Hold - Success={Success}, Message={Message}",
                        reapplyHoldResult.Success,
                        reapplyHoldResult.Message);

                    if (!reapplyHoldResult.Success)
                    {
                        var partialMessage = moveOutResult.Success
                            ? $"Holder was moved to 735617 RBF 2 but NOT re-held: {reapplyHoldResult.Message}"
                            : $"MoveOut failed ({moveOutResult.Message}) and the holder was NOT re-held: {reapplyHoldResult.Message}";

                        return new ScanHolderJobResponse
                        {
                            Success = false,
                            CanAssign = false,
                            Holder = holder,
                            Message = partialMessage,
                            HolderJob = row,
                            RawQueryResult = holderJobResult
                        };
                    }

                    if (!moveOutResult.Success)
                    {
                        return new ScanHolderJobResponse
                        {
                            Success = false,
                            CanAssign = false,
                            Holder = holder,
                            Message = $"MoveOut to 735617 RBF 2 failed: {moveOutResult.Message}. Hold was re-applied at the current location.",
                            HolderJob = row,
                            RawQueryResult = holderJobResult
                        };
                    }

                    return new ScanHolderJobResponse
                    {
                        Success = false,
                        CanAssign = false,
                        Holder = holder,
                        Message = $"Holder has hold/slider issue for operation '{validationOperation}'. AHS response: {sliderCheckResult.RawResponse}. Holder was released, moved to 735617 RBF 2, and re-held.",
                        HolderJob = row,
                        RawQueryResult = holderJobResult
                    };
                }

                if (isError)
                {
                    _logger.LogError(
                        "[SCAN VALIDATION] FAILED - AHS Check returned ERROR for operation '{Operation}'. Response: {Response}",
                        validationOperation,
                        sliderCheckResult.RawResponse);

                    return new ScanHolderJobResponse
                    {
                        Success = false,
                        CanAssign = false,
                        Holder = holder,
                        Message = $"Holder {holder} has error on AHS Check: {sliderCheckResult.RawResponse}",
                        HolderJob = row,
                        RawQueryResult = holderJobResult
                    };
                }

                // If result contains PASSED, continue to next operation
                // If result is anything else, log it but continue
                if (!isPassed)
                {
                    _logger.LogWarning(
                        "[SCAN VALIDATION] AHS SliderCheck2 returned unexpected response for holder={Holder}, operation={Operation}: {Response}",
                        holder,
                        validationOperation,
                        sliderCheckResult.RawResponse);
                }
                else
                {
                    _logger.LogInformation(
                        "[SCAN VALIDATION] AHS SliderCheck2 PASSED for operation={Operation}",
                        validationOperation);
                }
            }
            _logger.LogInformation("[SCAN VALIDATION] PASSED - AHS SliderCheck2 check (all operations)");

            // 7.  Holder QTY Validation
            // Check if Holder QTY is more than 1
            if (isFgi && !TryGetValidatedHolderQty(row, out _))
            {
                _logger.LogWarning("[SCAN VALIDATION] FAILED - Holder QTY is invalid");
                return new ScanHolderJobResponse
                {
                    Success = false,
                    CanAssign = false,
                    Holder = holder,
                    Message = "Holder QTY is invalid",
                    HolderJob = row,
                    RawQueryResult = holderJobResult
                };
            }
            _logger.LogInformation("[SCAN VALIDATION] PASSED - Holder QTY check");

            // 8. InProcess Validation (Last validation)
            // If InProcess is not "True", perform MoveIn then continue
            _logger.LogInformation(
                "[SCAN VALIDATION] Checking InProcess - InProcess={InProcess}",
                inProcess ?? "NULL");
            if (!string.Equals(inProcess, "True", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation(
                    "[SCAN VALIDATION] InProcess is not 'True', performing MoveIn...");
                var moveInResult = await _featsService.MoveInAsync(
                    holder: holder,
                    holderType: null,
                    resource: null,
                    username: credentials.Username,
                    password: credentials.Password,
                    camVersion: camVersion);

                _logger.LogInformation(
                    "[SCAN VALIDATION] MoveIn Result - Success={Success}, Message={Message}",
                    moveInResult.Success,
                    moveInResult.Message);
                if (!moveInResult.Success)
                {
                    _logger.LogError(
                        "[SCAN VALIDATION] FAILED - MoveIn failed: {Message}",
                        moveInResult.Message);
                    return new ScanHolderJobResponse
                    {
                        Success = false,
                        CanAssign = false,
                        Holder = holder,
                        Message = $"MoveIn failed: {moveInResult.Message}",
                        HolderJob = row,
                        RawQueryResult = holderJobResult
                    };
                }

                _logger.LogInformation(
                    "[SCAN VALIDATION] MoveIn successful for holder={Holder} with InProcess={InProcess}",
                    holder,
                    inProcess);
            }
            _logger.LogInformation("[SCAN VALIDATION] PASSED - InProcess check");

            HolderAssignInsertData? suggestionData = null;

            if (isFgi)
            {
                var penResult = await ResolveFgiPenNumAsync(
                    experimentId,
                    credentials.Username,
                    credentials.Password,
                    camVersion);

                if (!penResult.Success)
                {
                    return new ScanHolderJobResponse
                    {
                        Success = false,
                        CanAssign = false,
                        Holder = holder,
                        Message = penResult.QueryResult?.Message ??
                                  "Unable to query ExperimentDefinition.",
                        HolderJob = row,
                        RawQueryResult = penResult.QueryResult
                    };
                }

                suggestionData = new HolderAssignInsertData
                {
                    PartNum = partNumber.Trim(),
                    PenNum = penResult.PenNum,
                    ProductName = productName.Trim(),
                    CamVersion = camVersion,
                    Job = job,
                    Lec = BuildFgiLecOrNull(
                        config,
                        buildCode,
                        binName,
                        productName)
                };
            }
        #endregion

            // 5. If all validation checks pass, map the target for this holder.
            _logger.LogInformation("[SCAN VALIDATION] All validations passed, mapping target box...");
            var gridViewBoxMap = await MapGridViewBoxData(
                clientKey,
                suggestionData,
                token);

            if (!gridViewBoxMap.HasSuggestedTarget)
            {
                _logger.LogWarning(
                    "[SCAN VALIDATION] FAILED - No suggested target box. Message={Message}",
                    gridViewBoxMap.Message);
                return new ScanHolderJobResponse
                {
                    Success = false,
                    CanAssign = false,
                    Holder = holder,
                    CamVersion = camVersion,
                    Message = gridViewBoxMap.Message,
                    HolderJob = row,
                    RawQueryResult = holderJobResult,
                    GridViewBoxes = gridViewBoxMap.Boxes
                };
            }
            _logger.LogInformation(
                "[SCAN VALIDATION] SUCCESS - All validations passed for holder={Holder}",
                holder);
            return new ScanHolderJobResponse
            {
                Success = true,
                CanAssign = true,
                Holder = holder,
                CamVersion = camVersion,
                Message = "Validation Pass!",
                HolderJob = row,
                RawQueryResult = holderJobResult,
                GridViewBoxes = gridViewBoxMap.Boxes
            };

        }
        public async Task<AssignHolderResponse> AssignHolderAsync(AssignHolderRequest request, string token, string? clientKey)
        {
            var holder = request.Holder.Trim();
            var boxNo = request.BoxNo.Trim();
            var process = ResolveProcess(clientKey);
            var isFgi = string.Equals(process, "FGI", StringComparison.OrdinalIgnoreCase);
            var shipBoxName = request.ShipBoxName.Trim();

            if (isFgi && string.IsNullOrWhiteSpace(shipBoxName))
            {
                return new AssignHolderResponse
                {
                    Success = false,
                    Holder = holder,
                    BoxName = boxNo,
                    Message = "ShipBoxName is required for FGI assignment."
                };
            }

            if (!_credentialStore.TryGet(token, out var credentials))
            {
                return new AssignHolderResponse
                {
                    Success = false,
                    Holder = holder,
                    BoxName = boxNo,
                    Message = "Invalid or expired token."
                };
            }

            var holderJobFieldNames = new List<string>
            {
                "BuildCode",
                "BinName",
                "ProductName",
                "Job",
                "SliderCount",
                "HoldReason",
                "HoldComment"
            };

            if (isFgi)
            {
                holderJobFieldNames.AddRange(new[]
                {
                    "PartNumber",
                    "Experiment",
                    "QuantityGood",
                    "QuantityLoaded"
                });
            }

            FeatsQueryResponse holderJobResult;
            string? camVersion = WDC_STACKER.API.Models.CamVersion.IsValid(request.CamVersion)
                ? request.CamVersion!.Trim()
                : null;

            if (camVersion is not null)
            {
                holderJobResult = await ExecuteFeatsQueryAsync(
                    queryType: "HolderJob",
                    fieldNames: holderJobFieldNames.ToArray(),
                    filterName: "Holder",
                    filterValue: holder,
                    recordLimit: 250,
                    username: credentials.Username,
                    password: credentials.Password,
                    camVersion: camVersion);
            }
            else
            {
                (holderJobResult, camVersion) = await ExecuteFeatsQueryAcrossCamVersionsAsync(
                    queryType: "HolderJob",
                    fieldNames: holderJobFieldNames.ToArray(),
                    filterName: "Holder",
                    filterValue: holder,
                    recordLimit: 250,
                    username: credentials.Username,
                    password: credentials.Password);
            }

            if (!holderJobResult.Success)
            {
                return new AssignHolderResponse
                {
                    Success = false,
                    Holder = holder,
                    BoxName = boxNo,
                    Message = holderJobResult.Message,
                    RawQueryResult = holderJobResult
                };
            }

            var row = holderJobResult.ParsedResult.Rows.FirstOrDefault();

            if (row is null || camVersion is null)
            {
                return new AssignHolderResponse
                {
                    Success = false,
                    Holder = holder,
                    BoxName = boxNo,
                    Message = "HolderJob record was not found.",
                    RawQueryResult = holderJobResult
                };
            }

            var partNum = isFgi
                ? GetField(row, "PartNumber")
                : string.Empty;
            var experimentId = isFgi
                ? GetField(row, "Experiment")
                : string.Empty;
            string? penNum = null;
            var buildCode = GetField(row, "BuildCode");
            var binName = GetField(row, "BinName");
            var productName = GetField(row, "ProductName");
            var job = GetField(row, "Job");
            var sliderCount = GetField(row, "SliderCount");
            var holdReason = GetField(row, "HoldReason");
            var holdComment = GetField(row, "HoldComment");
            string? lecValue;
            int? holderQty = null;

            if (isFgi)
            {
                if (string.IsNullOrWhiteSpace(partNum) ||
                    string.IsNullOrWhiteSpace(productName))
                {
                    return new AssignHolderResponse
                    {
                        Success = false,
                        Holder = holder,
                        BoxName = boxNo,
                        Message = "PartNumber or ProductName is missing.",
                        RawQueryResult = holderJobResult
                    };
                }

                if (!TryGetValidatedHolderQty(row, out var validatedHolderQty))
                {
                    return new AssignHolderResponse
                    {
                        Success = false,
                        Holder = holder,
                        BoxName = boxNo,
                        Message = "Holder QTY is invalid",
                        RawQueryResult = holderJobResult
                    };
                }

                holderQty = validatedHolderQty;

                var penResult = await ResolveFgiPenNumAsync(
                    experimentId,
                    credentials.Username,
                    credentials.Password,
                    camVersion);

                if (!penResult.Success)
                {
                    return new AssignHolderResponse
                    {
                        Success = false,
                        Holder = holder,
                        BoxName = boxNo,
                        Message = penResult.QueryResult?.Message ??
                                  "Unable to query ExperimentDefinition.",
                        RawQueryResult = penResult.QueryResult
                    };
                }

                penNum = penResult.PenNum;
                var config = await _capacityConfigService.GetAsync(clientKey);
                lecValue = BuildFgiLecOrNull(
                    config,
                    buildCode,
                    binName,
                    productName);
            }
            else
            {
                if (int.TryParse(sliderCount, out var pwdQty))
                {
                    holderQty = pwdQty;
                }

                // Preserve the original PWD validation and LEC formula.
                if (string.IsNullOrWhiteSpace(buildCode) ||
                    string.IsNullOrWhiteSpace(productName))
                {
                    return new AssignHolderResponse
                    {
                        Success = false,
                        Holder = holder,
                        BoxName = boxNo,
                        Message = "BuildCode or ProductName is missing.",
                        RawQueryResult = holderJobResult
                    };
                }

                var config = await _capacityConfigService.GetAsync(clientKey);
                var firstPartKey = $"{buildCode[0]}{binName[^1]}";
                var firstPart = string.Empty;

                if (string.Equals(
                        firstPartKey,
                        nameof(config.SJ),
                        StringComparison.OrdinalIgnoreCase))
                {
                    firstPart = config.SJ;
                }
                else if (string.Equals(
                             firstPartKey,
                             nameof(config.SD),
                             StringComparison.OrdinalIgnoreCase))
                {
                    firstPart = config.SD;
                }
                else
                {
                    return new AssignHolderResponse
                    {
                        Success = false,
                        Holder = holder,
                        BoxName = boxNo,
                        Message = "BuildCode and BinName combination is not eligible.",
                        RawQueryResult = holderJobResult
                    };
                }

                var secondPart = productName[^1].ToString();
                var thirdPart = binName[..4];
                lecValue = firstPart + secondPart + thirdPart;
            }

            var boxExists = await _stackerSqlService.BoxNoExistsAsync(boxNo, process);

            var boxDetails = boxExists
                ? null
                : new BoxDetailsInsertData
                {
                    ClientCode = process,
                    BoxNo = boxNo,
                    RackNum = request.RackNum,
                    LayerRowNum = request.LayerRowNum,
                    LayerColNum = request.LayerColNum,
                    UpdateBy = credentials.Username,
                    UpdateTs = DateTime.Now
                };

            var holderAssign = new HolderAssignInsertData
            {
                Holder = holder,
                BoxName = boxNo,
                ShipBoxName = isFgi ? shipBoxName : string.Empty,
                Qty = holderQty,
                PartNum = isFgi
                    ? partNum.Trim()
                    : string.Empty,
                PenNum = isFgi
                    ? penNum
                    : null,
                ProductName = productName,
                Lec = lecValue,
                Factory = buildCode,
                Process = process,
                BinName = binName,
                CamVersion = isFgi ? camVersion : null,
                Job = job,
                Status = !isFgi &&
                         (!string.IsNullOrWhiteSpace(holdReason) || !string.IsNullOrWhiteSpace(holdComment))
                    ? "HOLD"
                    : null,
                UpdateBy = credentials.Username,
                UpdateTs = DateTime.Now
            };

            try
            {
                if (isFgi)
                {
                    var shipBoxExists =
                        await _stackerSqlService.ShipBoxNameExistsAsync(
                            boxNo,
                            shipBoxName);

                    var shipBoxDetails = shipBoxExists
                        ? null
                        : new ShipBoxDetailsInsertData
                        {
                            BoxNo = boxNo,
                            ShipBoxName = shipBoxName,
                            ShipBoxStatus = string.Empty,
                            ShipBoxNum = request.ShipBoxNum,
                            LayerRowNum = request.ShipBoxLayerRowNum,
                            LayerColNum = request.ShipBoxLayerColNum,
                            UpdateBy = credentials.Username,
                            UpdateTs = DateTime.Now
                        };

                    await _stackerSqlService.InsertFgiAssignmentAsync(boxDetails, shipBoxDetails, holderAssign);
                    _previewCache.Clear();
                }
                else
                {
                    await _stackerSqlService.InsertAssignmentAsync(boxDetails, holderAssign);
                }
            }
            catch (InvalidOperationException ex) when (isFgi)
            {
                return new AssignHolderResponse
                {
                    Success = false,
                    Holder = holder,
                    BoxName = boxNo,
                    Lec = lecValue ?? string.Empty,
                    Message = ex.Message,
                    RawQueryResult = holderJobResult
                };
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
                when (isFgi && ex.Number >= 51000 && ex.Number <= 51099)
            {
                return new AssignHolderResponse
                {
                    Success = false,
                    Holder = holder,
                    BoxName = boxNo,
                    Lec = lecValue ?? string.Empty,
                    Message = ex.Message,
                    RawQueryResult = holderJobResult
                };
            }
            catch
            {
                return new AssignHolderResponse
                {
                    Success = false,
                    Holder = holder,
                    BoxName = boxNo,
                    Lec = lecValue ?? string.Empty,
                    Message = "Unable to Assign.",
                    RawQueryResult = holderJobResult
                };
            }

            var gridViewBoxMap = await MapGridViewBoxData(
                clientKey,
                isFgi ? holderAssign : null,
                token);

            return new AssignHolderResponse
            {
                Success = true,
                Holder = holder,
                BoxName = boxNo,
                Lec = lecValue ?? string.Empty,
                BoxDetailsCreated = !boxExists,
                GridViewBoxes = gridViewBoxMap.Boxes,
                Message = isFgi
                    ? "Holder assigned to ShipBox successfully."
                    : boxExists
                        ? "Holder assigned successfully."
                        : "Box created and holder assigned successfully.",
                RawQueryResult = holderJobResult
            };
        }

        private static string GetField(Dictionary<string, string> row, string fieldName)
        {
            return row.TryGetValue(fieldName, out var value)
                ? value?.Trim() ?? string.Empty
                : string.Empty;
        }

        private static string? TryGetFieldValue(Dictionary<string, string> row, string fieldName)
        {
            return row.TryGetValue(fieldName, out var value)
                ? value?.Trim()
                : null;
        }

        private static bool TryGetValidatedHolderQty(Dictionary<string, string> row, out int qty)
        {
            qty = 0;

            if (!int.TryParse(
                    GetField(row, "QuantityGood"),
                    out var quantityGood) ||
                !int.TryParse(
                    GetField(row, "QuantityLoaded"),
                    out var quantityLoaded) ||
                !int.TryParse(
                    GetField(row, "SliderCount"),
                    out var sliderCount))
            {
                return false;
            }

            if (quantityGood != quantityLoaded ||
                quantityGood != sliderCount)
            {
                return false;
            }

            qty = quantityGood;
            return true;
        }

        private static bool TryExtractPenNum(string? experimentDescription, out string penNum)
        {
            penNum = string.Empty;

            if (string.IsNullOrWhiteSpace(experimentDescription))
                return false;

            const string marker = "PEN#";

            var markerIndex = experimentDescription.IndexOf(
                marker,
                StringComparison.OrdinalIgnoreCase);

            if (markerIndex < 0)
                return false;

            var valueStart = markerIndex + marker.Length;

            while (valueStart < experimentDescription.Length &&
                   char.IsWhiteSpace(experimentDescription[valueStart]))
            {
                valueStart++;
            }

            if (valueStart < experimentDescription.Length &&
                experimentDescription[valueStart] == ':')
            {
                valueStart++;

                while (valueStart < experimentDescription.Length &&
                       char.IsWhiteSpace(experimentDescription[valueStart]))
                {
                    valueStart++;
                }
            }

            var pipeIndex =
                experimentDescription.IndexOf('|', valueStart);

            if (pipeIndex < 0)
                return false;

            penNum = experimentDescription[valueStart..pipeIndex].Trim();

            return penNum.Length > 0;
        }

        private async Task<(
            bool Success,
            string? PenNum,
            FeatsQueryResponse? QueryResult)> ResolveFgiPenNumAsync(
                string experimentId,
                string username,
                string password,
                string camVersion)
        {
            if (string.IsNullOrWhiteSpace(experimentId))
                return (true, null, null);

            var result = await ExecuteFeatsQueryAsync(
                queryType: "ExperimentDefinition",
                fieldNames: new[] { "ExperimentDescription" },
                filterName: "ExperimentID",
                filterValue: experimentId,
                recordLimit: 1,
                username: username,
                password: password,
                camVersion: camVersion);

            if (!result.Success)
                return (false, null, result);

            var row = result.ParsedResult.Rows.FirstOrDefault();

            if (row is not null &&
                TryExtractPenNum(
                    GetField(row, "ExperimentDescription"),
                    out var penNum))
            {
                return (true, penNum, result);
            }

            // A missing experiment row, description, or PEN marker is a
            // valid null-PEN holder. Only a technical FEATS failure stops.
            return (true, null, result);
        }

        private static string? BuildFgiLecOrNull(
            CapacityConfig config,
            string buildCode,
            string binName,
            string productName)
        {
            if (string.IsNullOrWhiteSpace(buildCode) ||
                string.IsNullOrWhiteSpace(productName) ||
                binName.Length != 5)
            {
                return null;
            }

            var firstPartKey = $"{buildCode[0]}{binName[^1]}";

            var firstPart = string.Equals(
                firstPartKey,
                nameof(config.SJ),
                StringComparison.OrdinalIgnoreCase)
                ? config.SJ
                : string.Equals(
                    firstPartKey,
                    nameof(config.SD),
                    StringComparison.OrdinalIgnoreCase)
                    ? config.SD
                    : null;

            if (string.IsNullOrWhiteSpace(firstPart))
                return null;

            return firstPart.Trim()
                + productName[^1]
                + binName[..4];
        }

        private async Task<(bool Success, string Message, bool IsOnInSiteHold)> CheckHolderInSiteHoldAsync(string holder, string username, string password, string camVersion)
        {
            var holderJobResult = await ExecuteFeatsQueryAsync(
                queryType: "HolderJob",
                fieldNames:
                [
                    "Holder",
                    "HoldReason",
                    "HoldComment"
                ],
                filterName: "Holder",
                filterValue: holder,
                recordLimit: 250,
                username: username,
                password: password,
                camVersion: camVersion);

            if (!holderJobResult.Success)
            {
                return (
                    false,
                    holderJobResult.Message,
                    false
                );
            }

            var row =
                holderJobResult.ParsedResult.Rows.FirstOrDefault();

            if (row is null)
            {
                return (
                    false,
                    "HolderJob record was not found.",
                    false
                );
            }

            var holdReason = GetField(row, "HoldReason");
            var holdComment = GetField(row, "HoldComment");

            var isOnInSiteHold =
                !string.IsNullOrWhiteSpace(holdReason) ||
                !string.IsNullOrWhiteSpace(holdComment);

            return (
                true,
                string.Empty,
                isOnInSiteHold
            );
        }

        private async Task<(bool Success, bool IsOnInSiteHold)> GetCachedHolderInSiteHoldAsync(string holder, string username, string password, string camVersion)
        {
            var cacheKey = holder.Trim();
            var now = DateTime.UtcNow;

            if (InSiteHoldCache.TryGetValue(cacheKey, out var cached) &&
                cached.ExpiresAt > now)
            {
                return (true, cached.IsOnInSiteHold);
            }

            var result = await CheckHolderInSiteHoldAsync(
                holder,
                username,
                password,
                camVersion);

            if (result.Success)
            {
                InSiteHoldCache[cacheKey] = new InSiteHoldCacheEntry(
                    result.IsOnInSiteHold,
                    now.Add(InSiteHoldCacheDuration));
            }

            return (result.Success, result.IsOnInSiteHold);
        }

        /// <summary>
        /// Annotates each ShipBoxView in <paramref name="boxes"/> with in-site hold
        /// badge data (InSiteHoldHolders/InSiteHoldPositions) by checking every
        /// distinct holder currently assigned to those boxes against FEATS.
        /// No-op if the session token has no stored FEATS credentials.
        /// </summary>
        private async Task PopulateFgiInSiteHoldStatusAsync(
            IReadOnlyCollection<BoxView> boxes,
            string process,
            string token,
            string? boxNo = null)
        {
            if (!_credentialStore.TryGet(token, out var credentials))
                return;

            var locations = await _stackerSqlService.GetFgiHolderLocationsAsync(
                process,
                boxNo);

            if (locations.Count == 0)
                return;

            var camVersionByHolder = locations
                .Where(location => !string.IsNullOrWhiteSpace(location.Holder))
                .GroupBy(location => location.Holder, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(location => location.CamVersion).FirstOrDefault(cv => !string.IsNullOrWhiteSpace(cv)),
                    StringComparer.OrdinalIgnoreCase);

            var holders = camVersionByHolder.Keys.ToList();

            using var gate = new SemaphoreSlim(4);
            var checks = await Task.WhenAll(holders.Select(async holder =>
            {
                await gate.WaitAsync();

                try
                {
                    var holderCamVersion = camVersionByHolder.TryGetValue(holder, out var cv) && WDC_STACKER.API.Models.CamVersion.IsValid(cv)
                        ? cv!
                        : WDC_STACKER.API.Models.CamVersion.Cam3_4;

                    var check = await GetCachedHolderInSiteHoldAsync(
                        holder,
                        credentials.Username,
                        credentials.Password,
                        holderCamVersion);

                    return (Holder: holder, Check: check);
                }
                finally
                {
                    gate.Release();
                }
            }));

            var heldHolders = checks
                .Where(result =>
                    result.Check.Success &&
                    result.Check.IsOnInSiteHold)
                .Select(result => result.Holder)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var locationGroups = locations.GroupBy(location =>
                $"{location.BoxNo}\u0000{location.ShipBoxName}",
                StringComparer.OrdinalIgnoreCase);

            foreach (var locationGroup in locationGroups)
            {
                var firstLocation = locationGroup.First();
                var box = boxes.FirstOrDefault(candidate =>
                    string.Equals(
                        candidate.BoxNo,
                        firstLocation.BoxNo,
                        StringComparison.OrdinalIgnoreCase));
                var shipBox = box?.ShipBoxes.FirstOrDefault(candidate =>
                    string.Equals(
                        candidate.ShipBoxName,
                        firstLocation.ShipBoxName,
                        StringComparison.OrdinalIgnoreCase));

                if (shipBox is null)
                    continue;

                foreach (var indexedLocation in locationGroup.Select(
                             (location, index) => new { Location = location, Index = index }))
                {
                    if (!heldHolders.Contains(indexedLocation.Location.Holder))
                        continue;

                    if (!shipBox.InSiteHoldHolders.Contains(
                            indexedLocation.Location.Holder,
                            StringComparer.OrdinalIgnoreCase))
                    {
                        shipBox.InSiteHoldHolders.Add(
                            indexedLocation.Location.Holder);
                    }

                    shipBox.InSiteHoldPositions.Add(indexedLocation.Index);
                }
            }

            foreach (var shipBox in boxes.SelectMany(box => box.ShipBoxes))
            {
                shipBox.InSiteHoldHolders.Sort(StringComparer.OrdinalIgnoreCase);
                shipBox.InSiteHoldPositions.Sort();
            }
        }

        private async Task<FeatsQueryResponse> ExecuteFeatsQueryAsync(string queryType, string[] fieldNames, string filterName, string filterValue, int recordLimit, string username, string password, string camVersion)
        {
             var request = new FeatsQueryRequest
            {
                QueryType = queryType,
                FieldNames = fieldNames.ToList(),
                Filters = new List<FeatsQueryFilter>
                {
                    new FeatsQueryFilter
                    {
                        FilterName = filterName,
                        FilterValue = filterValue
                    }
                },
                RecordLimit = recordLimit
            };

            return await _featsService.QueryAsync(request, username, password, camVersion);
        }

        /// <summary>
        /// Fans out a "Holder"-filtered FEATS query to every enabled cam
        /// version concurrently and returns the first result that actually
        /// found a record, along with which cam version produced the hit.
        /// If both cam versions (incorrectly) return a match, cam3.4 wins
        /// deterministically and a warning is logged.
        /// </summary>
        private async Task<(FeatsQueryResponse Result, string? CamVersion)> ExecuteFeatsQueryAcrossCamVersionsAsync(
            string queryType,
            string[] fieldNames,
            string filterName,
            string filterValue,
            int recordLimit,
            string username,
            string password)
        {
            var enabledCamVersions = _featsService.GetEnabledCamVersions();

            if (enabledCamVersions.Count == 0)
            {
                return (
                    new FeatsQueryResponse
                    {
                        Success = false,
                        Message = "No FEATS cam versions are enabled.",
                        QueryType = queryType
                    },
                    null);
            }

            var resultsByCamVersion = new Dictionary<string, FeatsQueryResponse>();

            await Task.WhenAll(enabledCamVersions.Select(async camVersion =>
            {
                var result = await ExecuteFeatsQueryAsync(
                    queryType,
                    fieldNames,
                    filterName,
                    filterValue,
                    recordLimit,
                    username,
                    password,
                    camVersion);

                lock (resultsByCamVersion)
                {
                    resultsByCamVersion[camVersion] = result;
                }
            }));

            // Tie-break: cam3.4 wins if both cam versions return a match.
            var orderedCamVersions = enabledCamVersions
                .OrderBy(camVersion => string.Equals(camVersion, WDC_STACKER.API.Models.CamVersion.Cam3_4, StringComparison.OrdinalIgnoreCase) ? 0 : 1);

            var matches = orderedCamVersions
                .Where(camVersion =>
                    resultsByCamVersion.TryGetValue(camVersion, out var result) &&
                    result.Success &&
                    result.ParsedResult.Rows.Count > 0)
                .ToList();

            if (matches.Count > 1)
            {
                _logger.LogWarning(
                    "[CAM SCAN] Holder '{FilterValue}' matched in multiple cam versions ({CamVersions}). Prioritizing cam3.4.",
                    filterValue,
                    string.Join(", ", matches));
            }

            if (matches.Count > 0)
            {
                var chosenCamVersion = matches[0];
                return (resultsByCamVersion[chosenCamVersion], chosenCamVersion);
            }

            // No match found in any enabled cam version. Prefer returning a
            // technical failure (if any) over a plain "not found", so
            // callers see a real error message when FEATS itself failed.
            var failedResult = orderedCamVersions
                .Select(camVersion => resultsByCamVersion[camVersion])
                .FirstOrDefault(result => !result.Success);

            return (failedResult ?? resultsByCamVersion[orderedCamVersions.First()], null);
        }

        public Task<List<BoxAssignment>> GetBoxAssignmentsAsync(string boxName, string? clientKey)
        {
            var process = ResolveProcess(clientKey);
            return _stackerSqlService.GetBoxAssignmentsAsync(boxName, process);
        }

        public async Task<List<ShipBoxView>> GetShipBoxesAsync(
            string boxNo,
            bool suggest,
            string? clientKey,
            string? lec = null,
            bool hasLecContext = false,
            string? token = null)
        {
            var config = await _capacityConfigService.GetAsync(clientKey);
            var process = ResolveProcess(clientKey);

            if (!string.Equals(process, "FGI", StringComparison.OrdinalIgnoreCase))
                return new List<ShipBoxView>();

            var shipBoxes = await _stackerSqlService.GetFgiShipBoxesByBoxNoAsync(
                boxNo,
                config.MAX_ITEM_PER_BOX_SHIPBOX,
                process);

            if (!string.IsNullOrWhiteSpace(token))
            {
                await PopulateFgiInSiteHoldStatusAsync(
                    new List<BoxView>
                    {
                        new()
                        {
                            BoxNo = boxNo,
                            ShipBoxes = shipBoxes
                        }
                    },
                    process,
                    token,
                    boxNo);
            }

            if (!suggest || !hasLecContext)
                return shipBoxes;

            var box = new BoxView
            {
                BoxNo = boxNo,
                ShipBoxes = shipBoxes
            };

            TryEnsureSuggestedFgiShipBox(
                box,
                config,
                lec);

            return box.ShipBoxes;
        }

        public Task<List<FgiWithdrawalRequestView>> GetFgiWithdrawalRequestsAsync()
        {
            return _stackerSqlService.GetFgiWithdrawalRequestsAsync();
        }

        public async Task<( bool Success, string Message,  FgiWithdrawalDisassociationPreviewView? Preview)> GetFgiWithdrawalDisassociationPreviewAsync(string? lec, string? penNum, int total, string? partNum, string? grade, int actualOutput, string token, string? clientKey)
        {
            if (!_credentialStore.TryGet(
                    token,
                    out var credentials))
            {
                return (
                    false,
                    "Invalid or expired token.",
                    null
                );
            }

            var cacheKey = $"{lec}|{penNum ?? "null"}|{total}|{partNum ?? "null"}|{grade ?? "null"}|{actualOutput}".ToUpperInvariant();

            if (_previewCache.TryGetValue(cacheKey, out var cachedEntry))
            {
                if (DateTime.UtcNow - cachedEntry.CachedAt < _cacheExpiration)
                {
                    _logger.LogInformation("Preview cache hit for key={CacheKey}", cacheKey);
                    return (true, "Disassociation preview loaded from cache.", cachedEntry.Preview);
                }
                else
                {
                    _previewCache.TryRemove(cacheKey, out _);
                }
            }

            var preview =
                await _stackerSqlService
                    .GetFgiWithdrawalDisassociationPreviewAsync(
                        lec,
                        penNum,
                        total,
                        partNum,
                        grade,
                        actualOutput);

            var config = await _capacityConfigService.GetAsync(clientKey);
            var holdValidationOperations = config.HoldValidationOperations;

            //-- CHECK HOLD (FEATS + AHS) WITH FIFO BACKFILL: START ----------------------\\
            var runningTotal = 0L;

            foreach (var record in preview.SourceRecords)
            {
                var qualifiesByQty =
                    runningTotal < total &&
                    record.Qty <= preview.MaximumTotalQty - runningTotal;

                if (!qualifiesByQty)
                {
                    record.IsIncluded = false;
                    record.WasReviewedForHold = false;
                    record.RunningTotal = runningTotal;
                    continue;
                }

                record.WasReviewedForHold = true;

                var recordCamVersion = await _stackerSqlService.GetHolderCamVersionAsync(record.Holder, "FGI");
                var resolvedRecordCamVersion = WDC_STACKER.API.Models.CamVersion.IsValid(recordCamVersion)
                    ? recordCamVersion!
                    : WDC_STACKER.API.Models.CamVersion.Cam3_4;

                var holdCheck =
                    await CheckHolderHoldsAsync(
                        record.Holder,
                        credentials.Username,
                        credentials.Password,
                        holdValidationOperations,
                        resolvedRecordCamVersion);

                if (!holdCheck.Success)
                {
                    return (
                        false,
                        $"Unable to check holds for " +
                        $"{record.Holder}: {holdCheck.Message}",
                        null
                    );
                }

                if (holdCheck.IsOnHold)
                {
                    record.IsIncluded = false;
                    record.Status = holdCheck.HoldSource;
                    record.RunningTotal = runningTotal;
                }
                else
                {
                    record.IsIncluded = true;
                    record.Status = "HOLD PASS";
                    runningTotal += record.Qty;
                    record.RunningTotal = runningTotal;
                }
            }

            preview.TotalQty = runningTotal;
            //-- CHECK HOLD (FEATS + AHS) WITH FIFO BACKFILL: END ------------------------//

            _previewCache.TryAdd(cacheKey, (preview, DateTime.UtcNow));

            return (
                true,
                "Disassociation preview loaded successfully.",
                preview
            );
        }

        /// <summary>
        /// Combined "Check Hold" step: verifies a holder is not on FEATS
        /// InSite hold and is not on AHS hold for any of the configured
        /// <see cref="CapacityConfig.HoldValidationOperations"/>.
        /// </summary>
        private async Task<(bool Success, string Message, bool IsOnHold, string HoldSource)> CheckHolderHoldsAsync(string holder, string username, string password, IReadOnlyList<string> holdValidationOperations, string camVersion)
        {
            var cacheKey = holder.ToUpperInvariant();

            if (_holdCheckCache.TryGetValue(cacheKey, out var cachedResult))
            {
                return (true, string.Empty, cachedResult.IsOnHold, cachedResult.HoldSource);
            }

            //-- FEATS IN-SITE HOLD CHECK: START (comment out to disable) ------------------\\
            var inSiteHoldCheck =
                await CheckHolderInSiteHoldAsync(
                    holder,
                    username,
                    password,
                    camVersion);

            if (!inSiteHoldCheck.Success)
            {
                return (false, inSiteHoldCheck.Message, false, string.Empty);
            }

            if (inSiteHoldCheck.IsOnInSiteHold)
            {
                var result = (true, string.Empty, true, "IN-SITE HOLD");
                _holdCheckCache.TryAdd(cacheKey, (true, "IN-SITE HOLD"));
                return result;
            }
            //-- FEATS IN-SITE HOLD CHECK: END --------------------------------------------//

            var noHoldResult = (true, string.Empty, false, string.Empty);
            _holdCheckCache.TryAdd(cacheKey, (false, string.Empty));
            return noHoldResult;
        }

        /// <summary>
        /// "Verify ShipBox" step of the Job Withdrawal flow, run right after
        /// "Enter Shipping Box Allocation". Validates that the entered
        /// Shipping Id refers to an existing, available (empty) ShipBox
        /// holder in FEATS before continuing; if invalid, the UI should
        /// return the user to the Shipping Id input.
        ///
        /// Logic:
        /// 1. Query(Holder) for shippingId — must return a record and
        ///    HOLDERTYPE must equal "SHPBOX" (confirms the ShipBox exists).
        /// 2. Query(HolderJob) for shippingId — must return a record with a
        ///    ChildJobCount value (confirms the ShipBox is empty/available).
        /// </summary>
        private async Task<(bool Success, string Message, string? CamVersion)> VerifyShipBoxAsync(string shippingId, string username, string password)
        {
            if (string.IsNullOrWhiteSpace(shippingId))
            {
                return (false, "ShippingId is required.", null);
            }

            var (holderResult, resolvedCamVersion) = await ExecuteFeatsQueryAcrossCamVersionsAsync(
                queryType: "Holder",
                fieldNames: ["Holder", "HolderType"],
                filterName: "Holder",
                filterValue: shippingId,
                recordLimit: 1,
                username: username,
                password: password);

            if (!holderResult.Success)
            {
                return (false, holderResult.Message, null);
            }

            var holderRow = holderResult.ParsedResult.Rows.FirstOrDefault();

            if (holderRow is null || resolvedCamVersion is null)
            {
                return (false, $"ShippingId '{shippingId}' was not found in FEATS.", null);
            }

            var holderType = GetField(holderRow, "HolderType");

            if (!string.Equals(holderType, "SHPBOX", StringComparison.OrdinalIgnoreCase))
            {
                return (false, $"ShippingId '{shippingId}' is not a ShipBox (HolderType={holderType}).", null);
            }

            var holderJobResult = await ExecuteFeatsQueryAsync(
                queryType: "HolderJob",
                fieldNames: ["Holder", "ChildJobCount"],
                filterName: "Holder",
                filterValue: shippingId,
                recordLimit: 1,
                username: username,
                password: password,
                camVersion: resolvedCamVersion);

            if (!holderJobResult.Success)
            {
                return (false, holderJobResult.Message, null);
            }

            var holderJobRow = holderJobResult.ParsedResult.Rows.FirstOrDefault();

            // No record = shipbox is empty (pass)
            if (holderJobRow is null)
            {
                return (true, "ShipBox verified (empty).", resolvedCamVersion);
            }

            var childJobCount = GetField(holderJobRow, "ChildJobCount");

            // ChildJobCount is null/empty/0 = shipbox is empty (pass)
            if (string.IsNullOrWhiteSpace(childJobCount) || childJobCount == "0")
            {
                return (true, "ShipBox verified (empty).", resolvedCamVersion);
            }

            // ChildJobCount > 0 = shipbox has child holders (fail)
            return (false, $"ShippingId '{shippingId}' is not empty (has {childJobCount} child holders).", null);
        }

        /// <summary>
        /// "AddJob()" FEATS transaction of the Job Withdrawal flow: groups
        /// the verified withdrawal holders under the entered Shipping Id.
        /// </summary>
        private async Task<(bool Success, string Message)> AddJobForWithdrawalAsync(string shippingId, IReadOnlyList<string> holders, string token, string camVersion)
        {
            if (!_credentialStore.TryGet(token, out var credentials))
            {
                return (false, "Invalid or expired token.");
            }

            var newHolders = holders
                .Select((holder, index) => new child_holder_info
                {
                    Position = index + 1,
                    Name = holder,
                    Type = "MATTRA"
                })
                .ToArray();

            return await _featsService.AddJobAsync(
                holder: shippingId,
                holderType: "SHPBOX",
                newHolders: newHolders,
                allowMixingJobAttributes: true,
                username: credentials.Username,
                password: credentials.Password,
                camVersion: camVersion);
        }

        /// <summary>
        /// Public entry point so the client can verify a Shipping Id (ShipBox)
        /// against FEATS before the operator proceeds to scan/verify Holders.
        /// Wraps <see cref="VerifyShipBoxAsync"/>. The resolved CamVersion is
        /// returned so the client can carry it through the rest of the
        /// withdrawal flow (scanning holders, checkhold, addjob, moveout).
        /// </summary>
        public async Task<(bool Success, string Message, string? CamVersion)> VerifyFgiWithdrawalShipBoxAsync(string shippingId, string token)
        {
            if (!_credentialStore.TryGet(token, out var credentials))
            {
                return (false, "Invalid or expired token.", null);
            }

            return await VerifyShipBoxAsync(shippingId, credentials.Username, credentials.Password);
        }

        public async Task<FgiWithdrawalDisassociationResult> DisassociateFgiWithdrawalRequestAsync( long requestId, IReadOnlyCollection<string> includedHolders, string shippingId, string token)
        {
            if (!_credentialStore.TryGet(token, out var credentials))
            {
                return new FgiWithdrawalDisassociationResult
                {
                    Success = false,
                    Message = "Invalid or expired token."
                };
            }

            if (string.IsNullOrWhiteSpace(shippingId))
            {
                return new FgiWithdrawalDisassociationResult
                {
                    Success = false,
                    Message = "ShippingId is required."
                };
            }

            // Clear preview cache since data will change after successful disassociation
            _previewCache.Clear();

            // Get cam version from the first holder for withdrawal feats
            var firstHolder = includedHolders.FirstOrDefault();
            if (firstHolder == null)
            {
                return new FgiWithdrawalDisassociationResult
                {
                    Success = false,
                    Message = "No holders in withdrawal batch."
                };
            }

            var withdrawalCamVersion = await _stackerSqlService.GetHolderCamVersionAsync(firstHolder, "FGI");
            if (string.IsNullOrWhiteSpace(withdrawalCamVersion))
            {
                return new FgiWithdrawalDisassociationResult
                {
                    Success = false,
                    Message = $"Unable to determine cam version for holder '{firstHolder}'."
                };
            }

            // Defensive check: every holder in this withdrawal batch must
            // share the same cam version (batching should
            // already guarantee this via IsCompatibleFgiBox uniformity).
            foreach (var includedHolder in includedHolders)
            {
                var includedHolderCamVersion = await _stackerSqlService.GetHolderCamVersionAsync(includedHolder, "FGI");

                if (!string.Equals(includedHolderCamVersion?.Trim(), withdrawalCamVersion, StringComparison.OrdinalIgnoreCase))
                {
                    return new FgiWithdrawalDisassociationResult
                    {
                        Success = false,
                        Message = $"Holder '{includedHolder}' cam version ('{includedHolderCamVersion ?? "unknown"}') does not match first holder cam version ('{withdrawalCamVersion}')."
                    };
                }
            }
   
   #region FEATS for Withdrawal
 
        //FORK HERE DEPENDEING ON THE CAMVERSION
        //CAM3: ATTACHJOB(SHIPPINGID, 'SHPBOX'), MOVEOUT(SHIPPINGID), ADDJOB(), SETSHIP(SHIPPINGID, 'SHPBOX', 'ShipToSite'), TRANSFERHOLDERJOB(), SHIP1(SHIPPINGID, 'SHPBOX')
        //CAM7: ADDJOB(), MOVEOUT(SHIPPINGID)
 
        var isCam3 = string.Equals(withdrawalCamVersion, WDC_STACKER.API.Models.CamVersion.Cam3_4, StringComparison.OrdinalIgnoreCase);
 
        if (isCam3)
        {
            // Query HolderJob for the first holder to get job attributes for AttachJob
            if (firstHolder == null)
            {
                return new FgiWithdrawalDisassociationResult
                {
                    Success = false,
                    Message = "No holders in withdrawal batch for CAM3 AttachJob."
                };
            }
 
            var holderJobQueryResult = await ExecuteFeatsQueryAsync(
                queryType: "HolderJob",
                fieldNames: new[] { "ISCDSEM", "CDSEMSAMPLEPLAN", "WAFERNUM", "ROUTING", "SEGMENT", "CLASSNAME", "PRODUCTNAME", "MINORREV", "FACTORY", "BUILDCODE", "EXPERIMENT", "PTSJOBNUM" },
                filterName: "Holder",
                filterValue: firstHolder,
                recordLimit: 1,
                username: credentials.Username,
                password: credentials.Password,
                camVersion: withdrawalCamVersion);
 
            if (!holderJobQueryResult.Success || holderJobQueryResult.ParsedResult.Rows.Count == 0)
            {
                return new FgiWithdrawalDisassociationResult
                {
                    Success = false,
                    Message = $"Failed to query HolderJob for first holder '{firstHolder}': {holderJobQueryResult.Message}"
                };
            }
 
            var jobAttributesRow = holderJobQueryResult.ParsedResult.Rows[0];
 
            // CAM3 FLOW: ATTACHJOB(SHIPPINGID, 'SHPBOX')
            var attachJobResult = await _featsService.AttachJobAsync(
                holder: shippingId,
                holderType: "SHPBOX",
                holderGeometry: "1X24",
                startReason: "G5START",
                owner: "MFG",
                isCdSem: TryGetFieldValue(jobAttributesRow, "ISCDSEM"),
                cdSemSamplePlan: TryGetFieldValue(jobAttributesRow, "CDSEMSAMPLEPLAN"),
                waferNum: TryGetFieldValue(jobAttributesRow, "WAFERNUM"),
                routing: TryGetFieldValue(jobAttributesRow, "ROUTING"),
                segment: TryGetFieldValue(jobAttributesRow, "SEGMENT"),
                classCode: TryGetFieldValue(jobAttributesRow, "CLASSNAME"),
                productName: TryGetFieldValue(jobAttributesRow, "PRODUCTNAME"),
                minorRev: TryGetFieldValue(jobAttributesRow, "MINORREV"),
                factory: TryGetFieldValue(jobAttributesRow, "FACTORY"),
                buildCode: TryGetFieldValue(jobAttributesRow, "BUILDCODE"),
                experiment: TryGetFieldValue(jobAttributesRow, "EXPERIMENT"),
                ptsJobNum: TryGetFieldValue(jobAttributesRow, "PTSJOBNUM"),
                username: credentials.Username,
                password: credentials.Password);
 
            if (!attachJobResult.Success)
            {
                return new FgiWithdrawalDisassociationResult
                {
                    Success = false,
                    Message = $"CAM3 AttachJob failed: {attachJobResult.Message}"
                };
            }
 
            // CAM3 FLOW: SUPERMOVE(SHIPPINGID)
            var moveOutShippingBoxResult = await _featsService.SuperMoveAsync(
                holder: shippingId,
                holderType: null,
                operation: "735630 FGI",
                username: credentials.Username,
                password: credentials.Password,
                camVersion: withdrawalCamVersion);
 
            if (!moveOutShippingBoxResult.Success)
            {
                return new FgiWithdrawalDisassociationResult
                {
                    Success = false,
                    Message = $"CAM3 SuperMove (shipping box) failed: {moveOutShippingBoxResult.Message}"
                };
            }

            // CAM3 FLOW: MOVEIN(SHIPPINGID)
            var moveInShippingBoxResult = await _featsService.MoveInAsync(
                holder: shippingId,
                holderType: null,
                resource: null,
                username: credentials.Username,
                password: credentials.Password,
                camVersion: withdrawalCamVersion);

            if (!moveInShippingBoxResult.Success)
            {
                return new FgiWithdrawalDisassociationResult
                {
                    Success = false,
                    Message = $"CAM3 MoveIn (shipping box) failed: {moveInShippingBoxResult.Message}"
                };
            }
        }
 
        // COMMON: ADDJOB() (both CAM3 and CAM7)
        var addJobResult = await AddJobForWithdrawalAsync(
            shippingId,
            includedHolders.ToList(),
            token,
            withdrawalCamVersion);
 
        if (!addJobResult.Success)
        {
            return new FgiWithdrawalDisassociationResult
            {
                Success = false,
                Message = addJobResult.Message
            };
        }
 
        if (isCam3)
        {
            // CAM3 FLOW: SETSHIP(SHIPPINGID, 'SHPBOX', 'ShipToSite')
            var setShipResult = await _featsService.SetShipmentDestinationAsync(
                holder: shippingId,
                holderType: "SHPBOX",
                shipmentDestination: "ShipToSite",
                username: credentials.Username,
                password: credentials.Password,
                camVersion: withdrawalCamVersion);
 
            if (!setShipResult.Success)
            {
                return new FgiWithdrawalDisassociationResult
                {
                    Success = false,
                    Message = $"CAM3 SetShipmentDestination failed: {setShipResult.Message}"
                };
            }
 
            // CAM3 FLOW: TRANSFERHOLDERJOB() - loop through individual holders
            foreach (var holder in includedHolders)
            {
                var transferJobResult = await _featsService.TransferHolderJobAsync(
                    srcHolder: holder,
                    srcHolderType: "MATTRA",
                    dstHolder: $"{holder}-1",
                    dstHolderType: "MATTRADMY",
                    transposeFormula: null,
                    newHolderGeometry: null,
                    username: credentials.Username,
                    password: credentials.Password,
                    camVersion: withdrawalCamVersion);
 
                if (!transferJobResult.Success)
                {
                    return new FgiWithdrawalDisassociationResult
                    {
                        Success = false,
                        Message = $"CAM3 TransferHolderJob failed for holder '{holder}': {transferJobResult.Message}"
                    };
                }
            }
 
            // CAM3 FLOW: SHIP1(SHIPPINGID, 'SHPBOX') testing
            var shipResult = await _featsService.Ship1Async(
                holder: shippingId,
                holderType: "SHPBOX",
                username: credentials.Username,
                password: credentials.Password,
                camVersion: withdrawalCamVersion);
 
            if (!shipResult.Success)
            {
                return new FgiWithdrawalDisassociationResult
                {
                    Success = false,
                    Message = $"CAM3 Ship1 failed: {shipResult.Message}"
                };
            }
        }
        else
        {
            // CAM7 FLOW: MOVEOUT(SHIPPINGID) TESTING
            var moveOutResult = await _featsService.MoveOutAsync(
                holder: shippingId,
                holderType: null,
                resource: null,
                nextOp: "735617 RBF 2",
                username: credentials.Username,
                password: credentials.Password,
                camVersion: withdrawalCamVersion);
 
            if (!moveOutResult.Success)
            {
                return new FgiWithdrawalDisassociationResult
                {
                    Success = false,
                    Message = $"CAM7 MoveOut failed: {moveOutResult.Message}"
                };
            }
        }
    #endregion

            var result = await _stackerSqlService
                .DisassociateFgiWithdrawalAsync(
                    requestId,
                    includedHolders);

            if (!result.Success)
            {
                return result;
            }

            // Email notification (Partial/Completed/Closed) is sent from
            // StackerSqlService.DisassociateFgiWithdrawalAsync() based on the
            // computed status change. See EmailService/IEmailService.

            return result;
        }

        public async Task<(bool Success, string Message, string AcknowledgeBy)> AcknowledgeFgiWithdrawalRequestAsync(long requestId, string token)
        {
            if (!_credentialStore.TryGet(token, out var credentials))
            {
                return (
                    false,
                    "Invalid or expired token.",
                    string.Empty
                );
            }

            var userId = NormalizeUserId(credentials.Username);

            var updated =
                await _stackerSqlService.AcknowledgeFgiWithdrawalRequestAsync(
                    requestId,
                    userId);

            if (!updated)
            {
                return (
                    false,
                    "The request was not found or was already acknowledged.",
                    string.Empty
                );
            }

            return (
                true,
                "Withdrawal request acknowledged successfully.",
                userId
            );
        }

        public async Task<FgiWithdrawalRackView?> GetFgiWithdrawalLayoutAsync(
            string? lec,
            string? penNum,
            string? partNum,
            string? grade,
            string? clientKey,
            string? token = null)
        {
            var process = ResolveProcess(clientKey);
            var layout = await _stackerSqlService.GetFgiWithdrawalLayoutAsync(lec, penNum, partNum, grade, process);

            if (layout is null ||
                string.IsNullOrWhiteSpace(token) ||
                !_credentialStore.TryGet(token, out var credentials))
            {
                return layout;
            }

            var holders = layout.Boxes
                .SelectMany(box => box.ShipBoxes)
                .SelectMany(shipBox => shipBox.Holders)
                .Where(holder => !string.IsNullOrWhiteSpace(holder.Holder))
                .GroupBy(
                    holder => holder.Holder,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

            using var gate = new SemaphoreSlim(4);
            await Task.WhenAll(holders.Select(async holderGroup =>
            {
                await gate.WaitAsync();

                try
                {
                    var holderCamVersion = await _stackerSqlService.GetHolderCamVersionAsync(holderGroup.Key, process);
                    var resolvedHolderCamVersion = WDC_STACKER.API.Models.CamVersion.IsValid(holderCamVersion)
                        ? holderCamVersion!
                        : WDC_STACKER.API.Models.CamVersion.Cam3_4;

                    var check = await GetCachedHolderInSiteHoldAsync(
                        holderGroup.Key,
                        credentials.Username,
                        credentials.Password,
                        resolvedHolderCamVersion);

                    if (!check.Success || !check.IsOnInSiteHold)
                        return;

                    foreach (var holder in holderGroup)
                        holder.IsInSiteHold = true;
                }
                finally
                {
                    gate.Release();
                }
            }));

            return layout;
        }

        public Task<List<BoxAssignment>> GetShipBoxAssignmentsAsync(string boxName, string shipBoxName, string? clientKey)
        {
            var process = ResolveProcess(clientKey);
            return _stackerSqlService.GetShipBoxAssignmentsAsync(boxName, shipBoxName, process);
        }

        public async Task<(bool Success, string Message, List<BoxView> Boxes)> DisassociateHolderAsync(string holder, string token, string? clientKey)
        {
            if (!_credentialStore.TryGet(token, out var credentials))
            {
                return (
                    false,
                    "Invalid or expired token.",
                    new List<BoxView>()
                );
            }

            var process = ResolveProcess(clientKey);

            //-- RESOLVE STORED CAMVERSION: START ----------------------------------------\
            var storedCamVersion = await _stackerSqlService.GetHolderCamVersionAsync(holder, process);
            var camVersion = WDC_STACKER.API.Models.CamVersion.IsValid(storedCamVersion)
                ? storedCamVersion!
                : WDC_STACKER.API.Models.CamVersion.Cam3_4;
            //-- RESOLVE STORED CAMVERSION: END -------------------------------------------//

            //-- INSIGHT HOLD CHECK :START -----------------------------------------------\
            var holdCheck = await CheckHolderInSiteHoldAsync(holder, credentials.Username, credentials.Password, camVersion);

            if (!holdCheck.Success)
            {
                return (
                    false,
                    holdCheck.Message,
                    new List<BoxView>()
                );
            }

            if (holdCheck.IsOnInSiteHold)
            {
                return (
                    false,
                    $"{holder} is currently on InSite hold.",
                    new List<BoxView>()
                );
            }
            //-- INSIGHT HOLD CHECK :END -----------------------------------------------//

            //-- MOVE-OUT TRANSACTION: START---------------------------------------------\
            var moveOutResult = await _featsService.MoveOutAsync(
                holder: holder,
                holderType: null,
                resource: null,
                nextOp: null,
                username: credentials.Username,
                password: credentials.Password,
                camVersion: camVersion
            );
            if (!moveOutResult.Success)
            {
                return (
                    false,
                    $"The SQL assignment was deleted, but {moveOutResult.Message}",
                    new List<BoxView>()
                );
            }
            //-- MOVE-OUT TRANSACTION: END-----------------------------------------------//

            //-- SQL DELETE for HOLDER_ASSIGN: START------------------------------------\
            var deleted = await _stackerSqlService.DisassociateHolderAsync(holder, process);
            if (!deleted)
            {
                return (
                    false,
                    "The holder was not found or its status is not RELEASE.",
                    new List<BoxView>()
                );
            }

            _previewCache.Clear();
            //-- SQL DELETE for HOLDER_ASSIGN: END --------------------------------------//

            var gridView = await MapGridViewBoxData(clientKey);

            return (
                true,
                "Holder disassociated successfully.",
                gridView.Boxes
            );
        }

        
        public async Task<(bool Success, string Message, List<BoxView> Boxes)> DisassociateFgiHolderAsync(string holder, string token, string? clientKey)
        {
            if (!_credentialStore.TryGet(token, out var credentials))
            {
                return (
                    false,
                    "Invalid or expired token.",
                    new List<BoxView>()
                );
            }

            var process = ResolveProcess(clientKey);
            var rbf2Operation = _config["Stacker:FgiRbf2Operation"] ?? DefaultFgiRbf2Operation;

            var storedCamVersion = await _stackerSqlService.GetHolderCamVersionAsync(holder, process);
            var camVersion = WDC_STACKER.API.Models.CamVersion.IsValid(storedCamVersion)
                ? storedCamVersion!
                : WDC_STACKER.API.Models.CamVersion.Cam3_4;

            //-- STEP 1: QUERY HOLDER HOLD INFO: START -----------------------------------\\
            FeatsQueryResponse holderJobResult;
            try
            {
                holderJobResult = await ExecuteFeatsQueryAsync(
                    queryType: "HolderJob",
                    fieldNames: new[] { "Holder", "HoldReason", "HoldComment" },
                    filterName: "Holder",
                    filterValue: holder,
                    recordLimit: 250,
                    username: credentials.Username,
                    password: credentials.Password,
                    camVersion: camVersion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FGI DISASSOCIATE] HolderJob query threw for holder={Holder}", holder);
                return (
                    false,
                    $"Failed to query holder: {ex.Message}",
                    new List<BoxView>()
                );
            }

            if (!holderJobResult.Success)
            {
                return (
                    false,
                    holderJobResult.Message,
                    new List<BoxView>()
                );
            }

            var row = holderJobResult.ParsedResult.Rows.FirstOrDefault();

            if (row is null)
            {
                return (
                    false,
                    "HolderJob record was not found.",
                    new List<BoxView>()
                );
            }

            var holdReasonCode = GetField(row, "HoldReason");
            var holdComment = GetField(row, "HoldComment");
            var hasHold = !string.IsNullOrWhiteSpace(holdReasonCode) || !string.IsNullOrWhiteSpace(holdComment);
            //-- STEP 1: QUERY HOLDER HOLD INFO: END -------------------------------------//

            if (!hasHold)
            {
                //-- NO ACTIVE HOLD FOUND: clear STATUS instead of deleting the row --------\\
                var statusCleared = await _stackerSqlService.ClearFgiHolderAssignmentStatusAsync(holder, process);
                if (!statusCleared)
                {
                    _logger.LogWarning(
                        "[FGI DISASSOCIATE] Unable to clear HOLDER_ASSIGN status for holder={Holder}, process={Process}",
                        holder,
                        process);
                }

                _previewCache.Clear();

                var noHoldGridView = await MapGridViewBoxData(clientKey);

                return (
                    true,
                    $"{holder} has no active FEATS hold. Status cleared instead of disassociating.",
                    noHoldGridView.Boxes
                );
            }

            //-- STEP 2: RELEASE HOLDER: START --------------------------------------------\\
            var releaseResult = await ReleaseHolderWithRetryAsync(
                holder: holder,
                holderType: null,
                comment: string.Empty,
                username: credentials.Username,
                password: credentials.Password,
                camVersion: camVersion);

            if (!releaseResult.Success)
            {
                _logger.LogWarning(
                    "[FGI DISASSOCIATE] Release failed, proceeding to MoveOut anyway: {Message}",
                    releaseResult.Message);
            }
            //-- STEP 2: RELEASE HOLDER: END -----------------------------------------------//

            //-- STEP 3: MOVE TO RBF2 OPERATION: START -------------------------------------\\
            (bool Success, string Message) moveOutResult;
            try
            {
                moveOutResult = await _featsService.MoveOutAsync(
                    holder: holder,
                    holderType: null,
                    resource: null,
                    nextOp: rbf2Operation,
                    username: credentials.Username,
                    password: credentials.Password,
                    camVersion: camVersion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FGI DISASSOCIATE] MoveOut to {Rbf2Operation} threw for holder={Holder}", rbf2Operation, holder);
                moveOutResult = (false, $"MoveOut threw an exception: {ex.Message}");
            }

            var moveOutMessage = moveOutResult.Success
                ? string.Empty
                : moveOutResult.Message;
            //-- STEP 3: MOVE TO RBF2 OPERATION: END ---------------------------------------//

            //-- STEP 4: RE-APPLY SAVED HOLD: START ----------------------------------------\\
            // If MoveOut failed above, we still attempt to re-apply the hold here as a
            // safety net so the holder is never left un-held after being released in step 2.
            (bool Success, string Message) holdResult;
            try
            {
                holdResult = await _featsService.HoldHolderAsync(
                    holder: holder,
                    holderType: null,
                    holdReasonCode: holdReasonCode,
                    comment: holdComment,
                    username: credentials.Username,
                    password: credentials.Password,
                    camVersion: camVersion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FGI DISASSOCIATE] HoldHolder re-apply threw for holder={Holder}", holder);
                holdResult = (false, $"HoldHolder threw an exception: {ex.Message}");
            }

            if (!holdResult.Success)
            {
                var partialMessage = moveOutResult.Success
                    ? $"Holder was moved to {rbf2Operation} but NOT re-held: {holdResult.Message}"
                    : $"MoveOut failed ({moveOutMessage}) and the holder was NOT re-held: {holdResult.Message}";

                return (
                    false,
                    partialMessage,
                    new List<BoxView>()
                );
            }

            if (!moveOutResult.Success)
            {
                return (
                    false,
                    $"MoveOut to {rbf2Operation} failed: {moveOutMessage}. Hold was re-applied at the current location.",
                    new List<BoxView>()
                );
            }
            //-- STEP 4: RE-APPLY SAVED HOLD: END ------------------------------------------//

            //-- SQL DELETE for HOLDER_ASSIGN: START----------------------------------------\\
            var deleted = await _stackerSqlService.DeleteFgiHoldHolderAssignmentAsync(holder, process);
            if (!deleted)
            {
                _logger.LogWarning(
                    "[FGI DISASSOCIATE] Unable to delete HOLDER_ASSIGN row for holder={Holder}, process={Process}",
                    holder,
                    process);
            }

            _previewCache.Clear();
            //-- SQL DELETE for HOLDER_ASSIGN: END ------------------------------------------//

            var gridViewResult = await MapGridViewBoxData(clientKey);

            return (
                true,
                "Holder released, moved to RBF2, and re-held successfully.",
                gridViewResult.Boxes
            );
        }

        private async Task<(bool Success, string Message)> ReleaseHolderWithRetryAsync(
            string holder,
            string? holderType,
            string comment,
            string username,
            string password,
            string camVersion,
            int maxAttempts = 3)
        {
            (bool Success, string Message) result = (false, "Release was not attempted.");

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    result = await _featsService.ReleaseHolderAsync(
                        holder: holder,
                        holderType: holderType,
                        comment: comment,
                        username: username,
                        password: password,
                        camVersion: camVersion);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "[RELEASE RETRY] Attempt {Attempt}/{MaxAttempts} threw for holder={Holder}",
                        attempt,
                        maxAttempts,
                        holder);
                    result = (false, $"ReleaseHolder threw an exception: {ex.Message}");
                }

                if (result.Success)
                {
                    if (attempt > 1)
                    {
                        _logger.LogInformation(
                            "[RELEASE RETRY] Succeeded on attempt {Attempt}/{MaxAttempts} for holder={Holder}",
                            attempt,
                            maxAttempts,
                            holder);
                    }

                    return result;
                }

                _logger.LogWarning(
                    "[RELEASE RETRY] Attempt {Attempt}/{MaxAttempts} failed for holder={Holder}: {Message}",
                    attempt,
                    maxAttempts,
                    holder,
                    result.Message);
            }

            return result;
        }

        private static string NormalizeUserId(string username)
        {
            var userId = username.Trim();

            var slashIndex = userId.LastIndexOf('\\');
            if (slashIndex >= 0)
                userId = userId[(slashIndex + 1)..];

            var atIndex = userId.IndexOf('@');
            if (atIndex > 0)
                userId = userId[..atIndex];

            return userId;
        }

        private static string ResolveProcess(string? clientKey)
        {
            return string.Equals(clientKey, "WDC_STACKER.CLIENT.FGI", StringComparison.OrdinalIgnoreCase)
                ? "FGI"
                : "PWD";
        }

        public async Task<List<CsvExportRow>> GetAllHolderAssignmentsForCsvAsync(string? clientKey)
        {
            var process = ResolveProcess(clientKey);
            return await _stackerSqlService.GetAllHolderAssignmentsForCsvAsync(process);
        }
    }
}
