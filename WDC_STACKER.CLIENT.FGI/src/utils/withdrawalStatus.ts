export interface WithdrawalStatusInfo {
    label: string;
    color: string;
    backgroundColor: string;
}

export function getWithdrawalStatusInfo(
    status: string,
    actualOutput: number | null,
    total: number | null
): WithdrawalStatusInfo {
    const normalizedStatus = status.trim().toUpperCase();
    const actual = actualOutput ?? 0;
    const totalQty = total ?? 0;

    if (normalizedStatus === "CLOSED") {
        if (actual >= totalQty) {
            return {
                label: "COMPLETED",
                color: "#1e40af",
                backgroundColor: "#dbeafe",
            };
        } else {
            return {
                label: "CLOSED",
                color: "#dc2626",
                backgroundColor: "#fee2e2",
            };
        }
    } else {
        if (actual >= totalQty && totalQty > 0) {
            return {
                label: "COMPLETED",
                color: "#1e40af",
                backgroundColor: "#dbeafe",
            };
        } else if (actual > 0) {
            return {
                label: "PARTIAL",
                color: "#d97706",
                backgroundColor: "#fef3c7",
            };
        } else {
            return {
                label: "OPEN",
                color: "#059669",
                backgroundColor: "#d1fae5",
            };
        }
    }
}
