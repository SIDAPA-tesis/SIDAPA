<#
.SYNOPSIS
  Checker de las politicas front-end de SIDAPA (ver SKILL.md en esta misma carpeta).

.DESCRIPTION
  Revisa vistas .cshtml y hojas .css de SIDAPA.presentation y reporta:
    ERROR   CSS-LINES         hoja CSS con mas de 300 lineas
    ERROR   IMG-LAZY          <img> sin loading="lazy"
    ERROR   CSS-PER-PAGE      vista de pagina con mas de 1 CSS propio
    ERROR   COMPONENT-ASSETS  partial / view component con <link>, <script> o @section
    ERROR   NO-RAZOR          archivo .razor (el proyecto es MVC + .cshtml)
    WARNING HARDCODED-COLOR   #hex / rgb() / hsl() fuera de css/base/theme.css
    WARNING INLINE-JS         on*="..." en vistas o <script> sin src fuera de layouts
    WARNING PX-UNITS          medidas en px (salvo 0px / 1px)

  Sale con codigo 1 si hay errores (o warnings con -Strict); 0 si esta limpio.
  El script es ASCII a proposito: Windows PowerShell 5.1 lee .ps1 sin BOM como ANSI.

.PARAMETER Root
  Carpeta del proyecto web. Por defecto SIDAPA.presentation (resuelta desde la ubicacion del script).

.PARAMETER Include
  Patrones -like sobre la ruta relativa al Root (con /). Solo se reportan hallazgos de esos archivos.
  Ej: -Include 'Views/Usuarios/*','wwwroot/css/vistas/usuarios.css'

.PARAMETER Strict
  Los warnings tambien hacen fallar el checker.

.PARAMETER NoWarnings
  Oculta los warnings (util para ver solo lo que bloquea sobre todo el proyecto).
#>
param(
    [string]$Root,
    [string[]]$Include,
    [switch]$Strict,
    [switch]$NoWarnings
)

$ErrorActionPreference = 'Stop'

if (-not $Root) { $Root = Join-Path $PSScriptRoot '..\..\..\SIDAPA.presentation' }
$rootPath = (Resolve-Path -LiteralPath $Root).Path.TrimEnd('\', '/')

$MaxCssLines = 300
$findings = New-Object System.Collections.Generic.List[object]

function Get-Rel([string]$fullName) {
    return ($fullName.Substring($rootPath.Length).TrimStart('\', '/') -replace '\\', '/')
}

function Add-Finding([string]$level, [string]$rule, [string]$file, [int]$line, [string]$msg) {
    $findings.Add([pscustomobject]@{ Level = $level; Rule = $rule; File = $file; Line = $line; Msg = $msg })
}

function Get-ProjectFiles([string]$filter) {
    Get-ChildItem -LiteralPath $rootPath -Recurse -File -Filter $filter |
        Where-Object {
            $_.FullName -notmatch '[\\/](bin|obj|node_modules)[\\/]' -and
            $_.FullName -notmatch '[\\/]wwwroot[\\/]lib[\\/]'
        }
}

function Get-LineAt([string]$text, [int]$index) {
    return ([regex]::Matches($text.Substring(0, $index), "`n").Count + 1)
}

# Reemplaza comentarios por saltos de linea equivalentes: asi no se reportan ejemplos comentados
# y los numeros de linea siguen siendo correctos.
function Remove-Comments([string]$text, [string]$pattern) {
    return [regex]::Replace($text, $pattern, { param($m) ($m.Value -replace '[^\n]', '') })
}

# ---------------------------------------------------------------- CSS
foreach ($css in Get-ProjectFiles '*.css') {
    $rel = Get-Rel $css.FullName
    $lines = @(Get-Content -LiteralPath $css.FullName -Encoding UTF8)

    if ($lines.Count -gt $MaxCssLines) {
        Add-Finding 'ERROR' 'CSS-LINES' $rel ($MaxCssLines + 1) "$($lines.Count) lineas (max $MaxCssLines). Divide por componente o usa utilidades de Bootstrap."
    }

    $isTheme = $rel -match '(^|/)css/base/theme\.css$'
    $text = Remove-Comments ([string]::Join("`n", $lines)) '(?s)/\*.*?\*/'
    $clean = $text -split "`n"

    for ($i = 0; $i -lt $clean.Count; $i++) {
        $l = $clean[$i]
        if (-not $isTheme) {
            # Solo valores de propiedad (despues de ':'), para no confundir selectores #id con colores.
            if ($l -match ':[^{;]*(#[0-9a-fA-F]{3,8}\b|\b(rgba?|hsla?)\s*\()' -and $l -notmatch '\{\s*$') {
                Add-Finding 'WARNING' 'HARDCODED-COLOR' $rel ($i + 1) "color directo '$($Matches[1])'. Usa var(--sidapa-*) de css/base/theme.css."
            }
        }
        foreach ($m in [regex]::Matches($l, '(?<![\w.-])(\d*\.?\d+)px\b')) {
            if ($m.Groups[1].Value -notin @('0', '1')) {
                Add-Finding 'WARNING' 'PX-UNITS' $rel ($i + 1) "'$($m.Value)': usa rem / % / clamp()."
            }
        }
    }
}

# ---------------------------------------------------------------- Vistas
foreach ($view in Get-ProjectFiles '*.cshtml') {
    $rel = Get-Rel $view.FullName
    if ($rel -notmatch '^Views/') { continue }

    $name = $view.Name
    $isSpecial   = $name -in @('_ViewImports.cshtml', '_ViewStart.cshtml')
    if ($isSpecial) { continue }
    $isLayout    = $name -match '^_.*Layout\.cshtml$'
    $isComponent = (-not $isLayout) -and ($name.StartsWith('_') -or $rel -match '/(Components|Componets)/')
    $isPage      = -not $isLayout -and -not $isComponent

    $raw  = [System.IO.File]::ReadAllText($view.FullName)
    $text = Remove-Comments $raw '(?s)@\*.*?\*@'
    $text = Remove-Comments $text '(?s)<!--.*?-->'

    # IMG-LAZY
    foreach ($m in [regex]::Matches($text, '(?is)<img\b[^>]*>')) {
        if ($m.Value -notmatch "(?i)\bloading\s*=\s*[`"']lazy[`"']") {
            Add-Finding 'ERROR' 'IMG-LAZY' $rel (Get-LineAt $text $m.Index) '<img> sin loading="lazy".'
        }
    }

    # CSS-PER-PAGE (los CSS de wwwroot/lib, p. ej. Bootstrap, no cuentan: van en el layout)
    if ($isPage) {
        $links = @([regex]::Matches($text, '(?is)<link\b[^>]*\brel\s*=\s*["'']?stylesheet[^>]*>') |
                   Where-Object { $_.Value -notmatch '/lib/' })
        if ($links.Count -gt 1) {
            Add-Finding 'ERROR' 'CSS-PER-PAGE' $rel (Get-LineAt $text $links[1].Index) "$($links.Count) CSS propios (max 1). Junta los estilos en css/vistas/<vista>.css o subelos al layout."
        }
    }

    # COMPONENT-ASSETS
    if ($isComponent) {
        foreach ($m in [regex]::Matches($text, '(?i)<link\b|<script\b|@section\b')) {
            Add-Finding 'ERROR' 'COMPONENT-ASSETS' $rel (Get-LineAt $text $m.Index) "'$($m.Value)' dentro de un component. Los imports de CSS/JS van en la pagina o el layout."
        }
    }

    # INLINE-JS
    foreach ($m in [regex]::Matches($text, '(?i)\son(click|change|submit|input|load|keyup|keydown)\s*=')) {
        Add-Finding 'WARNING' 'INLINE-JS' $rel (Get-LineAt $text $m.Index) "atributo '$($m.Value.Trim())': usa un form/enlace o un listener en wwwroot/js/."
    }
    if ($isPage) {
        foreach ($m in [regex]::Matches($text, '(?is)<script\b(?![^>]*\bsrc\s*=)[^>]*>')) {
            Add-Finding 'WARNING' 'INLINE-JS' $rel (Get-LineAt $text $m.Index) '<script> en linea: mueve el codigo a wwwroot/js/<vista>.js.'
        }
    }
}

# ---------------------------------------------------------------- .razor
foreach ($razor in Get-ProjectFiles '*.razor') {
    Add-Finding 'ERROR' 'NO-RAZOR' (Get-Rel $razor.FullName) 1 'SIDAPA usa MVC + .cshtml; no se permiten componentes .razor / Blazor.'
}

# ---------------------------------------------------------------- Reporte
$report = $findings
if ($Include) {
    $report = @($findings | Where-Object { $f = $_.File; @($Include | Where-Object { $f -like $_ }).Count -gt 0 })
}
if ($NoWarnings) { $report = @($report | Where-Object Level -eq 'ERROR') }
$report = @($report | Sort-Object @{ e = { if ($_.Level -eq 'ERROR') { 0 } else { 1 } } }, File, Line)

foreach ($f in $report) {
    '{0,-7} {1,-16} {2}:{3}  {4}' -f $f.Level, $f.Rule, $f.File, $f.Line, $f.Msg
}

$errors   = @($report | Where-Object Level -eq 'ERROR').Count
$warnings = @($report | Where-Object Level -eq 'WARNING').Count
''
"check-frontend: $errors error(es), $warnings warning(s) en $rootPath"
$porRegla = $report | Group-Object Rule | Sort-Object Name | ForEach-Object { "$($_.Name)=$($_.Count)" }
if ($porRegla) { "por regla: " + ($porRegla -join ', ') }

if ($errors -gt 0 -or ($Strict -and $warnings -gt 0)) { exit 1 }
exit 0
