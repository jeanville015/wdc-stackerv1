import { useEffect, useRef, useState } from "react";
import { useAuth } from "../../context/AuthContext";
import {
    getFgiWithdrawalLayoutApi,
    getFgiWithdrawalRequestsApi,
    acknowledgeFgiWithdrawalRequestApi,
    getFgiWithdrawalDisassociationPreviewApi,
    disassociateFgiWithdrawalRequestApi,
} from "../../api/withdrawalApi";
import type { CapacityConfig } from "../../types/models";
import type {
    FgiWithdrawalDisassociationPreview,
    FgiWithdrawalRack,
    FgiWithdrawalRequest,
} from "../../types/withdrawal";
import WithdrawalRequestTable from "./WithdrawalRequestTable";
import WithdrawalDisassociationModal from "./WithdrawalDisassociationModal";
import SelectedWithdrawalRequestPanel from "./SelectedWithdrawalRequestPanel";
import WithdrawalRackPanel from "./WithdrawalRackPanel";

interface Props {
    config: CapacityConfig;
}

export default function JobWithdrawalPanel({ config }: Props) {
    const { user } = useAuth();
    const hasToken = Boolean(user?.token);

    const [requests, setRequests] = useState<FgiWithdrawalRequest[]>([]);
    const [selectedRequest, setSelectedRequest] = useState<FgiWithdrawalRequest | null>(null);
    const [disassociationModal, setDisassociationModal] =
        useState<{
            request: FgiWithdrawalRequest;
            preview: FgiWithdrawalDisassociationPreview;
        } | null>(null);

    const [
        disassociationLoadingRequestId,
        setDisassociationLoadingRequestId,
    ] = useState<number | null>(null);

    const [disassociationError, setDisassociationError] = useState("");
    const [disassociationSuccess, setDisassociationSuccess] =
        useState("");
    const [rack, setRack] = useState<FgiWithdrawalRack | null>(null);

    const [requestsLoading, setRequestsLoading] = useState(hasToken);
    const [layoutLoading, setLayoutLoading] = useState(false);

    const [acknowledgingRequestId, setAcknowledgingRequestId] =
        useState<number | null>(null);
    const [acknowledgeError, setAcknowledgeError] = useState("");

    const [requestsError, setRequestsError] = useState("");
    const displayRequestsError =
        requestsError || (!hasToken ? "Login token is missing." : "");
    const [layoutError, setLayoutError] = useState("");

    /*
     * This prevents a slower, previously clicked row from overwriting
     * the result of a more recently clicked row.
     */
    const selectionSequenceRef = useRef(0);

    useEffect(() => {
        if (!user?.token) {
            return;
        }

        let cancelled = false;

        getFgiWithdrawalRequestsApi(user.token)
            .then((result) => {
                if (!cancelled) {
                    setRequestsError("");
                    setRequests(result);
                }
            })
            .catch((error: unknown) => {
                if (!cancelled) {
                    setRequestsError(
                        error instanceof Error
                            ? error.message
                            : "Unable to load withdrawal requests."
                    );
                }
            })
            .finally(() => {
                if (!cancelled) {
                    setRequestsLoading(false);
                }
            });

        return () => {
            cancelled = true;
        };
    }, [user?.token]);

    const handleRequestSelected = async (
        request: FgiWithdrawalRequest
    ) => {
        if (!user?.token) {
            setLayoutError("Login token is missing.");
            return;
        }

        setDisassociationSuccess("");

        setSelectedRequest(request);
        setRack(null);
        setLayoutError("");

        const selectionSequence = ++selectionSequenceRef.current;

        setLayoutLoading(true);

        try {
            const result = await getFgiWithdrawalLayoutApi(
                request.Lec,
                request.PenNum,
                request.SliderPartNumber,
                request.Grade,
                user.token
            );

            /*
             * Only apply the result if this is still the most recently
             * selected request.
             */
            if (selectionSequence === selectionSequenceRef.current) {
                setRack(result);
            }
        } catch (error: unknown) {
            if (selectionSequence === selectionSequenceRef.current) {
                setLayoutError(
                    error instanceof Error
                        ? error.message
                        : "Unable to load withdrawal layout."
                );
            }
        } finally {
            if (selectionSequence === selectionSequenceRef.current) {
                setLayoutLoading(false);
            }
        }
    };

    const handleRequestAcknowledged = async (
        request: FgiWithdrawalRequest
    ) => {
        if (!user?.token) {
            setAcknowledgeError("Login token is missing.");
            return;
        }

        setAcknowledgingRequestId(request.RequestId);
        setAcknowledgeError("");

        try {
            const result = await acknowledgeFgiWithdrawalRequestApi(
                request.RequestId,
                user.token
            );

            setRequests((current) =>
                current.map((item) =>
                    item.RequestId === request.RequestId
                        ? {
                            ...item,
                            AcknowledgeBy: result.AcknowledgeBy,
                        }
                        : item
                )
            );

            setSelectedRequest((current) =>
                current?.RequestId === request.RequestId
                    ? {
                        ...current,
                        AcknowledgeBy: result.AcknowledgeBy,
                    }
                    : current
            );
        } catch (error: unknown) {
            setAcknowledgeError(
                error instanceof Error
                    ? error.message
                    : "Unable to acknowledge the withdrawal request."
            );
        } finally {
            setAcknowledgingRequestId(null);
        }
    };

    const handleDisassociationRequested = async (
        request: FgiWithdrawalRequest
    ) => {
        setDisassociationSuccess("");
        setAcknowledgeError("");
        setDisassociationError("");
        setDisassociationModal(null);

        if (!user?.token) {
            setDisassociationError(
                "Login token is missing."
            );
            return;
        }

        if (request.Total === null) {
            setDisassociationError(
                "The selected request does not contain a TOTAL."
            );
            return;
        }

        setDisassociationLoadingRequestId(
            request.RequestId
        );

        try {
            const preview =
                await getFgiWithdrawalDisassociationPreviewApi(
                    request.Lec,
                    request.PenNum,
                    request.Total,
                    request.SliderPartNumber,
                    request.Grade,
                    request.ActualOutput ?? 0,
                    user.token
                );

            /*
             * Set both values together only after the SQL preview
             * and every included-holder FEATS check succeed.
             */
            setDisassociationModal({
                request,
                preview,
            });
        } catch (error: unknown) {
            setDisassociationError(
                error instanceof Error
                    ? error.message
                    : "Unable to prepare the disassociation preview."
            );
        } finally {
            setDisassociationLoadingRequestId(null);
        }
    };

    const handleDisassociationConfirmed = async (
        shippingId: string,
        includedHolders: string[]
    ): Promise<void> => {
        const modal = disassociationModal;

        if (!modal) {
            throw new Error(
                "The disassociation details are no longer available."
            );
        }

        if (!user?.token) {
            throw new Error(
                "Login token is missing. Please sign in again."
            );
        }

        if (!shippingId.trim()) {
            throw new Error(
                "ShippingId is required."
            );
        }

        if (includedHolders.length === 0) {
            throw new Error(
                "There are no included Holders to disassociate."
            );
        }

        setDisassociationError("");
        setDisassociationSuccess("");

        const result =
            await disassociateFgiWithdrawalRequestApi(
                modal.request.RequestId,
                shippingId,
                includedHolders,
                user.token
            );

        setDisassociationSuccess(
            result.Message ||
            "The Holders were removed from STACKER data."
        );

        /*
         * A refresh failure must not be reported as a failed
         * deletion because the transaction already committed.
         */
        setLayoutLoading(true);
        setRequestsLoading(true);

        try {
            const refreshedRack =
                await getFgiWithdrawalLayoutApi(
                    modal.request.Lec,
                    modal.request.PenNum,
                    modal.request.SliderPartNumber,
                    modal.request.Grade,
                    user.token
                );

            setRack(refreshedRack);
            setLayoutError("");
        } catch {
            setLayoutError(
                "The Holders were removed, but the withdrawal layout could not be refreshed."
            );
        } finally {
            setLayoutLoading(false);
        }

        try {
            const refreshedRequests = await getFgiWithdrawalRequestsApi(user.token);
            setRequests(refreshedRequests);

            // Update selected request if it still exists
            if (selectedRequest) {
                const updatedSelectedRequest = refreshedRequests.find(
                    (r) => r.RequestId === selectedRequest.RequestId
                );
                if (updatedSelectedRequest) {
                    setSelectedRequest(updatedSelectedRequest);
                }
            }
        } catch {
            // Request refresh failure should not block the success message
        } finally {
            setRequestsLoading(false);
        }
    };

    return (
        <>
            <div className="withdrawal-layout">
                <WithdrawalRequestTable
                    rows={requests}
                    selectedRequest={selectedRequest}
                    loading={requestsLoading}
                    error={displayRequestsError}
                    onRequestSelected={handleRequestSelected}
                />

                <section className="withdrawal-detail">
                    {selectedRequest && (
                        <SelectedWithdrawalRequestPanel
                            request={selectedRequest}
                            actionError={
                                acknowledgeError || disassociationError
                            }
                            acknowledgingRequestId={
                                acknowledgingRequestId
                            }
                            disassociationLoadingRequestId={
                                disassociationLoadingRequestId
                            }
                            onAcknowledge={
                                handleRequestAcknowledged
                            }
                            onWithdraw={
                                handleDisassociationRequested
                            }
                            actionSuccess={disassociationSuccess}
                            
                        />
                    )}
                    {!selectedRequest && !layoutError && (
                        <div className="withdrawal-placeholder">
                            Please select the request to show the black-boxes and
                            ship-boxes and holders subject for withdrawal.
                        </div>
                    )}

                    {selectedRequest && layoutLoading && (
                        <div className="withdrawal-placeholder">
                            <span
                                className="spinner-border spinner-border-sm"
                                role="status"
                                aria-hidden="true"
                            />

                            <span>Loading withdrawal layout...</span>
                        </div>
                    )}

                    {selectedRequest &&
                        !layoutLoading &&
                        layoutError && (
                            <div
                                className="withdrawal-placeholder"
                                role="alert"
                                style={{ color: "#d23232" }}
                            >
                                {layoutError}
                            </div>
                        )}

                    {selectedRequest &&
                        !layoutLoading &&
                        !layoutError &&
                        !rack && (
                            <div className="withdrawal-placeholder">
                                No holders are available for the selected request.
                            </div>
                        )}

                    {!layoutLoading && !layoutError && rack && (
                        <WithdrawalRackPanel
                            rack={rack}
                            rackLayerCount={config.LAYER_COUNT}
                            rackColumnCount={config.BOX_COUNT}
                            shipBoxLayerCount={
                                config["LAYER_COUNT-SHIPBOX"]
                            }
                            shipBoxColumnCount={
                                config["BOX_COUNT-SHIPBOX"]
                            }
                            maxItemPerShipBox={
                                config["MAX_ITEM_PER_BOX-SHIPBOX"]
                            }
                        />
                    )}
                </section>
             </div>
            {disassociationModal && (
                <WithdrawalDisassociationModal
                    request={disassociationModal.request}
                    preview={disassociationModal.preview}
                    onWithdraw={handleDisassociationConfirmed}
                    onClose={() =>
                        setDisassociationModal(null)
                    }
                />
            )}
        </>
    );
}
