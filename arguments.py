'''
서버쪽 파이썬 exe 파일 주고 받기 예시
pyinstaller --onefile arguments.py -n arguments --noconfirm 
'''
import sys

if __name__ == "__main__":
    if len(sys.argv) > 2:
        server_type = sys.argv[1]
        language = sys.argv[2]
        print(f"Server Type: {server_type}, Language: {language}")
    else:
        print("Insufficient arguments provided.")
