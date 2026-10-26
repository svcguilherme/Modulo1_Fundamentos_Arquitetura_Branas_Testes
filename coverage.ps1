# Roda os testes com cobertura, gera o relatório HTML e abre no navegador.
# Uso: .\coverage.ps1           (gera e abre)
#      .\coverage.ps1 -NoOpen   (só gera; depois é só dar F5 no navegador)
param([switch]$NoOpen)

Set-Location $PSScriptRoot

# O dotnet watch usa a mesma pasta bin; rodando junto, a cobertura pode sair vazia
$watch = Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" | Where-Object { $_.CommandLine -like '*watch*test*' }
if ($watch) { Write-Warning "dotnet watch test está rodando (PID $($watch.ProcessId -join ', ')). Pare com Ctrl+C antes, ou a cobertura pode sair vazia." }

# Apaga os resultados antigos para o relatório não misturar execuções
Remove-Item TestResults -Recurse -Force -ErrorAction SilentlyContinue

dotnet test ProjetoTestes.slnx --collect:"XPlat Code Coverage" --results-directory TestResults
dotnet reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:"TestResults/coverage-report" -reporttypes:"Html;TextSummary"

Get-Content TestResults\coverage-report\Summary.txt | Select-String "Line coverage|Branch coverage"

if (-not $NoOpen) { Start-Process TestResults\coverage-report\index.html }
