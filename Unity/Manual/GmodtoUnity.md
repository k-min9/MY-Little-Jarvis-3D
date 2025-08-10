# GMod to Unity

- 개요 : Garry's mod 또는 Source Filmmaker의 gma 파일을 unity의 fbx로 변환 후 적용

## 작업

- 후보 선정 : Garry's mod 또는 Source Filmmaker의 창작마당
  - <https://steamcommunity.com/sharedfiles/filedetails/?id=3447029810>
  - <https://steamcommunity.com/sharedfiles/filedetails/?id=3249893613>
  - <https://steamcommunity.com/sharedfiles/filedetails/?id=3280173467>
  - <https://steamcommunity.com/sharedfiles/filedetails/?id=3386961967>
- 후보 다운로드 시작 : <https://steamworkshopdownloader.io/> 접속
- SteamCmd 기동
  - login anonymous
  - 각각의 파일 입력 후 나오는 명령문 입력
    - 예시 : workshop_download_item 4000 3447029810
  - SteamCmd에서 파일 다운로드(다운로드 완료시 위치까지 고지)
    - 확장명은 gma
- crowbar 실행 : gma 파일을 언패킹하는 툴
  - 링크 : <https://steamcommunity.com/groups/CrowbarTool>
  - 사용버전 : 0.74
- crowbar 메뉴 unpack으로 이동 후, gma 파일 드래그 앤드 드롭 후 unpack
  - 만들어진 산출물 중 mdl(models), vtf(material) 파일 사용
- crowbar 메뉴 Decompile로 이동 후, mdl 파일 드래그 앤드 드롭 후 Decompile
  - qc : 모델의 메쉬, 애니메이션, 히트박스, 물리 설정 등 모델 그 전체를 정의하는 스크립트 파일.
  - physics : 'GMOD'에서의 충돌설정, ragdoll, 애니메이션 등의 정보가 담김
  - smd 각종 reference
- Blender 기동
  - 사용버전 : 4.2.1 LTS
- Blender 파일 불러오기
  - File>Import>qc 파일 열기
- 여분의 파일 제거
  - physics 제거
  - skeleton 내부 animation 제거
  - ragdoll을 제외한 animator도 제거하는것도 괜찮을 듯 skeleton
  - skeleton은 humanoid 잡는데 필요하므로 제거하지 말 것
- Blender 파일 내보내기
  - File>Export>fbx 파일 내보내기
- Unity에 fbx를 드래그앤 드롭
  - Animation에서 import animation 체크 끄기
  - Rig>Animationtype>humanoid
- 사용 Texture 파악 : 해당 내용은 png로 필요함
  - VTFEdit열고, Tools > Convert Folder로 폴더째 일괄변환
  - 해당 이름을 가진 vtf 파일 찾기

## 기타

- SteamCmd 설치
  - <https://steamworkshopdownloader.io> 용 파일
  - 없을 경우 다운로드 하는 법까지 해당 사이트에서 안내해준다.
  - 다운로드링크 : <https://developer.valvesoftware.com/wiki/SteamCMD#Downloading_SteamCMD>
- Blender용 Tool : <https://developer.valvesoftware.com/wiki/Blender_Source_Tools>
  - qc, smd 파일을 Import, Export할 수 있게 해줌
