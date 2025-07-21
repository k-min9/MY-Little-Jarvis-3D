# Operator

1. 기존 Toon 방식 Prefab 사용가능
2. PortraitMask 하단으로 이동
3. FallingObject 비활성화
4. RectTransform을 Mask에 맞게 크기 조정
5. FaceController의 Type을 "Operator"로 변경
6. PortraitController의 변수에 옮긴 Prefab GameObject와 카메라의 중심이 되어줄 head 노드 연결
7. GameManager에 CurrentOperator 등록
