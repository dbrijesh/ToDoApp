#!/bin/bash

# Configuration
CERT_DIR="/app/ssl"
CERT_VALIDITY_DAYS=${SSL_CERT_VALIDITY_DAYS:-365}
CERT_COUNTRY=${SSL_CERT_COUNTRY:-"US"}
CERT_STATE=${SSL_CERT_STATE:-"CA"}
CERT_CITY=${SSL_CERT_CITY:-"San Francisco"}
CERT_ORG=${SSL_CERT_ORG:-"MyOrg"}
CERT_OU=${SSL_CERT_OU:-"IT"}
CERT_CN=${SSL_CERT_CN:-"localhost"}

# Create SSL directory
mkdir -p $CERT_DIR

# Generate private key
openssl genrsa -out $CERT_DIR/server.key 2048

# Generate certificate signing request
openssl req -new -key $CERT_DIR/server.key -out $CERT_DIR/server.csr -subj "/C=$CERT_COUNTRY/ST=$CERT_STATE/L=$CERT_CITY/O=$CERT_ORG/OU=$CERT_OU/CN=$CERT_CN"

# Generate self-signed certificate
openssl x509 -req -days $CERT_VALIDITY_DAYS -in $CERT_DIR/server.csr -signkey $CERT_DIR/server.key -out $CERT_DIR/server.crt

# Set proper permissions
chmod 600 $CERT_DIR/server.key
chmod 644 $CERT_DIR/server.crt

echo "SSL certificate generated successfully!"
echo "Certificate: $CERT_DIR/server.crt"
echo "Private Key: $CERT_DIR/server.key"
echo "Valid for: $CERT_VALIDITY_DAYS days"