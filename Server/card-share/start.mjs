// Starts a temporary public HTTPS service for the Unity Editor demo.
// Flow: start loopback server -> start tunnel -> read public URL -> write Unity config.
import { spawn } from 'node:child_process';
import { randomBytes } from 'node:crypto';
import { createServer } from 'node:http';
import { mkdir, writeFile, unlink } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { FileStore } from './src/file-store.mjs';
import { createCardHandler } from './src/app.mjs';

const port = Number(process.env.PORT || 8788);
const startupTimeoutMs = 60_000;
const cleanupIntervalMs = 60_000;
const uploadKey = randomBytes(32).toString('base64url');
const streamingAssetsDirectory = new URL('../../Assets/StreamingAssets/', import.meta.url);
const stateFile = new URL('./online-state.json', import.meta.url);
const unityConfigFile = new URL('photo-booth-share.json', streamingAssetsDirectory);
const imageDirectory = fileURLToPath(new URL('./data/', import.meta.url));

let tunnelProcess;
let requestHandler;
let isStopping = false;
let hasPublicUrl = false;
let tunnelLogBuffer = '';

async function writeState(state) {
  await writeFile(stateFile, JSON.stringify(state));
}

await mkdir(streamingAssetsDirectory, { recursive: true });
await writeState({ status: 'starting' });

const imageStore = new FileStore(imageDirectory);
await imageStore.init();
await imageStore.cleanup();

const server = createServer(handleRequest);
await new Promise((resolve, reject) => {
  server.once('error', reject);
  server.listen(port, '127.0.0.1', resolve);
});

const cleanupTimer = setInterval(() => {
  imageStore.cleanup().catch(() => console.error('Expired-image cleanup failed'));
}, cleanupIntervalMs);
cleanupTimer.unref();

function handleRequest(request, response) {
  if (!requestHandler) {
    response.writeHead(503);
    response.end('Starting online photo service');
    return;
  }

  // Unity uses the same per-run credential to stop the demo server.
  const isStopRequest = request.url === '/__stop' && request.method === 'POST';
  const isAuthorized = request.headers.authorization === 'Bearer ' + uploadKey;
  if (isStopRequest && isAuthorized) {
    response.end('Stopped');
    setTimeout(stopService, 50);
    return;
  }

  requestHandler(request, response);
}

async function stopService() {
  if (isStopping) {
    return;
  }

  isStopping = true;
  clearInterval(cleanupTimer);
  tunnelProcess?.kill();
  server.close();
  await writeState({ status: 'stopped' });

  // Remove the stale temporary URL; permanent settings live in a different file.
  await unlink(unityConfigFile).catch(() => {});
  setTimeout(() => process.exit(), 100).unref();
}

async function reportFailure(error) {
  console.error(error.message);
  await writeState({ status: 'error', message: error.message });
  await stopService();
}

process.on('SIGINT', stopService);
process.on('SIGTERM', stopService);

const tunnelExecutable = process.env.CLOUDFLARED_PATH
  || fileURLToPath(new URL('../../Tools/cloudflared.exe', import.meta.url));
const tunnelArguments = [
  'tunnel', '--no-autoupdate', '--protocol', 'http2',
  '--url', `http://127.0.0.1:${port}`,
];
tunnelProcess = spawn(tunnelExecutable, tunnelArguments, {
  windowsHide: true,
  stdio: ['ignore', 'pipe', 'pipe'],
});

async function handleTunnelOutput(chunk) {
  process.stdout.write(chunk);
  // Keep a bounded buffer because log chunks can split a URL across two events.
  tunnelLogBuffer = (tunnelLogBuffer + chunk.toString()).slice(-16_000);
  const publicUrl = tunnelLogBuffer.match(/https:\/\/(?!api\.)[a-z0-9-]+\.trycloudflare\.com/)?.[0];
  if (!publicUrl || hasPublicUrl) {
    return;
  }

  hasPublicUrl = true;
  requestHandler = createCardHandler({
    store: imageStore,
    kioskKey: uploadKey,
    publicBaseUrl: publicUrl,
  });

  const unityConfig = {
    serviceBaseUrl: publicUrl,
    kioskKey: uploadKey,
    timeoutSeconds: 45,
  };
  await writeFile(unityConfigFile, JSON.stringify(unityConfig, null, 2));
  await writeState({ status: 'ready', url: publicUrl, port });
  console.log(`ONLINE PHOTO BOOTH READY: ${publicUrl}`);
}

function forwardTunnelOutput(chunk) {
  handleTunnelOutput(chunk).catch(reportFailure);
}

tunnelProcess.stdout.on('data', forwardTunnelOutput);
tunnelProcess.stderr.on('data', forwardTunnelOutput);
tunnelProcess.on('error', reportFailure);
tunnelProcess.on('exit', () => {
  if (!isStopping) {
    reportFailure(new Error('HTTPS tunnel exited. Restart online test.'));
  }
});

setTimeout(() => {
  if (!hasPublicUrl) {
    reportFailure(new Error('HTTPS tunnel did not start in 60 seconds.'));
  }
}, startupTimeoutMs).unref();
