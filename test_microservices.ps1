$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:7000/api"
$Headers = @{ "Content-Type" = "application/json" }

Write-Host "--- PENSIONVAULT API AUTOMATED TEST SUITE ---" -ForegroundColor Cyan

# Wait for API to be up
Write-Host "Checking if API is up..."
try {
    Invoke-RestMethod -Uri "$BaseUrl/schemes" -Method Get -ErrorAction Stop > $null
    Write-Host "API is running!" -ForegroundColor Green
} catch {
    Write-Host "API is not responding at $BaseUrl. Please start the backend." -ForegroundColor Red
    exit 1
}

# 1. Fix Data (Anonymous)
Write-Host "`n1. Running Fix Data..."
$fixRes = Invoke-RestMethod -Uri "$BaseUrl/reports/fix-data" -Method Post -Headers $Headers
Write-Host "Fix Data Status: OK" -ForegroundColor Green

# 2. Register Admin
Write-Host "`n2. Registering Admin..."
$adminRegBody = @{
    name = "System Admin"
    email = "admin$(Get-Random)@pv.com"
    phone = "0000000000"
    password = "Password123!"
    role = "Admin"
} | ConvertTo-Json
$adminReg = Invoke-RestMethod -Uri "$BaseUrl/auth/register" -Method Post -Body $adminRegBody -Headers $Headers
Write-Host "Admin Registered: $($adminReg.email)" -ForegroundColor Green

# 3. Login Admin
Write-Host "`n3. Logging in Admin..."
$adminLoginBody = @{
    email = $adminReg.email
    password = "Password123!"
} | ConvertTo-Json
$adminLogin = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $adminLoginBody -Headers $Headers
$adminToken = $adminLogin.token
$adminAuthHeader = @{ "Authorization" = "Bearer $adminToken"; "Content-Type" = "application/json" }
Write-Host "Admin Logged In! Token Received." -ForegroundColor Green

# 4. Create Scheme
Write-Host "`n4. Creating Fund Scheme..."
$schemeBody = @{
    schemeName = "Test Scheme $(Get-Random)"
    schemeType = 1
    employeeContributionRate = 12.0
    employerContributionRate = 12.0
    interestRatePA = 8.5
    vestingSchedule = "5 Years"
} | ConvertTo-Json
$schemeRes = Invoke-RestMethod -Uri "$BaseUrl/schemes" -Method Post -Body $schemeBody -Headers $adminAuthHeader
$schemeId = $schemeRes.schemeId
Write-Host "Scheme Created: $schemeId" -ForegroundColor Green

# 5. Create Employer
Write-Host "`n5. Creating Employer..."
$empBody = @{
    companyName = "Test Corp $(Get-Random)"
    registrationNumber = "TC-$(Get-Random)"
    industry = "IT"
    contactEmail = "hr@testcorp.com"
    contactPhone = "555-0000"
} | ConvertTo-Json
$employerRes = Invoke-RestMethod -Uri "$BaseUrl/employers" -Method Post -Body $empBody -Headers $adminAuthHeader
$employerId = $employerRes.employerId
Write-Host "Employer Created: $employerId" -ForegroundColor Green

# 6. Register & Login Employer User
Write-Host "`n6. Registering Employer User..."
$empUserRegBody = @{
    name = "HR Manager"
    email = "hr$(Get-Random)@pv.com"
    phone = "5550000000"
    password = "Password123!"
    role = "Employer"
    organisationId = $employerId
} | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/auth/register" -Method Post -Body $empUserRegBody -Headers $Headers > $null

$empLoginBody = @{ email = ($empUserRegBody | ConvertFrom-Json).email; password = "Password123!" } | ConvertTo-Json
$empLogin = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $empLoginBody -Headers $Headers
$empAuthHeader = @{ "Authorization" = "Bearer $($empLogin.token)"; "Content-Type" = "application/json" }

# 7. Enroll Member
Write-Host "`n7. Enrolling Member..."
$memberBody = @{
    userId = "00000000-0000-0000-0000-000000000000"
    membershipNumber = "MB-$(Get-Random)"
    name = "Test Member"
    dateOfBirth = "1990-01-01"
    gender = "Male"
    nationalIdRef = "NID-$(Get-Random)"
    employerId = $employerId
    joiningDate = "2026-01-01"
    email = "member$(Get-Random)@testcorp.com"
} | ConvertTo-Json
$memberRes = Invoke-RestMethod -Uri "$BaseUrl/members" -Method Post -Body $memberBody -Headers $empAuthHeader
$memberId = $memberRes.memberId
Write-Host "Member Created: $memberId" -ForegroundColor Green

# 8. Submit Remittance
Write-Host "`n8. Submitting Monthly Remittance..."
$remittanceBody = @{
    employerId = $employerId
    remittancePeriod = "2026-06"
    totalEmployeeShare = 5000
    totalEmployerShare = 5000
    totalPensionAmount = 2000
    coverageCount = 1
    memberContributions = @(
        @{
            memberId = $memberId
            employeeAmount = 5000
            employerAmount = 5000
            pensionAmount = 2000
        }
    )
} | ConvertTo-Json -Depth 5
$remittanceRes = Invoke-RestMethod -Uri "$BaseUrl/remittances" -Method Post -Body $remittanceBody -Headers $empAuthHeader
$remittanceId = $remittanceRes.remittanceId
Write-Host "Remittance Submitted: $remittanceId" -ForegroundColor Green

# 9. Register & Login FundAdmin
Write-Host "`n9. Registering FundAdmin..."
$faRegBody = @{ name = "Fund Admin"; email = "fa$(Get-Random)@pv.com"; phone = "123"; password = "Password123!"; role = "FundAdmin" } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/auth/register" -Method Post -Body $faRegBody -Headers $Headers > $null
$faLogin = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body (@{ email = ($faRegBody | ConvertFrom-Json).email; password = "Password123!" } | ConvertTo-Json) -Headers $Headers
$faAuthHeader = @{ "Authorization" = "Bearer $($faLogin.token)"; "Content-Type" = "application/json" }

# 10. Reconcile Remittance
Write-Host "`n10. Reconciling Remittance..."
Invoke-RestMethod -Uri "$BaseUrl/remittances/$remittanceId/reconcile" -Method Post -Headers $faAuthHeader > $null
Write-Host "Remittance Reconciled Successfully!" -ForegroundColor Green

# 11. Credit Interest
Write-Host "`n11. Crediting Interest..."
$accounts = Invoke-RestMethod -Uri "$BaseUrl/members/$memberId/fund-accounts" -Method Get -Headers $faAuthHeader
$accId = $accounts[0].accountId
$intBody = @{ accountId = $accId; financialYear = "2025-26"; interestRate = 8.25 } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/ledger/interest-credit" -Method Post -Body $intBody -Headers $faAuthHeader > $null
Write-Host "Interest Credited Successfully!" -ForegroundColor Green

Write-Host "`n--- ALL CORE FLOW TESTS PASSED! ---" -ForegroundColor Green
