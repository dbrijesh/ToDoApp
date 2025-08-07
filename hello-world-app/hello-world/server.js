const express = require('express');
const path = require('path');
const https = require('https');
const http = require('http');
const fs = require('fs');

const app = express();
const port = process.env.PORT || 4000;
const distFolder = path.join(__dirname, 'dist/hello-world/browser');

// Serve static files
app.use(express.static(distFolder));

// Health check
app.get('/health', (req, res) => res.send('healthy'));

// Serve index.html for all routes
app.get('**', (req, res) => res.sendFile(path.join(distFolder, 'index.html')));

// Start server with HTTPS if certificates exist
const certPath = '/app/ssl/server.cert.pem';
const keyPath = '/app/ssl/server.key.pem';

try {
  if (fs.existsSync(certPath) && fs.existsSync(keyPath)) {
    const key = fs.readFileSync(keyPath, 'utf8');
    const cert = fs.readFileSync(certPath, 'utf8');
    https.createServer({ key, cert }, app).listen(port, () => {
      console.log(`🚀 HTTPS Server running on https://localhost:${port}`);
    });
  } else {
    http.createServer(app).listen(port, () => {
      console.log(`🚀 HTTP Server running on http://localhost:${port}`);
    });
  }
} catch (error) {
  http.createServer(app).listen(port, () => {
    console.log(`🚀 HTTP Server running on http://localhost:${port}`);
  });
}