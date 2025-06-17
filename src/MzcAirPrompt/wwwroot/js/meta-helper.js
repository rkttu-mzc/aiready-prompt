// 동적 메타 태그 관리를 위한 헬퍼 함수들

window.MetaTagHelper = {
    // 메타 태그 업데이트
    updateMetaTags: function(metaData) {
        // 기본 title 업데이트
        if (metaData.title) {
            document.title = metaData.title;
        }

        // Open Graph 태그 업데이트
        this.updateMetaTag('property', 'og:title', metaData.title);
        this.updateMetaTag('property', 'og:description', metaData.description);
        this.updateMetaTag('property', 'og:url', metaData.url);
        this.updateMetaTag('property', 'og:image', metaData.image);

        // Twitter Card 태그 업데이트
        this.updateMetaTag('name', 'twitter:title', metaData.title);
        this.updateMetaTag('name', 'twitter:description', metaData.description);
        this.updateMetaTag('name', 'twitter:image', metaData.image);

        // Description 태그 업데이트
        this.updateMetaTag('name', 'description', metaData.description);
    },

    // 개별 메타 태그 업데이트
    updateMetaTag: function(attribute, attributeValue, content) {
        if (!content) return;

        var selector = `meta[${attribute}="${attributeValue}"]`;
        var metaTag = document.querySelector(selector);

        if (metaTag) {
            metaTag.setAttribute('content', content);
        } else {
            // 메타 태그가 없으면 새로 생성
            metaTag = document.createElement('meta');
            metaTag.setAttribute(attribute, attributeValue);
            metaTag.setAttribute('content', content);
            document.head.appendChild(metaTag);
        }
    },

    // URL 복사 기능
    copyToClipboard: function(text) {
        return navigator.clipboard.writeText(text).then(function() {
            return true;
        }).catch(function(err) {
            console.error('클립보드 복사 실패:', err);
            // 폴백: 구식 방법
            var textArea = document.createElement('textarea');
            textArea.value = text;
            document.body.appendChild(textArea);
            textArea.focus();
            textArea.select();
            try {
                var successful = document.execCommand('copy');
                document.body.removeChild(textArea);
                return successful;
            } catch (err) {
                document.body.removeChild(textArea);
                return false;
            }
        });
    },

    // 소셜 공유 링크 생성
    generateShareLinks: function(url, title, description) {
        var encodedUrl = encodeURIComponent(url);
        var encodedTitle = encodeURIComponent(title);
        var encodedDescription = encodeURIComponent(description);

        return {
            twitter: `https://twitter.com/intent/tweet?url=${encodedUrl}&text=${encodedTitle}`,
            facebook: `https://www.facebook.com/sharer/sharer.php?u=${encodedUrl}`,
            linkedin: `https://www.linkedin.com/sharing/share-offsite/?url=${encodedUrl}`,
            kakao: `https://story.kakao.com/share?url=${encodedUrl}&text=${encodedTitle}`
        };
    },

    // 브라우저 네이티브 공유 API 사용 (모바일에서 유용)
    nativeShare: function(shareData) {
        if (navigator.share) {
            return navigator.share(shareData).then(function() {
                return true;
            }).catch(function(err) {
                console.error('네이티브 공유 실패:', err);
                return false;
            });
        }
        return Promise.resolve(false);
    },

    // 현재 페이지 URL 가져오기
    getCurrentUrl: function() {
        return window.location.href;
    },

    // 절대 URL 생성
    getAbsoluteUrl: function(relativePath) {
        return new URL(relativePath, window.location.origin).href;
    }
};

// Blazor 컴포넌트에서 사용할 수 있도록 전역 함수로도 등록
window.updatePageMeta = function(title, description, url, image) {
    window.MetaTagHelper.updateMetaTags({
        title: title,
        description: description,
        url: url || window.MetaTagHelper.getCurrentUrl(),
        image: image || window.MetaTagHelper.getAbsoluteUrl('/icon-512.png')
    });
};

window.copyUrlToClipboard = function(url) {
    return window.MetaTagHelper.copyToClipboard(url || window.MetaTagHelper.getCurrentUrl());
};

window.shareNatively = function(title, description, url) {
    return window.MetaTagHelper.nativeShare({
        title: title,
        text: description,
        url: url || window.MetaTagHelper.getCurrentUrl()
    });
};
