mkdir dist
cd dist

copy ..\..\FavoImgs\bin\x86\Release\FavoImgs.exe . /Y
copy ..\..\FavoImgs\bin\x86\Release\*.dll . /Y	
copy ..\..\packages\System.Data.SQLite.Core.1.0.93.0\content\net40\x86\*.* . /Y
copy ..\..\helper\*.* . /Y
..\libz.exe inject-dll --assembly FavoImgs.exe --include *.dll --move