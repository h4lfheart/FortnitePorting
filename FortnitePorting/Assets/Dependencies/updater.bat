TASKKILL /IM "FortnitePorting.exe" /F
TIMEOUT /t 1 /nobreak > NUL
MOVE "FortnitePorting.TEMP.exe" "FortnitePorting.exe"
START "" /B "FortnitePorting.exe"