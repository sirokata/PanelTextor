# PanelTextor

[日本語](README.md) | [English](README.en.md) | [简体中文](README.zh-CN.md) | [한국어](README.ko.md)

![PanelTextor](Assets/PanelTextor.png)

Windows 11용 로컬 다국어 텍스트 배치 도구입니다.

Copyright (c) 2026 **sirokata** · 자체 코드는 [MIT License](LICENSE)

## 다운로드 및 실행

1. 이 저장소의 **Releases**에서 Windows x64용 ZIP을 다운로드합니다.
2. 압축을 풀고 `PanelTextor.exe`를 실행합니다. .NET을 별도로 설치할 필요가 없습니다.
3. 처음 실행할 때 이용 약관과 제3자 라이선스를 읽고 동의하면 편집기가 열립니다. 동의하지 않으면 종료됩니다.
4. ‘이미지 추가’로 이미지를 불러온 후 텍스트를 추가하고 배치합니다.

자세한 절차는 **[사용 방법](USAGE.ko.md)**을 참조하세요.

exe에는 필요한 런타임과 라이선스가 포함됩니다. exe만으로도 실행되지만 배포ZIP에는 설명서도 제공합니다. 실행할 때 필요한 파일이 Windows 임시 폴더에 풀립니다.

## 주요 기능

- JP / EN / CN / KR별 텍스트, 위치, 크기, 색상 관리
- 일본어 세로쓰기, 주요 문장부호, 짧은 기호·숫자열의 자동 가로 조합
- Regular / Medium / Bold 등 가변 글꼴 굵기 선택
- 빈 줄 기준 입력 분할, 드래그 이동, 다중 선택 및 Delete 삭제
- 파일명으로 대응하는 여러 배경 세트와 불일치 검사
- 다국어×배경 세트 일괄 내보내기, 진행률 및 완료 알림
- PNG / JPEG 품질100, sRGB 변환 및 ICC 프로파일
- 페이지별 촬영 프레임, 경과 시간, 5단계 배터리
- 일본어·영어·간체 중국어·한국어 화면 표시

## 저장 및 이전

- 프로젝트: `作品.paneltextor.json` (이름 변경 가능). 이미지는 경로로 참조하므로 프로젝트와 이미지 폴더를 함께 보관하세요.
- 기존 `.polytext.json`도 열 수 있습니다. 저장할 때 새 이름을 제안하며 기존 파일을 자동 삭제하지 않습니다.
- 설정: `%LOCALAPPDATA%\PanelTextor\settings.json`
- 새 설정이 없으면 기존 `%LOCALAPPDATA%\PolyText\settings.json`을 자동 복사합니다. 기존 파일은 유지합니다.
- 동의 기록: `%LOCALAPPDATA%\PanelTextor\terms-acceptance.txt`. 약관 또는 라이선스 원문이 변경되면 다시 동의해야 합니다.

## 환경 및 제한

Windows 11 x64용입니다. 글꼴은 포함하지 않으므로 필요한 글꼴을 설치하세요. 자체 이미지 업로드·접속 분석 기능은 없습니다.

다른 이미지 편집기와 픽셀 단위 일치, 루비, 일본어 금칙 처리, 자동 줄바꿈, 실행 취소/다시 실행은 지원하지 않습니다. JPEG 품질100도 손실 압축입니다. 무손실 저장에는 PNG를 사용하세요. 

## 소스 빌드

Windows와 .NET 10 SDK가 필요합니다.

```powershell
dotnet restore PanelTextor.csproj
dotnet build PanelTextor.csproj -c Release
dotnet publish PanelTextor.csproj -c Release -r win-x64 --self-contained true -o dist/PanelTextor
```

배포ZIP 생성:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/package.ps1
```

출력 위치는 `release/`입니다. ZIP에는 실행 파일, 사용 문서 및 필요한 라이선스가 포함됩니다.

```powershell
dist/PanelTextor/PanelTextor.exe --self-test test-output
```

테스트에는 일본어·중국어·한국어 글꼴이 필요합니다. 가변 글꼴 테스트에는 Source Han Sans CN VF를 설치하세요. 테스트는 별도 폴더의 설정을 사용합니다.

## 라이선스 및 재배포

PanelTextor 자체 코드는 **MIT**입니다. 무료·상업적 사용, 수정 및 재배포를 허용합니다. 저작권 및 허가 고지를 유지하세요.

.NET/WPF 등 포함된 구성 요소는 각 권리자의 조건을 따르며 MIT로 변경되지 않습니다. [이용 약관](TERMS.ko.md), [제3자 고지](THIRD_PARTY_NOTICES.md), `licenses/`를 확인하세요. 배포판은 최초 실행 시 제3자 조건을 포함한 동의를 요구합니다. 수정·재배포 시에도 필요한 고지와 동의 절차를 유지하세요.

버그 보고에는 앱 버전, Windows 버전, 글꼴 및 재현 절차를 포함하세요. 비공개 작품이나 개인 정보를 공개Issue에 첨부하지 마세요.



