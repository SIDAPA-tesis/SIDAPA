<#
.SYNOPSIS
  Checker de las convenciones de pruebas de SIDAPA (ver SKILL.md en esta misma carpeta).

.DESCRIPTION
  Revisa los proyectos test/SIDAPA.<capa>.test y reporta:
    ERROR   NAMESPACE       el namespace no coincide con la carpeta (SIDAPA.<capa>.test.<Carpeta>.<...>)
    ERROR   LAYER-REF       el proyecto de pruebas referencia un proyecto que no es su capa
    ERROR   SLEEP           Thread.Sleep / Task.Delay en una prueba (no determinista y lento)
    ERROR   FAKE-DB         UseInMemoryDatabase / UseSqlite (usar PostgresFixture)
    ERROR   DEV-ENDPOINT    Dev*Controller en SIDAPA.presentation (se reemplazan por pruebas)
    WARNING CLOCK           DateTime.Now / Today / UtcNow en pruebas (usar TimeProvider / FixedTimeProvider)
    WARNING XUNIT-ASSERT    Assert.* distinto de Skip/SkipWhen/SkipUnless/Fail (usar Shouldly)
    WARNING NO-CT           llamada a ...Async( sin CancellationToken en un archivo que no usa TestContext

  Sale con codigo 1 si hay errores (o warnings con -Strict); 0 si esta limpio.
  El script es ASCII a proposito: Windows PowerShell 5.1 lee .ps1 sin BOM como ANSI.

.PARAMETER Root
  Raiz del repo. Por defecto se resuelve desde la ubicacion del script.

.PARAMETER Include
  Patrones -like sobre la ruta relativa a la raiz (con /). Solo se reportan hallazgos de esos archivos.
  Ej: -Include 'test/SIDAPA.application.test/Services/*'

.PARAMETER Strict
  Los warnings tambien hacen fallar el checker.

.PARAMETER NoWarnings
  Oculta los warnings.
#>
param(
    [string]$Root,
    [string[]]$Include,
    [switch]$Strict,
    [switch]$NoWarnings
)

$ErrorActionPreference = 'Stop'

if (-not $Root) { $Root = Join-Path $PSScriptRoot '..\..\..' }
$rootPath = (Resolve-Path -LiteralPath $Root).Path.TrimEnd('\', '/')
$testRoot = Join-Path $rootPath 'test'

$findings = New-Object System.Collections.Generic.List[object]

function Get-Rel([string]$fullName) {
    return ($fullName.Substring($rootPath.Length).TrimStart('\', '/') -replace '\\', '/')
}

function Add-Finding([string]$level, [string]$rule, [string]$file, [int]$line, [string]$msg) {
    $findings.Add([pscustomobject]@{ Level = $level; Rule = $rule; File = $file; Line = $line; Msg = $msg })
}

function Get-LineAt([string]$text, [int]$index) {
    return ([regex]::Matches($text.Substring(0, $index), "`n").Count + 1)
}

# Quita comentarios // y /* */ conservando los saltos de linea (para no reportar ejemplos comentados).
function Remove-Comments([string]$text) {
    $text = [regex]::Replace($text, '/\*[\s\S]*?\*/', { param($m) ($m.Value -replace '[^\n]', '') })
    return [regex]::Replace($text, '(?m)//.*$', '')
}

if (-not (Test-Path -LiteralPath $testRoot)) {
    Write-Output "No existe la carpeta test/ en $rootPath"
    exit 1
}

# ---------------------------------------------------------------- Proyectos de prueba
$projects = Get-ChildItem -LiteralPath $testRoot -Directory |
    Where-Object { $_.Name -match '^SIDAPA\.(\w+)\.test$' }

foreach ($dir in $projects) {
    $layer = ($dir.Name -replace '^SIDAPA\.', '') -replace '\.test$', ''
    $csproj = Join-Path $dir.FullName "$($dir.Name).csproj"

    if (-not (Test-Path -LiteralPath $csproj)) {
        Add-Finding 'ERROR' 'LAYER-REF' (Get-Rel $dir.FullName) 1 "Falta $($dir.Name).csproj (el nombre del proyecto debe coincidir con la carpeta)."
        continue
    }

    # LAYER-REF: solo se referencia la capa propia.
    $xml = Get-Content -LiteralPath $csproj -Raw
    foreach ($m in [regex]::Matches($xml, 'ProjectReference\s+Include="([^"]+)"')) {
        $refName = [IO.Path]::GetFileNameWithoutExtension($m.Groups[1].Value)
        if ($refName -ne "SIDAPA.$layer") {
            Add-Finding 'ERROR' 'LAYER-REF' (Get-Rel $csproj) (Get-LineAt $xml $m.Index) "Referencia a $refName. Un proyecto de pruebas solo referencia su capa (SIDAPA.$layer); si necesitas otra capa, la prueba va en otro proyecto."
        }
    }

    foreach ($file in Get-ChildItem -LiteralPath $dir.FullName -Recurse -File -Filter '*.cs' |
            Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }) {
        $rel = Get-Rel $file.FullName
        $raw = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8
        if (-not $raw) { continue }
        $code = Remove-Comments $raw

        # NAMESPACE = carpeta
        $folder = Split-Path -Parent ($file.FullName.Substring($dir.FullName.Length).TrimStart('\', '/'))
        $expected = $dir.Name
        if ($folder) { $expected += '.' + ($folder -replace '[\\/]', '.') }
        $ns = [regex]::Match($code, '(?m)^\s*namespace\s+([\w\.]+)')
        if (-not $ns.Success) {
            Add-Finding 'ERROR' 'NAMESPACE' $rel 1 "Sin namespace. Esperado: $expected"
        } elseif ($ns.Groups[1].Value -ne $expected) {
            Add-Finding 'ERROR' 'NAMESPACE' $rel (Get-LineAt $code $ns.Index) "namespace $($ns.Groups[1].Value); esperado $expected"
        }

        foreach ($m in [regex]::Matches($code, '\b(Thread\.Sleep|Task\.Delay)\s*\(')) {
            $what = $m.Groups[1].Value
            Add-Finding 'ERROR' 'SLEEP' $rel (Get-LineAt $code $m.Index) "No esperes con ${what}: controla el tiempo con TimeProvider o espera la Task."
        }

        foreach ($m in [regex]::Matches($code, '\b(Use(InMemoryDatabase|Sqlite))\s*\(')) {
            $what = $m.Groups[1].Value
            Add-Finding 'ERROR' 'FAKE-DB' $rel (Get-LineAt $code $m.Index) "$what no se comporta como PostgreSQL. Usa PostgresFixture (Testcontainers)."
        }

        foreach ($m in [regex]::Matches($code, '\bDateTime\.(Now|Today|UtcNow)\b')) {
            Add-Finding 'WARNING' 'CLOCK' $rel (Get-LineAt $code $m.Index) "$($m.Value): fija el reloj (FixedTimeProvider) salvo que pruebes contra el reloj real a proposito."
        }

        foreach ($m in [regex]::Matches($code, '\bAssert\.(\w+)\s*[(<]')) {
            if ($m.Groups[1].Value -notin @('Skip', 'SkipWhen', 'SkipUnless', 'Fail')) {
                Add-Finding 'WARNING' 'XUNIT-ASSERT' $rel (Get-LineAt $code $m.Index) "Assert.$($m.Groups[1].Value): usa Shouldly (ShouldBe, ShouldHaveSingleItem, Should.ThrowAsync...)."
            }
        }

        if ($code -match '\[(Fact|Theory)' -and $code -notmatch 'TestContext\.Current\.CancellationToken') {
            $m = [regex]::Match($code, '\bawait\s+[^;]*Async\s*\(\s*\)')
            if ($m.Success) {
                Add-Finding 'WARNING' 'NO-CT' $rel (Get-LineAt $code $m.Index) "Pasa TestContext.Current.CancellationToken a los metodos async."
            }
        }
    }
}

# ---------------------------------------------------------------- Endpoints Dev* en presentation
$presentation = Join-Path $rootPath 'SIDAPA.presentation'
if (Test-Path -LiteralPath $presentation) {
    foreach ($dev in Get-ChildItem -LiteralPath $presentation -Recurse -File -Filter 'Dev*Controller.cs' |
            Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }) {
        Add-Finding 'ERROR' 'DEV-ENDPOINT' (Get-Rel $dev.FullName) 1 "Endpoint manual de prueba. Convierte lo que verifica en pruebas automaticas de su capa y borralo."
    }
}

# ---------------------------------------------------------------- Salida
function Test-Included([string]$file) {
    if (-not $Include) { return $true }
    foreach ($pattern in $Include) { if ($file -like $pattern) { return $true } }
    return $false
}

$shown = $findings | Where-Object { (-not $NoWarnings -or $_.Level -eq 'ERROR') -and (Test-Included $_.File) }

foreach ($f in $shown | Sort-Object File, Line) {
    Write-Output ("{0,-7} {1,-13} {2}:{3} {4}" -f $f.Level, $f.Rule, $f.File, $f.Line, $f.Msg)
}

$errors = @($shown | Where-Object Level -eq 'ERROR').Count
$warnings = @($shown | Where-Object Level -eq 'WARNING').Count
Write-Output ""
Write-Output "Errores: $errors  Warnings: $warnings"

if ($errors -gt 0 -or ($Strict -and $warnings -gt 0)) { exit 1 }
exit 0
