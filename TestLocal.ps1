dotnet new uninstall BenjaminMichaelis.Dotnet.Templates
Remove-Item -Path "BenjaminMichaelis.Dotnet.Templates.*.nupkg"

dotnet pack -o .

dotnet new install $(Get-ChildItem -Path "BenjaminMichaelis.Dotnet.Templates.*.nupkg")
