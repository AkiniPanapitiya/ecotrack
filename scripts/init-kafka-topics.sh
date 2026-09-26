#!/bin/bash
# =============================================================================
# EcoTrack Kafka Topic Initialization Script
# Provisions Sprint 3 Event Topics, Partitions, and Dead-Letter Topics (.dlq)
# =============================================================================

set -e

BOOTSTRAP_SERVER="${KAFKA_BOOTSTRAP_SERVER:-kafka:9092}"
PARTITIONS=3
REPLICATION_FACTOR=1

echo "Waiting for Kafka broker at $BOOTSTRAP_SERVER to be ready..."
until /opt/kafka/bin/kafka-topics.sh --bootstrap-server "$BOOTSTRAP_SERVER" --list > /dev/null 2>&1; do
    echo "Kafka is unavailable - sleeping 2 seconds..."
    sleep 2
done

echo "Kafka broker is reachable. Initializing Sprint 3 Domain and DLQ topics..."

TOPICS=(
    "recycler.verification.changed"
    "recycler.verification.changed.dlq"
    "marketplace.order.placed"
    "marketplace.order.placed.dlq"
    "ewaste.disposal.certified"
    "ewaste.disposal.certified.dlq"
)

for TOPIC in "${TOPICS[@]}"; do
    echo "Creating topic: $TOPIC (Partitions: $PARTITIONS, Replication: $REPLICATION_FACTOR)"
    /opt/kafka/bin/kafka-topics.sh \
        --bootstrap-server "$BOOTSTRAP_SERVER" \
        --create \
        --if-not-exists \
        --topic "$TOPIC" \
        --partitions "$PARTITIONS" \
        --replication-factor "$REPLICATION_FACTOR"
done

echo "============================================================================="
echo "Kafka topics provisioned successfully:"
/opt/kafka/bin/kafka-topics.sh --bootstrap-server "$BOOTSTRAP_SERVER" --list
echo "============================================================================="
