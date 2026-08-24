import { useEffect, useRef, useState, type FormEvent, } from "react";
import { useAuth } from "../../context/useAuth";
import { scanUnshipShippingIdApi, unshipFgiJobApi } from "../../api/unshipApi";
import type { FgiUnshipChildHolder, FgiUnshipScanResult } from "../../types/unship";

function displayValue(value: string | number | null | undefined): string | number {
    return value === null || value === undefined || value === "" ? "—" : value;
}

export default function JobUnshipPanel() {
    const { user } = useAuth();
    const token = user?.token;

    const [shippingId, setShippingId] = useState("");
    const [scanning, setScanning] = useState(false);
    const [scanError, setScanError] = useState("");
    const [scanResult, setScanResult] = useState<FgiUnshipScanResult | null>(null);

    const [holder, setHolder] = useState("");
    const [holderNotFound, setHolderNotFound] = useState(false);
    const [verifiedHolders, setVerifiedHolders] = useState<Set<string>>(new Set());

    const [isUnshipping, setIsUnshipping] = useState(false);
    const [unshipError, setUnshipError] = useState("");
    const [unshipSuccess, setUnshipSuccess] = useState("");
    const [ isUnshipConfirmationOpen, setIsUnshipConfirmationOpen, ] = useState(false); 
    const unshipConfirmationCancelRef =
        useRef<HTMLButtonElement | null>(null);
    const [isUnshipStepExpanded, setIsUnshipStepExpanded] =
        useState(true);

    const childHolders: FgiUnshipChildHolder[] = scanResult?.ChildHolders ?? [];
    const includedHolderCount = childHolders.length;
    const verifiedHolderCount = childHolders.filter((child) =>
        verifiedHolders.has(child.Holder.trim().toUpperCase())
    ).length;
    const allHoldersVerified = includedHolderCount > 0 && verifiedHolderCount === includedHolderCount; 

    const remainingHolderCount = Math.max(
        0,
        includedHolderCount - verifiedHolderCount
    );

    const verifiedHolderIds = Array.from(verifiedHolders);

    const recentlyVerifiedHolder =
        verifiedHolderIds.length > 0
            ? verifiedHolderIds[verifiedHolderIds.length - 1]
            : null;

    const holderProgressRadius = 7;

    const holderProgressCircumference =
        2 * Math.PI * holderProgressRadius;

    const holderProgressRatio =
        includedHolderCount > 0
            ? Math.min(
                verifiedHolderCount / includedHolderCount,
                1
            )
            : 0;

    const holderProgressOffset =
        holderProgressCircumference *
        (1 - holderProgressRatio);

    const totalUnshipQty = childHolders.reduce(
        (total, child) =>
            total + (Number(child.Qty) || 0),
        0
    );

    const unshipHolderNoun =
        includedHolderCount === 1 ? "holder" : "holders";

    useEffect(() => {
        if (!isUnshipConfirmationOpen) {
            return;
        }

        if (!isUnshipping) {
            unshipConfirmationCancelRef.current?.focus();
        }

        const handleKeyDown = (event: KeyboardEvent) => {
            if (
                event.key === "Escape" &&
                !isUnshipping
            ) {
                setIsUnshipConfirmationOpen(false);
                setUnshipError("");
            }
        };

        document.addEventListener(
            "keydown",
            handleKeyDown
        );

        return () => {
            document.removeEventListener(
                "keydown",
                handleKeyDown
            );
        };
    }, [isUnshipConfirmationOpen, isUnshipping]);

    const resetForNewShippingId = () => {
        setScanResult(null);
        setScanError("");
        setVerifiedHolders(new Set());
        setHolder("");
        setHolderNotFound(false);
        setUnshipError("");
        setUnshipSuccess("");
        setIsUnshipConfirmationOpen(false);
        setIsUnshipStepExpanded(true);
    };

    const handleShippingIdRescan = () => {
        if (scanning || isUnshipping) return;

        resetForNewShippingId();
    };

    const handleShippingIdChange = (value: string) => {
        setShippingId(value);
        resetForNewShippingId();
    };

    const handleShippingIdScan = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();

        const normalizedShippingId = shippingId.trim();
        if (!normalizedShippingId || scanning) {
            return;
        }

        if (!token) {
            setScanError("Login token is missing. Please sign in again.");
            return;
        }

        setScanning(true);
        setScanError("");
        setUnshipSuccess("");

        try {
            const result = await scanUnshipShippingIdApi(normalizedShippingId, token);
            setScanResult(result);
            setVerifiedHolders(new Set());
            setIsUnshipStepExpanded(true);
        } catch (error: unknown) {
            setScanResult(null);
            setScanError(
                error instanceof Error
                    ? error.message
                    : "Unable to load the child holders for this Shipping Id."
            );
        } finally {
            setScanning(false);
        }
    };

    const handleHolderChange = (value: string) => {
        setHolder(value);

        if (
            holderNotFound &&
            childHolders.some(
                (child) => child.Holder.trim().toUpperCase() === value.trim().toUpperCase()
            )
        ) {
            setHolderNotFound(false);
        }
    };

    const handleHolderVerify = (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();

        const normalizedHolder = holder.trim().toUpperCase();
        if (
            !normalizedHolder ||
            !scanResult ||
            allHoldersVerified ||
            isUnshipping
        ) {
            return;
        }

        const isLoadedHolder = childHolders.some(
            (child) => child.Holder.trim().toUpperCase() === normalizedHolder
        );

        if (!isLoadedHolder) {
            setHolderNotFound(true);
            return;
        }

        setHolderNotFound(false);
        setVerifiedHolders((current) => {
            const next = new Set(current);
            next.add(normalizedHolder);
            return next;
        });
        setHolder("");
    };

    const openUnshipConfirmation = () => {
        if (
            isUnshipping ||
            !allHoldersVerified ||
            !scanResult
        ) {
            return;
        }

        setUnshipError("");
        setIsUnshipConfirmationOpen(true);
    };

    const closeUnshipConfirmation = () => {
        if (isUnshipping) {
            return;
        }

        setIsUnshipConfirmationOpen(false);
        setUnshipError("");
    };

    const handleUnship = async () => {
        if (isUnshipping || !allHoldersVerified || !scanResult) {
            return;
        }

        if (!token) {
            setUnshipError("Login token is missing. Please sign in again.");
            return;
        }

        setIsUnshipping(true);
        setUnshipError("");
        setUnshipSuccess("");

        try {
            const result = await unshipFgiJobApi(scanResult.ShippingId, token);
            setIsUnshipConfirmationOpen(false);
            setShippingId("");
            setScanResult(null);
            setScanError("");
            setVerifiedHolders(new Set());
            setHolder("");
            setHolderNotFound(false);
            setUnshipError("");
            setIsUnshipStepExpanded(true);
            setUnshipSuccess(
                result.Message ||
                    `ShippingId '${result.ShippingId}' was unshipped successfully (${result.ProcessedHolderCount} holder(s)).`
            );
        } catch (error: unknown) {
            setUnshipError(
                error instanceof Error ? error.message : "Unable to unship the selected Shipping Id."
            );
        } finally {
            setIsUnshipping(false);
        }
    };

    const authError = token ? "" : "Login token is missing.";

    return (
        <div
            className="job-unship-workflow"
            aria-label="Job UnShip workflow"
        >
            {authError && (
                <div className="alert alert-danger mb-3" role="alert">
                    {authError}
                </div>
            )}

            <div className="job-unship-steps">
                {/* Step 1: Load Shipping Box */}
                <section
                    className={[
                        "job-unship-step",
                        scanResult ? "is-complete" : "is-active",
                    ].join(" ")}
                >
                    <div
                        className="job-unship-step-marker"
                        aria-hidden="true"
                    >
                        <span className="job-unship-step-number">
                            {scanResult ? (
                                <i className="fa-solid fa-check" />
                            ) : (
                                "1"
                            )}
                        </span>
                    </div>

                    <div
                        className={[
                            "job-unship-step-panel",
                            "withdrawal-verification-stage",
                            "withdrawal-shipping-stage",
                            scanResult ? "is-complete" : "is-active",
                        ].join(" ")}
                    >
                        <header className="withdrawal-verification-stage-header">
                            <div className="withdrawal-verification-stage-heading">
                                <strong>
                                    {scanResult ? (
                                        <>
                                            Shipping Box{" "}
                                            {scanResult.ShippingId} loaded
                                        </>
                                    ) : (
                                        "Load Shipping Box"
                                    )}
                                </strong>

                                {!scanResult && (
                                    <small>
                                        Scan a Shipping Box ID to load its child
                                        holders.
                                    </small>
                                )}
                            </div>

                            {scanResult ? (
                                <div className="withdrawal-verification-stage-actions">
                                    <button
                                        type="button"
                                        className="withdrawal-verification-rescan"
                                        onClick={handleShippingIdRescan}
                                        disabled={scanning || isUnshipping}
                                    >
                                        <i
                                            className="fa-solid fa-arrows-rotate"
                                            aria-hidden="true"
                                        />
                                        Re-scan
                                    </button>

                                    <i
                                        className="fa-solid fa-chevron-down withdrawal-verification-chevron"
                                        aria-hidden="true"
                                    />
                                </div>
                            ) : (
                                <i
                                    className="fa-solid fa-chevron-up withdrawal-verification-chevron"
                                    aria-hidden="true"
                                />
                            )}
                        </header>

                        {!scanResult && (
                            <div className="withdrawal-verification-stage-body">
                                <div className="withdrawal-verification-field">
                                    <label htmlFor="unship-shipping-id">
                                        SHIPPING BOX ID
                                    </label>

                                    <form
                                        className="withdrawal-verify-control"
                                        onSubmit={handleShippingIdScan}
                                        noValidate
                                    >
                                        <div className="withdrawal-scan-input-wrap">
                                            <input
                                                id="unship-shipping-id"
                                                type="text"
                                                className="form-control"
                                                value={shippingId}
                                                placeholder="Scan or enter Shipping Box ID"
                                                disabled={
                                                    scanning || isUnshipping
                                                }
                                                onChange={(event) =>
                                                    handleShippingIdChange(
                                                        event.target.value
                                                    )
                                                }
                                                autoComplete="off"
                                                aria-invalid={Boolean(scanError)}
                                                aria-describedby="unship-shipping-id-feedback"
                                            />

                                            <i
                                                className="fa-solid fa-barcode withdrawal-scan-input-icon"
                                                aria-hidden="true"
                                            />
                                        </div>

                                        <button
                                            type="submit"
                                            className="btn btn-primary withdrawal-verify-button"
                                            disabled={
                                                scanning ||
                                                isUnshipping ||
                                                !shippingId.trim()
                                            }
                                        >
                                            {scanning ? "LOADING..." : "LOAD"}
                                        </button>
                                    </form>

                                    <div
                                        id="unship-shipping-id-feedback"
                                        className={[
                                            "withdrawal-stage-message",
                                            scanError
                                                ? "is-error"
                                                : "is-guidance",
                                        ].join(" ")}
                                        role={scanError ? "alert" : "status"}
                                        aria-live="polite"
                                    >
                                        <i
                                            className={
                                                scanError
                                                    ? "fa-regular fa-circle-xmark"
                                                    : "fa-solid fa-circle-info"
                                            }
                                            aria-hidden="true"
                                        />

                                        <span>
                                            {scanError ||
                                                "Scan a Shipping Box ID to load its child holders."}
                                        </span>
                                    </div>
                                </div>
                            </div>
                        )}
                    </div>
                </section>

                {/* Step 2: Scan Child Holders */}
                <section
                    className={[
                        "job-unship-step",
                        !scanResult
                            ? "is-locked"
                            : allHoldersVerified
                              ? "is-complete"
                              : "is-active",
                    ].join(" ")}
                >
                    <div
                        className="job-unship-step-marker"
                        aria-hidden="true"
                    >
                        <span className="job-unship-step-number">
                            {!scanResult ? (
                                <i className="fa-solid fa-lock" />
                            ) : allHoldersVerified ? (
                                <i className="fa-solid fa-check" />
                            ) : (
                                "2"
                            )}
                        </span>
                    </div>

                    <div
                        className={[
                            "job-unship-step-panel",
                            "withdrawal-verification-stage",
                            "withdrawal-holder-stage",
                            !scanResult
                                ? "is-locked"
                                : allHoldersVerified
                                  ? "is-complete"
                                  : "is-active",
                        ].join(" ")}
                    >
                        <header className="withdrawal-verification-stage-header">
                            <div className="withdrawal-verification-stage-heading">
                                <strong>
                                    Scan Child Holders
                                    {scanResult && (
                                        <>
                                            {" "}({verifiedHolderCount} of{" "}
                                            {includedHolderCount})
                                        </>
                                    )}
                                </strong>

                                <small>
                                    {!scanResult ? (
                                        "Available after the Shipping Box ID is loaded."
                                    ) : allHoldersVerified ? (
                                        "All child holders have been verified."
                                    ) : (
                                        <>
                                            {remainingHolderCount}{" "}
                                            {remainingHolderCount === 1
                                                ? "holder"
                                                : "holders"}{" "}
                                            remaining.
                                        </>
                                    )}
                                </small>
                            </div>

                            <i
                                className={[
                                    "fa-solid",
                                    scanResult
                                        ? "fa-chevron-up"
                                        : "fa-chevron-down",
                                    "withdrawal-verification-chevron",
                                ].join(" ")}
                                aria-hidden="true"
                            />
                        </header>

                        {scanResult && (
                            <div className="withdrawal-verification-stage-body job-unship-holder-stage-content">
                                <div className="withdrawal-holder-stage-body">
                                    <div className="withdrawal-verification-scan">
                                        <label htmlFor="unship-holder">
                                            SCAN HOLDER BARCODE
                                        </label>

                                        <form
                                            className="withdrawal-verify-control"
                                            onSubmit={handleHolderVerify}
                                            noValidate
                                        >
                                            <div className="withdrawal-scan-input-wrap">
                                                <input
                                                    id="unship-holder"
                                                    type="text"
                                                    className="form-control"
                                                    value={holder}
                                                    placeholder="Scan or enter holder barcode"
                                                    disabled={
                                                        !scanResult ||
                                                        allHoldersVerified ||
                                                        isUnshipping
                                                    }
                                                    onChange={(event) =>
                                                        handleHolderChange(
                                                            event.target.value
                                                        )
                                                    }
                                                    autoComplete="off"
                                                    aria-invalid={holderNotFound}
                                                    aria-describedby="unship-holder-feedback"
                                                />

                                                <i
                                                    className="fa-solid fa-barcode withdrawal-scan-input-icon"
                                                    aria-hidden="true"
                                                />
                                            </div>

                                            <button
                                                type="submit"
                                                className="btn btn-primary withdrawal-verify-button"
                                                disabled={
                                                    !scanResult ||
                                                    allHoldersVerified ||
                                                    isUnshipping ||
                                                    !holder.trim()
                                                }
                                            >
                                                VERIFY
                                            </button>
                                        </form>

                                        <div
                                            id="unship-holder-feedback"
                                            className={[
                                                "withdrawal-stage-message",
                                                holderNotFound
                                                    ? "is-error"
                                                    : allHoldersVerified
                                                      ? "is-success"
                                                      : "is-guidance",
                                            ].join(" ")}
                                            role={
                                                holderNotFound
                                                    ? "alert"
                                                    : "status"
                                            }
                                            aria-live="polite"
                                        >
                                            <i
                                                className={
                                                    holderNotFound
                                                        ? "fa-regular fa-circle-xmark"
                                                        : allHoldersVerified
                                                          ? "fa-regular fa-circle-check"
                                                          : "fa-solid fa-circle-info"
                                                }
                                                aria-hidden="true"
                                            />

                                            <span>
                                                {holderNotFound
                                                    ? "Holder not found in the loaded list."
                                                    : allHoldersVerified
                                                      ? "All child holders verified."
                                                      : "Scan the next child holder."}
                                            </span>
                                        </div>
                                    </div>

                                    <div
                                        className="withdrawal-holder-progress-panel"
                                        role="status"
                                        aria-label={[
                                            verifiedHolderCount,
                                            "out of",
                                            includedHolderCount,
                                            "child holders verified",
                                        ].join(" ")}
                                    >
                                        <div className="withdrawal-holder-progress-ring-visual">
                                            <svg
                                                className="withdrawal-holder-progress-ring"
                                                viewBox="0 0 18 18"
                                                aria-hidden="true"
                                            >
                                                <circle
                                                    className="withdrawal-holder-progress-ring-track"
                                                    cx="9"
                                                    cy="9"
                                                    r={holderProgressRadius}
                                                />

                                                <circle
                                                    className="withdrawal-holder-progress-ring-value"
                                                    cx="9"
                                                    cy="9"
                                                    r={holderProgressRadius}
                                                    strokeDasharray={
                                                        holderProgressCircumference
                                                    }
                                                    strokeDashoffset={
                                                        holderProgressOffset
                                                    }
                                                    transform="rotate(-90 9 9)"
                                                />
                                            </svg>

                                            <strong>
                                                {verifiedHolderCount}/
                                                {includedHolderCount}
                                            </strong>
                                        </div>

                                        <div className="withdrawal-holder-progress-details">
                                            <strong>
                                                {verifiedHolderCount} of{" "}
                                                {includedHolderCount} child holders
                                                verified
                                            </strong>

                                            <span>
                                                {remainingHolderCount} remaining
                                            </span>

                                            {recentlyVerifiedHolder && (
                                                <div className="withdrawal-recently-verified">
                                                    <span>RECENTLY VERIFIED</span>

                                                    <div>
                                                        <i
                                                            className="fa-regular fa-circle-check"
                                                            aria-hidden="true"
                                                        />

                                                        <strong>
                                                            {recentlyVerifiedHolder}
                                                        </strong>
                                                    </div>
                                                </div>
                                            )}
                                        </div>
                                    </div>
                                </div>

                                <div className="table-responsive withdrawal-fifo-table-scroll is-scrollable job-unship-holder-table">
                                    <table className="table align-middle mb-0 withdrawal-fifo-table">
                                        <thead>
                                            <tr>
                                                <th scope="col">HOLDER</th>
                                                <th scope="col">PART NUM</th>
                                                <th scope="col">GRADE</th>
                                                <th scope="col">MODEL</th>
                                                <th scope="col">QTY</th>
                                                <th scope="col">STATUS</th>
                                            </tr>
                                        </thead>

                                        <tbody>
                                            {childHolders.map((child) => {
                                                const isScanned =
                                                    verifiedHolders.has(
                                                        child.Holder
                                                            .trim()
                                                            .toUpperCase()
                                                    );

                                                return (
                                                    <tr
                                                        key={child.Holder}
                                                        className={
                                                            isScanned
                                                                ? "is-verified"
                                                                : ""
                                                        }
                                                    >
                                                        <td>{child.Holder}</td>

                                                        <td>
                                                            {displayValue(
                                                                child.PartNumber
                                                            )}
                                                        </td>

                                                        <td>
                                                            {displayValue(
                                                                child.Grade
                                                            )}
                                                        </td>

                                                        <td>
                                                            {displayValue(
                                                                child.Model
                                                            )}
                                                        </td>

                                                        <td>
                                                            {displayValue(
                                                                child.Qty
                                                            )}
                                                        </td>

                                                        <td>
                                                            {isScanned ? (
                                                                <span className="withdrawal-fifo-status is-verified">
                                                                    SCANNED
                                                                </span>
                                                            ) : (
                                                                "—"
                                                            )}
                                                        </td>
                                                    </tr>
                                                );
                                            })}

                                            {childHolders.length === 0 && (
                                                <tr>
                                                    <td
                                                        colSpan={6}
                                                        className="text-center text-muted p-3"
                                                    >
                                                        No child holders were
                                                        loaded.
                                                    </td>
                                                </tr>
                                            )}
                                        </tbody>
                                    </table>
                                </div>
                            </div>
                        )}
                    </div>
                </section>
                {/* Step 3: Complete Unship */}
                <section
                    className={`job-unship-step ${allHoldersVerified
                            ? "is-active"
                            : "is-locked"
                        }`}
                >
                    <div
                        className="job-unship-step-marker"
                        aria-hidden="true"
                    >
                        <span className="job-unship-step-number">
                            3
                        </span>
                    </div>

                    <div className="job-unship-step-panel">
                        {!allHoldersVerified ? (
                            <div className="job-unship-locked-row">
                                <div className="job-unship-locked-copy">
                                    <i
                                        className="fa-solid fa-lock job-unship-lock-icon"
                                        aria-hidden="true"
                                    />

                                    <div>
                                        <h3>Complete Unship</h3>
                                        <p>
                                            Complete holder scanning to continue.
                                        </p>
                                    </div>
                                </div>

                                <i
                                    className="fa-solid fa-chevron-down job-unship-locked-chevron"
                                    aria-hidden="true"
                                />
                            </div>
                        ) : (
                            <div className="job-unship-step-content">
                                <div className="job-unship-step-header">
                                    <div>
                                        <h3>Complete Unship</h3>
                                    </div>

                                    <button
                                        type="button"
                                        className="job-unship-step-toggle"
                                        onClick={() =>
                                            setIsUnshipStepExpanded(
                                                (current) => !current
                                            )
                                        }
                                        aria-label={
                                            isUnshipStepExpanded
                                                ? "Collapse Complete Unship step"
                                                : "Expand Complete Unship step"
                                        }
                                        aria-expanded={
                                            isUnshipStepExpanded
                                        }
                                    >
                                        <i
                                            className={`fa-solid ${isUnshipStepExpanded
                                                    ? "fa-chevron-up"
                                                    : "fa-chevron-down"
                                                }`}
                                            aria-hidden="true"
                                        />
                                    </button>
                                </div>

                                {isUnshipStepExpanded && (
                                    <div className="job-unship-ready-bar">
                                        <div
                                            className="job-unship-ready-message"
                                            role="status"
                                            aria-live="polite"
                                        >
                                            <i
                                                className="fa-regular fa-circle-check"
                                                aria-hidden="true"
                                            />

                                            <span>
                                                All holders verified. Ready
                                                to unship.
                                            </span>
                                        </div>

                                        <button
                                            type="button"
                                            className="btn withdrawal-disassociate-button job-unship-submit"
                                            disabled={
                                                !allHoldersVerified ||
                                                isUnshipping
                                            }
                                                onClick={openUnshipConfirmation}
                                        >
                                            {isUnshipping
                                                ? "UNSHIPPING..."
                                                : "UNSHIP"}
                                        </button>
                                    </div>
                                )}
                            </div>
                        )}
                    </div>
                </section>
            </div>

            {isUnshipConfirmationOpen && scanResult && (
                <div
                    className="job-unship-confirmation-backdrop"
                    onMouseDown={(event) => {
                        if (
                            event.target === event.currentTarget
                        ) {
                            closeUnshipConfirmation();
                        }
                    }}
                >
                    <section
                        className="job-unship-confirmation"
                        role="alertdialog"
                        aria-modal="true"
                        aria-labelledby="job-unship-confirmation-title"
                        aria-describedby="job-unship-confirmation-warning"
                    >
                        <header className="job-unship-confirmation-header">
                            <div className="job-unship-confirmation-heading">
                                <span className="job-unship-confirmation-eyebrow">
                                    FINAL STEP
                                </span>

                                <h2 id="job-unship-confirmation-title">
                                    Unship Shipping Box?
                                </h2>
                            </div>

                            <div className="job-unship-confirmation-header-actions">
                                <span
                                    className="job-unship-confirmation-warning-icon"
                                    aria-hidden="true"
                                >
                                    <i className="fa-solid fa-exclamation" />
                                </span>

                                <button
                                    type="button"
                                    className="job-unship-confirmation-close"
                                    disabled={isUnshipping}
                                    onClick={closeUnshipConfirmation}
                                    aria-label="Close Unship confirmation"
                                >
                                    <i
                                        className="fa-solid fa-xmark"
                                        aria-hidden="true"
                                    />
                                </button>
                            </div>
                        </header>

                        <div className="job-unship-confirmation-body">
                            <div className="job-unship-confirmation-identity">
                                <span
                                    className="job-unship-confirmation-box-icon"
                                    aria-hidden="true"
                                >
                                    <i className="fa-solid fa-box" />
                                </span>

                                <div className="job-unship-confirmation-identity-copy">
                                    <strong>
                                        {scanResult.ShippingId}
                                    </strong>

                                    <div className="job-unship-confirmation-meta">
                                        <span>
                                            {verifiedHolderCount} verified{" "}
                                            {unshipHolderNoun}
                                        </span>

                                        <span>
                                            Total qty {totalUnshipQty}
                                        </span>
                                    </div>
                                </div>
                            </div>

                            <div className="job-unship-confirmation-consequences">
                                <div>
                                    <i
                                        className="fa-regular fa-trash-can"
                                        aria-hidden="true"
                                    />

                                    <span>
                                        Remove the {unshipHolderNoun} from
                                        this Shipping Box
                                    </span>
                                </div>

                                <div>
                                    <i
                                        className="fa-solid fa-arrow-rotate-left"
                                        aria-hidden="true"
                                    />

                                    <span>
                                        Return the {unshipHolderNoun} for
                                        reassignment
                                    </span>
                                </div>
                            </div>

                            <p
                                id="job-unship-confirmation-warning"
                                className="job-unship-confirmation-warning"
                            >
                                This action cannot be undone.
                            </p>

                            {unshipError && (
                                <div
                                    className="alert alert-danger job-unship-confirmation-error"
                                    role="alert"
                                >
                                    {unshipError}
                                </div>
                            )}
                        </div>

                        <footer className="job-unship-confirmation-footer">
                            <button
                                ref={unshipConfirmationCancelRef}
                                type="button"
                                className="btn btn-outline-secondary"
                                disabled={isUnshipping}
                                onClick={closeUnshipConfirmation}
                            >
                                CANCEL
                            </button>

                            <button
                                type="button"
                                className="btn withdrawal-disassociate-button"
                                disabled={isUnshipping}
                                onClick={() => void handleUnship()}
                            >
                                {isUnshipping && (
                                    <span
                                        className="spinner-border spinner-border-sm me-2"
                                        aria-hidden="true"
                                    />
                                )}

                                {isUnshipping
                                    ? "UNSHIPPING..."
                                    : "YES, UNSHIP"}
                            </button>
                        </footer>
                    </section>
                </div>
            )}

            {unshipError && !isUnshipConfirmationOpen && (
                <div
                    className="alert alert-danger job-unship-result"
                    role="alert"
                >
                    {unshipError}
                </div>
            )}

            {unshipSuccess && (
                <div
                    className="alert alert-success job-unship-result"
                    role="status"
                >
                    <i
                        className="fa-regular fa-circle-check"
                        aria-hidden="true"
                    />
                    <span>{unshipSuccess}</span>
                </div>
            )}
        </div>
    );
}
