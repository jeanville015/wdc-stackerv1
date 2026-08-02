
using WDC_STACKER.API.Models;
using WDC_STACKER.API.Models.Feats;
using WDC_STACKER.API.Models.Stacker;
using WDC_STACKER.API.Services;

namespace WDC_STACKER.API.Aggregate
{
    public class StackerAggregate
    {
        private readonly FeatsService _featsService;
        private readonly AhsService _ahsService;
        private readonly FeatsCredentialStore _credentialStore;
        private readonly CapacityConfigService _capacityConfigService;
        private readonly StackerSqlService _stackerSqlService;

        public StackerAggregate(FeatsService featsService, AhsService ahsService, FeatsCredentialStore credentialStore, CapacityConfigService capacityConfigService, StackerSqlService stackerSqlService)
        {
            _featsService = featsService;
            _ahsService = ahsService;
            _credentialStore = credentialStore;
            _capacityConfigService = capacityConfigService;
            _stackerSqlService = stackerSqlService;

        }

        public bool IsSessionTokenValid(string token)
        {
            return _credentialStore.TryGet(token, out _);
        }

        public async Task<bool> CanAccessConfigurationAsync(string username, string password)
        {
            var result = await ExecuteFeatsQueryAsync(
                queryType: "UsersByPrivilege",
                fieldNames: new[] { "EmployeeName", "FullName" },
                filterName: "Privilege",
                filterValue: "TAP_FAB3ADMIN",
                recordLimit: 250,
                username: username,
                password: password);

            if (!result.Success)
                return false;

            return result.ParsedResult.Rows.Any(row =>
                row.TryGetValue("EmployeeName", out var employeeName) &&
                !string.IsNullOrWhiteSpace(employeeName) &&
                employeeName.Contains(username, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<GridViewBoxMapResult> MapGridViewBoxData(
            string? clientKey,
            HolderAssignInsertData? holderData = null)
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
                .OrderBy(x => x.BoxListPercentage)
                .ThenBy(x => x.RackNum)
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
                       holderData.ProductName);
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
                Lec = normalizedLec ?? string.Empty
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

            var holderJobResult = await ExecuteFeatsQueryAsync(
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

            if (row is null)
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
            var holdReason = GetField(row, "HoldReason");
            var holdComment = GetField(row, "HoldComment");
            var inProcess = GetField(row, "InProcess");

            // 1. Operation must match config.ValidOperation
            if (!string.Equals(operation, config.ValidOperation, StringComparison.OrdinalIgnoreCase))
            {
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

            // Note: disable for now while on development/QA, enable once on UAT/PROD
            // 2. ParentHolder must have No value 
            //if (!string.IsNullOrWhiteSpace(parentHolder))
            //{
            //    return new ScanHolderJobResponse
            //    {
            //        Success = false,
            //        CanAssign = false,
            //        Holder = holder,
            //        Message = "ParentHolder has value!",
            //        HolderJob = row,
            //        RawQueryResult = holderJobResult
            //    };
            //}

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
            if (isFgi &&
                (string.IsNullOrWhiteSpace(partNumber) ||
                 string.IsNullOrWhiteSpace(productName)))
            {
                // MoveOut to RBF2
                var moveOutResult = await _featsService.MoveOutAsync(
                    holder: holder,
                    holderType: null,
                    resource: "735617 RBF2",
                    nextOp: null,
                    username: credentials.Username,
                    password: credentials.Password);

                if (!moveOutResult.Success)
                {
                    return new ScanHolderJobResponse
                    {
                        Success = false,
                        CanAssign = false,
                        Holder = holder,
                        Message = $"MoveOut to 735617 RBF2 failed: {moveOutResult.Message}",
                        HolderJob = row,
                        RawQueryResult = holderJobResult
                    };
                }

                // Apply Hold with reason TAP and comment NO PART NUMBER
                var holdResult = await _featsService.HoldHolderAsync(
                    holder: holder,
                    holderType: null,
                    holdReasonCode: "TAP",
                    comment: "NO PART NUMBER",
                    username: credentials.Username,
                    password: credentials.Password);

                if (!holdResult.Success)
                {
                    // Compensating logic: Hold failed after MoveOut succeeded
                    // Log the partial failure state and return error
                    _logger.LogWarning(
                        "Partial failure for holder={Holder}: MoveOut succeeded but Hold failed. Message: {HoldMessage}",
                        holder,
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

                return new ScanHolderJobResponse
                {
                    Success = false,
                    CanAssign = false,
                    Holder = holder,
                    Message = "PartNumber is missing. Holder has been moved out to RBF2 and held with reason TAP: NO PART NUMBER.",
                    HolderJob = row,
                    RawQueryResult = holderJobResult
                };
            }

            if (isFgi && string.IsNullOrWhiteSpace(productName))
            {
                return new ScanHolderJobResponse
                {
                    Success = false,
                    CanAssign = false,
                    Holder = holder,
                    Message = "ProductName is missing.",
                    HolderJob = row,
                    RawQueryResult = holderJobResult
                };
            }

            // 5. BinName must be exactly 5 characters for FGI.
            // If not, MoveOut to RBF2.
            if (isFgi && binName?.Length != 5)
            {
                var moveOutResult = await _featsService.MoveOutAsync(
                    holder: holder,
                    holderType: null,
                    resource: "735617 RBF2",
                    nextOp: null,
                    username: credentials.Username,
                    password: credentials.Password);

                if (!moveOutResult.Success)
                {
                    return new ScanHolderJobResponse
                    {
                        Success = false,
                        CanAssign = false,
                        Holder = holder,
                        Message = $"MoveOut to 735617 RBF2 failed: {moveOutResult.Message}",
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

            // 6. Two-step Hold Validation
            // Step 1: Check FEATS HoldReason and HoldComment
            // If both are null, holder is free of holds
            if (!string.IsNullOrWhiteSpace(holdReason) || !string.IsNullOrWhiteSpace(holdComment))
            {
                return new ScanHolderJobResponse
                {
                    Success = false,
                    CanAssign = false,
                    Holder = holder,
                    Message = $"Holder has FEATS hold. HoldReason: '{holdReason}', HoldComment: '{holdComment}'",
                    HolderJob = row,
                    RawQueryResult = holderJobResult
                };
            }

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

            foreach (var validationOperation in holdValidationOperations)
            {
                var sliderCheckResult = await _ahsService.SliderCheck2Async(
                    holder: holder,
                    operation: validationOperation,
                    checkExist: false);

                if (!sliderCheckResult.Success)
                {
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

                // Check if result is EXISTS or ONHOLD - if so, fail validation
                var responseUpper = sliderCheckResult.RawResponse?.ToUpperInvariant();
                if (responseUpper == "EXISTS" || responseUpper == "ONHOLD")
                {
                    // MoveOut to RBF2
                    var moveOutResult = await _featsService.MoveOutAsync(
                        holder: holder,
                        holderType: null,
                        resource: "735617 RBF2",
                        nextOp: null,
                        username: credentials.Username,
                        password: credentials.Password);

                    if (!moveOutResult.Success)
                    {
                        return new ScanHolderJobResponse
                        {
                            Success = false,
                            CanAssign = false,
                            Holder = holder,
                            Message = $"MoveOut to 735617 RBF2 failed: {moveOutResult.Message}",
                            HolderJob = row,
                            RawQueryResult = holderJobResult
                        };
                    }

                    return new ScanHolderJobResponse
                    {
                        Success = false,
                        CanAssign = false,
                        Holder = holder,
                        Message = $"Holder has hold/slider issue for operation '{validationOperation}'. AHS response: {sliderCheckResult.RawResponse}. Holder has been moved out to RBF2.",
                        HolderJob = row,
                        RawQueryResult = holderJobResult
                    };
                }

                // If result is PASSED, continue to next operation
                // If result is anything else, log it but continue
                if (responseUpper != "PASSED")
                {
                    _logger.LogWarning(
                        "AHS SliderCheck2 returned unexpected response for holder={Holder}, operation={Operation}: {Response}",
                        holder,
                        validationOperation,
                        sliderCheckResult.RawResponse);
                }
            }

            if (isFgi && !TryGetValidatedHolderQty(row, out _))
            {
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

            // 7. InProcess Validation (Last validation)
            // If InProcess is not "True", perform MoveIn then continue
            if (!string.Equals(inProcess, "True", StringComparison.OrdinalIgnoreCase))
            {
                var moveInResult = await _featsService.MoveInAsync(
                    holder: holder,
                    holderType: null,
                    username: credentials.Username,
                    password: credentials.Password);

                if (!moveInResult.Success)
                {
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
                    "MoveIn successful for holder={Holder} with InProcess={InProcess}",
                    holder,
                    inProcess);
            }

            HolderAssignInsertData? suggestionData = null;

            if (isFgi)
            {
                var penResult = await ResolveFgiPenNumAsync(
                    experimentId,
                    credentials.Username,
                    credentials.Password);

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
                    Lec = BuildFgiLecOrNull(
                        config,
                        buildCode,
                        binName,
                        productName)
                };
            }

            // 5. If all validation checks pass, map the target for this holder.
            var gridViewBoxMap = await MapGridViewBoxData(
                clientKey,
                suggestionData);

            if (!gridViewBoxMap.HasSuggestedTarget)
            {
                return new ScanHolderJobResponse
                {
                    Success = false,
                    CanAssign = false,
                    Holder = holder,
                    Message = gridViewBoxMap.Message,
                    HolderJob = row,
                    RawQueryResult = holderJobResult,
                    GridViewBoxes = gridViewBoxMap.Boxes
                };
            }
            return new ScanHolderJobResponse
            {
                Success = true,
                CanAssign = true,
                Holder = holder,
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
                "ProductName"
            };

            if (isFgi)
            {
                holderJobFieldNames.AddRange(new[]
                {
                    "PartNumber",
                    "Experiment",
                    "QuantityGood",
                    "QuantityLoaded",
                    "SliderCount"
                });
            }

            var holderJobResult = await ExecuteFeatsQueryAsync(
                queryType: "HolderJob",
                fieldNames: holderJobFieldNames.ToArray(),
                filterName: "Holder",
                filterValue: holder,
                recordLimit: 250,
                username: credentials.Username,
                password: credentials.Password);

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

            if (row is null)
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
                    credentials.Password);

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
                // Preserve the original PWD validation and LEC formula.
                if (binName.Length != 5)
                {
                    return new AssignHolderResponse
                    {
                        Success = false,
                        Holder = holder,
                        BoxName = boxNo,
                        Message = "BinName length is not eligible.",
                        RawQueryResult = holderJobResult
                    };
                }

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

            var holderAlreadyAssigned = await _stackerSqlService.HolderAssignExistsAsync(holder, process);

            if (holderAlreadyAssigned)
            {
                return new AssignHolderResponse
                {
                    Success = false,
                    Holder = holder,
                    BoxName = boxNo,
                    Message = "Holder is already assigned.",
                    RawQueryResult = holderJobResult
                };
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
                Qty = isFgi ? holderQty : null,
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
                isFgi ? holderAssign : null);

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
                string password)
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
                password: password);

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

        private async Task<(bool Success, string Message, bool IsOnInSiteHold)> CheckHolderInSiteHoldAsync(string holder, string username, string password)
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
                password: password);

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

        private async Task<FeatsQueryResponse> ExecuteFeatsQueryAsync(string queryType, string[] fieldNames, string filterName, string filterValue, int recordLimit, string username, string password)
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

            return await _featsService.QueryAsync(request, username, password);
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
            bool hasLecContext = false)
        {
            var config = await _capacityConfigService.GetAsync(clientKey);
            var process = ResolveProcess(clientKey);

            if (!string.Equals(process, "FGI", StringComparison.OrdinalIgnoreCase))
                return new List<ShipBoxView>();

            var shipBoxes = await _stackerSqlService.GetFgiShipBoxesByBoxNoAsync(
                boxNo,
                config.MAX_ITEM_PER_BOX_SHIPBOX,
                process);

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

        public async Task<( bool Success, string Message,  FgiWithdrawalDisassociationPreviewView? Preview)> GetFgiWithdrawalDisassociationPreviewAsync(string lec, string? penNum, int total, string token)
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

            var preview =
                await _stackerSqlService
                    .GetFgiWithdrawalDisassociationPreviewAsync(
                        lec,
                        penNum,
                        total);

            foreach (var record in preview.SourceRecords.Where(
                         record => record.IsIncluded))
            {
                var holdCheck =
                    await CheckHolderInSiteHoldAsync(
                        record.Holder,
                        credentials.Username,
                        credentials.Password);

                if (!holdCheck.Success)
                {
                    return (
                        false,
                        $"Unable to check InSite hold for " +
                        $"{record.Holder}: {holdCheck.Message}",
                        null
                    );
                }

                record.Status = holdCheck.IsOnInSiteHold
                    ? "IN-SITE HOLD"
                    : "HOLD PASS";
            }

            return (
                true,
                "Disassociation preview loaded successfully.",
                preview
            );
        }

        /// <summary>
        /// "Verify ShipBox" step of the Job Withdrawal flow, run right after
        /// "Enter Shipping Box Allocation". Intended to validate that the
        /// entered Shipping Id is valid before continuing; if invalid, the
        /// UI should return the user to the Shipping Id input.
        ///
        /// STUBBED/BYPASSED per request: always passes (aside from a
        /// non-empty check) until the real validation rule is provided. See
        /// JOB_WITHDRAWAL_CHANGES.md.
        /// </summary>
        private (bool Success, string Message) VerifyShipBox(string shippingId)
        {
            if (string.IsNullOrWhiteSpace(shippingId))
            {
                return (false, "ShippingId is required.");
            }

            // TODO: implement real ShipBox verification. Bypassed for now.
            return (true, "ShipBox verification bypassed (not yet implemented).");
        }

        public async Task<FgiWithdrawalDisassociationResult> DisassociateFgiWithdrawalRequestAsync( long requestId, string shippingId, IReadOnlyCollection<string> includedHolders, string token)
        {
            if (!_credentialStore.TryGet(token, out _))
            {
                return new FgiWithdrawalDisassociationResult
                {
                    Success = false,
                    Message = "Invalid or expired token."
                };
            }

            //-- VERIFY SHIPBOX: START (bypassed) ----------------------------------------\\
            var shipBoxCheck = VerifyShipBox(shippingId);

            if (!shipBoxCheck.Success)
            {
                return new FgiWithdrawalDisassociationResult
                {
                    Success = false,
                    Message = shipBoxCheck.Message
                };
            }
            //-- VERIFY SHIPBOX: END ------------------------------------------------------//

            // PENDING: AddJob() and MoveOut() FEATS transactions using
            // `shippingId` will be wired in here, before the SQL update
            // below. See JOB_WITHDRAWAL_CHANGES.md.

            var result = await _stackerSqlService
                .DisassociateFgiWithdrawalAsync(
                    requestId,
                    includedHolders);

            if (!result.Success)
            {
                return result;
            }

            //-- SEND EMAIL: START (placeholder) ------------------------------------------\\
            // TODO: send withdrawal completion email to HGA/FGI for
            // requestId (not yet implemented). See JOB_WITHDRAWAL_CHANGES.md.
            //-- SEND EMAIL: END -----------------------------------------------------------//

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

        public Task<FgiWithdrawalRackView?> GetFgiWithdrawalLayoutAsync(
            string lec,
            string? clientKey)
        {
            var process = ResolveProcess(clientKey);
            return _stackerSqlService.GetFgiWithdrawalLayoutAsync(lec, process);
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

            //-- INSIGHT HOLD CHECK :START -----------------------------------------------\\
            var holdCheck = await CheckHolderInSiteHoldAsync(holder, credentials.Username, credentials.Password);

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

            //-- MOVE-OUT TRANSACTION: START---------------------------------------------\\
            var moveOutResult = await _featsService.MoveOutAsync(
                holder: holder,
                holderType: null,
                resource: null,
                nextOp: null,
                username: credentials.Username,
                password: credentials.Password
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

            //-- SQL DELETE for HOLDER_ASSIGN: START------------------------------------\\
            var process = ResolveProcess(clientKey);
            var deleted = await _stackerSqlService.DisassociateHolderAsync(holder, process);
            if (!deleted)
            {
                return (
                    false,
                    "The holder was not found or its status is not RELEASE.",
                    new List<BoxView>()
                );
            }
            //-- SQL DELETE for HOLDER_ASSIGN: END --------------------------------------//

            var gridView = await MapGridViewBoxData(clientKey);

            return (
                true,
                "Holder disassociated successfully.",
                gridView.Boxes
            );
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


    }
}
