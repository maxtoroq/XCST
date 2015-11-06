@echo off
cd %~dp0

java -jar \lib\trang\20091111\trang.jar -o any-process-contents=lax -o indent=3 xcst.rng xcst.xsd
java -jar \lib\trang\20091111\trang.jar -o any-process-contents=lax -o indent=3 xcst-app.rng xcst-app.xsd