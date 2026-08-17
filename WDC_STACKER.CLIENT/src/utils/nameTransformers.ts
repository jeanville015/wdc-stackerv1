/**
 * Name transformation utilities for simplified rack and box display names.
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
 */
export const formatBoxName = (boxNo: string, rackNumber: number): string => {
  const match = boxNo.match(/R\d+L\d+C(\d+)/);
  if (match) {
    const columnNum = parseInt(match[1], 10);
    return `B${String(columnNum).padStart(2, '0')}`;
  }

  // Fallback: use the provided rack number
  return `B${String(rackNumber).padStart(2, '0')}`;
};
