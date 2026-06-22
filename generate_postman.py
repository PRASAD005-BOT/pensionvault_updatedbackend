import json
import os

collection = {
  "info": {
    "name": "PensionVault E2E Workflow",
    "description": "Full lifecycle PensionVault API collection.",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "Module 2.1: Identity & Access Management",
      "item": [
        {
          "name": "[1] Admin Login",
          "request": {
            "method": "POST",
            "header": [{"key":"Content-Type","value":"application/json"}],
            "body": {"mode":"raw","raw":"{\n  \"email\": \"admin@pensionvault.com\",\n  \"password\": \"Admin@123\"\n}"},
            "url": {"raw": "http://localhost:5000/api/auth/login"}
          }
        },
        {
          "name": "[3] Register Employer Person Account",
          "request": {
            "method": "POST",
            "header": [{"key":"Content-Type","value":"application/json"}],
            "body": {"mode":"raw","raw":"{\n  \"name\": \"John Doe\",\n  \"email\": \"hr@techcorp.com\",\n  \"password\": \"Password123!\",\n  \"role\": \"Employer\",\n  \"organisationId\": \"982aa022-1266-4851-9551-9f034a3bafda\"\n}"},
            "url": {"raw": "http://localhost:5000/api/auth/register"}
          }
        },
        {
          "name": "[4] FundAdmin Login",
          "request": {
            "method": "POST",
            "header": [{"key":"Content-Type","value":"application/json"}],
            "body": {"mode":"raw","raw":"{\n  \"email\": \"fundadmin@pensionvault.com\",\n  \"password\": \"FundAdmin@123\"\n}"},
            "url": {"raw": "http://localhost:5000/api/auth/login"}
          }
        },
        {
          "name": "[5] Register Member User Account",
          "request": {
            "method": "POST",
            "header": [{"key":"Content-Type","value":"application/json"}],
            "body": {"mode":"raw","raw":"{\n  \"name\": \"Alice Smith\",\n  \"email\": \"alice@techcorp.com\",\n  \"password\": \"Password@123!\",\n  \"role\": \"Member\"\n}"},
            "url": {"raw": "http://localhost:5000/api/auth/register"}
          }
        },
        {
          "name": "[7] Employer Login",
          "request": {
            "method": "POST",
            "header": [{"key":"Content-Type","value":"application/json"}],
            "body": {"mode":"raw","raw":"{\n  \"email\": \"hr@acmetech.com\",\n  \"password\": \"Employer@123\"\n}"},
            "url": {"raw": "http://localhost:5000/api/auth/login"}
          }
        },
        {
          "name": "[10] Investment Officer Login",
          "request": {
            "method": "POST",
            "header": [{"key":"Content-Type","value":"application/json"}],
            "body": {"mode":"raw","raw":"{\n  \"email\": \"investment@pensionvault.com\",\n  \"password\": \"Invest@123\"\n}"},
            "url": {"raw": "http://localhost:5000/api/auth/login"}
          }
        },
        {
          "name": "[16] Member Login (Alice)",
          "request": {
            "method": "POST",
            "header": [{"key":"Content-Type","value":"application/json"}],
            "body": {"mode":"raw","raw":"{\n  \"email\": \"alice@techcorp.com\",\n  \"password\": \"Password@123!\"\n}"},
            "url": {"raw": "http://localhost:5000/api/auth/login"}
          }
        },
        {
          "name": "[22] Compliance Officer Login",
          "request": {
            "method": "POST",
            "header": [{"key":"Content-Type","value":"application/json"}],
            "body": {"mode":"raw","raw":"{\n  \"email\": \"compliance@pensionvault.com\",\n  \"password\": \"Compliance@123\"\n}"},
            "url": {"raw": "http://localhost:5000/api/auth/login"}
          }
        },
        {
          "name": "[23] Review System Audit Trail",
          "request": {
            "method": "GET",
            "auth": {"type":"bearer","bearer":[{"key":"token","value":"<PASTE_COMPLIANCE_TOKEN>","type":"string"}]},
            "url": {"raw": "http://localhost:5000/api/reports/audit-trail"}
          }
        }
      ]
    },
    {
      "name": "Module 2.2: Member Enrolment",
      "item": [
        {
          "name": "[6] Enroll Member",
          "request": {
            "method": "POST",
            "header": [{"key":"Content-Type","value":"application/json"}],
            "auth": {"type":"bearer","bearer":[{"key":"token","value":"<PASTE_FUNDADMIN_TOKEN>","type":"string"}]},
            "body": {"mode":"raw","raw":"{\n  \"userId\": \"7b632294-4f64-47a9-8f2d-16f8a15cf916\",\n  \"membershipNumber\": \"MEM-001\",\n  \"name\": \"Alice Smith\",\n  \"dateOfBirth\": \"1990-01-01T00:00:00Z\",\n  \"gender\": \"Female\",\n  \"nationalIdRef\": \"NAT-12345\",\n  \"employerId\": \"982aa022-1266-4851-9551-9f034a3bafda\",\n  \"joiningDate\": \"2024-01-01T00:00:00Z\",\n  \"nomineeDetails\": \"{\\\"name\\\":\\\"Bob Smith\\\",\\\"relation\\\":\\\"Spouse\\\",\\\"percent\\\":100}\"\n}"},
            "url": {"raw": "http://localhost:5000/api/members"}
          }
        },
        {
          "name": "[14] Fetch Member's Fund Account Details",
          "request": {
            "method": "GET",
            "auth": {"type":"bearer","bearer":[{"key":"token","value":"<PASTE_FUNDADMIN_TOKEN>","type":"string"}]},
            "url": {"raw": "http://localhost:5000/api/members/a3bf3168-b7a1-4355-8c8a-25dd58f11867/fund-accounts"}
          }
        }
      ]
    },
    {
      "name": "Module 2.3: Contribution Processing",
      "item": [
        {
          "name": "[2] Create Employer Organization",
          "request": {
            "method": "POST",
            "header": [{"key":"Content-Type","value":"application/json"}],
            "auth": {"type":"bearer","bearer":[{"key":"token","value":"<PASTE_ADMIN_TOKEN>","type":"string"}]},
            "body": {"mode":"raw","raw":"{\n  \"companyName\": \"TechCorp Global\",\n  \"registrationNumber\": \"REG-TECH-001\",\n  \"industry\": \"IT\",\n  \"remittanceFrequency\": 0,\n  \"contactDetails\": \"hr@techcorp.com\"\n}"},
            "url": {"raw": "http://localhost:5000/api/employers"}
          }
        },
        {
          "name": "[8] Submit Payroll Remittance",
          "request": {
            "method": "POST",
            "header": [{"key":"Content-Type","value":"application/json"}],
            "auth": {"type":"bearer","bearer":[{"key":"token","value":"<PASTE_EMPLOYER_TOKEN>","type":"string"}]},
            "body": {"mode":"raw","raw":"{\n  \"employerId\": \"519f3cfb-ce6c-4ad4-ad3f-e13ea231e096\",\n  \"remittancePeriod\": \"2026-06\",\n  \"totalEmployeeShare\": 5000,\n  \"totalEmployerShare\": 5000,\n  \"coverageCount\": 1,\n  \"memberContributions\": [\n    {\n      \"memberId\": \"a3bf3168-b7a1-4355-8c8a-25dd58f11867\",\n      \"employeeAmount\": 5000,\n      \"employerAmount\": 5000\n    }\n  ]\n}"},
            "url": {"raw": "http://localhost:5000/api/remittances"}
          }
        },
        {
          "name": "[9] Reconcile Remittance",
          "request": {
            "method": "POST",
            "auth": {"type":"bearer","bearer":[{"key":"token","value":"<PASTE_FUNDADMIN_TOKEN>","type":"string"}]},
            "url": {"raw": "http://localhost:5000/api/remittances/462213e7-893f-4044-bb80-2bb69db0f542/reconcile"}
          }
        }
      ]
    },
    {
      "name": "Module 2.4: Member Account Ledger",
      "item": [
        {
          "name": "[15] Credit Annual Interest",
          "request": {
            "method": "POST",
            "header": [{"key":"Content-Type","value":"application/json"}],
            "auth": {"type":"bearer","bearer":[{"key":"token","value":"<PASTE_FUNDADMIN_TOKEN>","type":"string"}]},
            "body": {"mode":"raw","raw":"{\n  \"accountId\": \"6d496f0a-c9e0-44c3-8248-b92567bb79b2\",\n  \"financialYear\": \"2025-2026\",\n  \"interestRate\": 8.15\n}"},
            "url": {"raw": "http://localhost:5000/api/ledger/interest-credit"}
          }
        }
      ]
    },
    {
      "name": "Module 2.5: Benefit Claim",
      "item": [
        {
          "name": "[17] Submit Partial Withdrawal",
          "request": {
            "method": "POST",
            "header": [{"key":"Content-Type","value":"application/json"}],
            "auth": {"type":"bearer","bearer":[{"key":"token","value":"<PASTE_MEMBER_TOKEN>","type":"string"}]},
            "body": {"mode":"raw","raw":"{\n  \"memberId\": \"a3bf3168-b7a1-4355-8c8a-25dd58f11867\",\n  \"requestedAmount\": 2000.00,\n  \"reason\": \"Housing\"\n}"},
            "url": {"raw": "http://localhost:5000/api/claims/partial-withdrawal"}
          }
        },
        {
          "name": "[18] Approve Claim",
          "request": {
            "method": "PUT",
            "auth": {"type":"bearer","bearer":[{"key":"token","value":"<PASTE_FUNDADMIN_TOKEN>","type":"string"}]},
            "url": {"raw": "http://localhost:5000/api/claims/0793bf19-c7a1-41db-ad87-3bccf3afabc0/approve"}
          }
        },
        {
          "name": "[19] Disburse Claim",
          "request": {
            "method": "POST",
            "header": [{"key":"Content-Type","value":"application/json"}],
            "auth": {"type":"bearer","bearer":[{"key":"token","value":"<PASTE_FUNDADMIN_TOKEN>","type":"string"}]},
            "body": {"mode":"raw","raw":"{\n  \"disbursedAmount\": 2000.00,\n  \"bankAccountRef\": \"HDFC-12345\"\n}"},
            "url": {"raw": "http://localhost:5000/api/claims/0793bf19-c7a1-41db-ad87-3bccf3afabc0/disburse-partial-withdrawal"}
          }
        }
      ]
    },
    {
      "name": "Module 2.6: Investment Allocation",
      "item": [
        {
          "name": "[11] Get Scheme ID",
          "request": {
            "method": "GET",
            "url": {"raw": "http://localhost:5000/api/schemes"}
          }
        },
        {
          "name": "[12] Create Portfolio Allocation",
          "request": {
            "method": "POST",
            "header": [{"key":"Content-Type","value":"application/json"}],
            "auth": {"type":"bearer","bearer":[{"key":"token","value":"<PASTE_INVEST_TOKEN>","type":"string"}]},
            "body": {"mode":"raw","raw":"{\n  \"schemeId\": \"1ecb5a92-243a-46b7-a4ff-5455ba61097e\",\n  \"assetClass\": 0,\n  \"allocationPercent\": 50.00,\n  \"investedValue\": 1000000.00,\n  \"currentValue\": 1050000.00,\n  \"yieldEarned\": 50000.00\n}"},
            "url": {"raw": "http://localhost:5000/api/portfolios"}
          }
        },
        {
          "name": "[13] Log Corpus Valuation",
          "request": {
            "method": "POST",
            "header": [{"key":"Content-Type","value":"application/json"}],
            "auth": {"type":"bearer","bearer":[{"key":"token","value":"<PASTE_INVEST_TOKEN>","type":"string"}]},
            "body": {"mode":"raw","raw":"{\n  \"schemeId\": \"1ecb5a92-243a-46b7-a4ff-5455ba61097e\",\n  \"recordDate\": \"2026-06-30T00:00:00Z\",\n  \"totalContributions\": 1000000.00,\n  \"totalWithdrawals\": 50000.00,\n  \"investmentIncome\": 50000.00,\n  \"managementExpenses\": 5000.00\n}"},
            "url": {"raw": "http://localhost:5000/api/corpus"}
          }
        }
      ]
    },
    {
      "name": "Module 2.7: Pension Annuity",
      "item": [
        {
          "name": "[20] Move Remaining Balance to Annuity",
          "request": {
            "method": "POST",
            "header": [{"key":"Content-Type","value":"application/json"}],
            "auth": {"type":"bearer","bearer":[{"key":"token","value":"<PASTE_FUNDADMIN_TOKEN>","type":"string"}]},
            "body": {"mode":"raw","raw":"{\n  \"memberId\": \"a3bf3168-b7a1-4355-8c8a-25dd58f11867\",\n  \"planType\": 0,\n  \"purchaseValue\": 8000.00,\n  \"monthlyPension\": 100.00,\n  \"annuityStartDate\": \"2026-07-01T00:00:00Z\",\n  \"nomineeDetails\": \"Bob Smith\"\n}"},
            "url": {"raw": "http://localhost:5000/api/annuity"}
          }
        },
        {
          "name": "[21] Disburse Monthly Pension",
          "request": {
            "method": "POST",
            "header": [{"key":"Content-Type","value":"application/json"}],
            "auth": {"type":"bearer","bearer":[{"key":"token","value":"<PASTE_FUNDADMIN_TOKEN>","type":"string"}]},
            "body": {"mode":"raw","raw":"{\n  \"month\": 7,\n  \"year\": 2026,\n  \"taxDeducted\": 10.00\n}"},
            "url": {"raw": "http://localhost:5000/api/annuity/bd2e3dab-9a3d-4ac8-8b75-6cdab558fc28/disburse"}
          }
        }
      ]
    },
    {
      "name": "Module 2.8: Notifications & Alerts",
      "item": [
        {
          "name": "[24] Fetch My Notifications",
          "request": {
            "method": "GET",
            "auth": {"type":"bearer","bearer":[{"key":"token","value":"<PASTE_MEMBER_TOKEN>","type":"string"}]},
            "url": {"raw": "http://localhost:5000/api/notifications"}
          }
        },
        {
          "name": "[25] Mark Notification as Read",
          "request": {
            "method": "PUT",
            "auth": {"type":"bearer","bearer":[{"key":"token","value":"<PASTE_MEMBER_TOKEN>","type":"string"}]},
            "url": {"raw": "http://localhost:5000/api/notifications/075735a0-9021-45da-8708-ef800b631cb9/read"}
          }
        },
        {
          "name": "[26] Mark All Notifications as Read",
          "request": {
            "method": "PUT",
            "auth": {"type":"bearer","bearer":[{"key":"token","value":"<PASTE_MEMBER_TOKEN>","type":"string"}]},
            "url": {"raw": "http://localhost:5000/api/notifications/read-all"}
          }
        }
      ]
    }
  ]
}

with open(r'c:\Users\vadla\Downloads\Pensionvault backend\PensionVault.postman_collection.json', 'w') as f:
    json.dump(collection, f, indent=4)
