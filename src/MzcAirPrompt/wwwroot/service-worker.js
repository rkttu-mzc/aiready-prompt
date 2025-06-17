// 개발 환경에서는 캐싱을 비활성화합니다.
// 이는 개발을 더 쉽게 만들기 위함입니다 (변경사항이 첫 번째 로드 후 바로 반영됩니다).

const CACHE_NAME = 'aiready-prompt-dev-cache';

// 개발 환경에서는 모든 요청을 네트워크에서 가져옵니다
self.addEventListener('fetch', (event) => {
    // 개발 환경에서는 캐시를 사용하지 않고 항상 네트워크에서 가져옵니다
    console.log('Development mode: 네트워크에서 리소스를 가져옵니다:', event.request.url);
});

// 서비스 워커 설치 이벤트
self.addEventListener('install', (event) => {
    console.log('Service Worker: 설치됨');
    self.skipWaiting(); // 즉시 활성화
});

// 서비스 워커 활성화 이벤트
self.addEventListener('activate', (event) => {
    console.log('Service Worker: 활성화됨');
    event.waitUntil(self.clients.claim()); // 모든 클라이언트 제어
});

// 백그라운드 동기화 (향후 사용을 위해 준비)
self.addEventListener('sync', (event) => {
    console.log('Service Worker: 백그라운드 동기화', event.tag);
});

// 푸시 메시지 처리 (향후 사용을 위해 준비)
self.addEventListener('push', (event) => {
    console.log('Service Worker: 푸시 메시지 수신', event);
});
