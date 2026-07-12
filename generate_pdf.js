const PDFDocument = require('pdfkit');
const fs = require('fs');

// Initialize A4 document with 36pt (0.5 inch) margins
// Use bufferPages: true to allow switching back to write page numbers on footer
const doc = new PDFDocument({ size: 'A4', margin: 36, bufferPages: true });
const outputPath = 'C:\\Users\\vadla\\Downloads\\Pensionvault backend\\PensionVault_Developer_Reference.pdf';
const stream = fs.createWriteStream(outputPath);
doc.pipe(stream);

// ── COLOR PALETTE ───────────────────────────────────────────────────────────
const PALETTE = {
  navy: '#0f1e35',
  navy2: '#1a2e4a',
  blue: '#2563eb',
  lblue: '#dbeafe',
  teal: '#0f766e',
  lteal: '#ccfbf1',
  purple: '#7c3aed',
  lpurp: '#ede9fe',
  amber: '#b45309',
  lamber: '#fef3c7',
  gold: '#d97706',
  gray1: '#f1f5f9',
  gray2: '#94a3b8',
  gray3: '#334155',
  codebg: '#1e293b',
  codefg: '#e2e8f0',
  green: '#16a34a',
  lgreen: '#dcfce7',
  red: '#dc2626',
  lred: '#fee2e2',
  white: '#ffffff',
  accent: '#0ea5e9',
  silver: '#94a3b8',
};

// ════════════════════════════════════════════════════════════════════════════
// PAGE 1 — COVER PAGE
// ════════════════════════════════════════════════════════════════════════════
function drawCover() {
  const W = 595.28;
  const H = 841.89;

  // Background
  doc.rect(0, 0, W, H).fill(PALETTE.navy);

  // Decorative circles
  doc.fillColor('#162540').circle(W + 50, 100, 200).fill();
  doc.fillColor('rgba(255, 255, 255, 0.02)').circle(-50, 800, 180).fill();

  // Top accent bar
  doc.rect(0, 0, W, 8).fill(PALETTE.blue);

  // Top header text
  doc.fillColor(PALETTE.accent).font('Helvetica-Bold').fontSize(8.5).text('PENSIONVAULT  •  BACKEND REFERENCE', 50, 40);
  doc.fillColor(PALETTE.silver).font('Helvetica').fontSize(8.5).text('v1.0  •  Developer Reference', 300, 40, { align: 'right', width: 245 });

  doc.strokeColor('#2563eb').lineWidth(0.5).moveTo(50, 55).lineTo(545, 55).stroke();

  const midY = H * 0.46;

  // Badge card
  doc.fillColor('#1e3a5f').roundedRect(150, midY - 60, 295, 20, 4).fill();
  doc.fillColor(PALETTE.accent).font('Helvetica-Bold').fontSize(9).text('Clean Architecture  •  13 Files  •  4 Layers', 150, midY - 54, { align: 'center', width: 295 });

  // Main Titles
  doc.fillColor(PALETTE.white).font('Helvetica-Bold').fontSize(44).text('PensionVault', 50, midY - 20, { align: 'center', width: 495 });
  doc.fillColor('#93c5fd').font('Helvetica').fontSize(18).text('Complete Developer Walkthrough Guide', 50, midY + 30, { align: 'center', width: 495 });

  // Divider
  doc.strokeColor(PALETTE.blue).lineWidth(1).moveTo(247, midY + 60).lineTo(347, midY + 60).stroke();

  // Subtitles
  doc.fillColor(PALETTE.silver).font('Helvetica').fontSize(11.5).text('Module 2.1 — Identity & Access Management', 50, midY + 75, { align: 'center', width: 495 });
  doc.text('Module 2.7 — Pension Annuity & Settlement', 50, midY + 95, { align: 'center', width: 495 });

  // Layer cards (4 columns)
  const cy = midY + 140;
  const cw = 105;
  const ch = 75;
  const gap = 15;
  const sx = (W - (4 * cw + 3 * gap)) / 2;

  const cards = [
    { title: 'API Layer', count: '3 files', mainCol: PALETTE.blue, bg: '#1e3a8a' },
    { title: 'Application', count: '4 files', mainCol: PALETTE.teal, bg: '#134e4a' },
    { title: 'Domain', count: '3 interfaces', mainCol: PALETTE.purple, bg: '#2e1065' },
    { title: 'Infrastructure', count: '3 files', mainCol: PALETTE.amber, bg: '#78350f' }
  ];

  cards.forEach((card, i) => {
    const cx = sx + i * (cw + gap);
    doc.fillColor(card.bg).roundedRect(cx, cy, cw, ch, 6).fill();
    doc.fillColor(card.mainCol).rect(cx, cy, 5, ch).fill();
    doc.fillColor(PALETTE.white).font('Helvetica-Bold').fontSize(9.5).text(card.title, cx + 12, cy + 18, { width: cw - 20 });
    doc.fillColor(PALETTE.silver).font('Helvetica').fontSize(8.5).text(card.count, cx + 12, cy + 42);
  });

  // Tech chips
  const cy2 = cy + 105;
  const chips = ['ASP.NET Core', 'EF Core', 'JWT Bearer', 'BCrypt', 'Serilog', 'SQL Server'];
  let chipX = 75;
  doc.font('Helvetica').fontSize(7.5);
  chips.forEach((chip) => {
    const width = doc.widthOfString(chip) + 12;
    doc.fillColor('#1e293b').roundedRect(chipX, cy2, width, 16, 8).fill();
    doc.strokeColor('#334155').lineWidth(0.5).roundedRect(chipX, cy2, width, 16, 8).stroke();
    doc.fillColor(PALETTE.silver).text(chip, chipX + 6, cy2 + 4.5);
    chipX += width + 6;
  });

  // Bottom footer strip
  doc.fillColor('#0a1628').rect(0, H - 40, W, 40).fill();
  doc.strokeColor('#1e3a5f').lineWidth(0.5).moveTo(0, H - 40).lineTo(W, H - 40).stroke();
  doc.fillColor(PALETTE.silver).font('Helvetica').fontSize(8).text('PensionVault Backend  •  Internal Technical Documentation', 50, H - 24);
  doc.fillColor(PALETTE.blue).font('Helvetica-Bold').fontSize(8).text('PAGE 1  /  COVER', W - 150, H - 24, { align: 'right', width: 100 });
  doc.rect(0, H - 4, W, 4).fill(PALETTE.blue);
}

// ════════════════════════════════════════════════════════════════════════════
// PAGE 2 — ARCHITECTURE DIAGRAM
// ════════════════════════════════════════════════════════════════════════════
function drawDiagram() {
  doc.addPage();
  const W = 595.28;
  const H = 841.89;

  // Background
  doc.fillColor('#f8fafc').rect(0, 0, W, H).fill();

  // Top header bar
  doc.fillColor(PALETTE.navy2).rect(0, 0, W, 70).fill();
  doc.fillColor(PALETTE.blue).rect(0, 0, W, 5).fill();
  doc.fillColor(PALETTE.white).font('Helvetica-Bold').fontSize(16).text('PensionVault — Clean Architecture', 50, 22, { align: 'center', width: 495 });
  doc.fillColor('#93c5fd').font('Helvetica').fontSize(8.5).text('13 files across 4 layers  |  Module 2.1 Auth  &  Module 2.7 Annuity', 50, 44, { align: 'center', width: 495 });

  const LX = 50;
  const LW = 495;

  function drawLayerBox(y, h, title, project, desc, col, bg, files) {
    doc.fillColor(bg).roundedRect(LX, y, LW, h, 6).fill();
    doc.strokeColor(col).lineWidth(0.8).roundedRect(LX, y, LW, h, 6).stroke();
    doc.fillColor(col).rect(LX, y, LW, 16).fill();
    doc.fillColor(PALETTE.white).font('Helvetica-Bold').fontSize(9.5).text(title, LX + 10, y + 4.5);
    doc.fillColor('rgba(255, 255, 255, 0.8)').font('Helvetica').fontSize(8.5).text(project, LX + LW - 200, y + 4.5, { align: 'right', width: 190 });
    doc.fillColor(PALETTE.gray3).font('Helvetica').fontSize(8).text(desc, LX + 10, y + 21);

    let chipX = LX + 10;
    const chipY = y + 33;
    const chipH = 15;
    files.forEach((file) => {
      doc.font('Courier').fontSize(7.5);
      const fw = doc.widthOfString(file) + 10;
      doc.fillColor('rgba(0, 0, 0, 0.06)').roundedRect(chipX, chipY, fw, chipH, 2).fill();
      doc.fillColor(col).text(file, chipX + 5, chipY + 4.5);
      chipX += fw + 6;
    });
  }

  function drawConnectorArrow(y, label) {
    const cx = W / 2;
    doc.strokeColor(PALETTE.gray2).lineWidth(1.2).moveTo(cx, y).lineTo(cx, y + 15).stroke();
    doc.fillColor(PALETTE.gray2).polygon([cx - 3, y + 15], [cx + 3, y + 15], [cx, y + 19]).fill();
    doc.fillColor(PALETTE.gray3).font('Helvetica-Oblique').fontSize(7.5).text(label, cx + 8, y + 4);
  }

  // API Layer
  drawLayerBox(90, 56, 'API Layer', 'PensionVault.API',
    'Receives HTTP endpoints, verifies roles, handles HTTP responses.',
    PALETTE.blue, '#eff6ff', ['AuthController.cs', 'AnnuityController.cs', 'AuditLogFilter.cs']);
  
  drawConnectorArrow(148, 'delegates request to');

  // Application Layer
  drawLayerBox(169, 56, 'Application Layer', 'PensionVault.Application',
    'Business logic, calculation orchestrator, DTO contracts.',
    PALETTE.teal, '#f0fdf9', ['IAuthService.cs', 'AuthService.cs', 'IAnnuityService.cs', 'AnnuityService.cs']);

  drawConnectorArrow(227, 'depends on contracts from');

  // Domain Layer
  drawLayerBox(248, 56, 'Domain Layer', 'PensionVault.Domain',
    'Core blueprints, entities, and database access interfaces.',
    PALETTE.purple, '#f5f3ff', ['IUserRepository.cs', 'IAnnuityRepository.cs', 'IUnitOfWork.cs']);

  drawConnectorArrow(306, 'implemented by');

  // Infrastructure Layer
  drawLayerBox(327, 56, 'Infrastructure Layer', 'PensionVault.Infrastructure',
    'SQL Server connections, EF Core, database migrations & seeders.',
    PALETTE.amber, '#fffbeb', ['UserRepository.cs', 'AnnuityRepository.cs', 'UnitOfWork.cs']);

  drawConnectorArrow(385, 'persists details in');

  // Database Icon/Box
  const dbX = W / 2 - 50;
  const dbY = 406;
  doc.fillColor('#1e293b').roundedRect(dbX, dbY, 100, 24, 4).fill();
  doc.fillColor(PALETTE.white).font('Helvetica-Bold').fontSize(8.5).text('SQL Server', dbX, dbY + 7.5, { align: 'center', width: 100 });

  // Incoming Request Badge
  doc.strokeColor(PALETTE.blue).lineWidth(1.2).moveTo(LX + 30, 78).lineTo(LX + 30, 88).stroke();
  doc.fillColor(PALETTE.blue).polygon([LX + 28, 88], [LX + 32, 88], [LX + 30, 90]).fill();
  doc.fillColor(PALETTE.blue).font('Helvetica-Bold').fontSize(8.5).text('HTTP Request', LX + 38, 79);

  // Footer
  doc.fillColor(PALETTE.gray2).font('Helvetica').fontSize(7.5).text('Architecture Diagram Overview  •  Page 2', 50, H - 24, { align: 'center', width: 495 });
}

// ════════════════════════════════════════════════════════════════════════════
// PAGE 3 — CLIENT-SERVER REQUEST FLOW DIAGRAM
// ════════════════════════════════════════════════════════════════════════════
function drawRequestFlowDiagram() {
  doc.addPage();
  const W = 595.28;
  const H = 841.89;

  // Background
  doc.fillColor('#f8fafc').rect(0, 0, W, H).fill();

  // Top header bar
  doc.fillColor(PALETTE.navy2).rect(0, 0, W, 70).fill();
  doc.fillColor(PALETTE.teal).rect(0, 0, W, 5).fill();
  doc.fillColor(PALETTE.white).font('Helvetica-Bold').fontSize(16).text('PensionVault — End-to-End Request Payout Flow', 50, 22, { align: 'center', width: 495 });
  doc.fillColor('#93c5fd').font('Helvetica').fontSize(8.5).text('Step-by-step execution path of a monthly pension disbursement trigger', 50, 44, { align: 'center', width: 495 });

  // Draw timeline columns
  const colX = [80, 200, 340, 480];
  const colW = 85;
  const colNames = ['1. Client (Postman)', '2. API Controller', '3. Application Service', '4. Database (SQL)'];
  const colCols = [PALETTE.gray3, PALETTE.blue, PALETTE.teal, PALETTE.amber];

  colX.forEach((x, i) => {
    doc.fillColor(colCols[i]).roundedRect(x - 5, 90, colW, 20, 3).fill();
    doc.fillColor(PALETTE.white).font('Helvetica-Bold').fontSize(7.5).text(colNames[i], x - 5, 96, { align: 'center', width: colW });
    doc.strokeColor(PALETTE.gray2).lineWidth(0.5).dash(4, { space: 4 }).moveTo(x + colW / 2 - 5, 110).lineTo(x + colW / 2 - 5, 780).stroke();
    doc.undash();
  });

  function drawStepArrow(fromIdx, toIdx, y, label, stepNum) {
    const startX = colX[fromIdx] + colW / 2 - 5;
    const endX = colX[toIdx] + colW / 2 - 5;
    doc.strokeColor(PALETTE.navy).lineWidth(1).moveTo(startX, y).lineTo(endX, y).stroke();
    const dir = endX > startX ? 1 : -1;
    doc.fillColor(PALETTE.navy).polygon([endX, y], [endX - 4 * dir, y - 3], [endX - 4 * dir, y + 3]).fill();
    doc.fillColor(PALETTE.gray3).font('Helvetica-Bold').fontSize(7.5).text(`${stepNum}. ${label}`, Math.min(startX, endX) + 10, y - 10, { width: Math.abs(endX - startX) - 20, align: 'center' });
  }

  drawStepArrow(0, 1, 150, 'POST api/annuity/{id}/disburse', '1');
  drawStepArrow(1, 2, 230, 'ProcessDisbursementAsync(req)', '2');
  drawStepArrow(2, 3, 310, 'Query plan & Validate Status', '3');
  drawStepArrow(3, 2, 390, 'AnnuityPlan entity returned', '4');
  
  const midX = colX[2] + colW / 2 - 45;
  doc.fillColor(PALETTE.lteal).roundedRect(midX, 420, 80, 50, 4).fill();
  doc.strokeColor(PALETTE.teal).lineWidth(0.8).roundedRect(midX, 420, 80, 50, 4).stroke();
  doc.fillColor(PALETTE.teal).font('Helvetica-Bold').fontSize(6.5).text('Service Computes:\n- NetAmount\n- Deducts Balance\n- Builds Ledger Entry', midX + 5, 426, { width: 70, align: 'center', lineGap: 1.5 });

  drawStepArrow(2, 3, 520, 'AddDisbursement() & Update Balance', '5');
  drawStepArrow(2, 3, 590, 'SaveChangesAsync() (Transaction Commit)', '6');
  drawStepArrow(3, 2, 660, 'Success & Transaction Logged', '7');
  drawStepArrow(2, 1, 710, 'Return PensionDisbursementResponse', '8');
  drawStepArrow(1, 0, 760, 'HTTP 200 OK (JSON Payload)', '9');

  doc.fillColor(PALETTE.gray2).font('Helvetica').fontSize(7.5).text('Request & Payout Execution Flow Chart  •  Page 3', 50, H - 24, { align: 'center', width: 495 });
}

// ════════════════════════════════════════════════════════════════════════════
// PAGES 4+ — CONTENT PAGES GENERATION HELPERS
// ════════════════════════════════════════════════════════════════════════════
let currentY = 50;

function checkNewPage(neededHeight = 40) {
  const H = 841.89;
  if (currentY + neededHeight > H - 55) {
    doc.addPage();
    currentY = 50;
    return true;
  }
  return false;
}

function writePageHeader(title) {
  doc.fillColor(PALETTE.silver).font('Helvetica').fontSize(8).text(title, 36, 25);
  doc.strokeColor(PALETTE.gray1).lineWidth(0.5).moveTo(36, 36).lineTo(559.28, 36).stroke();
}

function writeH1(text) {
  checkNewPage(45);
  const w = 523.28;
  doc.fillColor(PALETTE.navy2).roundedRect(36, currentY, w, 24, 4).fill();
  doc.fillColor(PALETTE.white).font('Helvetica-Bold').fontSize(11).text(text, 46, currentY + 6.5);
  currentY += 34;
}

function writeH2(text, mainCol, bgCol) {
  checkNewPage(35);
  const w = 523.28;
  doc.fillColor(bgCol).roundedRect(36, currentY, w, 20, 3).fill();
  doc.fillColor(mainCol).font('Helvetica-Bold').fontSize(9.5).text(text, 44, currentY + 5);
  currentY += 28;
}

function writeH3(text) {
  checkNewPage(25);
  doc.fillColor(PALETTE.teal).font('Helvetica-Bold').fontSize(8.5).text(text, 36, currentY);
  currentY += 13;
}

function writeBody(text) {
  const textHeight = doc.heightOfString(text, { width: 523.28, fontSize: 8.5, font: 'Helvetica', lineGap: 3 });
  checkNewPage(textHeight + 10);
  doc.fillColor(PALETTE.gray3).font('Helvetica').fontSize(8.5).text(text, 36, currentY, { width: 523.28, lineGap: 3 });
  currentY += textHeight + 8;
}

function writeBullet(text) {
  const textHeight = doc.heightOfString(text, { width: 505.28, fontSize: 8.5, font: 'Helvetica', lineGap: 3 });
  checkNewPage(textHeight + 10);
  doc.fillColor(PALETTE.gray3).font('Helvetica').fontSize(8.5).text('•', 42, currentY);
  doc.text(text, 54, currentY, { width: 505.28, lineGap: 3 });
  currentY += textHeight + 6;
}

// Render code blocks line-by-line so they flow across page breaks naturally without leaving empty white pages
function writeCodeBlock(code) {
  const cleanCode = code.replace(/\u2192/g, '->').replace(/\u2013/g, '--').replace(/\u2014/g, '--');
  const lines = cleanCode.split('\n');
  const lineHeight = 10;

  doc.font('Courier').fontSize(7.5);
  lines.forEach((line) => {
    checkNewPage(lineHeight + 2.5);
    // Draw background slice for this line
    doc.fillColor(PALETTE.codebg).rect(36, currentY, 523.28, lineHeight + 2.5).fill();
    doc.fillColor(PALETTE.codefg).text(line, 42, currentY + 2, { width: 511.28 });
    currentY += lineHeight + 2.5;
  });
  currentY += 8;
}

function writeKeepTogetherCodeBlock(code) {
  // Alias to ensure clean page flow
  writeCodeBlock(code);
}

const BADGES = {
  GET: { bg: PALETTE.lgreen, fg: PALETTE.green },
  POST: { bg: PALETTE.lblue, fg: PALETTE.blue },
  PUT: { bg: PALETTE.lamber, fg: PALETTE.gold },
  DELETE: { bg: PALETTE.lred, fg: PALETTE.red }
};

function writeMethodRow(method, route, access, desc) {
  const badge = BADGES[method.toUpperCase()] || { bg: PALETTE.gray1, fg: PALETTE.gray3 };
  const descHeight = doc.heightOfString(desc, { width: 440, fontSize: 8.5, font: 'Helvetica', lineGap: 2.5 });
  const rowHeight = Math.max(descHeight + 35, 45);
  
  checkNewPage(rowHeight + 10);

  const startX = 36;
  const w = 523.28;

  doc.fillColor(PALETTE.gray1).rect(startX, currentY, w, rowHeight).fill();
  doc.strokeColor(PALETTE.gray2).lineWidth(0.5).rect(startX, currentY, w, rowHeight).stroke();

  doc.fillColor(badge.bg).rect(startX + 8, currentY + 6, 32, 14).fill();
  doc.fillColor(badge.fg).font('Helvetica-Bold').fontSize(8).text(method, startX + 8, currentY + 9.5, { align: 'center', width: 32 });
  doc.fillColor(PALETTE.gray3).font('Courier-Bold').fontSize(8).text(route, startX + 48, currentY + 7);
  doc.fillColor(PALETTE.teal).font('Helvetica-Oblique').fontSize(7.5).text(`Access: ${access}`, startX + 48, currentY + 20);
  doc.strokeColor(PALETTE.gray2).lineWidth(0.3).moveTo(startX + 8, currentY + 31).lineTo(startX + w - 8, currentY + 31).stroke();
  doc.fillColor(PALETTE.gray3).font('Helvetica').fontSize(8.5).text(desc, startX + 12, currentY + 36, { width: w - 24, lineGap: 2.5 });

  currentY += rowHeight + 8;
}

function writeLayerRow(name, proj, desc, filesStr, tc, bg) {
  checkNewPage(56);
  const w = 523.28;

  doc.fillColor(bg).roundedRect(36, currentY, w, 46, 4).fill();
  doc.strokeColor(tc).lineWidth(0.8).roundedRect(36, currentY, w, 46, 4).stroke();
  doc.fillColor(tc).rect(36, currentY, w, 14).fill();
  doc.fillColor(PALETTE.white).font('Helvetica-Bold').fontSize(9).text(name, 44, currentY + 3);
  doc.fillColor('rgba(255, 255, 255, 0.8)').font('Helvetica').fontSize(8).text(proj, 300, currentY + 3, { align: 'right', width: 249 });
  doc.fillColor(PALETTE.gray3).font('Helvetica').fontSize(8).text(desc, 44, currentY + 18);
  doc.fillColor(tc).font('Courier-Bold').fontSize(8).text(filesStr, 44, currentY + 31);

  currentY += 54;
}

// Draw a table for line-by-line code explanation mapping, splitting rows across pages dynamically
function writeExplanationTable(headers, colWidths, rows) {
  const headerHeight = 16;
  const padding = 5;
  
  checkNewPage(headerHeight + 25); // Ensure space for header + at least 1 row

  function drawHeader(y) {
    let startX = 36;
    headers.forEach((h, i) => {
      const width = colWidths[i];
      doc.fillColor(PALETTE.navy2).rect(startX, y, width, headerHeight).fill();
      doc.strokeColor(PALETTE.gray2).lineWidth(0.5).rect(startX, y, width, headerHeight).stroke();
      doc.fillColor(PALETTE.white).font('Helvetica-Bold').fontSize(8.5).text(h, startX + 5, y + 4, { width: width - 10 });
      startX += width;
    });
  }

  drawHeader(currentY);
  currentY += headerHeight;

  rows.forEach((row, rIdx) => {
    // Calculate row height based on contents
    let rowH = 15;
    row.forEach((cell, i) => {
      const width = colWidths[i] - 10;
      const height = doc.heightOfString(cell, { fontSize: 8, font: i === 0 ? 'Courier' : 'Helvetica', width });
      if (height + 2 * padding > rowH) {
        rowH = height + 2 * padding;
      }
    });

    // Check if row overflows the page. If yes, add page and redraw header!
    const didBreak = checkNewPage(rowH + 6);
    if (didBreak) {
      drawHeader(currentY);
      currentY += headerHeight;
    }

    let startX = 36;
    const isAlt = rIdx % 2 === 1;
    row.forEach((cell, cIdx) => {
      const width = colWidths[cIdx];
      doc.fillColor(isAlt ? PALETTE.gray1 : PALETTE.white).rect(startX, currentY, width, rowH).fill();
      doc.strokeColor(PALETTE.gray2).lineWidth(0.5).rect(startX, currentY, width, rowH).stroke();
      
      const isCode = cIdx === 0;
      doc.fillColor(PALETTE.gray3).font(isCode ? 'Courier-Bold' : 'Helvetica').fontSize(8)
         .text(cell, startX + 5, currentY + padding, { width: width - 10, lineGap: 2 });
      startX += width;
    });
    currentY += rowH;
  });
  
  currentY += 10;
}

function writeTable(headers, colWidths, rows, themeCol = PALETTE.teal, bgCol = PALETTE.lteal) {
  const headerHeight = 16;
  const rowHeight = 15;
  
  checkNewPage(headerHeight + rowHeight + 10);

  function drawHeader(y) {
    let startX = 36;
    headers.forEach((h, i) => {
      const width = colWidths[i];
      doc.fillColor(themeCol).rect(startX, y, width, headerHeight).fill();
      doc.strokeColor(PALETTE.gray2).lineWidth(0.5).rect(startX, y, width, headerHeight).stroke();
      doc.fillColor(PALETTE.white).font('Helvetica-Bold').fontSize(8.5).text(h, startX + 5, y + 4, { width: width - 10 });
      startX += width;
    });
  }

  drawHeader(currentY);
  currentY += headerHeight;

  rows.forEach((row, rIdx) => {
    const didBreak = checkNewPage(rowHeight + 4);
    if (didBreak) {
      drawHeader(currentY);
      currentY += headerHeight;
    }

    let startX = 36;
    const isAlt = rIdx % 2 === 1;
    row.forEach((cell, cIdx) => {
      const width = colWidths[cIdx];
      doc.fillColor(isAlt ? bgCol : PALETTE.white).rect(startX, currentY, width, rowHeight).fill();
      doc.strokeColor(PALETTE.gray2).lineWidth(0.5).rect(startX, currentY, width, rowHeight).stroke();
      
      const isCode = cell.includes('Task') || cell.includes('Async') || cell.includes('()') || cell.includes('SELECT') || cell.includes('UPDATE');
      doc.fillColor(PALETTE.gray3).font(isCode ? 'Courier' : 'Helvetica').fontSize(isCode ? 7.5 : 8).text(cell, startX + 5, currentY + 3.5, { width: width - 10 });
      startX += width;
    });
    currentY += rowHeight;
  });
  
  currentY += 8;
}

// ════════════════════════════════════════════════════════════════════════════
// RUN DOCUMENT BUILD
// ════════════════════════════════════════════════════════════════════════════
console.log('Generating page 1 (cover page)...');
drawCover();

console.log('Generating page 2 (architecture diagram)...');
drawDiagram();

console.log('Generating page 3 (execution flow diagram)...');
drawRequestFlowDiagram();

// Start generating the content page (Page 4)
doc.addPage();
currentY = 50;

// SECTION 1
writeH1('1.  PensionVault Overview & Architecture Map');

writeH2('1.1 What is PensionVault?', PALETTE.navy2, PALETTE.lblue);
writeBody('PensionVault is a robust, enterprise-grade pension administration backend built on Clean Architecture principles. It serves as a secure central ledger and core transaction manager to automate member lifecycle stages including:');
writeBullet('Member Enrolment & Schemes Configuration (EPF, Gratuity, Superannuation, NPS, PPF, etc.)');
writeBullet('Contribution Remittance Processing (Employer collections, bank file uploads, and reconciliations)');
writeBullet('Ledger Accounting (Double-entry recording of contributions, interest credits, and claim debits)');
writeBullet('Retirement Claim Disbursements & Automated Annuity Payout Distributions');

writeH2('1.2 Codebase Project Structure Directory Map', PALETTE.navy2, PALETTE.lblue);
writeTable(['Layer Name', 'Folder Path', 'Primary Responsibility'], [100, 160, 263.28], [
  ['API Layer', 'PensionVault.API/', 'Defines HTTP Controllers, routing templates, global filters, and Kestrel server startup configs.'],
  ['Application', 'PensionVault.Application/', 'Houses service logic interfaces/implementations, validation constraints, and request DTOs.'],
  ['Domain Layer', 'PensionVault.Domain/', 'Central blueprints layer containing SQL entities, custom status enums, and repository abstractions.'],
  ['Infrastructure', 'PensionVault.Infrastructure/', 'Interacts with physical SQL Server using EF Core migrations, database seeds, and raw repositories.']
]);

writeH2('1.3 Architectural Map -- 13 Files / 4 Layers', PALETTE.navy2, PALETTE.lblue);
writeLayerRow('API Layer', 'PensionVault.API',
  'Receives HTTP endpoints, verifies roles, handles HTTP responses.',
  'AnnuityController.cs  |  AuthController.cs  |  AuditLogFilter.cs',
  PALETTE.blue, PALETTE.lblue);
writeLayerRow('Application Layer', 'PensionVault.Application',
  'Houses all business rules, calculations, and DTO data mappings.',
  'IAuthService.cs  |  AuthService.cs  |  IAnnuityService.cs  |  AnnuityService.cs',
  PALETTE.teal, PALETTE.lteal);
writeLayerRow('Domain Layer', 'PensionVault.Domain',
  'Defines the core interfaces and contracts for data access.',
  'IUserRepository.cs  |  IAnnuityRepository.cs  |  IUnitOfWork.cs',
  PALETTE.purple, PALETTE.lpurp);
writeLayerRow('Infrastructure Layer', 'PensionVault.Infrastructure',
  'Connects to SQL Server using EF Core and executes queries.',
  'UserRepository.cs  |  AnnuityRepository.cs  |  UnitOfWork.cs',
  PALETTE.amber, PALETTE.lamber);

// SECTION 2
writeH1('2.  Identity & Access Management (IAM) Workflow & Classes');
writeBody('PensionVault secures its REST API using JSON Web Tokens (JWT) and BCrypt password encryption. The core authentication logic implements a token rotation model for persistent sessions:');
writeBullet('Registration: User details are passed to AuthService. Registration encrypts the plaintext password using BCrypt.HashPassword before staging the entity.');
writeBullet('Login Gate: Login verifies user emails and compares passwords using BCrypt.Verify. On credentials match, the server generates a JWT containing Claims (UserId, Email, Name, Role).');
writeBullet('Token Validation: Program.cs configures AddJwtBearer options to validate: 1. Token Signature, 2. Expire Lifespan, 3. Issuer/Audience match.');
writeBullet('Security Gates: Active endpoint controllers are protected using the [Authorize(Roles = "...")] attributes, limiting critical actions like create plan or disburse to FundAdmin or Admin roles.');
writeBullet('Session Renewal: RefreshTokenRequest validates base64-encoded 64-byte refresh token. Generates a fresh JWT pair to preserve authentication state.');

// 2.1 AuthController
writeH2('2.1  AuthController.cs', PALETTE.blue, PALETTE.lblue);
writeBody('Handles registration, login, and token refresh requests by mapping incoming HTTP requests directly to IAuthService methods.');
writeKeepTogetherCodeBlock(`[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    var result = await _authService.LoginAsync(request);
    return Ok(result);
}`);
writeExplanationTable(['Code Segment', 'Line-by-Line Technical Explanation'], [180, 343.28], [
  ['[HttpPost("login")]', 'Defines an HTTP POST route at "api/auth/login" corresponding to this method.'],
  ['[FromBody] LoginRequest', 'Uses C# model binding to deserialize the raw incoming JSON body directly into a C# LoginRequest record.'],
  ['await _authService.LoginAsync(...)', 'Invokes the AuthService block asynchronously to verify password hashes and account status.'],
  ['return Ok(result)', 'Wraps the AuthResponse (JWT + Refresh token + user metadata) inside a standard HTTP 200 OK wrapper.']
]);

// 2.2 AnnuityController
writeH2('2.2  AnnuityController.cs', PALETTE.blue, PALETTE.lblue);
writeBody('Exposes routing points to create annuity contracts, trigger monthly disbursements, execute nominee settlements, and terminate plans.');
writeKeepTogetherCodeBlock(`[HttpPost("{id:guid}/disburse")]
[Authorize(Roles = "FundAdmin,Admin")]
public async Task<IActionResult> ProcessDisbursement(Guid id, [FromBody] ProcessDisbursementRequest request)
{
    var req = request with { AnnuityId = id };
    return Ok(await _annuityService.ProcessDisbursementAsync(req));
}`);
writeExplanationTable(['Code Segment', 'Line-by-Line Technical Explanation'], [180, 343.28], [
  ['[HttpPost("{id:guid}/disburse")]', 'Maps route "api/annuity/{id}/disburse". Restricts path parameter "id" to be a valid 128-bit GUID constraint.'],
  ['[Authorize(Roles = "...")]', 'Authorization filter gate. Rejects requests with HTTP 403 Forbidden unless user JWT claim holds role "Admin" or "FundAdmin".'],
  ['request with { AnnuityId = id }', 'C# C-Sharp 9.0 record mutator pattern. Creates a copy of the request, forcing the body AnnuityId to match the URL segment.'],
  ['await _annuityService.Process...', 'Sends updated request data parameters down to the Application service Layer within an async task.']
]);

// 2.3 AuditLogFilter
writeH2('2.3  AuditLogFilter.cs', PALETTE.blue, PALETTE.lblue);
writeBody('Interceptors built on the Decorator Pattern to automatically track POST, PUT, and DELETE DB modifications.');
writeKeepTogetherCodeBlock(`var resultContext = await next();

if (resultContext.Exception == null && 
    resultContext.HttpContext.Response.StatusCode >= 200 && 
    resultContext.HttpContext.Response.StatusCode < 300)
{
    // ... extract claims and log action ...
}`);
writeExplanationTable(['Code Segment', 'Line-by-Line Technical Explanation'], [180, 343.28], [
  ['await next()', 'Delegate call. Halts filter execution and awaits the execution of the target controller method to complete first.'],
  ['resultContext.Exception == null', 'Validation check. Verifies the controller executed successfully without crashing or throwing unhandled errors.'],
  ['Response.StatusCode >= 200...', 'Verifies the response returned an HTTP 2xx Success status code before logging changes to avoid false flags.'],
  ['httpContext.User.FindFirst(...)', 'Queries the HttpContext ClaimsPrincipal to extract the User ID claim injected by JWT validation.']
]);

// SECTION 3
writeH1('3.  Application Layer -- Services & Business Workflows');

// 3.2 AuthService
writeH2('3.2  AuthService.cs (Login Gate)', PALETTE.teal, PALETTE.lteal);
writeBody('Implements login credential validation, BCrypt password hashing, and token generation.');
writeKeepTogetherCodeBlock(`public async Task<AuthResponse> LoginAsync(LoginRequest request)
{
    var user = await _userRepo.FindByEmailAsync(request.Email)
        ?? throw new UnauthorizedAccessException("Invalid credentials.");

    if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        throw new UnauthorizedAccessException("Invalid credentials.");

    return await GenerateAuthResponseAsync(user);
}`);
writeExplanationTable(['Code Segment', 'Line-by-Line Technical Explanation'], [180, 343.28], [
  ['FindByEmailAsync(...)', 'Invokes repository interface. Queries SQL table for matches against input email address.'],
  ['throw new UnauthorizedAccessException', 'Security measure. Throws exception if user email is missing. Does not indicate whether email or password failed (prevents enumeration).'],
  ['BCrypt.Net.BCrypt.Verify(...)', 'Cryptographically compares plaintext input password with salted hash stored in database. Secure computation.'],
  ['GenerateAuthResponseAsync(...)', 'Generates symmetric signature JWT token, issues base64 random refresh token, and saves details to DB.']
]);

// 3.4 AnnuityService
writeH2('3.4  AnnuityService.cs (Disbursement Process)', PALETTE.teal, PALETTE.lteal);
writeBody('Processes monthly pension payments. Decrements fund accounts, issues ledger logs, and secures transactions.');
writeKeepTogetherCodeBlock(`var netAmount = plan.MonthlyPension - request.TaxDeducted;
var disbursement = new MonthlyPensionDisbursement {
    AnnuityId = plan.AnnuityId,
    NetAmount = netAmount,
    Status = PensionDisbursementStatus.Disbursed
};
await _annuityRepo.AddDisbursementAsync(disbursement);
account.TotalBalance -= plan.MonthlyPension;
await _unitOfWork.SaveChangesAsync();`);
writeExplanationTable(['Code Segment', 'Line-by-Line Technical Explanation'], [180, 343.28], [
  ['plan.MonthlyPension - TaxDeducted', 'Business calculation. Subtracts tax rate to arrive at final net cash payout.'],
  ['AddDisbursementAsync(...)', 'Stages insertion of a disbursement payment receipt record matching the payout month/year.'],
  ['account.TotalBalance -= ...', 'Decrements gross amount from retiree\'s personal fund account balance.'],
  ['_unitOfWork.SaveChangesAsync()', 'EF Core transaction commit. Commits disbursement creation, account deduction, and ledger updates atomically.']
]);

// SECTION 5
writeH1('5.  Infrastructure Layer Repositories & SQL Queries');

// 5.2 AnnuityRepository
writeH2('5.2  AnnuityRepository.cs', PALETTE.gray3, PALETTE.gray1);
writeBody('Executes SQL transactions via Entity Framework Core joins.');
writeKeepTogetherCodeBlock(`public Task<AnnuityPlan?> FindByIdAsync(Guid annuityId)
    => _context.AnnuityPlans
        .Include(a => a.Member)
        .FirstOrDefaultAsync(a => a.AnnuityId == annuityId);`);
writeExplanationTable(['Code Segment', 'Line-by-Line Technical Explanation'], [180, 343.28], [
  ['_context.AnnuityPlans', 'Points target query against AnnuityPlans database table.'],
  ['.Include(a => a.Member)', 'Eager loading command. Forces EF Core to generate an SQL INNER JOIN statement to pull related Member details in one query.'],
  ['.FirstOrDefaultAsync(...)', 'SQL command: SELECT TOP(1) with WHERE check on AnnuityId. Executed asynchronously to avoid blocking threads.']
]);

// 5.4 Program.cs setup
writeH2('5.4  Program.cs Middleware Pipeline', PALETTE.gray3, PALETTE.gray1);
writeBody('Startup configurations and middleware pipeline registrations:');
writeKeepTogetherCodeBlock(`app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionMiddleware>();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();`);
writeExplanationTable(['Code Segment', 'Line-by-Line Technical Explanation'], [180, 343.28], [
  ['UseSerilogRequestLogging()', 'Logs endpoint route, status code, response time, and request payload parameters for audit.'],
  ['UseMiddleware<ExceptionMiddleware>()', 'Global try-catch interceptor. Intercepts runtime errors and formats clean JSON error responses.'],
  ['UseAuthentication()', 'Analyzes HTTP request Authorization header, extracts JWT bearer token, and decrypts/verifies claims.'],
  ['UseAuthorization()', 'Executes access role verification (e.g. restricts route to Admin/FundAdmin).']
]);

// SECTION 6
writeH1('6.  Annuity Plan Types and Calculations');
writeTable(['Plan Type', 'Business Definition', 'Payout / Status Logic'], [110, 160, 253.28], [
  ['LifeAnnuity', 'Standard lifetime pension', 'Pays fixed MonthlyPension until member\'s death. On death: Status -> Terminated. No nominee refund.'],
  ['JointAnnuity', 'Survivor benefits for spouse', 'Pays for member\'s lifetime. On death: Status -> Suspended. Spouse/nominee can claim via ProcessNomineeSettlementAsync.'],
  ['TemporaryAnnuity', 'Term-limited payout', 'Pays for a fixed number of years set at purchase. After the term ends: Status -> Lapsed. Payouts stop.'],
  ['GuaranteedAnnuity', 'Guaranteed return term', 'Pays for a minimum term (e.g. 10 years). If member dies early, remaining term value is settled to nominee.']
], PALETTE.navy2, PALETTE.lamber);

writeH2('Calculation Formulas', PALETTE.amber, PALETTE.lamber);
writeBody('Monthly Disbursement Payout:');
writeCodeBlock(
  '  NetAmount = MonthlyPension (from AnnuityPlan, NOT from request)\n' +
  '            - TaxDeducted (from ProcessDisbursementRequest)\n\n' +
  '  FundAccount.TotalBalance -= GrossAmount (MonthlyPension)\n\n' +
  '  LedgerEntry: EntryType   = AnnuityDebit\n' +
  '               Amount      = MonthlyPension\n' +
  '               ReferenceId = DisbursementId.ToString()'
);

writeBody('Nominee Settlement on Member Death:');
writeCodeBlock(
  '  SettlementAmount = FundAccount.TotalBalance (current balance)\n\n' +
  '  AnnuityPlan.Status = AnnuityStatus.Settled\n' +
  '  AnnuityPlan.NomineeDetails = "{NomineeName} (Settled to {BankAccountRef})"\n\n' +
  '  FundAccount.TotalBalance -= SettlementAmount  -- reduces balance to 0.00\n\n' +
  '  LedgerEntry: EntryType   = AnnuityDebit\n' +
  '               ReferenceId = "SETTLEMENT-{annuityId}"'
);

// Write Page Numbers dynamically on footers of buffered pages!
const range = doc.bufferedPageRange();
console.log(`Applying footer page numbers across all ${range.count} pages...`);

for (let i = 0; i < range.count; i++) {
  doc.switchToPage(i);
  
  // Temporarily disable margins during header/footer draw to prevent PDFKit from appending extra blank pages
  const oldBottom = doc.page.margins.bottom;
  const oldTop = doc.page.margins.top;
  doc.page.margins.bottom = 0;
  doc.page.margins.top = 0;

  // Page 0 is Cover. Page 1 is architecture diagram. Page 2 is request flow.
  // We draw headers/footers on all pages starting from index 1 (Page 2)
  if (i > 0) {
    doc.fillColor(PALETTE.silver).font('Helvetica').fontSize(8);
    // Draw footer line (moved up to y=780 to stay safely away from page bottom edge)
    doc.strokeColor(PALETTE.gray2).lineWidth(0.5).moveTo(36, 780).lineTo(559.28, 780).stroke();
    // Draw footer text (moved up to y=788)
    doc.text('PensionVault Backend  •  Core Files Developer Walkthrough Reference', 36, 788);
    doc.text(`Page ${i + 1} of ${range.count}`, 450, 788, { align: 'right', width: 109 });

    // For content pages (i >= 3), draw page header
    if (i >= 3) {
      writePageHeader('PensionVault Core Files Developer Reference');
    }
  }

  // Restore margins
  doc.page.margins.bottom = oldBottom;
  doc.page.margins.top = oldTop;
}

// Finalize
doc.end();

stream.on('finish', () => {
  console.log('PDF generation finished successfully.');
});
