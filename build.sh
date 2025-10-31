dotnet run --project ./CakeBuild/CakeBuild.csproj -- "$@" \
&& cd Releases \
&& ln -sf unofficialbugfix_*.zip unofficialbugfix.zip
