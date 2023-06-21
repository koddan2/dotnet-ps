$VerbosePreference = 'Continue'
#$ErrorActionPreference = 'Stop'
$ErrorActionPreference = 'Continue'
$DebugPreference='Continue'

Write-Host 'write-host hello'
for ($i = 0; $i -le 100; ++$i) {
    Write-Progress -Activity testprogress -Status ok -PercentComplete $i
    if ($i -gt 75) {
        break;
    }
    Start-Sleep -Seconds 0.01
}
Write-Progress -Activity testprogress -Status ok -PercentComplete $i -Completed
Write-Warning 'write-warning hello'
Write-Error 'write-error hello'
Write-Verbose 'write-verbose hello'
write-debug 'write-debug hello'
#return 'value: ' + $testvar
write-host 'a' $testvar
write-host 'b' $testvar_complex.IntVal
write-host 'c' $testvar_complex.StringVal

$myVar = @{
    complex        = $testvar_complex
    something_else = [datetime]::now
}
$myVar | format-table | Out-String | Write-Warning
$testvar_complex | Select-Object -Property * | Out-String | Write-Verbose
$args | write-host
Get-ChildItem env: |
Where-Object { $_.Name -imatch 'program*' }
