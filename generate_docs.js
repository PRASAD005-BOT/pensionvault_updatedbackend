const {
  Document, Packer, Paragraph, TextRun, Table, TableRow, TableCell,
  HeadingLevel, AlignmentType, BorderStyle, WidthType, ShadingType,
  VerticalAlign, LevelFormat, PageNumber, PageBreak, Header, Footer,
  TabStopType, TabStopPosition
} = require('docx');
const fs = require('fs');

// ── Colors ───────────────────────────────────────────────────────────────────
const C = {
  navy:       "1B3A6B",
  navyLight:  "2E5FA3",
  gold:       "B8860B",
  goldBg:     "FFF8E6",
  get_green:  "1A7A4A",
  get_bg:     "EAF7EF",
  post_blue:  "1A4A8A",
  post_bg:    "EAF0FA",
  put_amber:  "8A5A00",
  put_bg:     "FFF4E0",
  del_red:    "8A1A1A",
  del_bg:     "FAEAEA",
  codeBg:     "F4F6FA",
  headBg:     "D6E4F0",
  rowAlt:     "F0F4FA",
  white:      "FFFFFF",
  gray:       "5A6070",
  lightGray:  "E8ECF2",
  darkText:   "1A2035",
};

// ── Borders ──────────────────────────────────────────────────────────────────
const b1 = { style: BorderStyle.SINGLE, size: 1, color: "CCCCCC" };
const bNone = { style: BorderStyle.NONE, size: 0, color: "FFFFFF" };
const borders = { top: b1, bottom: b1, left: b1, right: b1 };
const bordersNone = { top: bNone, bottom: bNone, left: bNone, right: bNone };

// ── Helpers ──────────────────────────────────────────────────────────────────
function hr(color = C.navyLight, thickness = 8) {
  return new Paragraph({
    border: { bottom: { style: BorderStyle.SINGLE, size: thickness, color, space: 1 } },
    spacing: { before: 0, after: 160 },
    children: [],
  });
}

function spacer(pts = 80) {
  return new Paragraph({ spacing: { before: pts, after: 0 }, children: [] });
}

function heading1(text) {
  return new Paragraph({
    heading: HeadingLevel.HEADING_1,
    spacing: { before: 320, after: 80 },
    children: [new TextRun({ text, font: "Arial", size: 36, bold: true, color: C.navy })],
  });
}

function heading2(text) {
  return new Paragraph({
    heading: HeadingLevel.HEADING_2,
    spacing: { before: 240, after: 80 },
    children: [new TextRun({ text, font: "Arial", size: 28, bold: true, color: C.navyLight })],
  });
}

function heading3(text) {
  return new Paragraph({
    heading: HeadingLevel.HEADING_3,
    spacing: { before: 200, after: 60 },
    children: [new TextRun({ text, font: "Arial", size: 24, bold: true, color: C.navy })],
  });
}

function bodyText(text, opts = {}) {
  return new Paragraph({
    spacing: { before: 60, after: 60 },
    children: [new TextRun({ text, font: "Arial", size: 20, color: C.darkText, ...opts })],
  });
}

function inlineCode(text) {
  return new TextRun({ text, font: "Courier New", size: 18, color: C.post_blue, bold: true });
}

function methodBadge(method) {
  const colors = {
    GET:    { bg: C.get_bg,   fg: C.get_green },
    POST:   { bg: C.post_bg,  fg: C.post_blue },
    PUT:    { bg: C.put_bg,   fg: C.put_amber },
    DELETE: { bg: C.del_bg,   fg: C.del_red },
  };
  const { bg, fg } = colors[method] || { bg: C.codeBg, fg: C.darkText };
  return { bg, fg };
}

// Endpoint card: method + path + step badge
function endpointHeader(method, path, step) {
  const { bg, fg } = methodBadge(method);
  const stepBg = C.goldBg;
  const stepFg = C.gold;

  return new Table({
    width: { size: 9360, type: WidthType.DXA },
    columnWidths: [900, 6560, 1900],
    borders: { top: bNone, bottom: bNone, left: bNone, right: bNone,
               insideH: bNone, insideV: bNone },
    rows: [
      new TableRow({
        children: [
          // Method badge
          new TableCell({
            borders: bordersNone,
            shading: { fill: bg, type: ShadingType.CLEAR },
            margins: { top: 80, bottom: 80, left: 120, right: 120 },
            verticalAlign: VerticalAlign.CENTER,
            width: { size: 900, type: WidthType.DXA },
            children: [new Paragraph({
              alignment: AlignmentType.CENTER,
              children: [new TextRun({ text: method, font: "Arial", size: 20, bold: true, color: fg })],
            })],
          }),
          // Path
          new TableCell({
            borders: bordersNone,
            shading: { fill: C.codeBg, type: ShadingType.CLEAR },
            margins: { top: 80, bottom: 80, left: 160, right: 120 },
            verticalAlign: VerticalAlign.CENTER,
            width: { size: 6560, type: WidthType.DXA },
            children: [new Paragraph({
              children: [new TextRun({ text: path, font: "Courier New", size: 18, color: C.post_blue, bold: true })],
            })],
          }),
          // Step badge
          new TableCell({
            borders: bordersNone,
            shading: { fill: stepBg, type: ShadingType.CLEAR },
            margins: { top: 80, bottom: 80, left: 120, right: 120 },
            verticalAlign: VerticalAlign.CENTER,
            width: { size: 1900, type: WidthType.DXA },
            children: [new Paragraph({
              alignment: AlignmentType.CENTER,
              children: [new TextRun({ text: "⚡ Step " + step, font: "Arial", size: 18, bold: true, color: stepFg })],
            })],
          }),
        ],
      }),
    ],
  });
}

// Key-value row for request details
function kvRow(label, value, rowBg = C.white) {
  return new TableRow({
    children: [
      new TableCell({
        borders,
        shading: { fill: C.headBg, type: ShadingType.CLEAR },
        margins: { top: 80, bottom: 80, left: 120, right: 120 },
        width: { size: 1800, type: WidthType.DXA },
        children: [new Paragraph({
          children: [new TextRun({ text: label, font: "Arial", size: 18, bold: true, color: C.navy })],
        })],
      }),
      new TableCell({
        borders,
        shading: { fill: rowBg, type: ShadingType.CLEAR },
        margins: { top: 80, bottom: 80, left: 120, right: 120 },
        width: { size: 7560, type: WidthType.DXA },
        children: [new Paragraph({
          children: [new TextRun({ text: value, font: "Courier New", size: 18, color: C.darkText })],
        })],
      }),
    ],
  });
}

function detailsTable(rows) {
  return new Table({
    width: { size: 9360, type: WidthType.DXA },
    columnWidths: [1800, 7560],
    rows,
  });
}

// Code block (multi-line JSON)
function codeBlock(json) {
  const lines = json.split('\n');
  return new Table({
    width: { size: 9360, type: WidthType.DXA },
    columnWidths: [9360],
    rows: [
      new TableRow({
        children: [
          new TableCell({
            borders,
            shading: { fill: C.codeBg, type: ShadingType.CLEAR },
            margins: { top: 100, bottom: 100, left: 200, right: 200 },
            width: { size: 9360, type: WidthType.DXA },
            children: lines.map(line =>
              new Paragraph({
                spacing: { before: 0, after: 0 },
                children: [new TextRun({ text: line, font: "Courier New", size: 16, color: C.post_blue })],
              })
            ),
          }),
        ],
      }),
    ],
  });
}

function sectionLabel(text) {
  return new Paragraph({
    spacing: { before: 100, after: 60 },
    children: [new TextRun({ text, font: "Arial", size: 18, bold: true, color: C.gray, allCaps: true })],
  });
}

// ── Execution Order Overview Table ───────────────────────────────────────────
function executionTable() {
  const steps = [
    ["1",  "Admin Login",                        "Auth"],
    ["2",  "Create Employer Organization",        "Employers"],
    ["3",  "Register Employer Person Account",    "Auth"],
    ["4",  "FundAdmin Login",                     "Auth"],
    ["5",  "Register Member User Account",        "Auth"],
    ["6",  "Enroll Member (Auto-Creates FundAccount)", "Members"],
    ["7",  "Employer Login",                      "Auth"],
    ["8",  "Submit Payroll Remittance",            "Remittances"],
    ["9",  "Reconcile Remittance",                "Remittances"],
    ["10", "Investment Officer Login",            "Auth"],
    ["11", "Get Scheme ID",                       "Schemes"],
    ["12", "Create Portfolio Allocation",         "Portfolios"],
    ["13", "Log Corpus Valuation",                "Corpus"],
    ["14", "Fetch Member's Fund Account",         "Members"],
    ["15", "Credit Annual Interest",              "Ledger"],
    ["16", "Member (Alice) Login",                "Auth"],
    ["17", "Submit Partial Withdrawal",           "Claims"],
    ["18", "Approve Claim",                       "Claims"],
    ["19", "Disburse Claim",                      "Claims"],
    ["20", "Move Balance to Annuity",             "Annuity"],
    ["21", "Disburse Monthly Pension",            "Annuity"],
    ["22", "Compliance Officer Login",            "Auth"],
    ["23", "Review System Audit Trail",           "Reports"],
    ["24", "Fetch Notifications",                 "Notifications"],
    ["25", "Mark Notification as Read",           "Notifications"],
    ["26", "Mark All Notifications as Read",      "Notifications"],
  ];

  const headerRow = new TableRow({
    tableHeader: true,
    children: ["Step", "Action", "Module"].map((h, i) => new TableCell({
      borders,
      shading: { fill: C.navy, type: ShadingType.CLEAR },
      margins: { top: 80, bottom: 80, left: 120, right: 120 },
      width: { size: [800, 6560, 2000][i], type: WidthType.DXA },
      children: [new Paragraph({
        alignment: AlignmentType.CENTER,
        children: [new TextRun({ text: h, font: "Arial", size: 20, bold: true, color: C.white })],
      })],
    })),
  });

  const dataRows = steps.map(([step, action, mod], idx) =>
    new TableRow({
      children: [
        new TableCell({
          borders,
          shading: { fill: C.goldBg, type: ShadingType.CLEAR },
          margins: { top: 60, bottom: 60, left: 120, right: 120 },
          width: { size: 800, type: WidthType.DXA },
          children: [new Paragraph({
            alignment: AlignmentType.CENTER,
            children: [new TextRun({ text: step, font: "Arial", size: 18, bold: true, color: C.gold })],
          })],
        }),
        new TableCell({
          borders,
          shading: { fill: idx % 2 === 0 ? C.white : C.rowAlt, type: ShadingType.CLEAR },
          margins: { top: 60, bottom: 60, left: 120, right: 120 },
          width: { size: 6560, type: WidthType.DXA },
          children: [new Paragraph({
            children: [new TextRun({ text: action, font: "Arial", size: 18, color: C.darkText })],
          })],
        }),
        new TableCell({
          borders,
          shading: { fill: idx % 2 === 0 ? C.white : C.rowAlt, type: ShadingType.CLEAR },
          margins: { top: 60, bottom: 60, left: 120, right: 120 },
          width: { size: 2000, type: WidthType.DXA },
          children: [new Paragraph({
            children: [new TextRun({ text: mod, font: "Arial", size: 18, color: C.navyLight })],
          })],
        }),
      ],
    })
  );

  return new Table({
    width: { size: 9360, type: WidthType.DXA },
    columnWidths: [800, 6560, 2000],
    rows: [headerRow, ...dataRows],
  });
}

// ── Document ──────────────────────────────────────────────────────────────────
const doc = new Document({
  styles: {
    default: { document: { run: { font: "Arial", size: 20 } } },
    paragraphStyles: [
      { id: "Heading1", name: "Heading 1", basedOn: "Normal", next: "Normal", quickFormat: true,
        run: { size: 36, bold: true, font: "Arial" },
        paragraph: { spacing: { before: 320, after: 80 }, outlineLevel: 0 } },
      { id: "Heading2", name: "Heading 2", basedOn: "Normal", next: "Normal", quickFormat: true,
        run: { size: 28, bold: true, font: "Arial" },
        paragraph: { spacing: { before: 240, after: 80 }, outlineLevel: 1 } },
      { id: "Heading3", name: "Heading 3", basedOn: "Normal", next: "Normal", quickFormat: true,
        run: { size: 24, bold: true, font: "Arial" },
        paragraph: { spacing: { before: 200, after: 60 }, outlineLevel: 2 } },
    ],
  },

  sections: [{
    properties: {
      page: {
        size: { width: 12240, height: 15840 },
        margin: { top: 1080, right: 1080, bottom: 1080, left: 1080 },
      },
    },

    headers: {
      default: new Header({
        children: [
          new Paragraph({
            border: { bottom: { style: BorderStyle.SINGLE, size: 6, color: C.navy, space: 1 } },
            tabStops: [{ type: TabStopType.RIGHT, position: TabStopPosition.MAX }],
            spacing: { after: 120 },
            children: [
              new TextRun({ text: "PensionVault API — Testing Documentation", font: "Arial", size: 18, bold: true, color: C.navy }),
              new TextRun({ text: "\t", font: "Arial", size: 18 }),
              new TextRun({ text: "CONFIDENTIAL · INTERNAL USE ONLY", font: "Arial", size: 16, color: C.gray }),
            ],
          }),
        ],
      }),
    },

    footers: {
      default: new Footer({
        children: [
          new Paragraph({
            border: { top: { style: BorderStyle.SINGLE, size: 6, color: C.navy, space: 1 } },
            tabStops: [{ type: TabStopType.RIGHT, position: TabStopPosition.MAX }],
            spacing: { before: 120 },
            children: [
              new TextRun({ text: "PensionVault Platform — v1.0 · June 2026", font: "Arial", size: 16, color: C.gray }),
              new TextRun({ text: "\tPage ", font: "Arial", size: 16, color: C.gray }),
              new TextRun({ children: [PageNumber.CURRENT], font: "Arial", size: 16, color: C.navy }),
            ],
          }),
        ],
      }),
    },

    children: [

      // ── COVER ──────────────────────────────────────────────────────────────
      new Paragraph({ spacing: { before: 1200, after: 0 }, children: [] }),

      new Paragraph({
        alignment: AlignmentType.CENTER,
        spacing: { before: 0, after: 80 },
        children: [new TextRun({ text: "PensionVault Platform", font: "Arial", size: 56, bold: true, color: C.navy })],
      }),
      new Paragraph({
        alignment: AlignmentType.CENTER,
        spacing: { before: 0, after: 160 },
        children: [new TextRun({ text: "API Testing Documentation", font: "Arial", size: 40, color: C.navyLight })],
      }),

      hr(C.gold, 16),

      new Paragraph({
        alignment: AlignmentType.CENTER,
        spacing: { before: 120, after: 80 },
        children: [new TextRun({ text: "Module-Wise Workflow Guide", font: "Arial", size: 28, italic: true, color: C.gray })],
      }),
      new Paragraph({
        alignment: AlignmentType.CENTER,
        spacing: { before: 0, after: 60 },
        children: [new TextRun({ text: "8 Modules  ·  26 Execution Steps  ·  Postman Compatible", font: "Arial", size: 22, color: C.gray })],
      }),
      new Paragraph({
        alignment: AlignmentType.CENTER,
        spacing: { before: 0, after: 0 },
        children: [new TextRun({ text: "June 2026  ·  Internal Use Only", font: "Arial", size: 20, color: C.lightGray })],
      }),

      new Paragraph({ children: [new PageBreak()] }),

      // ── INTRO ──────────────────────────────────────────────────────────────
      heading1("Overview"),
      hr(C.navyLight, 6),
      spacer(60),

      bodyText("This document provides a complete, module-wise API testing guide for the PensionVault Platform. Each section corresponds to one of the 8 architectural modules defined in the PensionVault Problem Statement, and every endpoint is annotated with its required Execution Step number for sequential Postman testing."),
      spacer(80),

      new Paragraph({
        spacing: { before: 60, after: 60 },
        children: [
          new TextRun({ text: "⚠  Important: ", font: "Arial", size: 20, bold: true, color: C.put_amber }),
          new TextRun({ text: "Always execute steps in the order shown by the ", font: "Arial", size: 20, color: C.darkText }),
          new TextRun({ text: "⚡ Step N", font: "Arial", size: 20, bold: true, color: C.gold }),
          new TextRun({ text: " badges. Database foreign-key constraints require this sequence.", font: "Arial", size: 20, color: C.darkText }),
        ],
      }),
      spacer(80),

      heading2("Legend"),
      spacer(40),

      detailsTable([
        kvRow("GET",    "Retrieve data — no request body required",    C.get_bg),
        kvRow("POST",   "Create a new resource — requires JSON body",  C.post_bg),
        kvRow("PUT",    "Update or approve an existing resource",       C.put_bg),
        kvRow("Auth",   "Bearer token from the relevant Login step",   C.white),
        kvRow("⚡ Step","Postman execution order number",              C.goldBg),
      ]),
      spacer(120),

      heading2("Execution Order"),
      spacer(40),
      executionTable(),

      new Paragraph({ children: [new PageBreak()] }),

      // ── MODULE 2.1 ────────────────────────────────────────────────────────
      heading1("Module 2.1 — Identity & Access Management"),
      hr(C.navyLight, 6),
      bodyText("Authentication, RBAC, User Registration, and Audit Trails.", { italic: true, color: C.gray }),
      spacer(80),

      // A. Admin Login
      heading3("A. Admin Login"),
      spacer(40),
      endpointHeader("POST", "/api/auth/login", "1"),
      spacer(40),
      detailsTable([
        kvRow("Auth",     "None (Public)"),
        kvRow("Body",     '{ "email": "admin@pensionvault.com", "password": "Admin@123" }'),
      ]),
      spacer(60),
      sectionLabel("Expected Response"),
      codeBlock(`{
    "userId": "69320210-7912-44b4-95b0-2b5c52f248a3",
    "name": "System Administrator",
    "email": "admin@pensionvault.com",
    "role": "Admin",
    "token": "eyJhbGci...",
    "refreshToken": "XU+KlTmAAFMMkhm2cvETgva6...",
    "tokenExpiry": "2026-06-22T17:22:16.8374505Z"
}`),
      spacer(100),

      // B. Register Employer Person Account
      heading3("B. Register Employer Person Account"),
      spacer(40),
      endpointHeader("POST", "/api/auth/register", "3"),
      spacer(40),
      detailsTable([kvRow("Auth", "None (Public)")]),
      spacer(40),
      sectionLabel("Request Body"),
      codeBlock(`{
    "name": "John Doe",
    "email": "hr@techcorp.com",
    "password": "Password123!",
    "role": "Employer",
    "organisationId": "982aa022-1266-4851-9551-9f034a3bafda"
}`),
      spacer(60),
      sectionLabel("Expected Response"),
      codeBlock(`{
    "userId": "8173db31-cebd-4837-9c0c-72cd44e3baca",
    "name": "John Doe",
    "email": "hr@techcorp.com",
    "role": "Employer",
    "token": "eyJhbGci..."
}`),
      spacer(100),

      // C. FundAdmin Login
      heading3("C. FundAdmin Login"),
      spacer(40),
      endpointHeader("POST", "/api/auth/login", "4"),
      spacer(40),
      detailsTable([
        kvRow("Auth", "None (Public)"),
        kvRow("Body", '{ "email": "fundadmin@pensionvault.com", "password": "FundAdmin@123" }'),
      ]),
      spacer(60),
      sectionLabel("Expected Response"),
      codeBlock(`{
    "userId": "3dbcc581-39bc-4f91-958e-253cba46bafc",
    "name": "Fund Administrator",
    "email": "fundadmin@pensionvault.com",
    "role": "FundAdmin",
    "token": "eyJhbGci..."
}`),
      spacer(100),

      // D. Register Member User Account
      heading3("D. Register Member User Account"),
      spacer(40),
      endpointHeader("POST", "/api/auth/register", "5"),
      spacer(40),
      detailsTable([kvRow("Auth", "None (Public)")]),
      spacer(40),
      sectionLabel("Request Body"),
      codeBlock(`{
    "name": "Alice Smith",
    "email": "alice@techcorp.com",
    "password": "Password@123!",
    "role": "Member"
}`),
      spacer(60),
      sectionLabel("Expected Response"),
      codeBlock(`{
    "userId": "7b632294-4f64-47a9-8f2d-16f8a15cf916",
    "name": "Alice Smith",
    "email": "alice@techcorp.com",
    "role": "Member",
    "token": "eyJhbGci..."
}`),
      spacer(100),

      // E. Employer Login
      heading3("E. Employer Login"),
      spacer(40),
      endpointHeader("POST", "/api/auth/login", "7"),
      spacer(40),
      detailsTable([
        kvRow("Auth", "None (Public)"),
        kvRow("Body", '{ "email": "hr@acmetech.com", "password": "Employer@123" }'),
      ]),
      spacer(60),
      sectionLabel("Expected Response"),
      codeBlock(`{
    "userId": "be622caa-ab4a-4b6b-94b1-89c8b35c8562",
    "name": "Acme HR Manager",
    "email": "hr@acmetech.com",
    "role": "Employer",
    "token": "eyJhbGci..."
}`),
      spacer(100),

      // F. Investment Officer Login
      heading3("F. Investment Officer Login"),
      spacer(40),
      endpointHeader("POST", "/api/auth/login", "10"),
      spacer(40),
      detailsTable([
        kvRow("Auth", "None (Public)"),
        kvRow("Body", '{ "email": "investment@pensionvault.com", "password": "Invest@123" }'),
      ]),
      spacer(60),
      sectionLabel("Expected Response"),
      codeBlock(`{
    "userId": "13d46062-778e-4618-95ce-988b326b7345",
    "name": "Investment Officer",
    "email": "investment@pensionvault.com",
    "role": "InvestmentOfficer",
    "token": "eyJhbGci..."
}`),
      spacer(100),

      // G. Compliance Officer Login
      heading3("G. Compliance Officer Login"),
      spacer(40),
      endpointHeader("POST", "/api/auth/login", "22"),
      spacer(40),
      detailsTable([
        kvRow("Auth", "None (Public)"),
        kvRow("Body", '{ "email": "compliance@pensionvault.com", "password": "Compliance@123" }'),
      ]),
      spacer(60),
      sectionLabel("Expected Response"),
      codeBlock(`{
    "userId": "10cf002b-70d4-4ebe-a5de-23688996a495",
    "name": "Compliance Officer",
    "email": "compliance@pensionvault.com",
    "role": "Compliance",
    "token": "eyJhbGci..."
}`),
      spacer(100),

      // H. Audit Trail
      heading3("H. Review System Audit Trail"),
      spacer(40),
      endpointHeader("GET", "/api/reports/audit-trail", "23"),
      spacer(40),
      detailsTable([kvRow("Auth", "Compliance Bearer Token")]),
      spacer(60),
      sectionLabel("Expected Response"),
      codeBlock(`[
    {
        "auditId": "1499954d-b826-4d8e-a715-426f2950b72a",
        "userName": "Fund Administrator",
        "action": "ProcessDisbursementAnnuity",
        "entityType": "Annuity",
        "recordId": "bd2e3dab-9a3d-4ac8-8b75-6cdab558fc28",
        "timestamp": "2026-06-22T16:57:40.9738119"
    }
]`),

      new Paragraph({ children: [new PageBreak()] }),

      // ── MODULE 2.2 ────────────────────────────────────────────────────────
      heading1("Module 2.2 — Member Enrolment & Account Management"),
      hr(C.navyLight, 6),
      bodyText("Manages member registrations, employer linkages, and individual fund account records.", { italic: true, color: C.gray }),
      spacer(80),

      // A. Enroll Member
      heading3("A. Enroll Member (Auto-Creates FundAccount)"),
      spacer(40),
      endpointHeader("POST", "/api/members", "6"),
      spacer(40),
      detailsTable([kvRow("Auth", "FundAdmin Bearer Token")]),
      spacer(40),
      sectionLabel("Request Body"),
      codeBlock(`{
    "userId": "7b632294-4f64-47a9-8f2d-16f8a15cf916",
    "membershipNumber": "MEM-001",
    "name": "Alice Smith",
    "dateOfBirth": "1990-01-01T00:00:00Z",
    "gender": "Female",
    "nationalIdRef": "NAT-12345",
    "employerId": "982aa022-1266-4851-9551-9f034a3bafda",
    "joiningDate": "2024-01-01T00:00:00Z",
    "nomineeDetails": "{\\"name\\":\\"Bob Smith\\",\\"relation\\":\\"Spouse\\",\\"percent\\":100}"
}`),
      spacer(60),
      sectionLabel("Expected Response"),
      codeBlock(`{
    "memberId": "a3bf3168-b7a1-4355-8c8a-25dd58f11867",
    "membershipNumber": "MEM-001",
    "name": "Alice Smith",
    "employerName": "TechCorp Global",
    "joiningDate": "2024-01-01T00:00:00Z",
    "dateOfRetirement": "2050-01-01T00:00:00Z",
    "status": "Active"
}`),
      spacer(100),

      // B. Fetch Fund Account
      heading3("B. Fetch Member's Fund Account Details"),
      spacer(40),
      endpointHeader("GET", "/api/members/{memberId}/fund-accounts", "14"),
      spacer(40),
      detailsTable([kvRow("Auth", "FundAdmin Bearer Token")]),
      spacer(60),
      sectionLabel("Expected Response"),
      codeBlock(`[
    {
        "accountId": "6d496f0a-c9e0-44c3-8248-b92567bb79b2",
        "schemeName": "Gratuity Trust Fund",
        "employeeContributionBalance": 5000.00,
        "employerContributionBalance": 5000.00,
        "interestAccrued": 0.00,
        "totalBalance": 10000.00,
        "vestingPercent": 100.00,
        "status": "Active"
    }
]`),

      new Paragraph({ children: [new PageBreak()] }),

      // ── MODULE 2.3 ────────────────────────────────────────────────────────
      heading1("Module 2.3 — Contribution Processing & Employer Management"),
      hr(C.navyLight, 6),
      bodyText("Handles monthly employer contribution remittances, reconciliation, and default tracking.", { italic: true, color: C.gray }),
      spacer(80),

      // A. Create Employer
      heading3("A. Create Employer Organization"),
      spacer(40),
      endpointHeader("POST", "/api/employers", "2"),
      spacer(40),
      detailsTable([kvRow("Auth", "Admin Bearer Token")]),
      spacer(40),
      sectionLabel("Request Body"),
      codeBlock(`{
    "companyName": "TechCorp Global",
    "registrationNumber": "REG-TECH-001",
    "industry": "IT",
    "remittanceFrequency": 0,
    "contactDetails": "hr@techcorp.com"
}`),
      spacer(60),
      sectionLabel("Expected Response"),
      codeBlock(`{
    "employerId": "982aa022-1266-4851-9551-9f034a3bafda",
    "companyName": "TechCorp Global",
    "registrationNumber": "REG-TECH-001",
    "enrolledMemberCount": 0,
    "remittanceFrequency": "Monthly",
    "status": "Active"
}`),
      spacer(100),

      // B. Submit Remittance
      heading3("B. Submit Payroll Remittance"),
      spacer(40),
      endpointHeader("POST", "/api/remittances", "8"),
      spacer(40),
      detailsTable([kvRow("Auth", "Employer Bearer Token")]),
      spacer(40),
      sectionLabel("Request Body"),
      codeBlock(`{
    "employerId": "519f3cfb-ce6c-4ad4-ad3f-e13ea231e096",
    "remittancePeriod": "2026-06",
    "totalEmployeeShare": 5000,
    "totalEmployerShare": 5000,
    "coverageCount": 1,
    "memberContributions": [
        {
            "memberId": "a3bf3168-b7a1-4355-8c8a-25dd58f11867",
            "employeeAmount": 5000,
            "employerAmount": 5000
        }
    ]
}`),
      spacer(60),
      sectionLabel("Expected Response"),
      codeBlock(`{
    "remittanceId": "462213e7-893f-4044-bb80-2bb69db0f542",
    "remittancePeriod": "2026-06",
    "totalAmount": 10000,
    "status": "Received"
}`),
      spacer(100),

      // C. Reconcile
      heading3("C. Reconcile Remittance"),
      spacer(40),
      endpointHeader("POST", "/api/remittances/{remittanceId}/reconcile", "9"),
      spacer(40),
      detailsTable([kvRow("Auth", "FundAdmin Bearer Token")]),
      spacer(60),
      sectionLabel("Expected Response"),
      codeBlock(`{
    "remittanceId": "462213e7-893f-4044-bb80-2bb69db0f542",
    "employerName": "Acme Technologies Pvt Ltd",
    "remittancePeriod": "2026-06",
    "totalAmount": 10000.00,
    "status": "Reconciled"
}`),

      new Paragraph({ children: [new PageBreak()] }),

      // ── MODULE 2.4 ────────────────────────────────────────────────────────
      heading1("Module 2.4 — Member Account Ledger & Interest Crediting"),
      hr(C.navyLight, 6),
      bodyText("Maintains individual member account ledgers with contribution postings and annual interest credits.", { italic: true, color: C.gray }),
      spacer(80),

      heading3("A. Credit Annual Interest"),
      spacer(40),
      endpointHeader("POST", "/api/ledger/interest-credit", "15"),
      spacer(40),
      detailsTable([kvRow("Auth", "FundAdmin Bearer Token")]),
      spacer(40),
      sectionLabel("Request Body"),
      codeBlock(`{
    "accountId": "6d496f0a-c9e0-44c3-8248-b92567bb79b2",
    "financialYear": "2025-2026",
    "interestRate": 8.15
}`),
      spacer(60),
      sectionLabel("Expected Response"),
      codeBlock(`{
    "interestId": "9081904f-79cf-4efc-b12c-b9f0d45ea0c3",
    "financialYear": "2025-2026",
    "openingBalance": 0.00,
    "totalContributions": 10000.00,
    "interestRateApplied": 8.15,
    "interestAmount": 407.50,
    "closingBalance": 10407.50,
    "status": "Credited"
}`),

      new Paragraph({ children: [new PageBreak()] }),

      // ── MODULE 2.5 ────────────────────────────────────────────────────────
      heading1("Module 2.5 — Benefit Claim & Withdrawal Management"),
      hr(C.navyLight, 6),
      bodyText("Processes retirement, resignation, partial withdrawal, and nominee claim applications.", { italic: true, color: C.gray }),
      spacer(80),

      // A. Submit Partial Withdrawal
      heading3("A. Submit Partial Withdrawal"),
      spacer(40),
      endpointHeader("POST", "/api/claims/partial-withdrawal", "17"),
      spacer(40),
      detailsTable([
        kvRow("Auth", "Member Bearer Token (Alice)"),
        kvRow("Note",  "Alice must login first — see Step 16"),
      ]),
      spacer(40),
      sectionLabel("Request Body"),
      codeBlock(`{
    "memberId": "a3bf3168-b7a1-4355-8c8a-25dd58f11867",
    "requestedAmount": 2000.00,
    "reason": "Housing"
}`),
      spacer(60),
      sectionLabel("Expected Response"),
      codeBlock(`{
    "claimId": "0793bf19-c7a1-41db-ad87-3bccf3afabc0",
    "memberName": "Alice Smith",
    "claimType": "PartialWithdrawal",
    "eligibleAmount": 2000.00,
    "vestedAmount": 10407.50,
    "taxDeductible": 200.00,
    "status": "Submitted"
}`),
      spacer(100),

      // B. Approve Claim
      heading3("B. Approve Claim"),
      spacer(40),
      endpointHeader("PUT", "/api/claims/{claimId}/approve", "18"),
      spacer(40),
      detailsTable([kvRow("Auth", "FundAdmin Bearer Token")]),
      spacer(60),
      sectionLabel("Expected Response"),
      codeBlock(`{
    "claimId": "0793bf19-c7a1-41db-ad87-3bccf3afabc0",
    "memberName": "Alice Smith",
    "claimType": "PartialWithdrawal",
    "eligibleAmount": 2000.00,
    "processedByName": "Fund Administrator",
    "status": "Approved"
}`),
      spacer(100),

      // C. Disburse Claim
      heading3("C. Disburse Claim"),
      spacer(40),
      endpointHeader("POST", "/api/claims/{claimId}/disburse-partial-withdrawal", "19"),
      spacer(40),
      detailsTable([kvRow("Auth", "FundAdmin Bearer Token")]),
      spacer(40),
      sectionLabel("Request Body"),
      codeBlock(`{
    "disbursedAmount": 2000.00,
    "bankAccountRef": "HDFC-12345"
}`),
      spacer(60),
      sectionLabel("Expected Response"),
      codeBlock(`{
    "disbursementId": "391f25cd-ea2d-45f3-848d-d402774fa3f2",
    "disbursedAmount": 2000.00,
    "taxDeducted": 200.00,
    "netAmount": 1800.00,
    "bankAccountRef": "HDFC-12345",
    "status": "Processed"
}`),

      new Paragraph({ children: [new PageBreak()] }),

      // ── MODULE 2.6 ────────────────────────────────────────────────────────
      heading1("Module 2.6 — Investment Fund Allocation & Corpus Management"),
      hr(C.navyLight, 6),
      bodyText("Manages fund-level investment portfolio allocation, yield tracking, and corpus reporting.", { italic: true, color: C.gray }),
      spacer(80),

      // A. Get Scheme ID
      heading3("A. Get Scheme ID"),
      spacer(40),
      endpointHeader("GET", "/api/schemes", "11"),
      spacer(40),
      detailsTable([kvRow("Auth", "None required")]),
      spacer(60),
      sectionLabel("Expected Response"),
      codeBlock(`[
    {
        "schemeId": "1ecb5a92-243a-46b7-a4ff-5455ba61097e",
        "schemeName": "Employee Provident Fund",
        "schemeType": "EPF",
        "status": "Active"
    }
]`),
      spacer(100),

      // B. Create Portfolio Allocation
      heading3("B. Create Portfolio Allocation"),
      spacer(40),
      endpointHeader("POST", "/api/portfolios", "12"),
      spacer(40),
      detailsTable([kvRow("Auth", "Investment Officer Bearer Token")]),
      spacer(40),
      sectionLabel("Request Body"),
      codeBlock(`{
    "schemeId": "1ecb5a92-243a-46b7-a4ff-5455ba61097e",
    "assetClass": 0,
    "allocationPercent": 50.00,
    "investedValue": 1000000.00,
    "currentValue": 1050000.00,
    "yieldEarned": 50000.00
}`),
      spacer(100),

      // C. Log Corpus Valuation
      heading3("C. Log Corpus Valuation"),
      spacer(40),
      endpointHeader("POST", "/api/corpus", "13"),
      spacer(40),
      detailsTable([kvRow("Auth", "Investment Officer Bearer Token")]),
      spacer(40),
      sectionLabel("Request Body"),
      codeBlock(`{
    "schemeId": "1ecb5a92-243a-46b7-a4ff-5455ba61097e",
    "recordDate": "2026-06-30T00:00:00Z",
    "totalContributions": 1000000.00,
    "totalWithdrawals": 50000.00,
    "investmentIncome": 50000.00,
    "managementExpenses": 5000.00
}`),
      spacer(60),
      sectionLabel("Expected Response"),
      codeBlock(`{
    "corpusId": "c7eac7b9-9bde-4ebc-a605-92f669ebb698",
    "schemeName": "Employee Provident Fund",
    "openingCorpus": 10580000.00,
    "closingCorpus": 10580000.00,
    "status": "Draft"
}`),

      new Paragraph({ children: [new PageBreak()] }),

      // ── MODULE 2.7 ────────────────────────────────────────────────────────
      heading1("Module 2.7 — Pension Annuity & Settlement Management"),
      hr(C.navyLight, 6),
      bodyText("Manages annuity plan selection, monthly pension disbursements, and nominee settlements.", { italic: true, color: C.gray }),
      spacer(80),

      // A. Move to Annuity
      heading3("A. Move Remaining Balance to Annuity"),
      spacer(40),
      endpointHeader("POST", "/api/annuity", "20"),
      spacer(40),
      detailsTable([kvRow("Auth", "FundAdmin Bearer Token")]),
      spacer(40),
      sectionLabel("Request Body"),
      codeBlock(`{
    "memberId": "a3bf3168-b7a1-4355-8c8a-25dd58f11867",
    "planType": 0,
    "purchaseValue": 8000.00,
    "monthlyPension": 100.00,
    "annuityStartDate": "2026-07-01T00:00:00Z",
    "nomineeDetails": "Bob Smith"
}`),
      spacer(60),
      sectionLabel("Expected Response"),
      codeBlock(`{
    "annuityId": "bd2e3dab-9a3d-4ac8-8b75-6cdab558fc28",
    "memberName": "Alice Smith",
    "planType": "LifeAnnuity",
    "purchaseValue": 8000.00,
    "monthlyPension": 100.00,
    "annuityStartDate": "2026-07-01T00:00:00Z",
    "status": "Active"
}`),
      spacer(100),

      // B. Disburse Monthly Pension
      heading3("B. Disburse Monthly Pension"),
      spacer(40),
      endpointHeader("POST", "/api/annuity/{annuityId}/disburse", "21"),
      spacer(40),
      detailsTable([kvRow("Auth", "FundAdmin Bearer Token")]),
      spacer(40),
      sectionLabel("Request Body"),
      codeBlock(`{
    "month": 7,
    "year": 2026,
    "taxDeducted": 10.00
}`),
      spacer(60),
      sectionLabel("Expected Response"),
      codeBlock(`{
    "disbursementId": "e1b694c4-6673-4ae4-a3fb-f53ea74709c6",
    "memberName": "Alice Smith",
    "month": 7,
    "year": 2026,
    "grossAmount": 100.00,
    "taxDeducted": 10.00,
    "netAmount": 90.00,
    "status": "Disbursed"
}`),

      new Paragraph({ children: [new PageBreak()] }),

      // ── MODULE 2.8 ────────────────────────────────────────────────────────
      heading1("Module 2.8 — Notifications & Alerts"),
      hr(C.navyLight, 6),
      bodyText("Contribution credit confirmations, interest crediting alerts, claim status updates, and compliance reminders.", { italic: true, color: C.gray }),
      spacer(60),
      bodyText("Notifications are generated automatically by the system during Member Registration, Interest Crediting, and Claim processing events."),
      spacer(80),

      // A. Fetch Notifications
      heading3("A. Fetch My Notifications"),
      spacer(40),
      endpointHeader("GET", "/api/notifications", "24"),
      spacer(40),
      detailsTable([kvRow("Auth", "Member Bearer Token (Alice)")]),
      spacer(60),
      sectionLabel("Expected Response"),
      codeBlock(`[
    {
        "notificationId": "33283ce7-119e-483e-ba80-14474c7f812f",
        "message": "Your claim payout of Rs.1,800.00 has been disbursed to HDFC-12345.",
        "category": "Claim",
        "status": "Unread",
        "createdDate": "2026-06-22T16:54:34.3911946"
    },
    {
        "notificationId": "6d7c53f1-acfc-4c35-9a2a-8b3a5fe77839",
        "message": "Your PartialWithdrawal claim for Rs.2,000.00 has been APPROVED.",
        "category": "Claim",
        "status": "Unread"
    }
]`),
      spacer(100),

      // B. Mark as Read
      heading3("B. Mark Notification as Read"),
      spacer(40),
      endpointHeader("PUT", "/api/notifications/{notificationId}/read", "25"),
      spacer(40),
      detailsTable([
        kvRow("Auth",     "Member Bearer Token (Alice)"),
        kvRow("Response", "204 No Content"),
      ]),
      spacer(100),

      // C. Mark All as Read
      heading3("C. Mark All Notifications as Read"),
      spacer(40),
      endpointHeader("PUT", "/api/notifications/read-all", "26"),
      spacer(40),
      detailsTable([
        kvRow("Auth",     "Member Bearer Token (Alice)"),
        kvRow("Response", "204 No Content"),
      ]),
      spacer(120),

      hr(C.gold, 8),
      new Paragraph({
        alignment: AlignmentType.CENTER,
        spacing: { before: 120, after: 60 },
        children: [new TextRun({ text: "End of Document", font: "Arial", size: 20, bold: true, color: C.gray })],
      }),
      new Paragraph({
        alignment: AlignmentType.CENTER,
        spacing: { before: 0, after: 0 },
        children: [new TextRun({ text: "PensionVault Platform  ·  All 8 Modules Covered  ·  26 Execution Steps", font: "Arial", size: 18, color: C.lightGray })],
      }),
    ],
  }],
});

Packer.toBuffer(doc).then(buf => {
  fs.writeFileSync("c:\\Users\\vadla\\Downloads\\Pensionvault backend\\PensionVault_API_Testing_Documentation.docx", buf);
  console.log("Done.");
});
