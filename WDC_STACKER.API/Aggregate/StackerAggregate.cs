
using WDC_STACKER.API.Models;
using WDC_STACKER.API.Models.Feats;
using WDC_STACKER.API.Models.Stacker;
using WDC_STACKER.API.Services;

namespace WDC_STACKER.API.Aggregate
{
    public class StackerAggregate
    {
        private readonly FeatsService _featsService;
        private readonly FeatsCredentialStore _credentialStore;
        private readonly CapacityConfigService _capacityConfigService;
        private readonly StackerSqlService _stackerSqlService;

        public StackerAggregate(FeatsService featsService, FeatsCredentialStore credentialStore, CapacityConfigService capacityConfigService, StackerSqlService stackerSqlService)
        {
            _featsService = featsService;
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

        public async Task<GridViewBoxMapResult> MapGridViewBoxData(string? clientKey)
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

            var suggested = SelectSuggestedBox(boxes);

            if (suggested is not null)
            {
                suggested.IsSuggestedTarget = true;

                if (isFgi && !EnsureSuggestedShipBox(suggested, config))
                {
                    return new GridViewBoxMapResult
                    {
                        Boxes = boxes,
                        HasSuggestedTarget = false,
                        Message = "No suggested target ShipBox was found."
                    };
                }

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

                if (isFgi)
                {
                    EnsureSuggestedShipBox(newBox, config);
                }

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

        private static bool EnsureSuggestedShipBox(BoxView box, CapacityConfig config)
        {
            foreach (var shipBox in box.ShipBoxes)
            {
                shipBox.IsSuggestedTarget = false;
            }

            if (box.ShipBoxes.Any(x => x.ShipBoxListPercentage < 100))
            {
                var suggested = box.ShipBoxes
                    .Where(x => x.ShipBoxListPercentage < 100)
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
                newShipBoxNum = lastShipBox.ShipBoxNum + 1;
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
                ShipBoxName = $"{box.BoxNo}-S{newShipBoxNum:00}",
                ShipBoxStatus = "RELEASE",
                ShipBoxNum = newShipBoxNum,
                LayerRowNum = newLayerRowNum,
                LayerColNum = newLayerColNum,
                ShipBoxListCount = 0,
                ShipBoxListPercentage = 0,
                IsSuggestedTarget = true,
                HasReleaseStatus = true
            });

            return true;
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

            var fieldNames = new[]
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
                "WaferNum"
            };

            var holderJobResult = await ExecuteFeatsQueryAsync(
                queryType: "HolderJob",
                fieldNames: fieldNames,
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

            // 2. ParentHolder must have value
            //if (string.IsNullOrWhiteSpace(parentHolder))
            if (!string.IsNullOrWhiteSpace(parentHolder))
            {
                return new ScanHolderJobResponse
                {
                    Success = false,
                    CanAssign = false,
                    Holder = holder,
                    Message = "ParentHolder has no value!",
                    HolderJob = row,
                    RawQueryResult = holderJobResult
                };
            }

            // 3. ShipTicket must be empty
            if (!string.IsNullOrWhiteSpace(shipTicket))
            {
                return new ScanHolderJobResponse
                {
                    Success = false,
                    CanAssign = false,
                    Holder = holder,
                    Message = "ShipTicket has value!",
                    HolderJob = row,
                    RawQueryResult = holderJobResult
                };
            }

            // 4. If All checks are okay (steps 1 to 3), get the grid view box mapping data
            var gridViewBoxMap = await MapGridViewBoxData(clientKey);

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

            var holderJobResult = await ExecuteFeatsQueryAsync(
                queryType: "HolderJob",
                fieldNames: new[] { "BuildCode", "BinName", "ProductName" },
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

            var buildCode = GetField(row, "BuildCode");
            var binName = GetField(row, "BinName");
            var productName = GetField(row, "ProductName");

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

            if (string.IsNullOrWhiteSpace(buildCode) || string.IsNullOrWhiteSpace(productName))
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

            if (string.Equals(firstPartKey, nameof(config.SJ), StringComparison.OrdinalIgnoreCase))
            {
                firstPart = config.SJ;
            }
            else if (string.Equals(firstPartKey, nameof(config.SD), StringComparison.OrdinalIgnoreCase))
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
            var lecValue = firstPart + secondPart + thirdPart;

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
                    var shipBoxExists = await _stackerSqlService.ShipBoxNameExistsAsync(shipBoxName);

                    var shipBoxDetails = shipBoxExists
                        ? null
                        : new ShipBoxDetailsInsertData
                        {
                            BoxNo = boxNo,
                            ShipBoxName = shipBoxName,
                            ShipBoxStatus = "RELEASE",
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
            catch
            {
                return new AssignHolderResponse
                {
                    Success = false,
                    Holder = holder,
                    BoxName = boxNo,
                    Lec = lecValue,
                    Message = "Unable to Assign.",
                    RawQueryResult = holderJobResult
                };
            }

            var gridViewBoxMap = await MapGridViewBoxData(clientKey);

            return new AssignHolderResponse
            {
                Success = true,
                Holder = holder,
                BoxName = boxNo,
                Lec = lecValue,
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

        public async Task<List<ShipBoxView>> GetShipBoxesAsync(string boxNo, bool suggest, string? clientKey)
        {
            var config = await _capacityConfigService.GetAsync(clientKey);
            var process = ResolveProcess(clientKey);

            if (!string.Equals(process, "FGI", StringComparison.OrdinalIgnoreCase))
                return new List<ShipBoxView>();

            var shipBoxes = await _stackerSqlService.GetFgiShipBoxesByBoxNoAsync(
                boxNo,
                config.MAX_ITEM_PER_BOX_SHIPBOX,
                process);

            if (!suggest)
                return shipBoxes;

            var box = new BoxView
            {
                BoxNo = boxNo,
                ShipBoxes = shipBoxes
            };

            EnsureSuggestedShipBox(box, config);
            return box.ShipBoxes;
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
            var holderJobResult = await ExecuteFeatsQueryAsync( queryType: "HolderJob", fieldNames: new[] { "Holder","HoldReason", "HoldComment" },
                filterName: "Holder",
                filterValue: holder,
                recordLimit: 250,
                username: credentials.Username,
                password: credentials.Password
            ); 
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
            var holdReason = GetField(row, "HoldReason");
            var holdComment = GetField(row, "HoldComment"); 
            var fieldsAreEmpty =
                string.IsNullOrWhiteSpace(holdReason) &&
                string.IsNullOrWhiteSpace(holdComment); 
            if (!fieldsAreEmpty)
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

        private static string ResolveProcess(string? clientKey)
        {
            return string.Equals(clientKey, "WDC_STACKER.CLIENT.FGI", StringComparison.OrdinalIgnoreCase)
                ? "FGI"
                : "PWD";
        }


    }
}