let deferredPrompt;
let isInstalled = false;

// PWA 설치 이벤트 리스너
window.addEventListener('beforeinstallprompt', (e) => {
    console.log('beforeinstallprompt 이벤트가 발생했습니다.');
    e.preventDefault();
    deferredPrompt = e;
});

// 앱이 설치되었을 때
window.addEventListener('appinstalled', (e) => {
    console.log('PWA가 설치되었습니다.');
    isInstalled = true;
    deferredPrompt = null;
});

// 설치 가능 여부 확인
export function canInstall() {
    // 이미 설치되어 있거나 deferredPrompt가 없으면 false
    if (isInstalled || !deferredPrompt) {
        return false;
    }
    
    // 이미 standalone 모드로 실행 중이면 false (이미 설치됨)
    if (window.matchMedia('(display-mode: standalone)').matches) {
        return false;
    }
    
    return true;
}

// 앱 설치 실행
export async function installApp() {
    if (!deferredPrompt) {
        console.log('설치 프롬프트가 없습니다.');
        return false;
    }
    
    try {
        deferredPrompt.prompt();
        const { outcome } = await deferredPrompt.userChoice;
        
        if (outcome === 'accepted') {
            console.log('사용자가 PWA 설치를 수락했습니다.');
            isInstalled = true;
        } else {
            console.log('사용자가 PWA 설치를 거부했습니다.');
        }
        
        deferredPrompt = null;
        return outcome === 'accepted';
    } catch (error) {
        console.error('PWA 설치 중 오류가 발생했습니다:', error);
        return false;
    }
}

// 현재 앱이 PWA로 실행 중인지 확인
export function isPWA() {
    return window.matchMedia('(display-mode: standalone)').matches || 
           window.navigator.standalone === true;
}

// PWA 상태 정보 반환
export function getPWAInfo() {
    return {
        isInstalled: isInstalled,
        isPWAMode: isPWA(),
        canInstall: canInstall(),
        hasServiceWorker: 'serviceWorker' in navigator
    };
}
