// update-notification.js
let dotnetHelper = null;
let hasShownNotification = false;

export function initialize(dotnetObjectReference) {
    dotnetHelper = dotnetObjectReference;
    console.log('Update notification initialized with dotnet helper');
    
    // Service Worker 등록 및 업데이트 감지
    if ('serviceWorker' in navigator) {
        navigator.serviceWorker.ready.then(registration => {
            console.log('Service Worker ready, checking for updates...');
            
            // 새로운 Service Worker가 설치될 때
            registration.addEventListener('updatefound', () => {
                console.log('New service worker found!');
                const newWorker = registration.installing;
                
                if (newWorker) {
                    newWorker.addEventListener('statechange', () => {
                        if (newWorker.state === 'installed' && navigator.serviceWorker.controller) {
                            console.log('New service worker installed, showing update notification');
                            showUpdateNotification();
                        }
                    });
                }
            });
            
            // 새로운 Service Worker가 제어권을 가져갈 때
            let refreshing = false;
            navigator.serviceWorker.addEventListener('controllerchange', () => {
                if (!refreshing) {
                    console.log('Service worker controller changed');
                    refreshing = true;
                    // 자동으로 새로고침하지 않고 알림만 표시
                    showUpdateNotification();
                }
            });
            
            // 수동으로 업데이트 확인
            checkForUpdates(registration);
        });
        
        // Service Worker 메시지 수신
        navigator.serviceWorker.addEventListener('message', event => {
            console.log('Message received from service worker:', event.data);
            if (event.data && event.data.type === 'UPDATE_AVAILABLE') {
                console.log('Update message received from service worker');
                showUpdateNotification();
            } else if (event.data && event.data.type === 'RELOAD_REQUEST') {
                console.log('Reload request received from service worker');
                window.location.reload();
            }
        });
    }
    
    // 커스텀 이벤트 리스너 추가 (백업 방법)
    window.addEventListener('sw-update-available', () => {
        console.log('Custom update event received');
        showUpdateNotification();
    });
    
    // 전역 함수로 노출 (테스트용)
    window.showUpdateNotificationTest = () => {
        console.log('Manual test function called');
        showUpdateNotification();
    };
    
    // 주기적으로 업데이트 확인 (5분마다)
    setInterval(() => {
        if ('serviceWorker' in navigator) {
            navigator.serviceWorker.ready.then(registration => {
                checkForUpdates(registration);
            });
        }
    }, 5 * 60 * 1000); // 5분
}

function checkForUpdates(registration) {
    // 업데이트 확인
    registration.update().then(() => {
        console.log('Manual update check completed');
    }).catch(error => {
        console.log('Update check failed:', error);
    });
}

function showUpdateNotification() {
    console.log('showUpdateNotification called, hasShownNotification:', hasShownNotification, 'dotnetHelper:', !!dotnetHelper);
    
    if (!hasShownNotification && dotnetHelper) {
        console.log('Calling Blazor component to show notification');
        hasShownNotification = true;
        
        try {
            dotnetHelper.invokeMethodAsync('ShowUpdateNotification')
                .then(() => {
                    console.log('ShowUpdateNotification method called successfully');
                })
                .catch(error => {
                    console.error('Error calling ShowUpdateNotification:', error);
                    hasShownNotification = false; // 실패시 다시 시도할 수 있도록
                });
        } catch (error) {
            console.error('Exception calling ShowUpdateNotification:', error);
            hasShownNotification = false;
        }
    } else if (!dotnetHelper) {
        console.error('dotnetHelper is not available');
        
        // 백업 방법: 직접 DOM 조작
        setTimeout(() => {
            const updateElements = document.querySelectorAll('.update-notification');
            console.log('Found update notification elements:', updateElements.length);
            updateElements.forEach(el => {
                el.classList.add('show');
                el.style.transform = 'translateX(0)';
            });
        }, 100);
    } else {
        console.log('Notification already shown or conditions not met');
    }
}

// 사용자가 업데이트를 승인했을 때 호출되는 함수
export function acceptUpdate() {
    if ('serviceWorker' in navigator && navigator.serviceWorker.controller) {
        navigator.serviceWorker.controller.postMessage({
            type: 'SKIP_WAITING'
        });
    } else {
        // Service Worker가 없으면 바로 새로고침
        window.location.reload();
    }
}

// 페이지 가시성 변경 시 업데이트 확인
document.addEventListener('visibilitychange', () => {
    if (!document.hidden && 'serviceWorker' in navigator) {
        navigator.serviceWorker.ready.then(registration => {
            checkForUpdates(registration);
        });
    }
});

// 포커스 시 업데이트 확인
window.addEventListener('focus', () => {
    if ('serviceWorker' in navigator) {
        navigator.serviceWorker.ready.then(registration => {
            checkForUpdates(registration);
        });
    }
});
