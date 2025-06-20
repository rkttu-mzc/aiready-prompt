# MzcAirPrompt

> **AI 프롬프트 갤러리**  

## 📋 프로젝트 개요

MzcAirPrompt는 사내에서 사용하는 AI 프롬프트를 체계적으로 관리하고 공유할 수 있는 웹 애플리케이션입니다. 다양한 업무 영역별로 정리된 프롬프트 템플릿을 통해 팀 내 AI 활용 효율성을 높이는 것을 목표로 합니다.

## 🛠️ 기술 스택

- **.NET 9.0** - 최신 .NET 플랫폼
- **Blazor WebAssembly** - 클라이언트 사이드 웹 애플리케이션
- **C#** - 주 개발 언어
- **Markdig** - 마크다운 파싱 및 렌더링
- **Progressive Web App (PWA)** - 오프라인 지원 및 앱 설치 가능

## 📁 프로젝트 구조

```text
src/
├── MzcAirPrompt/
│   ├── Components/           # 재사용 가능한 Blazor 컴포넌트
│   │   ├── NavMenu.razor
│   │   ├── PromptCard.razor
│   │   ├── PromptMiniCard.razor
│   │   └── PromptModal.razor
│   ├── Layout/              # 레이아웃 컴포넌트
│   │   └── MainLayout.razor
│   ├── Models/              # 데이터 모델
│   │   └── PromptItem.cs
│   ├── Pages/               # 페이지 컴포넌트
│   │   ├── Home.razor
│   │   └── Prompts.razor
│   ├── Services/            # 비즈니스 로직 서비스
│   │   └── PromptService.cs
│   └── wwwroot/             # 정적 파일
│       └── prompts/         # 프롬프트 마크다운 파일
│           ├── business/    # 비즈니스 관련 프롬프트
│           ├── development/ # 개발 관련 프롬프트
│           ├── education/   # 교육 관련 프롬프트
│           └── writing/     # 글쓰기 관련 프롬프트
```

## 🚀 실행 방법

### 사전 요구사항

- .NET 9.0 SDK 설치

### 로컬 개발 환경 실행

1. 프로젝트 클론 (내부 저장소에서)
2. 프로젝트 폴더로 이동:

   ```powershell
   cd src/MzcAirPrompt
   ```

3. 의존성 패키지 복원:

   ```powershell
   dotnet restore
   ```

4. 애플리케이션 실행:

   ```powershell
   dotnet run
   ```

5. 브라우저에서 `https://localhost:5001` 또는 `http://localhost:5000` 접속

### 빌드 및 배포

```powershell
# 릴리스 빌드
dotnet build --configuration Release

# 퍼블리시 (배포용)
dotnet publish --configuration Release --output ./publish
```

### GitHub Pages 배포

프로젝트는 GitHub Actions를 통해 자동으로 GitHub Pages에 배포됩니다:

1. `main` 브랜치에 푸시하면 자동으로 배포 프로세스가 시작됩니다
2. GitHub Actions가 .NET 프로젝트를 빌드합니다
3. SPA 라우팅 문제 해결을 위해 필요한 파일들이 자동으로 설정됩니다:
   - `404.html`: 서브 페이지 새로고침 시 404 오류 해결
   - `.nojekyll`: Jekyll 처리 비활성화
   - `CNAME`: 커스텀 도메인 설정 (`prompts.aiready.ai.kr`)
4. 커스텀 도메인 `https://prompts.aiready.ai.kr/`로 서비스됩니다

**📌 커스텀 도메인 사용**: 이 프로젝트는 GitHub Pages의 서브디렉토리(`username.github.io/repo-name`) 대신 커스텀 도메인 `https://prompts.aiready.ai.kr/`을 사용합니다. 따라서 base href는 `/`로 유지되며, 별도의 경로 설정이 필요하지 않습니다.

**📌 SPA 라우팅 문제 해결**: GitHub Pages에서 `/prompts` 같은 서브 페이지를 새로고침할 때 404 오류가 발생하는 문제를 해결하기 위해 특별한 리다이렉트 로직이 구현되어 있습니다.

> **📌 배포 시 주의사항**: 새 버전을 배포한 후 사용자들이 기존 페이지를 열어두고 있다면, 자동으로 업데이트 알림이 표시됩니다. 이는 Service Worker가 새 버전을 감지하여 사용자에게 알리는 기능입니다.

## 📚 주요 기능

### 🎯 프롬프트 관리

- **카테고리별 분류**: 비즈니스, 개발, 교육, 글쓰기 등 업무 영역별 정리
- **검색 및 필터링**: 키워드, 태그, 카테고리별 프롬프트 검색
- **즐겨찾기**: 자주 사용하는 프롬프트 북마크 기능
- **상세 보기**: 프롬프트 미리보기 및 전체 내용 확인

### 🔧 사용자 경험

- **반응형 디자인**: 데스크톱, 태블릿, 모바일 지원
- **PWA 지원**: 브라우저에서 앱처럼 설치 및 오프라인 사용 가능
- **마크다운 렌더링**: 풍부한 텍스트 포맷팅으로 가독성 향상
- **업데이트 알림**: 새 배포 시 자동 알림 및 새로고침 안내

### 🔄 업데이트 알림 시스템

애플리케이션에 새로운 버전이 배포되면 자동으로 업데이트 알림이 표시됩니다:

- **자동 감지**: Service Worker가 새 버전을 자동으로 감지
- **사용자 친화적 알림**: 우측 상단에 예쁜 알림 창 표시
- **선택적 업데이트**: 사용자가 원할 때 새로고침 가능
- **테스트 페이지**: `/test-update` 경로에서 기능 테스트 가능

#### 업데이트 알림 작동 방식

1. 새 버전이 배포되면 Service Worker가 변경사항을 감지
2. 사용자에게 업데이트 알림 표시
3. "새로고침" 버튼 클릭 시 최신 버전으로 업데이트
4. "나중에" 버튼으로 알림 숨기기 가능

### 📊 현재 제공 프롬프트 카테고리

- **🏢 Business**: 비즈니스 전략, 마케팅 전략
- **💻 Development**: API 문서 생성, 코드 리뷰
- **📚 Education**: 언어 학습 도우미
- **✍️ Writing**: 글쓰기 지원

## 🔧 개발 가이드

### 새 프롬프트 추가

1. 해당 카테고리 폴더(`wwwroot/prompts/{category}/`)에 마크다운 파일 생성
2. `wwwroot/prompts/manifest.json` 파일에 새 프롬프트 정보 추가:

   ```json
   {
       "relPath": "카테고리명",
       "fileName": "파일명.md"
   }
   ```

### 마크다운 파일 형식

프롬프트 마크다운 파일은 다음 형식을 따라야 합니다:

```markdown
---
title: "프롬프트 제목"
description: "프롬프트 설명"
category: "카테고리"
tags: ["태그1", "태그2"]
author: "작성자"
---

# 프롬프트 내용

프롬프트의 실제 내용을 여기에 작성합니다.
```

## 📝 라이선스 및 면책조항

**Copyright © 2025 MEGAZONE CLOUD. All Rights Reserved.**

### 사용 조건

- 본 소프트웨어는 **사내 직원의 편의를 위해서만** 제공됩니다.
- **상업적 용도로의 외부 제공 및 재배포를 금지**합니다.
- 사내 업무 목적으로만 사용이 허용됩니다.

### 면책조항

본 소프트웨어는 **"있는 그대로(AS IS)"** 제공되며, **명시적이거나 묵시적인 어떠한 보증도 하지 않습니다**. 여기에는 다음이 포함되지만 이에 국한되지 않습니다:

- ✗ **상품성 보증 없음**
- ✗ **특정 목적에의 적합성 보증 없음**
- ✗ **오류 없음 보증 없음**
- ✗ **중단 없는 작동 보증 없음**

**회사는 본 소프트웨어의 사용으로 인해 발생하는 어떠한 직접적, 간접적, 우발적, 특별한, 징벌적 또는 결과적 손해에 대해서도 책임을 지지 않습니다.**

사용자는 본 소프트웨어를 **자신의 책임 하에** 사용해야 하며, 업무에 미치는 영향에 대해서는 사용자가 직접 판단하고 대응해야 합니다.
