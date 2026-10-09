#!/bin/bash
# =============================================================================
# EcoTrack Kafka Topic Initialization Script (Sprint 3 + Sprint 4)
# For every main topic, a matching dead-letter topic (<topic>.dlq) is created.
# =============================================================================

set -e

BOOTSTRAP_SERVER="${KAFKA_BOOTSTRAP_SERVER:-kafka:9092}"
PARTITIONS=3
REPLICATION_FACTOR=1
MAIN_RETENTION_MS=604800000
DLQ_RETENTION_MS=1209600000

echo "Waiting for Kafka broker at $BOOTSTRAP_SERVER to be ready..."
until /opt/kafka/bin/kafka-topics.sh --bootstrap-server "$BOOTSTRAP_SERVER" --list > /dev/null 2>&1; do
    echo "Kafka is unavailable - sleeping 2 seconds..."
    sleep 2
done

echo "Kafka broker is reachable. Creating EcoTrack topics..."

MAIN_TOPICS=(
    "recycler.verification.changed"
    "marketplace.order.placed"
    "ewaste.disposal.certified"
    "logistics.pickup.scheduled"
    "logistics.pickup.collected"
    "marketplace.payment.status.changed"
    "ewaste.item.disposed"
    "ewaste.item.refurbished"
)

create_topic() {
    local TOPIC_NAME="$1"
    local RETENTION="$2"
    echo "Creating topic: $TOPIC_NAME (retention: $RETENTION ms)"
    /opt/kafka/bin/kafka-topics.sh \
        --bootstrap-server "$BOOTSTRAP_SERVER" \
        --create \
        --if-not-exists \
        --topic "$TOPIC_NAME" \
        --partitions "$PARTITIONS" \
        --replication-factor "$REPLICATION_FACTOR" \
        --config retention.ms="$RETENTION"
}

for TOPIC in "${MAIN_TOPICS[@]}"; do
    create_topic "$TOPIC" "$MAIN_RETENTION_MS"
    create_topic "$TOPIC.dlq" "$DLQ_RETENTION_MS"
done

echo "============================================================================="
echo "Kafka topics provisioned successfully:"
/opt/kafka/bin/kafka-topics.sh --bootstrap-server "$BOOTSTRAP_SERVER" --list
echo "============================================================================="