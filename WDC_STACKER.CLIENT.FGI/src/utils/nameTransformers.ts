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
 * Transform box name for display.
 * Input: "R01L01C01" (BoxNo from API)
 * Output: "B01", "B02", etc. (simplified format)
 * 
 * For now, this extracts the rack number and converts to B format.
 * The sequential numbering per rack can be refined later based on business requirements.
 */
export const formatBoxName = (boxNo: string, rackNumber: number): string => {
  // Extract the box's column number (its position within the rack/layer)
  const match = boxNo.match(/R\d+L\d+C(\d+)/);
  if (match) {
    const columnNum = parseInt(match[1], 10);
    return `B${String(columnNum).padStart(2, '0')}`;
  }

  // Fallback: use the provided rack number
  return `B${String(rackNumber).padStart(2, '0')}`;
};

/**
 * Transform shipbox name for display.
 * Input: "SB01L01C01" or "S01L01C01" (ShipBoxName from API)
 * Output: "S01", "S02", etc. (simplified format)
 */
export const formatShipBoxName = (shipBoxName: string): string => {
  // Extract the shipbox's column number (its position within the box/layer)
  const match = shipBoxName.match(/(?:SB)?\d+L\d+C(\d+)/);
  if (match) {
    return `S${String(parseInt(match[1], 10)).padStart(2, '0')}`;
  }
  
  // Fallback: return original name if pattern doesn't match
  return shipBoxName;
};

/**
 * Transform API validation messages to use simplified naming.
 * Replaces old naming patterns in error messages with new format.
 */
export const transformValidationMessage = (message: string): string => {
  if (!message) return message;
  
  let transformed = message;
  

  transformed = transformed.replace(
    /Box R(\d+)L\d+C(\d+) \(Rack \d+, Layer \d+, Column \d+\), ShipBox S\d+L\d+C(\d+)/g,
    (_match, rackNum, boxColNum, shipBoxColNum) => {
      return `<br/>RACK: R${String(parseInt(rackNum, 10)).padStart(2, '0')},<br/>BLACKBOX: B${String(parseInt(boxColNum, 10)).padStart(2, '0')},<br/>SHIPBOX: S${String(parseInt(shipBoxColNum, 10)).padStart(2, '0')}`;
    }
  );
  
  // Transform box names: R01L01C04 -> B04 (column identifies the box)
  transformed = transformed.replace(/R\d+L\d+C(\d+)/g, (_match, colNum) => {
    return `B${String(parseInt(colNum, 10)).padStart(2, '0')}`;
  });
  
  // Transform shipbox names: S01L01C02 -> S02 (column identifies the shipbox)
  transformed = transformed.replace(/S\d+L\d+C(\d+)/g, (_match, colNum) => {
    return `S${String(parseInt(colNum, 10)).padStart(2, '0')}`;
  });
  
  // Transform "Rack 1, Layer 1, Column 4" -> simplified format
  transformed = transformed.replace(/Rack (\d+), Layer \d+, Column \d+/g, (_match, rack) => {
    return `R${String(parseInt(rack, 10)).padStart(2, '0')}`;
  });
  
  return transformed;
};
