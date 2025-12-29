/**
 * server.js
 * Express + Multer -> upload to Google Cloud Storage
 * Supports signed URLs when MAKE_PUBLIC=false
 *
 * Endpoints:
 *  POST /upload         -> upload 'file' form field -> returns { url, key, name, size, signedExpiresIn }
 *  DELETE /upload       -> delete file by body { key }
 *  GET /upload/signed-url?key=images/xxx.png -> returns { url } (fresh signed url)
 *
 * Requirements:
 *  - Set GOOGLE_APPLICATION_CREDENTIALS env var to your service account JSON file
 *  - Set GCS_BUCKET and related env variables (.env.example below)
 */

require('dotenv').config();
const express = require('express');
const multer = require('multer');
const { Storage } = require('@google-cloud/storage');
const path = require('path');
const cors = require('cors');

const app = express();
app.use(cors());
app.use(express.json()); // for DELETE body and other JSON endpoints

// env vars
const PORT = process.env.PORT || 3000;
const BUCKET = process.env.GCS_BUCKET;        // e.g., "landportal-images"
const GCS_PREFIX = process.env.GCS_PREFIX || 'uploads';
const MAKE_PUBLIC = process.env.MAKE_PUBLIC === 'true'; // if true -> make object public
const SIGNED_URL_EXPIRES_SECONDS = parseInt(process.env.SIGNED_URL_EXPIRES_SECONDS || '900', 10); // default 15 min

if (!BUCKET) {
  console.error('FATAL: GCS_BUCKET not set. See .env.example');
  process.exit(1);
}

// Initialize GCS client (uses GOOGLE_APPLICATION_CREDENTIALS)
const storage = new Storage();
const bucket = storage.bucket(BUCKET);

// Use multer memory storage
const upload = multer({
  storage: multer.memoryStorage(),
  limits: { fileSize: 50 * 1024 * 1024 } // 50MB, adjust as needed
});

// Helper: public URL
function publicUrl(bucketName, filename) {
  return `https://storage.googleapis.com/${bucketName}/${encodeURIComponent(filename)}`;
}

/**
 * POST /upload
 * Accepts single file field named 'file'
 */
app.post('/upload', upload.single('file'), async (req, res) => {
  try {
    if (!req.file) return res.status(400).json({ error: 'No file uploaded' });

    // build unique destination name
    const safeName = req.file.originalname.replace(/\s+/g, '_');
    const unique = `${Date.now()}-${Math.round(Math.random() * 1e6)}-${safeName}`;
    const destination = path.posix.join(GCS_PREFIX, unique);

    const file = bucket.file(destination);

    // create write stream to GCS
    const stream = file.createWriteStream({
      metadata: {
        contentType: req.file.mimetype
      }
    });

    stream.on('error', (err) => {
      console.error('Upload stream error', err);
      return res.status(500).json({ error: 'Upload failed' });
    });

    stream.on('finish', async () => {
      try {
        if (MAKE_PUBLIC) {
          // make object public
          await file.makePublic();
          const url = publicUrl(BUCKET, destination);
          return res.json({
            url,
            key: destination,
            name: req.file.originalname,
            size: req.file.size
          });
        } else {
          // generate signed URL v4
          const [signedUrl] = await file.getSignedUrl({
            version: 'v4',
            action: 'read',
            expires: Date.now() + SIGNED_URL_EXPIRES_SECONDS * 1000
          });

          return res.json({
            url: signedUrl,
            key: destination,
            name: req.file.originalname,
            size: req.file.size,
            signedExpiresIn: SIGNED_URL_EXPIRES_SECONDS
          });
        }
      } catch (err) {
        console.error('Post-upload processing error', err);
        return res.status(500).json({ error: 'Post-upload processing failed' });
      }
    });

    // upload buffer to stream
    stream.end(req.file.buffer);
  } catch (err) {
    console.error('Upload endpoint error', err);
    res.status(500).json({ error: 'Server error' });
  }
});

/**
 * DELETE /upload
 * Body: { key: 'images/xxxx.png' }
 */
app.delete('/upload', async (req, res) => {
  try {
    const { key } = req.body;
    if (!key) return res.status(400).json({ error: 'Missing key' });

    const file = bucket.file(String(key));
    const [exists] = await file.exists();
    if (!exists) return res.status(404).json({ error: 'Not found' });

    await file.delete();
    return res.json({ ok: true });
  } catch (err) {
    console.error('Delete error', err);
    return res.status(500).json({ error: 'Delete failed' });
  }
});

/**
 * GET /upload/signed-url?key=images/xxx.png
 * Returns fresh signed read URL for a stored object (useful if urls expire)
 */
app.get('/upload/signed-url', async (req, res) => {
  try {
    const key = req.query.key;
    if (!key) return res.status(400).json({ error: 'Missing key' });

    const file = bucket.file(String(key));
    const [exists] = await file.exists();
    if (!exists) return res.status(404).json({ error: 'Not found' });

    const [signedUrl] = await file.getSignedUrl({
      version: 'v4',
      action: 'read',
      expires: Date.now() + SIGNED_URL_EXPIRES_SECONDS * 1000
    });

    res.json({ url: signedUrl, expiresIn: SIGNED_URL_EXPIRES_SECONDS });
  } catch (err) {
    console.error('Signed URL generation error', err);
    res.status(500).json({ error: 'Failed to generate signed url' });
  }
});

app.get('/', (req, res) => res.json({ ok: true, message: 'Upload server (signed urls) running' }));

app.listen(PORT, () => {
  console.log(`Upload server listening on port ${PORT}`);
  console.log(`GCS_BUCKET=${BUCKET}  GCS_PREFIX=${GCS_PREFIX}  MAKE_PUBLIC=${MAKE_PUBLIC}`);
});

