# Format is {variable_name}={value_1}, each line will hold one variable
foreach($line in Get-Content .\03_IaC\00_AzureCLI\MyDeploymentValues.txt) {
    Write-Output $line | Out-File -FilePath $env:GITHUB_ENV -Encoding utf8 -Append
}