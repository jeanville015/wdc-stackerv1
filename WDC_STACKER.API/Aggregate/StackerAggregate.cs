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

        public async Task<GridViewBoxMapResult> MapGridViewBoxData()
        {
            var config = await _capacityConfigService.GetAsync();
            var boxes = await _stackerSqlService.GetBoxListCountAndPercentageAsync(config.MAX_ITEM_PER_BOX);

            ///<summary>
            ///Identifying certain box inside the GridView with the following rules/steps:
            /// 
            /// 1. Check the items in List<BoxView>, Identify the one with the smallest value (less than 100.00 but above 0.00) in BoxListPercentage . 
            ///    Highlight that box by adding an 2px border on it with darker blue shade and padding of 2px. 
            ///    
            /// 2. If more than one item in List<BoxView> has the same smallest value(less than 100.00 but above 0.00) in BoxListPercentage(eg. 1.00 is the smallest value and 2 or 3 items' BoxListPercentage has that same value), 
            ///    Find the one that has the smallest RackNum, LayerRowNum, and LayerColNum. Highlight that box by adding an 2px border on it with darker blue shade and padding of 2px.
            ///
            /// 3. If there is no item / items in List <BoxView> whose BoxListPercentage value is below 100.00 and above 0.00, find the one with 0.00 value.Highlight that box by adding an 2px border on it with darker blue shade and padding of 2px.
            ///    But if there are more than one whose BoxListPercentage = 0.00, find the one that has the smallest RackNum, LayerRowNum, and LayerColNum. Highlight that box by adding an 2px border on it with darker blue shade and padding of 2px.
            /// 
            /// </summary> 

            foreach (var box in boxes)
            {
                box.IsSuggestedTarget = false;
            }

            var suggested = boxes
                .Where(x => x.BoxListPercentage > 0 && x.BoxListPercentage < 100)
                .OrderBy(x => x.BoxListPercentage)
                .ThenBy(x => x.RackNum)
                .ThenBy(x => x.LayerRowNum)
                .ThenBy(x => x.LayerColNum)
                .FirstOrDefault();

            suggested ??= boxes
                .Where(x => x.BoxListPercentage == 0)
                .OrderBy(x => x.RackNum)
                .ThenBy(x => x.LayerRowNum)
                .ThenBy(x => x.LayerColNum)
                .FirstOrDefault();

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

            var allBoxesAreFull = boxes.Count > 0 && boxes.All(x => x.BoxListPercentage >= 100);

            if (allBoxesAreFull)
            {
                var lastBox = boxes
                    .OrderByDescending(x => x.RackNum)
                    .ThenByDescending(x => x.LayerRowNum)
                    .ThenByDescending(x => x.LayerColNum)
                    .FirstOrDefault();

                if (lastBox is not null)
                {
                    var newRackNum = lastBox.RackNum;
                    var newLayerRowNum = lastBox.LayerRowNum;
                    var newLayerColNum = lastBox.LayerColNum;

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

                    boxes.Add(new BoxView
                    {
                        BoxNo = $"R{newRackNum:00}L{newLayerRowNum:00}C{newLayerColNum:00}",
                        RackNum = newRackNum,
                        LayerRowNum = newLayerRowNum,
                        LayerColNum = newLayerColNum,
                        BoxListCount = 0,
                        BoxListPercentage = 0,
                        IsSuggestedTarget = true
                    });

                    return new GridViewBoxMapResult
                    {
                        Boxes = boxes,
                        HasSuggestedTarget = true,
                        Message = "Grid view box data mapped successfully."
                    };
                }
            }

            return new GridViewBoxMapResult
            {
                Boxes = boxes,
                HasSuggestedTarget = false,
                Message = "No suggested target box was found."
            };
        }

        public async Task<ScanHolderJobResponse> ScanHolderJobAsync(string holder, string token)
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
                Holder: holder,
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

            var config = await _capacityConfigService.GetAsync();

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
            var gridViewBoxMap = await MapGridViewBoxData();

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

        public async Task<AssignHolderResponse> AssignHolderAsync(AssignHolderRequest request, string token)
        {
            var holder = request.Holder.Trim();
            var boxNo = request.BoxNo.Trim();

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
                Holder: holder,
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

            var config = await _capacityConfigService.GetAsync();
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

            var holderAlreadyAssigned = await _stackerSqlService.HolderAssignExistsAsync(holder);

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

            var boxExists = await _stackerSqlService.BoxNoExistsAsync(boxNo);

            var boxDetails = boxExists
                ? null
                : new BoxDetailsInsertData
                {
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
                ProductName = productName,
                Lec = lecValue,
                Factory = buildCode,
                UpdateBy = credentials.Username,
                UpdateTs = DateTime.Now
            };

            try
            {
                await _stackerSqlService.InsertAssignmentAsync(boxDetails, holderAssign);
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

            var gridViewBoxMap = await MapGridViewBoxData();

            return new AssignHolderResponse
            {
                Success = true,
                Holder = holder,
                BoxName = boxNo,
                Lec = lecValue,
                BoxDetailsCreated = !boxExists,
                GridViewBoxes = gridViewBoxMap.Boxes,
                Message = boxExists
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

        private async Task<FeatsQueryResponse> ExecuteFeatsQueryAsync(string Holder, string queryType, string[] fieldNames, string filterName, string filterValue, int recordLimit, string username, string password)
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

        public Task<List<BoxAssignment>> GetBoxAssignmentsAsync(string boxName)
        {
            return _stackerSqlService.GetBoxAssignmentsAsync(boxName);
        }

        public async Task<(bool Success, string Message, List<BoxView> Boxes)> DisassociateHolderAsync(string holder, string token)
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
            var holderJobResult = await ExecuteFeatsQueryAsync(
                Holder: holder,
                queryType: "HolderJob",
                fieldNames: new[] { "HoldReason", "HoldComment" },
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
            var deleted = await _stackerSqlService.DisassociateHolderAsync(holder); 
            if (!deleted)
            {
                return (
                    false,
                    "The holder was not found or its status is not RELEASE.",
                    new List<BoxView>()
                );
            }
            //-- SQL DELETE for HOLDER_ASSIGN: END --------------------------------------//

            var gridView = await MapGridViewBoxData();

            return (
                true,
                "Holder disassociated successfully.",
                gridView.Boxes
            );
        }

    }
}