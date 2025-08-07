#!/bin/bash

# SSL Certificate Generation Script
# Usage: ./generate-ssl-certs.sh [output_directory] [cert_name]

set -e

# Configuration with defaults
OUTPUT_DIR=${1:-"./ssl-certs"}
CERT_NAME=${2:-"server"}
CERT_VALIDITY_DAYS=${SSL_CERT_VALIDITY_DAYS:-365}
CERT_COUNTRY=${SSL_CERT_COUNTRY:-"US"}
CERT_STATE=${SSL_CERT_STATE:-"CA"}
CERT_CITY=${SSL_CERT_CITY:-"San Francisco"}
CERT_ORG=${SSL_CERT_ORG:-"MyOrg"}
CERT_OU=${SSL_CERT_OU:-"IT"}
CERT_CN=${SSL_CERT_CN:-"localhost"}

echo "🔐 Generating SSL certificates..."
echo "   Output directory: $OUTPUT_DIR"
echo "   Certificate name: $CERT_NAME"
echo "   Validity: $CERT_VALIDITY_DAYS days"
echo "   Common Name: $CERT_CN"

# Create output directory
mkdir -p "$OUTPUT_DIR"

# Generate private key
echo "📝 Generating private key..."
openssl genrsa -out "$OUTPUT_DIR/${CERT_NAME}.key.pem" 2048

# Generate certificate signing request
echo "📝 Generating certificate signing request..."
openssl req -new \
    -key "$OUTPUT_DIR/${CERT_NAME}.key.pem" \
    -out "$OUTPUT_DIR/${CERT_NAME}.csr" \
    -subj "/C=$CERT_COUNTRY/ST=$CERT_STATE/L=$CERT_CITY/O=$CERT_ORG/OU=$CERT_OU/CN=$CERT_CN"

# Generate self-signed certificate
echo "📝 Generating self-signed certificate..."
openssl x509 -req \
    -days $CERT_VALIDITY_DAYS \
    -in "$OUTPUT_DIR/${CERT_NAME}.csr" \
    -signkey "$OUTPUT_DIR/${CERT_NAME}.key.pem" \
    -out "$OUTPUT_DIR/${CERT_NAME}.cert.pem"

# Set proper permissions
chmod 600 "$OUTPUT_DIR/${CERT_NAME}.key.pem"
chmod 644 "$OUTPUT_DIR/${CERT_NAME}.cert.pem"

# Cleanup CSR file
rm "$OUTPUT_DIR/${CERT_NAME}.csr"

echo "✅ SSL certificates generated successfully!"
echo "   📜 Certificate: $OUTPUT_DIR/${CERT_NAME}.cert.pem"
echo "   🔑 Private Key: $OUTPUT_DIR/${CERT_NAME}.key.pem"
echo ""
echo "📋 Certificate Information:"
openssl x509 -in "$OUTPUT_DIR/${CERT_NAME}.cert.pem" -text -noout | grep -E "(Subject:|Not Before|Not After)"

echo ""
echo "🚀 Usage with Docker:"
echo "   docker build --build-arg SSL_CERT_PATH=$OUTPUT_DIR/${CERT_NAME}.cert.pem --build-arg SSL_KEY_PATH=$OUTPUT_DIR/${CERT_NAME}.key.pem -t my-app ."