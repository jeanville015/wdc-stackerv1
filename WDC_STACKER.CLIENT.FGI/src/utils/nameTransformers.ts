/**
 * Name transformation utilities for simplified rack, box, and shipbox display names.
 * These functions transform the database names to simplified sequential numbering
 * for display purposes only, while keeping all API calls unchanged.
 */

/**
 * Transform rack number for display.
 * Input: 1, 2, 3 (rackNumber)
 * Output: "R01", "R02", "R03"
 */
export const formatRackName = (rackNumber: number): string => {
  return `R${String(rackNumber).padStart(2, '0')}`;
};

/**
 * Compute a globally sequential number from a layer/column position,
 * given how many items exist per layer. This keeps numbering unique
 * and adaptive across layers, e.g. with itemsPerLayer = 3:
 *   Layer 1 -> columns 1..3 => 1, 2, 3
 *   Layer 2 -> columns 1..3 => 4, 5, 6
 *   Layer 3 -> columns 1..3 => 7, 8, 9
 */
const toSequentialNumber = (
  layerNum: number,
  columnNum: number,
  itemsPerLayer: number
): number => {
  const perLayer = Math.max(1, itemsPerLayer);
  return (Math.max(1, layerNum) - 1) * perLayer + columnNum;
};

/**
 * Transform box name for display.
 * Input: the Box's LayerRowNum/LayerColNum (structured position fields from
 * the API), boxCountPerLayer (config BOX_COUNT)
 * Output: "B01", "B02", etc. (simplified, sequential across layers)
 *
 * The column number alone repeats every layer (C01, C02, ...), so it is
 * combined with the layer number and the configured box-count-per-layer
 * to produce a globally unique, sequential display number.
 *
 * NOTE: This intentionally uses the structured LayerRowNum/LayerColNum
 * fields rather than parsing them out of the BoxNo string, since the
 * persisted/display name string can drift from the object's actual
 * current position (e.g. a reused BoxNo/ShipBoxName that was not
 * regenerated after a move).
 */
export const formatBoxName = (
  layerRowNum: number,
  layerColNum: number,
  boxCountPerLayer = 1
): string => {
  const sequential = toSequentialNumber(layerRowNum, layerColNum, boxCountPerLayer);
  return `B${String(sequential).padStart(2, '0')}`;
};

/**
 * Transform shipbox name for display.
 * Input: the ShipBox's LayerRowNum/LayerColNum (structured position fields
 * from the API), shipBoxCountPerLayer (config BOX_COUNT-SHIPBOX)
 * Output: "S01", "S02", etc. (simplified, sequential across layers)
 *
 * See formatBoxName's note above on why this uses structured fields
 * instead of parsing the ShipBoxName string.
 */
export const formatShipBoxName = (
  layerRowNum: number,
  layerColNum: number,
  shipBoxCountPerLayer = 1
): string => {
  const sequential = toSequentialNumber(layerRowNum, layerColNum, shipBoxCountPerLayer);
  return `S${String(sequential).padStart(2, '0')}`;
};

/**
 * Transform API validation messages to use simplified naming.
 * Replaces old naming patterns in error messages with new format.
 */
export const transformValidationMessage = (
  message: string,
  boxCountPerLayer = 1,
  shipBoxCountPerLayer = 1
): string => {
  if (!message) return message;
  
  let transformed = message;
  

  transformed = transformed.replace(
    /Box R(\d+)L(\d+)C(\d+) \(Rack \d+, Layer \d+, Column \d+\), ShipBox S\d+L(\d+)C(\d+)/g,
    (_match, rackNum, boxLayer, boxCol, shipBoxLayer, shipBoxCol) => {
      const boxSeq = toSequentialNumber(parseInt(boxLayer, 10), parseInt(boxCol, 10), boxCountPerLayer);
      const shipBoxSeq = toSequentialNumber(parseInt(shipBoxLayer, 10), parseInt(shipBoxCol, 10), shipBoxCountPerLayer);
      return `<br/>RACK: R${String(parseInt(rackNum, 10)).padStart(2, '0')},<br/>BLACKBOX: B${String(boxSeq).padStart(2, '0')},<br/>SHIPBOX: S${String(shipBoxSeq).padStart(2, '0')}`;
    }
  );
  
  // Transform box names: R01L01C04 -> B04 (layer + column identify the box)
  transformed = transformed.replace(/R\d+L(\d+)C(\d+)/g, (_match, layerNum, colNum) => {
    const sequential = toSequentialNumber(parseInt(layerNum, 10), parseInt(colNum, 10), boxCountPerLayer);
    return `B${String(sequential).padStart(2, '0')}`;
  });
  
  // Transform shipbox names: S01L01C02 -> S02 (layer + column identify the shipbox)
  transformed = transformed.replace(/S\d+L(\d+)C(\d+)/g, (_match, layerNum, colNum) => {
    const sequential = toSequentialNumber(parseInt(layerNum, 10), parseInt(colNum, 10), shipBoxCountPerLayer);
    return `S${String(sequential).padStart(2, '0')}`;
  });
  
  // Transform "Rack 1, Layer 1, Column 4" -> simplified format
  transformed = transformed.replace(/Rack (\d+), Layer \d+, Column \d+/g, (_match, rack) => {
    return `R${String(parseInt(rack, 10)).padStart(2, '0')}`;
  });
  
  return transformed;
};
