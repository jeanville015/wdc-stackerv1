const HOLDCHECK_API_URL = import.meta.env.VITE_HOLDCHECK_API_URL || 'http://localhost:5003';

export interface HoldCheckResult {
  success: boolean;
  message: string;
  data?: {
    totalHolders: number;
    holdersSetToHold: number;
    holdersCleared: number;
    holdersAlreadyOnHold: number;
    holdersAlreadyClear: number;
    startTime: string;
    endTime: string;
  };
  error?: string;
}

export async function runHoldCheck(): Promise<HoldCheckResult> {
  try {
    const response = await fetch(`${HOLDCHECK_API_URL}/api/holdcheck/run`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    const result: HoldCheckResult = await response.json();
    return result;
  } catch (error) {
    console.error('Hold check API error:', error);
    return {
      success: false,
      message: 'Failed to connect to hold check service',
      error: error instanceof Error ? error.message : 'Unknown error',
    };
  }
}
