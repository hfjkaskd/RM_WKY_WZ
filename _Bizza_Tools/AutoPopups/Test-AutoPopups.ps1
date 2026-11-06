param([string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$testOutput = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else { Join-Path $projectRoot 'Library/AutoPopupTests' }
New-Item -ItemType Directory -Path $testOutput -Force | Out-Null

# Execute the production queue, account triggers, daily page and UI gates against isolated doubles.
# Task replaces UniTask only as the test scheduler; no Unity session, save or server is touched.
function Read-Section([string]$path, [string]$start, [string]$end) {
    $source = [IO.File]::ReadAllText((Join-Path $projectRoot $path))
    $first = $source.IndexOf($start, [StringComparison]::Ordinal)
    if ($first -lt 0) { throw "Missing start marker in $path" }
    $last = $source.IndexOf($end, $first, [StringComparison]::Ordinal)
    if ($last -lt 0) { throw "Missing end marker in $path" }
    return $source.Substring($first, $last - $first)
}
$account = Read-Section 'Assets/BizzaWZ/Final/Connect_SDK/SDK_WKY/API/AccountModule.cs' `
    '    private UserInfo Os_Uso;' '    public static string version'
$accountConstructor = Read-Section 'Assets/BizzaWZ/Final/Connect_SDK/SDK_WKY/API/AccountModule.cs' `
    '    public AccountModule()' '    public void Login()'
$update = Read-Section 'Assets/BizzaWZ/Common/Framework/Runtime/Module/UI/UIModule.cs' `
    '    private void Update()' '    private void ExitCurrentView()'
$gates = Read-Section 'Assets/BizzaWZ/Common/Framework/Runtime/Module/UI/UIModule.cs' `
    '    internal bool CanPresentAutomaticPopup' '    private int m_advertisticsLimit'
$openCore = Read-Section 'Assets/BizzaWZ/Common/Framework/Runtime/Module/UI/UIModule.cs' `
    '    public UniTask<UIPageBase> OpenPage(PageId pageId)' '    private bool LayerMultiPages'
$registration = Read-Section 'Assets/BizzaWZ/Common/Framework/Runtime/Module/UI/UIModule.cs' `
    '    private void InitPage(' '    private async UniTask<UIPageBase> _OpenPage('
$closePage = Read-Section 'Assets/BizzaWZ/Common/Framework/Runtime/Module/UI/UIModule.cs' `
    '    public void ClosePage(UIPageBase pageBase)' '    public void ClosePage(PageId pageId'
$flow = Read-Section 'Assets/BizzaWZ/Common/Framework/Runtime/Module/FlowModule.cs' `
    '    internal static bool AutomaticPopupFlowBlocked' '    private static string analyticsLevelId'
$daily = Read-Section 'Assets/BizzaWZ/Final/Connect_SDK/SDK_WKY/API/AccountModule.cs' `
    '    private void QueueDailyWithdrawal()' '    #endregion'
$info = Read-Section 'Assets/BizzaWZ/Final/Real/UI/ExchangeRatePanel/ExchangeRatePanel.cs' `
    'public struct ExchangeRateInfo' '    public (string, string) ExchangeDefiniteRate()'
$subject = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
public partial class AccountModule {
$accountConstructor
$account
$daily
}
public partial class UIModule {
$gates
$openCore
$registration
$closePage
$update
}
public static class FlowModule {
$flow
}
$info
}
"@
$subjectFile = Join-Path $testOutput 'ProductionSubject.cs'
[IO.File]::WriteAllText($subjectFile, $subject.Replace('UniTask', 'Task'), [Text.UTF8Encoding]::new($false))
$sources = @(
    'Assets/BizzaWZ/Common/Framework/Runtime/Module/UI/AutoPopupQueue.cs',
    'Assets/BizzaWZ/Common/Framework/Runtime/Module/UI/PageId.cs',
    'Assets/BizzaWZ/Common/Framework/Runtime/Module/UI/UIPage.cs',
    'Assets/BizzaWZ/Final/Real/UI/DailyWithdrawPanel/DailyWithdrawPanel.cs'
)
$compileItems = foreach ($source in $sources) {
    $text = [IO.File]::ReadAllText((Join-Path $projectRoot $source))
    $text = $text.Replace('using Cysharp.Threading.Tasks;', 'using System.Threading.Tasks;').Replace('UniTask', 'Task')
    $target = Join-Path $testOutput ([IO.Path]::GetFileName($source))
    [IO.File]::WriteAllText($target, $text, [Text.UTF8Encoding]::new($false))
    '<Compile Include="' + [Security.SecurityElement]::Escape($target) + '" />'
}
$programFile = [Security.SecurityElement]::Escape((Join-Path $PSScriptRoot 'AutoPopupTests/Program.cs'))
$subjectFileEscaped = [Security.SecurityElement]::Escape($subjectFile)
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <DefineConstants>BIZZA_REAL_WITHDRAW</DefineConstants>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$subjectFileEscaped" />
    <Compile Include="$programFile" />
    <Compile Include="$([Security.SecurityElement]::Escape((Join-Path $PSScriptRoot 'AutoPopupTests/UnityDoubles.cs')))" />
    <Compile Include="$([Security.SecurityElement]::Escape((Join-Path $PSScriptRoot 'AutoPopupTests/ReviewRegressions.cs')))" />
    <Compile Include="$([Security.SecurityElement]::Escape((Join-Path $PSScriptRoot 'AutoPopupTests/ProjectRegressions.cs')))" />
    $($compileItems -join [Environment]::NewLine)
  </ItemGroup>
</Project>
"@
$projectFile = Join-Path $testOutput 'AutoPopupTests.csproj'
[IO.File]::WriteAllText($projectFile, $project, [Text.UTF8Encoding]::new($false))
$sdk = @(& dotnet --list-sdks | Where-Object { $_ -match '^8\.' })[-1]
if (-not $sdk) { throw 'An installed .NET 8 SDK is required for the isolated regression tests.' }
$sdkVersion = ($sdk -split ' ')[0]
[IO.File]::WriteAllText((Join-Path $testOutput 'global.json'),
    ('{"sdk":{"version":"' + $sdkVersion + '","rollForward":"disable"}}'), [Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText((Join-Path $testOutput 'NuGet.Config'),
    '<configuration><packageSources><clear /></packageSources></configuration>', [Text.UTF8Encoding]::new($false))
Push-Location $testOutput
try {
    & dotnet run --project $projectFile --configuration Release
    if ($LASTEXITCODE -ne 0) { throw 'Automatic popup regressions failed' }
} finally { Pop-Location }
