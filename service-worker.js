/* Manifest version: zLdOLcwS */
// Caution! Be sure you understand the caveats before publishing an application with
// offline support. See https://aka.ms/blazor-offline-considerations

self.importScripts('./service-worker-assets.js');
self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));
self.addEventListener('message', event => event.waitUntil(onMessage(event)));

// PWA 관련 이벤트 리스너 추가
self.addEventListener('sync', event => event.waitUntil(onBackgroundSync(event)));
self.addEventListener('push', event => event.waitUntil(onPushMessage(event)));

const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
const offlineAssetsInclude = [ /\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/, /\.md$/ ];
const offlineAssetsExclude = [ /^service-worker\.js$/ ];

// Replace with your base path if you are hosting on a subfolder. Ensure there is a trailing '/'.
const base = "/";
const baseUrl = new URL(base, self.origin);
const manifestUrlList = self.assetsManifest.assets.map(asset => new URL(asset.url, baseUrl).href);

async function onInstall(event) {
    console.info('Service worker: Install');
    
    // 새로운 버전이 설치될 때 클라이언트에 알림
    const clients = await self.clients.matchAll();
    clients.forEach(client => {
        client.postMessage({
            type: 'UPDATE_AVAILABLE',
            version: self.assetsManifest.version
        });
    });
    
    // 즉시 활성화하지 않고 대기 (사용자가 새로고침할 때까지)
    // self.skipWaiting();

    // Fetch and cache all matching items from the assets manifest
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
        .map(asset => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));
    await caches.open(cacheName).then(cache => cache.addAll(assetsRequests));
}

async function onActivate(event) {
    console.info('Service worker: Activate');
    
    // 모든 클라이언트 제어
    self.clients.claim();

    // Delete unused caches
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
        .map(key => caches.delete(key)));
        
    // 클라이언트에 활성화 알림
    const clients = await self.clients.matchAll();
    clients.forEach(client => {
        client.postMessage({
            type: 'SW_ACTIVATED',
            version: self.assetsManifest.version
        });
    });
}

async function onFetch(event) {
    let cachedResponse = null;
    if (event.request.method === 'GET') {
        // For all navigation requests, try to serve index.html from cache,
        // unless that request is for an offline resource.
        // If you need some URLs to be server-rendered, edit the following check to exclude those URLs
        const shouldServeIndexHtml = event.request.mode === 'navigate'
            && !manifestUrlList.some(url => url === event.request.url);

        const request = shouldServeIndexHtml ? 'index.html' : event.request;
        const cache = await caches.open(cacheName);
        cachedResponse = await cache.match(request);
    }

    // 네트워크 우선 전략으로 변경하여 최신 데이터 보장
    try {
        const fetchResponse = await fetch(event.request);
        // 성공적으로 가져온 경우 캐시 업데이트
        if (fetchResponse && fetchResponse.status === 200 && event.request.method === 'GET') {
            const cache = await caches.open(cacheName);
            cache.put(event.request, fetchResponse.clone());
        }
        return fetchResponse;
    } catch (error) {
        // 네트워크 실패 시 캐시에서 반환
        console.log('네트워크 요청 실패, 캐시에서 응답:', event.request.url);
        return cachedResponse || new Response('오프라인 상태입니다.', { 
            status: 503, 
            statusText: 'Service Unavailable' 
        });
    }
}

// 백그라운드 동기화 처리
async function onBackgroundSync(event) {
    console.log('백그라운드 동기화:', event.tag);
    // 향후 오프라인 데이터 동기화 기능을 여기에 구현할 수 있습니다.
}

// 푸시 메시지 처리
async function onPushMessage(event) {
    console.log('푸시 메시지 수신:', event);
    // 향후 푸시 알림 기능을 여기에 구현할 수 있습니다.
}

// 클라이언트 메시지 처리
async function onMessage(event) {
    console.log('Service worker received message:', event.data);
    
    if (event.data && event.data.type === 'SKIP_WAITING') {
        // 클라이언트가 업데이트를 승인했을 때 즉시 활성화
        self.skipWaiting();
        
        // 모든 클라이언트에 새로고침 요청
        const clients = await self.clients.matchAll();
        clients.forEach(client => {
            client.postMessage({
                type: 'RELOAD_REQUEST'
            });
        });
    }
}
