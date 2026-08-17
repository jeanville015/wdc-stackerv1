import { useEffect, useRef, useState, useCallback } from "react";
import StackerOperationControls from "../components/StackerOperationControls";
import RackBoard from "../components/home/RackBoard";
import { useCapacityConfig } from "../hooks/useCapacityConfig";
import { useAuth } from "../context/useAuth";
import type { BoxView } from "../types/stacker";
import { getBoxesApi, exportCsvApi } from "../api/stackerApi";

export default function HomePage() {  
    const { config, loading, error } = useCapacityConfig();
    const { user } = useAuth();
    const token = user?.token;
    const [gridViewBoxes, setGridViewBoxes] = useState<BoxView[]>([]);
    const [selectedTargetBox, setSelectedTargetBox] = useState<BoxView | null>(null);
    const [isExistingHolderLocation, setIsExistingHolderLocation] = useState(false);
    const [recentlyAssignedBoxNo, setRecentlyAssignedBoxNo] = useState<string | null>(null);

    const handleGridViewBoxesLoaded = useCallback((boxes: BoxView[]) => {
        setGridViewBoxes(boxes);
        setIsExistingHolderLocation(false); // Clear flag when boxes are loaded
    }, []);

    const handleSelectedTargetBoxChanged = useCallback((box: BoxView | null, isExistingLocation = false) => {
        setSelectedTargetBox(box);
        setIsExistingHolderLocation(isExistingLocation);
    }, []);
    const assignedBoxTimerRef = useRef<number | null>(null);
    const [refreshLoading, setRefreshLoading] = useState(false);
    const [csvExportLoading, setCsvExportLoading] = useState(false);

    useEffect(() => {
        return () => {
            if (assignedBoxTimerRef.current) {
                window.clearTimeout(assignedBoxTimerRef.current);
            }
        };
    }, []);

    const showAssignedBoxConfirmation = (boxNo: string) => {
        setRecentlyAssignedBoxNo(boxNo);

        if (assignedBoxTimerRef.current) {
            window.clearTimeout(assignedBoxTimerRef.current);
        }

        assignedBoxTimerRef.current = window.setTimeout(() => {
            setRecentlyAssignedBoxNo(null);
        }, 4000);
    };

    const handleRefresh = useCallback(async () => {
        if (!token) return;

        setRefreshLoading(true);
        setSelectedTargetBox(null); // Clear before loading
        setIsExistingHolderLocation(false); // Clear flag

        try {
            const result = await getBoxesApi(token);

            if (result.GridViewBoxes) {
                setGridViewBoxes(result.GridViewBoxes);
            }
        } catch (err) {
            console.error("Failed to refresh boxes:", err);
        } finally {
            setRefreshLoading(false);
        }
    }, [token]);

    const handleExportCsv = async () => {
        if (!token) {
            alert("Login token is missing. Please sign in again.");
            return;
        }

        setCsvExportLoading(true);
        try {
            await exportCsvApi(token);
        } catch (err) {
            alert(err instanceof Error ? err.message : "CSV export failed.");
        } finally {
            setCsvExportLoading(false);
        }
    };

    // Auto-refresh boxes on login (when user becomes available)
    useEffect(() => {
        if (!token || loading || !config) {
            return;
        }

        let isCancelled = false;

        Promise.resolve().then(async () => {
            if (isCancelled) return;

            setRefreshLoading(true);
            setSelectedTargetBox(null); // Clear before loading
            setIsExistingHolderLocation(false); // Clear flag

            try {
                const result = await getBoxesApi(token);

                if (!isCancelled && result.GridViewBoxes) {
                    setGridViewBoxes(result.GridViewBoxes);
                }
            } catch (err) {
                if (!isCancelled) {
                    console.error("Failed to refresh boxes:", err);
                }
            } finally {
                if (!isCancelled) {
                    setRefreshLoading(false);
                }
            }
        });

        return () => {
            isCancelled = true;
        };
    }, [token, loading, config]);

    return (
        <div
            style={{
                display: "grid",
                gridTemplateColumns: "minmax(220px, 280px) minmax(0, 1fr)",
                gap: "1rem",
                alignItems: "start",
                width: "100%",
            }}
        >
            <StackerOperationControls
                onGridViewBoxesLoaded={handleGridViewBoxesLoaded}
                onAssignedBoxConfirmed={showAssignedBoxConfirmation}
                onSelectedTargetBoxChanged={handleSelectedTargetBoxChanged}
            />

            <section style={{ minWidth: 0, width: "100%" }}>
                <div
                    style={{
                        display: "flex",
                        justifyContent: "space-between",
                        alignItems: "center",
                        marginBottom: "1rem",
                    }}
                >
                    <h2
                        style={{
                            fontSize: "1.1rem",
                            fontWeight: "600",
                            color: "#172b4d",
                            margin: 0,
                        }}
                    >
                        Rack View
                    </h2>

                    <div style={{ display: "flex", gap: "0.5rem" }}>
                        <button
                            className="btn btn-sm"
                            onClick={handleRefresh}
                            disabled={refreshLoading || loading}
                            style={{
                                background: refreshLoading || loading
                                    ? "#a0b4d6"
                                    : "#0052cc",
                                color: "#ffffff",
                                border: "none",
                                borderRadius: "6px",
                                padding: "0.4rem 0.8rem",
                                fontSize: "0.8rem",
                                fontWeight: "500",
                                display: "flex",
                                alignItems: "center",
                                gap: "0.5rem",
                                cursor: refreshLoading || loading ? "not-allowed" : "pointer",
                            }}
                        >
                            {refreshLoading ? (
                                <>
                                    <span
                                        className="spinner-border spinner-border-sm"
                                        role="status"
                                        aria-hidden="true"
                                    />
                                    Refreshing...
                                </>
                            ) : (
                                <>
                                    <svg
                                        xmlns="http://www.w3.org/2000/svg"
                                        width="14"
                                        height="14"
                                        viewBox="0 0 24 24"
                                        fill="none"
                                        stroke="currentColor"
                                        strokeWidth="2"
                                        strokeLinecap="round"
                                        strokeLinejoin="round"
                                    >
                                        <path d="M21 12a9 9 0 0 0-9-9 9.75 9.75 0 0 0-6.74 2.74L3 8" />
                                        <path d="M3 3v5h5" />
                                        <path d="M3 12a9 9 0 0 0 9 9 9.75 9.75 0 0 0 6.74-2.74L21 16" />
                                        <path d="M16 21h5v-5" />
                                    </svg>
                                    Refresh
                                </>
                            )}
                        </button>

                        <button
                            className="btn btn-sm"
                            onClick={handleExportCsv}
                            disabled={csvExportLoading || loading}
                            style={{
                                background: csvExportLoading || loading
                                    ? "#a0b4d6"
                                    : "#0052cc",
                                color: "#ffffff",
                                border: "none",
                                borderRadius: "6px",
                                padding: "0.4rem 0.8rem",
                                fontSize: "0.8rem",
                                fontWeight: "500",
                                display: "flex",
                                alignItems: "center",
                                gap: "0.5rem",
                                cursor: csvExportLoading || loading ? "not-allowed" : "pointer",
                            }}
                        >
                            {csvExportLoading ? (
                                <>
                                    <span
                                        className="spinner-border spinner-border-sm"
                                        role="status"
                                        aria-hidden="true"
                                    />
                                    Exporting...
                                </>
                            ) : (
                                <>
                                    <i className="fa-solid fa-file-csv" aria-hidden="true" />
                                    Download CSV
                                </>
                            )}
                        </button>
                    </div>
                </div>

                {loading && (
                    <div
                        style={{
                            background: "#ffffff",
                            border: "1px solid #dde1e9",
                            borderRadius: "12px",
                            boxShadow: "0 4px 18px rgba(23,43,77,0.08)",
                            padding: "1rem 1.1rem",
                            color: "#172b4d",
                            display: "flex",
                            alignItems: "center",
                            gap: "0.75rem",
                            fontSize: "0.88rem",
                        }}
                    >
                        <span
                            className="spinner-border spinner-border-sm"
                            role="status"
                            aria-hidden="true"
                        />
                        Loading capacity configuration...
                    </div>
                )}

                {!loading && error && (
                    <div
                        role="alert"
                        style={{
                            background: "#ffffff",
                            border: "1px solid #dde1e9",
                            borderLeft: "3px solid #d23232",
                            borderRadius: "12px",
                            boxShadow: "0 4px 18px rgba(23,43,77,0.08)",
                            padding: "1rem 1.1rem",
                            color: "#d23232",
                            fontSize: "0.88rem",
                        }}
                    >
                        {error}
                    </div>
                )}

                {!loading && !error && config && (
                    <RackBoard
                        config={{
                            RACK_COUNT: config.RACK_COUNT,
                            LAYER_COUNT: config.LAYER_COUNT,
                            BOX_COUNT: config.BOX_COUNT,
                            MAX_ITEM_PER_BOX: config.MAX_ITEM_PER_BOX,
                        }}
                        boxes={gridViewBoxes}
                        selectedTargetBox={selectedTargetBox}
                        isExistingHolderLocation={isExistingHolderLocation}
                        recentlyAssignedBoxNo={recentlyAssignedBoxNo}
                        onBoxesChanged={setGridViewBoxes}
                    />
                )}
            </section>
        </div>
    );
}
