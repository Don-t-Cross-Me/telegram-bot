install: 
	dotnet restore 

start: 
	dotnet run 

build: 
	dotnet publish -c Release -r ubuntu.18.04-x64 --output ./release

serve:
	./release/telegram-bot